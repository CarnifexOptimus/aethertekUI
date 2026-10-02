using System.Numerics;
using System.Reflection;
using CombatReborn.MaterialUI;
using Dalamud.Bindings.ImGui;
using GatherBuddy.Gui;

unsafe class Program
{
    private delegate bool Integer(string label, ref int value, int step, int stepFast, ImGuiInputTextFlags flags);
    static void Require(bool condition,string message) { if (!condition) throw new Exception(message); }

    static void Main()
    {
        var adapter=typeof(MaterialUiCombo<int>).Assembly.GetType("GatherBuddy.Gui.MaterialUi",true)!;
        var active=adapter.GetProperty("Active",BindingFlags.Static|BindingFlags.NonPublic)!;
        active.SetValue(null,true);
        var input=adapter.GetMethod("InputInt",BindingFlags.Static|BindingFlags.NonPublic)!.CreateDelegate<Integer>();
        foreach(var density in Enum.GetValues<MaterialDensity>())
        foreach(var scale in new[] {1f,1.5f})
        {
            var context=ImGui.CreateContext();
            try
            {
                var io=ImGui.GetIO(); io.IniFilename=null; io.LogFilename=null;
                io.DisplaySize=new(1200,900); io.DeltaTime=1f/60; io.FontGlobalScale=scale;
                io.Fonts.AddFontDefault(); byte* pixels=null; int w=0,h=0;
                io.Fonts.GetTexDataAsRGBA32(0,&pixels,&w,&h); io.Fonts.SetTexID(0,new ImTextureID((nint)1));
                var theme=new MaterialTheme(new(.3f,.7f,.4f),density);
                var combo=new MaterialUiCombo<int>("choice","",200,new List<int>{0,1,2},v=>new[]{"First","Second","Third"}[v]);
                var empty=new MaterialUiCombo<int>("empty","",200,new List<int>(),v=>v.ToString());
                var selected=-1; var number=12; var narrow=false; Vector2 comboPoint=default,numberPoint=default;
                void Draw()
                {
                    using var themeScope=MaterialTheme.Push(theme,scale);
                    using var controls=MaterialControls.Push(MaterialControlContext.Dense);
                    ImGui.SetNextWindowPos(new(20,20)); ImGui.SetNextWindowSize(new(340*scale,700));
                    ImGui.Begin("GBR adapters",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
                    if (combo.Draw("First",out var result,fieldWidth:110)) selected=result;
                    comboPoint=ImGui.GetItemRectMin()+ImGui.GetItemRectSize()*.5f;
                    if ((bool)active.GetValue(null)!)
                        Require(ImGui.GetItemRectSize().X<=110*scale+.1f,"Combo field exceeds requested width");
                    ImGui.SetNextItemWidth((narrow?35:240)*scale);
                    input("##quantity",ref number,1,10,ImGuiInputTextFlags.None);
                    numberPoint=ImGui.GetItemRectMax()-new Vector2(5,ImGui.GetItemRectSize().Y*.5f);
                    var emptyChanged=empty.Draw(0,out _);
                    if ((bool)active.GetValue(null)!) Require(!emptyChanged,"Empty combo emitted an invalid selection");
                    ImGui.End();
                }
                void Frame() { ImGui.NewFrame(); Draw(); ImGui.Render(); }
                void Click(Vector2 point)
                {
                    io.AddMousePosEvent(point.X,point.Y); Frame();
                    io.AddMouseButtonEvent(0,true); Frame(); io.AddMouseButtonEvent(0,false); Frame();
                }
                void Enter()
                {
                    io.AddKeyEvent(ImGuiKey.Enter,true); Frame(); io.AddKeyEvent(ImGuiKey.Enter,false); Frame();
                }
                Frame(); Frame(); Click(comboPoint); Frame(); Frame();
                foreach(var ch in "Second") io.AddInputCharacter(ch);
                Frame(); Frame(); Require(selected==-1,"Typing in a focused search must not select a result"); Enter();
                Require(selected==1,$"Search + Enter failed: {density}/{scale}, selected {selected}");
                selected=-1; Click(comboPoint); Frame(); Frame();
                foreach(var ch in "First") io.AddInputCharacter(ch);
                Frame(); Frame(); Enter();
                Require(selected==0,"Selecting current value must still emit an action");
                io.AddKeyEvent(ImGuiKey.ModCtrl,true); Frame(); Click(numberPoint);
                io.AddKeyEvent(ImGuiKey.ModCtrl,false); Frame();
                Require(number==22,$"Ctrl step must be 10, got {number}");
                narrow=true; Frame(); Frame(); Click(numberPoint);
                Require(number==22,"Narrow number should suppress step buttons");
                active.SetValue(null,false); Frame(); Frame();
                Require(number==22,"Non-Material fallback changed numeric state");
                active.SetValue(null,true);
                Console.WriteLine($"PASS {density}/{scale}: search/Enter, repeat selection, empty list, Ctrl-step, narrow editor, fallback");
            }
            finally { ImGui.DestroyContext(context); }
        }
        active.SetValue(null,false);
    }
}

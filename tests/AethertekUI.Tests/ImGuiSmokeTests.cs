using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AethertekUI.Tests;

public unsafe class ImGuiSmokeTests
{
    [Fact]
    public void RealNativeContextRendersAndRestoresNestedStyles()
    {
        var context=ImGui.CreateContext();
        try
        {
            var io=ImGui.GetIO();
            io.IniFilename=null;
            io.LogFilename=null;
            io.DisplaySize=new(1000,800);
            io.DeltaTime=1f/60;
            io.Fonts.AddFontDefault();
            byte* pixels=null; int width=0,height=0;
            io.Fonts.GetTexDataAsRGBA32(0,&pixels,&width,&height);
            io.Fonts.SetTexID(0,new ImTextureID((nint)1));
            var original=ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
            var originalColors=new Vector4[Enum.GetValues<ImGuiCol>().Max(role=>(int)role)];
            for(var i=0;i<originalColors.Length;i++) originalColors[i]=ImGui.GetStyle().Colors[i];
            var padding=ImGui.GetStyle().WindowPadding;
            var theme=new MaterialTheme(new(.3f,.7f,.4f));
            ImGui.NewFrame();
            using(var scope=MaterialTheme.Push(theme,1))
            {
                ImGui.SetNextWindowSize(new(900,700));
                ImGui.Begin("Native smoke",ImGuiWindowFlags.NoSavedSettings);
                var beforeNested=ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
                using(var nested=MaterialTheme.Push(new(new(.7f,.3f,.4f)),1.5f))
                    Assert.NotEqual(beforeNested,ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);
                Assert.Equal(beforeNested,ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);
                MaterialButton.FilledButton("button","Test");
                MaterialButton.FilledTonalButton("tonal","Test");
                MaterialButton.OutlinedButton("outline","Test");
                MaterialButton.TextButton("text","Test");
                MaterialButton.IconButton("icon",MaterialIcon.Settings);
                var enabled=true;
                MaterialSwitch.Draw("switch","Enabled",ref enabled);
                var selected=0;
                MaterialSegment<int>[] options=[new(0,"Auto"),new(1,"Manual"),new(2,"Off",Disabled:true)];
                MaterialSegmented.Draw<int>("segments",options,ref selected);
                var query="";
                MaterialTextField.SearchField("search",ref query);
                MaterialTextField.TextField("field",ref query,error:true);
                MaterialChip.FilterChip("chip","Filter",ref enabled);
                MaterialStatus.Badge("Ready");
                MaterialStatus.LinearProgress(.4f);
                using(var card=MaterialSurface.Card("card",new(-1,80)))
                    if(card.Visible) MaterialNavigation.Item("navigation","Page",MaterialIcon.Home,true);
                MaterialActionTile.Draw("tile",MaterialIcon.Play,"Start",size:new(140,80));
                var open=false;
                using(var dialog=MaterialDialog.Begin("closed",ref open)) Assert.False(dialog.Visible);
                ImGui.End();
            }
            Assert.Equal(original,ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);
            Assert.Equal(padding,ImGui.GetStyle().WindowPadding);
            for(var i=0;i<originalColors.Length;i++) Assert.Equal(originalColors[i],ImGui.GetStyle().Colors[i]);
            ImGui.Render();
            Assert.True(ImGui.GetDrawData().TotalVtxCount>0);
            Assert.True(width>0&&height>0&&pixels!=null);
        }
        finally { ImGui.DestroyContext(context); }
    }
}

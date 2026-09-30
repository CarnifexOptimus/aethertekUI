using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public class ComponentBehaviorTests
{
    [Theory]
    [InlineData(MaterialButtonVariant.Filled)] [InlineData(MaterialButtonVariant.Tonal)]
    [InlineData(MaterialButtonVariant.Outlined)] [InlineData(MaterialButtonVariant.Text)]
    public void EachButtonVariantActivatesOnRelease(MaterialButtonVariant variant)
    {
        using var host=new NativeTestContext(); var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var activated=false;Vector2 center=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0));ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Button",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            activated|=MaterialButton.Draw("stable","Action",variant,leading:MaterialIcon.Play,trailing:MaterialIcon.ArrowRight);
            center=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Click(center,Draw);Assert.True(activated);
    }

    [Fact]
    public void ModalTablesAndChildScopesRestoreStylesEvenWhenClosedOrClipped()
    {
        using var host=new NativeTestContext(); var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var first=true;var open=true;var seen=false;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Scopes",ImGuiWindowFlags.NoSavedSettings);
            var padding=ImGui.GetStyle().CellPadding;
            using(var tableStyle=MaterialTable.PushStyle())
            {
                if(ImGui.BeginTable("table",2,ImGuiTableFlags.RowBg))
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    ImGui.Selectable("Row");MaterialTable.RowState(true,false);
                    ImGui.TableNextColumn();ImGui.TextUnformatted("Data");
                    ImGui.EndTable();
                }
            }
            Assert.Equal(padding,ImGui.GetStyle().CellPadding);
            using(var card=MaterialSurface.Card("child",new(200,30)))
                if(card.Visible) ImGui.TextUnformatted("Content");
            if(first) { ImGui.OpenPopup("modal");first=false; }
            using(var modal=MaterialDialog.Begin("modal",ref open))
                if(modal.Visible) { seen=true;ImGui.CloseCurrentPopup();open=false; }
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);Assert.True(seen);
    }

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void SelectionAndNavigationControlsActivate(int control)
    {
        using var host=new NativeTestContext();var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var value=false;var choice=0;var activated=false;Vector2 center=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0));ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Other controls",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            activated |= control switch
            {
                0=>MaterialChip.FilterChip("chip","Selected",ref value),
                1=>MaterialNavigation.Item("nav","Data",MaterialIcon.Table,value),
                2=>MaterialActionTile.Draw("tile",MaterialIcon.Play,"Start"),
                3=>MaterialSelection.Checkbox("checkbox","Check",ref value),
                _=>MaterialSelection.Radio("radio","Choice",1,ref choice)
            };
            center=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Click(center,Draw);Assert.True(activated);
        if(control is 0 or 3)Assert.True(value);
        if(control==4)Assert.Equal(1,choice);
    }

    [Fact]
    public void PerOptionDisableRejectsSelection()
    {
        using var host=new NativeTestContext();var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var selected=0;Vector2 center=default;
        MaterialSegment<int>[] options=[new(0,"Auto"),new(1,"Blocked",Disabled:true),new(2,"Off")];
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0));ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Disabled option",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            MaterialSegmented.Draw<int>("segments",options,ref selected,300);
            center=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Click(center,Draw);Assert.Equal(0,selected);
    }
}

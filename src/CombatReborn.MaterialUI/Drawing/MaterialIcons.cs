using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public enum MaterialIcon { None, Search, Close, Settings, Check, Play, Pause, Stop, ArrowRight, Return, Edit, Delete, More, ChevronDown, Home, Grid, Table, Sliders, Info }

/// <summary>Small vector icons; no font or texture dependency.</summary>
public static class MaterialIcons
{
    public static void Draw(MaterialIcon icon, Vector2 origin, float size, Vector4 color, float? alpha = null)
    {
        var dl=ImGui.GetWindowDrawList();
        var ink=MaterialDraw.Color(color,alpha);
        var stroke=Math.Max(1,size/10);
        Vector2 P(float x,float y) => origin+new Vector2(x,y)*size;
        void L(float x,float y,float a,float b) => dl.AddLine(P(x,y),P(a,b),ink,stroke);
        switch(icon)
        {
            case MaterialIcon.None: break;
            case MaterialIcon.Search:
                dl.AddCircle(P(.42f,.42f),size*.26f,ink,16,stroke); L(.62f,.62f,.88f,.88f); break;
            case MaterialIcon.Close: L(.22f,.22f,.78f,.78f); L(.78f,.22f,.22f,.78f); break;
            case MaterialIcon.Check: L(.16f,.5f,.4f,.74f); L(.4f,.74f,.86f,.24f); break;
            case MaterialIcon.Play: dl.AddTriangleFilled(P(.28f,.15f),P(.82f,.5f),P(.28f,.85f),ink); break;
            case MaterialIcon.Pause: dl.AddRectFilled(P(.22f,.18f),P(.4f,.82f),ink); dl.AddRectFilled(P(.6f,.18f),P(.78f,.82f),ink); break;
            case MaterialIcon.Stop: dl.AddRectFilled(P(.23f,.23f),P(.77f,.77f),ink,size*.08f); break;
            case MaterialIcon.ArrowRight: L(.15f,.5f,.85f,.5f); L(.55f,.2f,.85f,.5f); L(.55f,.8f,.85f,.5f); break;
            case MaterialIcon.Return: L(.8f,.2f,.8f,.65f); L(.8f,.65f,.2f,.65f); L(.2f,.65f,.43f,.42f); L(.2f,.65f,.43f,.88f); break;
            case MaterialIcon.ChevronDown: L(.2f,.35f,.5f,.65f); L(.5f,.65f,.8f,.35f); break;
            case MaterialIcon.Edit: L(.22f,.78f,.75f,.25f); L(.3f,.86f,.83f,.33f); L(.2f,.8f,.16f,.9f); break;
            case MaterialIcon.Delete: L(.2f,.25f,.8f,.25f); L(.38f,.12f,.62f,.12f); L(.3f,.35f,.34f,.85f); L(.7f,.35f,.66f,.85f); L(.34f,.85f,.66f,.85f); break;
            case MaterialIcon.More: for(var i=0;i<3;i++) dl.AddCircleFilled(P(.25f+i*.25f,.5f),size*.07f,ink); break;
            case MaterialIcon.Home: L(.1f,.45f,.5f,.12f); L(.5f,.12f,.9f,.45f); L(.25f,.4f,.25f,.85f); L(.25f,.85f,.75f,.85f); L(.75f,.85f,.75f,.4f); break;
            case MaterialIcon.Grid:
                for(var x=0;x<2;x++) for(var y=0;y<2;y++) dl.AddRect(P(.16f+x*.4f,.16f+y*.4f),P(.44f+x*.4f,.44f+y*.4f),ink,size*.04f,ImDrawFlags.RoundCornersAll,stroke);
                break;
            case MaterialIcon.Table:
                dl.AddRect(P(.12f,.15f),P(.88f,.85f),ink,0,ImDrawFlags.None,stroke); L(.12f,.38f,.88f,.38f); L(.12f,.62f,.88f,.62f); L(.42f,.15f,.42f,.85f); break;
            case MaterialIcon.Sliders:
                for(var i=0;i<3;i++) { var y=.22f+i*.28f; L(.12f,y,.88f,y); dl.AddCircleFilled(P(i==1?.65f:.35f,y),size*.09f,ink); }
                break;
            case MaterialIcon.Info:
                dl.AddCircle(P(.5f,.5f),size*.37f,ink,20,stroke); L(.5f,.46f,.5f,.72f); dl.AddCircleFilled(P(.5f,.28f),size*.05f,ink); break;
            case MaterialIcon.Settings:
                dl.AddCircle(P(.5f,.5f),size*.27f,ink,16,stroke);
                dl.AddCircle(P(.5f,.5f),size*.09f,ink,12,stroke);
                for(var i=0;i<8;i++) { var a=i*MathF.PI/4; var v=new Vector2(MathF.Cos(a),MathF.Sin(a)); dl.AddLine(P(.5f,.5f)+v*size*.27f,P(.5f,.5f)+v*size*.4f,ink,stroke); }
                break;
        }
    }
}

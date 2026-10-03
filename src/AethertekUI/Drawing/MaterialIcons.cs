using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public enum MaterialIcon { None, Search, Close, Settings, Check, Play, Pause, Stop, ArrowRight, Return, Edit, Delete, More, ChevronDown, Home, Grid, Table, Sliders, Info, Document, Database, Refresh, Chart, ExternalLink, Download, Palette, Globe, Map, Clock, Link, Calculator }

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
            case MaterialIcon.Calculator:
                dl.AddRect(P(.22f,.08f),P(.78f,.92f),ink,size*.05f,ImDrawFlags.None,stroke);
                dl.AddRect(P(.32f,.19f),P(.68f,.36f),ink,0,ImDrawFlags.None,stroke*.6f);
                for(var row=0;row<3;row++) for(var column=0;column<3;column++)
                    dl.AddRectFilled(P(.31f+column*.15f,.47f+row*.13f),P(.38f+column*.15f,.54f+row*.13f),ink);
                break;
            case MaterialIcon.Map:
                L(.12f,.25f,.36f,.12f); L(.36f,.12f,.64f,.25f); L(.64f,.25f,.88f,.12f);
                L(.12f,.25f,.12f,.88f); L(.12f,.88f,.36f,.75f); L(.36f,.75f,.64f,.88f);
                L(.64f,.88f,.88f,.75f); L(.88f,.75f,.88f,.12f); L(.36f,.12f,.36f,.75f); L(.64f,.25f,.64f,.88f); break;
            case MaterialIcon.Clock:
                dl.AddCircle(P(.5f,.5f),size*.38f,ink,24,stroke); L(.5f,.25f,.5f,.5f); L(.5f,.5f,.7f,.61f); break;
            case MaterialIcon.Link:
                for(var link=0;link<2;link++)
                {
                    var center=link==0 ? P(.35f,.65f) : P(.65f,.35f);
                    for(var i=0;i<24;i++)
                    {
                        var t=i*MathF.Tau/24; var x=MathF.Cos(t)*size*.25f; var y=MathF.Sin(t)*size*.13f;
                        dl.PathLineTo(center+new Vector2((x+y)*.7071f,(-x+y)*.7071f));
                    }
                    dl.PathStroke(ink,ImDrawFlags.Closed,stroke);
                }
                L(.4f,.6f,.6f,.4f); break;
            case MaterialIcon.Palette:
                dl.AddCircle(P(.5f,.5f),size*.38f,ink,24,stroke);
                for(var i=0;i<4;i++) { var angle=i*MathF.PI/3; dl.AddCircleFilled(P(.5f+.24f*MathF.Cos(angle),.5f+.24f*MathF.Sin(angle)),size*.06f,ink); }
                break;
            case MaterialIcon.Globe:
                dl.AddCircle(P(.5f,.5f),size*.38f,ink,24,stroke); L(.12f,.5f,.88f,.5f); L(.5f,.12f,.5f,.88f);
                for(var i=0;i<24;i++) { var angle=i*MathF.Tau/24; dl.PathLineTo(P(.5f+.18f*MathF.Cos(angle),.5f+.38f*MathF.Sin(angle))); }
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke); break;
            case MaterialIcon.None: break;
            case MaterialIcon.Document:
                L(.22f,.12f,.62f,.12f); L(.62f,.12f,.8f,.3f); L(.8f,.3f,.8f,.88f); L(.8f,.88f,.22f,.88f); L(.22f,.88f,.22f,.12f);
                L(.62f,.12f,.62f,.3f); L(.62f,.3f,.8f,.3f); L(.34f,.48f,.68f,.48f); L(.34f,.64f,.68f,.64f); break;
            case MaterialIcon.Database:
                L(.18f,.23f,.18f,.77f); L(.82f,.23f,.82f,.77f);
                for(var band=0;band<3;band++)
                {
                    for(var i=0;i<24;i++)
                    {
                        var angle=i*MathF.Tau/24;
                        dl.PathLineTo(P(.5f+.32f*MathF.Cos(angle),.23f+band*.27f+.13f*MathF.Sin(angle)));
                    }
                    dl.PathStroke(ink,ImDrawFlags.Closed,stroke);
                }
                break;
            case MaterialIcon.Refresh:
                dl.PathArcTo(P(.5f,.5f),size*.31f,.2f,MathF.PI*1.7f,20); dl.PathStroke(ink,ImDrawFlags.None,stroke);
                L(.65f,.1f,.7f,.29f); L(.7f,.29f,.5f,.27f); break;
            case MaterialIcon.Chart:
                dl.AddRectFilled(P(.16f,.56f),P(.32f,.86f),ink); dl.AddRectFilled(P(.42f,.34f),P(.58f,.86f),ink);
                dl.AddRectFilled(P(.68f,.12f),P(.84f,.86f),ink); break;
            case MaterialIcon.ExternalLink:
                L(.42f,.2f,.18f,.2f); L(.18f,.2f,.18f,.82f); L(.18f,.82f,.8f,.82f); L(.8f,.82f,.8f,.58f);
                L(.45f,.55f,.85f,.15f); L(.62f,.15f,.85f,.15f); L(.85f,.15f,.85f,.38f); break;
            case MaterialIcon.Download:
                L(.5f,.12f,.5f,.65f); L(.28f,.43f,.5f,.65f); L(.5f,.65f,.72f,.43f);
                L(.18f,.65f,.18f,.85f); L(.18f,.85f,.82f,.85f); L(.82f,.85f,.82f,.65f); break;
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

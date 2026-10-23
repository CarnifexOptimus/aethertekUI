using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public enum MaterialIcon { None, Search, Close, Settings, Check, Play, Pause, Stop, ArrowRight, Return, Edit, Delete, More, ChevronDown, Home, Grid, Table, Sliders, Info, Document, Database, Refresh, Chart, ExternalLink, Download, Palette, Globe, Map, Clock, Link, Calculator, Person, Shield, Monitor, Location, Send, Save, Copy, Heart, List, Sword, Star, Folder, Egg, Wrench, DoorExit, Cube, Chest, Cart, Chat, Group, Warning, Plus, Pulse, Utensils, Crystal, Calendar, Crown, Book, Sparkle, Bell, Terminal, Power, Target, MapPin, HeartOutline, Discord, KoFi, Backpack, Chocobo }

/// <summary>Small vector icons; no font or texture dependency.</summary>
public static class MaterialIcons
{
    public static void Draw(MaterialIcon icon, Vector2 origin, float size, Vector4 color, float? alpha = null)
        => Draw(icon, ImGui.GetWindowDrawList(), origin, size, color, alpha);

    /// <summary>Draw into a borrowed list while a native popup owns the current window.</summary>
    public static void Draw(MaterialIcon icon, ImDrawListPtr drawList, Vector2 origin, float size, Vector4 color, float? alpha = null)
    {
        var dl=drawList;
        var ink=MaterialDraw.Color(color,alpha);
        var stroke=Math.Max(1,size/10);
        Vector2 P(float x,float y) => origin+new Vector2(x,y)*size;
        void L(float x,float y,float a,float b) => dl.AddLine(P(x,y),P(a,b),ink,stroke);
        switch(icon)
        {
            case MaterialIcon.Chocobo:
                for(var i=0;i<32;i++) { var angle=i*MathF.Tau/32; dl.PathLineTo(P(.42f+MathF.Cos(angle)*.26f,.53f+MathF.Sin(angle)*.22f)); }
                dl.PathFillConvex(ink);
                dl.AddRectFilled(P(.57f,.25f),P(.73f,.58f),ink,size*.06f);
                dl.AddCircleFilled(P(.69f,.22f),size*.13f,ink,20);
                dl.AddTriangleFilled(P(.78f,.2f),P(.94f,.26f),P(.78f,.3f),ink);
                dl.AddTriangleFilled(P(.16f,.52f),P(.04f,.29f),P(.29f,.43f),ink);
                L(.36f,.69f,.32f,.88f); L(.52f,.69f,.56f,.88f);
                L(.23f,.89f,.41f,.89f); L(.48f,.89f,.67f,.89f);
                break;
            case MaterialIcon.Backpack:
                dl.AddRect(P(.37f,.08f),P(.63f,.29f),ink,size*.08f,ImDrawFlags.RoundCornersAll,stroke);
                dl.AddRect(P(.19f,.22f),P(.81f,.9f),ink,size*.12f,ImDrawFlags.RoundCornersAll,stroke);
                dl.AddRect(P(.3f,.53f),P(.7f,.78f),ink,size*.05f,ImDrawFlags.RoundCornersAll,stroke);
                L(.33f,.38f,.67f,.38f);L(.19f,.47f,.12f,.76f);L(.81f,.47f,.88f,.76f);
                break;
            case MaterialIcon.Discord:
            {
                var flags=dl.Flags;
                // Curved rings leave genuine transparent eye openings during window fades.
                dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                try
                {
                    for(var side=0;side<2;side++)
                    {
                        Vector2 SidePoint(Vector2 point)=>origin+new Vector2(side==0?point.X:1-point.X,point.Y)*size;
                        var eye=new Vector2(.35f,.54f);
                        Vector2 Rim(Vector2 point)
                        {
                            var direction=point-eye;
                            var distance=(direction/new Vector2(.067f,.086f)).Length();
                            return eye+direction/distance;
                        }
                        var from=new Vector2(.5f,.22f);
                        void Point(Vector2 point,bool outline=true)
                        {
                            dl.AddQuadFilled(SidePoint(from),SidePoint(point),SidePoint(Rim(point)),SidePoint(Rim(from)),ink);
                            if(outline) dl.PathLineTo(SidePoint(point));
                            from=point;
                        }
                        void Curve(Vector2 a,Vector2 b,Vector2 end)
                        {
                            var start=from;
                            for(var i=1;i<=12;i++)
                            {
                                var t=i/12f;var u=1-t;
                                Point(u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end);
                            }
                        }
                        dl.PathLineTo(SidePoint(from));
                        Curve(new(.44f,.20f),new(.42f,.20f),new(.38f,.22f));
                        Curve(new(.37f,.19f),new(.365f,.17f),new(.36f,.16f));
                        Curve(new(.29f,.17f),new(.21f,.20f),new(.16f,.23f));
                        Curve(new(.08f,.34f),new(.035f,.55f),new(.05f,.74f));
                        Curve(new(.10f,.78f),new(.18f,.82f),new(.25f,.83f));
                        Curve(new(.27f,.805f),new(.29f,.775f),new(.30f,.755f));
                        Curve(new(.37f,.79f),new(.43f,.79f),new(.5f,.79f));
                        dl.PathStroke(ink,ImDrawFlags.None,.7f);
                        var bottom=from;
                        for(var i=1;i<=24;i++) Point(Vector2.Lerp(bottom,new(.5f,.22f),i/24f),outline:false);
                        for(var i=0;i<32;i++)
                        {
                            var angle=i*MathF.Tau/32;
                            dl.PathLineTo(SidePoint(eye+new Vector2(MathF.Cos(angle)*.067f,MathF.Sin(angle)*.086f)));
                        }
                        // Native antialias fringes can cover the entire eye at small sizes.
                        var eyeFlags=dl.Flags;
                        dl.Flags &= ~ImDrawListFlags.AntiAliasedLines;
                        try { dl.PathStroke(ink,ImDrawFlags.Closed,Math.Min(.7f,size*.035f)); }
                        finally { dl.Flags=eyeFlags; }
                    }
                }
                finally { dl.Flags=flags; }
                break;
            }
            case MaterialIcon.KoFi:
                dl.PathLineTo(P(.69f,.26f));
                dl.PathBezierCubicCurveTo(P(.97f,.21f),P(.94f,.65f),P(.69f,.65f));
                dl.PathStroke(ink,ImDrawFlags.None,size*.095f);
                dl.AddRectFilled(P(.06f,.18f),P(.75f,.84f),ink,size*.08f);
                Draw(MaterialIcon.Heart,dl,P(.11f,.23f),size*.60f,new Vector4(1,.29f,.33f,color.W),alpha);
                break;
            case MaterialIcon.Wrench:
                dl.PathLineTo(P(.16f,.74f));dl.PathLineTo(P(.5f,.4f));dl.PathLineTo(P(.49f,.23f));
                dl.PathLineTo(P(.62f,.1f));dl.PathLineTo(P(.7f,.1f));dl.PathLineTo(P(.64f,.27f));
                dl.PathLineTo(P(.75f,.37f));dl.PathLineTo(P(.91f,.3f));dl.PathLineTo(P(.91f,.39f));
                dl.PathLineTo(P(.77f,.51f));dl.PathLineTo(P(.6f,.5f));dl.PathLineTo(P(.26f,.84f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);break;
            case MaterialIcon.DoorExit:
                dl.AddRect(P(.14f,.14f),P(.52f,.87f),ink,0,ImDrawFlags.None,stroke);
                dl.PathLineTo(P(.16f,.15f));dl.PathLineTo(P(.42f,.24f));dl.PathLineTo(P(.42f,.85f));
                dl.PathLineTo(P(.16f,.78f));dl.PathFillConvex(ink);
                L(.57f,.51f,.9f,.51f);L(.77f,.38f,.9f,.51f);L(.77f,.64f,.9f,.51f);break;
            case MaterialIcon.Cube:
                dl.PathLineTo(P(.5f,.1f));dl.PathLineTo(P(.88f,.3f));dl.PathLineTo(P(.88f,.73f));
                dl.PathLineTo(P(.5f,.84f));dl.PathLineTo(P(.12f,.73f));dl.PathLineTo(P(.12f,.3f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);
                L(.12f,.3f,.5f,.5f);L(.5f,.5f,.88f,.3f);L(.5f,.5f,.5f,.84f);
                dl.AddQuadFilled(P(.5f,.22f),P(.64f,.3f),P(.5f,.38f),P(.36f,.3f),ink);break;
            case MaterialIcon.Chest:
                dl.AddRect(P(.13f,.16f),P(.87f,.85f),ink,size*.09f,ImDrawFlags.None,stroke);
                L(.13f,.46f,.87f,.46f);L(.29f,.18f,.29f,.44f);L(.71f,.18f,.71f,.44f);
                dl.AddRectFilled(P(.42f,.39f),P(.58f,.6f),ink,size*.025f);break;
            case MaterialIcon.Cart:
                L(.1f,.16f,.23f,.16f);L(.23f,.16f,.36f,.71f);L(.36f,.71f,.8f,.71f);
                dl.PathLineTo(P(.28f,.3f));dl.PathLineTo(P(.89f,.3f));dl.PathLineTo(P(.8f,.58f));
                dl.PathLineTo(P(.34f,.58f));dl.PathFillConvex(ink);
                dl.AddCircleFilled(P(.41f,.85f),size*.06f,ink,16);dl.AddCircleFilled(P(.75f,.85f),size*.06f,ink,16);break;
            case MaterialIcon.Chat:
                dl.AddRect(P(.35f,.34f),P(.91f,.77f),ink,size*.04f,ImDrawFlags.None,stroke);
                L(.76f,.77f,.84f,.89f);L(.84f,.89f,.84f,.77f);
                dl.AddRectFilled(P(.09f,.12f),P(.72f,.57f),ink,size*.055f);
                dl.AddTriangleFilled(P(.2f,.54f),P(.31f,.54f),P(.2f,.69f),ink);break;
            case MaterialIcon.Group:
                dl.AddCircleFilled(P(.35f,.29f),size*.12f,ink,24);dl.AddCircleFilled(P(.68f,.31f),size*.105f,ink,24);
                dl.AddRectFilled(P(.1f,.52f),P(.59f,.82f),ink,size*.1f,ImDrawFlags.RoundCornersTop);
                dl.AddRectFilled(P(.63f,.54f),P(.9f,.82f),ink,size*.09f,ImDrawFlags.RoundCornersTop);break;
            case MaterialIcon.Warning:
            {
                // Triangulate the outline so the acute tip has no stroke-miter overshoot.
                Vector2[] outer=[P(.5f,.12f),P(.88f,.88f),P(.12f,.88f)];
                Vector2[] inner=[P(.5f,.31f),P(.7f,.75f),P(.3f,.75f)];
                var flags=dl.Flags;dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                try
                {
                    for(var i=0;i<3;i++)
                    {
                        var next=(i+1)%3;
                        dl.AddTriangleFilled(outer[i],outer[next],inner[next],ink);
                        dl.AddTriangleFilled(outer[i],inner[next],inner[i],ink);
                    }
                }
                finally { dl.Flags=flags; }
                L(.5f,.42f,.5f,.58f);dl.AddCircleFilled(P(.5f,.69f),size*.03f,ink,12);break;
            }
            case MaterialIcon.Plus:
                dl.AddRectFilled(P(.43f,.12f),P(.57f,.88f),ink);dl.AddRectFilled(P(.12f,.43f),P(.88f,.57f),ink);break;
            case MaterialIcon.Pulse:
                L(.08f,.52f,.3f,.52f);L(.3f,.52f,.41f,.13f);L(.41f,.13f,.55f,.87f);
                L(.55f,.87f,.67f,.42f);L(.67f,.42f,.75f,.52f);L(.75f,.52f,.92f,.52f);break;
            case MaterialIcon.Utensils:
                L(.19f,.12f,.19f,.4f);L(.3f,.12f,.3f,.88f);L(.41f,.12f,.41f,.4f);
                dl.PathLineTo(P(.19f,.4f));dl.PathLineTo(P(.3f,.49f));dl.PathLineTo(P(.41f,.4f));dl.PathStroke(ink,ImDrawFlags.None,stroke);
                dl.PathLineTo(P(.77f,.18f));dl.PathLineTo(P(.62f,.37f));dl.PathLineTo(P(.62f,.57f));
                dl.PathLineTo(P(.77f,.57f));dl.PathFillConvex(ink);L(.77f,.18f,.77f,.88f);break;
            case MaterialIcon.Crystal:
                dl.PathLineTo(P(.48f,.16f));dl.PathLineTo(P(.52f,.16f));dl.PathLineTo(P(.74f,.34f));dl.PathLineTo(P(.7f,.68f));
                dl.PathLineTo(P(.52f,.84f));dl.PathLineTo(P(.48f,.84f));dl.PathLineTo(P(.3f,.68f));dl.PathLineTo(P(.26f,.34f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);
                L(.5f,.16f,.5f,.84f);L(.26f,.34f,.5f,.44f);L(.5f,.44f,.74f,.34f);
                L(.3f,.68f,.5f,.61f);L(.5f,.61f,.7f,.68f);break;
            case MaterialIcon.Calendar:
                dl.AddRect(P(.12f,.23f),P(.88f,.87f),ink,size*.04f,ImDrawFlags.None,stroke);
                L(.12f,.4f,.88f,.4f);L(.3f,.1f,.3f,.31f);L(.7f,.1f,.7f,.31f);
                for(var row=0;row<2;row++)for(var column=0;column<3;column++)
                    dl.AddRectFilled(P(.26f+column*.2f,.51f+row*.19f),P(.32f+column*.2f,.57f+row*.19f),ink);
                break;
            case MaterialIcon.Crown:
                L(.12f,.29f,.32f,.48f);L(.32f,.48f,.5f,.15f);L(.5f,.15f,.68f,.48f);
                L(.68f,.48f,.88f,.29f);L(.88f,.29f,.77f,.75f);L(.77f,.75f,.23f,.75f);L(.23f,.75f,.12f,.29f);
                L(.23f,.86f,.77f,.86f);dl.AddCircleFilled(P(.5f,.13f),size*.045f,ink,12);
                dl.AddCircleFilled(P(.11f,.26f),size*.045f,ink,12);dl.AddCircleFilled(P(.89f,.26f),size*.045f,ink,12);break;
            case MaterialIcon.Book:
                dl.PathLineTo(P(.14f,.2f));dl.PathLineTo(P(.33f,.2f));dl.PathLineTo(P(.5f,.3f));
                dl.PathLineTo(P(.67f,.2f));dl.PathLineTo(P(.86f,.2f));dl.PathLineTo(P(.86f,.83f));
                dl.PathLineTo(P(.67f,.83f));dl.PathLineTo(P(.5f,.87f));dl.PathLineTo(P(.33f,.83f));
                dl.PathLineTo(P(.14f,.83f));dl.PathStroke(ink,ImDrawFlags.Closed,stroke);
                L(.5f,.3f,.5f,.87f);L(.23f,.35f,.23f,.68f);L(.77f,.35f,.77f,.68f);break;
            case MaterialIcon.Sparkle:
                void Spark(float x,float y,float radius)
                {
                    dl.AddQuadFilled(P(x,y-radius),P(x+radius*.32f,y-radius*.32f),P(x+radius,y),P(x,y),ink);
                    dl.AddQuadFilled(P(x,y),P(x+radius,y),P(x+radius*.32f,y+radius*.32f),P(x,y+radius),ink);
                    dl.AddQuadFilled(P(x-radius,y),P(x-radius*.32f,y-radius*.32f),P(x,y-radius),P(x,y),ink);
                    dl.AddQuadFilled(P(x,y),P(x,y+radius),P(x-radius*.32f,y+radius*.32f),P(x-radius,y),ink);
                }
                var sparkleFlags=dl.Flags;dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                try { Spark(.38f,.52f,.3f);Spark(.78f,.23f,.12f);Spark(.78f,.81f,.1f); }
                finally { dl.Flags=sparkleFlags; }break;
            case MaterialIcon.Bell:
                dl.PathLineTo(P(.13f,.76f));dl.PathLineTo(P(.25f,.64f));dl.PathLineTo(P(.25f,.37f));
                dl.PathBezierCubicCurveTo(P(.25f,.03f),P(.75f,.03f),P(.75f,.37f));
                dl.PathLineTo(P(.75f,.64f));dl.PathLineTo(P(.87f,.76f));
                dl.PathStroke(ink,ImDrawFlags.None,stroke);L(.13f,.76f,.87f,.76f);
                dl.AddCircleFilled(P(.5f,.88f),size*.065f,ink,16);break;
            case MaterialIcon.Terminal:
                L(.15f,.23f,.42f,.5f);L(.42f,.5f,.15f,.77f);L(.54f,.78f,.88f,.78f);break;
            case MaterialIcon.Power:
                dl.PathArcTo(P(.5f,.55f),size*.32f,-MathF.PI*.5f+.6f,MathF.PI*1.5f-.6f,32);
                dl.PathStroke(ink,ImDrawFlags.None,stroke);L(.5f,.09f,.5f,.48f);break;
            case MaterialIcon.Target:
                dl.AddCircle(P(.5f,.5f),size*.28f,ink,32,stroke);
                L(.5f,.06f,.5f,.3f);L(.5f,.7f,.5f,.94f);
                L(.06f,.5f,.3f,.5f);L(.7f,.5f,.94f,.5f);
                dl.AddCircleFilled(P(.5f,.5f),size*.055f,ink,16);break;
            case MaterialIcon.MapPin:
                L(.13f,.3f,.25f,.26f);L(.13f,.3f,.13f,.85f);L(.13f,.85f,.5f,.75f);
                L(.5f,.75f,.87f,.85f);L(.87f,.85f,.87f,.3f);L(.87f,.3f,.75f,.26f);
                L(.5f,.62f,.5f,.75f);
                dl.PathLineTo(P(.5f,.62f));
                dl.PathBezierCubicCurveTo(P(.39f,.48f),P(.25f,.34f),P(.25f,.25f));
                dl.PathBezierCubicCurveTo(P(.25f,.05f),P(.75f,.05f),P(.75f,.25f));
                dl.PathBezierCubicCurveTo(P(.75f,.34f),P(.61f,.48f),P(.5f,.62f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);break;
            case MaterialIcon.HeartOutline:
                dl.PathLineTo(P(.5f,.31f));
                dl.PathBezierCubicCurveTo(P(.32f,.1f),P(.1f,.14f),P(.12f,.38f));
                dl.PathBezierCubicCurveTo(P(.12f,.59f),P(.34f,.75f),P(.5f,.86f));
                dl.PathBezierCubicCurveTo(P(.66f,.75f),P(.88f,.59f),P(.88f,.38f));
                dl.PathBezierCubicCurveTo(P(.9f,.14f),P(.68f,.1f),P(.5f,.31f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);break;
            case MaterialIcon.Folder:
                dl.PathLineTo(P(.1f,.22f));dl.PathLineTo(P(.4f,.22f));dl.PathLineTo(P(.5f,.32f));
                dl.PathLineTo(P(.9f,.32f));dl.PathLineTo(P(.9f,.82f));dl.PathLineTo(P(.1f,.82f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);L(.1f,.42f,.9f,.42f);break;
            case MaterialIcon.Egg:
                for(var i=0;i<48;i++)
                {
                    var angle=i*MathF.Tau/48;var y=MathF.Sin(angle);
                    dl.PathLineTo(P(.5f+MathF.Cos(angle)*(.27f+.065f*y),.5f+.4f*y));
                }
                dl.PathFillConvex(ink);break;
            case MaterialIcon.Sword:
            {
                void Blade(bool mirrored)
                {
                    Vector2 S(float x,float y)=>P(mirrored?1-x:x,y);
                    dl.PathLineTo(S(.42f,.5f));dl.PathLineTo(S(.77f,.15f));dl.PathLineTo(S(.9f,.1f));
                    dl.PathLineTo(S(.85f,.23f));dl.PathLineTo(S(.5f,.58f));dl.PathFillConvex(ink);
                    dl.AddLine(S(.3f,.43f),S(.6f,.73f),ink,stroke);
                    dl.AddLine(S(.23f,.81f),S(.44f,.6f),ink,stroke);
                    dl.AddCircleFilled(S(.2f,.84f),size*.06f,ink,12);
                }
                Blade(false);Blade(true);break;
            }
            case MaterialIcon.Star:
            {
                // The binding lacks concave fills. Avoid antialias fringes on the fan's internal edges.
                var flags=dl.Flags;dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                try
                {
                    for(var i=0;i<10;i++)
                    {
                        var angle=-MathF.PI*.5f+i*MathF.Tau/10;
                        var next=-MathF.PI*.5f+(i+1)*MathF.Tau/10;
                        var radius=i%2==0?.4f:.17f;var nextRadius=i%2==0?.17f:.4f;
                        dl.AddTriangleFilled(P(.5f,.5f),P(.5f+MathF.Cos(angle)*radius,.5f+MathF.Sin(angle)*radius),
                            P(.5f+MathF.Cos(next)*nextRadius,.5f+MathF.Sin(next)*nextRadius),ink);
                    }
                }
                finally { dl.Flags=flags; }
                break;
            }
            case MaterialIcon.Save:
                dl.PathLineTo(P(.14f,.12f));dl.PathLineTo(P(.72f,.12f));dl.PathLineTo(P(.87f,.27f));
                dl.PathLineTo(P(.87f,.88f));dl.PathLineTo(P(.14f,.88f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);
                dl.AddRect(P(.29f,.12f),P(.66f,.4f),ink,0,ImDrawFlags.None,stroke);
                dl.AddRect(P(.29f,.57f),P(.72f,.88f),ink,0,ImDrawFlags.None,stroke);break;
            case MaterialIcon.Copy:
                L(.22f,.69f,.12f,.69f);L(.12f,.69f,.12f,.12f);L(.12f,.12f,.68f,.12f);L(.68f,.12f,.68f,.23f);
                dl.AddRect(P(.32f,.32f),P(.88f,.88f),ink,size*.04f,ImDrawFlags.None,stroke);break;
            case MaterialIcon.Heart:
            {
                var center=P(.5f,.55f);var from=P(.5f,.31f);
                // Keep internal fan edges opaque, as for the concave Star mesh.
                var flags=dl.Flags;dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                void Curve(Vector2 a,Vector2 b,Vector2 end)
                {
                    var start=from;
                    for(var i=1;i<=12;i++)
                    {
                        var t=i/12f;var u=1-t;
                        var point=u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end;
                        dl.AddTriangleFilled(center,point,from,ink);from=point;
                    }
                }
                try
                {
                    Curve(P(.32f,.1f),P(.1f,.14f),P(.12f,.38f));
                    Curve(P(.12f,.59f),P(.34f,.75f),P(.5f,.86f));
                    Curve(P(.66f,.75f),P(.88f,.59f),P(.88f,.38f));
                    Curve(P(.9f,.14f),P(.68f,.1f),P(.5f,.31f));
                }
                finally { dl.Flags=flags; }
                break;
            }
            case MaterialIcon.List:
                for(var i=0;i<3;i++)
                {
                    var y=.22f+i*.28f;
                    dl.AddRectFilled(P(.13f,y-.045f),P(.23f,y+.045f),ink);
                    L(.35f,y,.87f,y);
                }
                break;
            case MaterialIcon.Send:
                L(.12f,.13f,.92f,.5f); L(.92f,.5f,.12f,.87f);
                L(.12f,.87f,.28f,.5f); L(.28f,.5f,.12f,.13f);
                L(.28f,.5f,.92f,.5f); break;
            case MaterialIcon.Location:
            {
                // Filled pin triangulated around its transparent circular cutout.
                const int segments=48;
                var center=P(.5f,.33f);
                var flags=dl.Flags;dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                try
                {
                    for(var i=0;i<segments;i++)
                    {
                        var a=i*MathF.Tau/segments;var b=(i+1)*MathF.Tau/segments;
                        var u=new Vector2(MathF.Cos(a),MathF.Sin(a));var v=new Vector2(MathF.Cos(b),MathF.Sin(b));
                        var r=.3f+.31f*MathF.Pow(Math.Max(0,u.Y),6);var s=.3f+.31f*MathF.Pow(Math.Max(0,v.Y),6);
                        var outerA=center+u*r*size;var outerB=center+v*s*size;
                        var innerA=center+u*.11f*size;var innerB=center+v*.11f*size;
                        dl.AddTriangleFilled(outerA,outerB,innerB,ink);
                        dl.AddTriangleFilled(outerA,innerB,innerA,ink);
                    }
                }
                finally { dl.Flags=flags; }
                break;
            }
            case MaterialIcon.Monitor:
                dl.AddRect(P(.1f,.12f),P(.9f,.68f),ink,size*.04f,ImDrawFlags.None,stroke);
                L(.5f,.68f,.5f,.86f); L(.3f,.86f,.7f,.86f); break;
            case MaterialIcon.Person:
                dl.AddCircleFilled(P(.5f,.27f),size*.17f,ink,20);
                dl.PathArcTo(P(.5f,.82f),size*.33f,MathF.PI,MathF.Tau,20);
                dl.PathLineTo(P(.83f,.9f));dl.PathLineTo(P(.17f,.9f));
                dl.PathFillConvex(ink);break;
            case MaterialIcon.Shield:
                dl.PathLineTo(P(.5f,.08f));dl.PathLineTo(P(.84f,.21f));dl.PathLineTo(P(.8f,.58f));
                dl.PathLineTo(P(.68f,.77f));dl.PathLineTo(P(.5f,.92f));dl.PathLineTo(P(.32f,.77f));
                dl.PathLineTo(P(.2f,.58f));dl.PathLineTo(P(.16f,.21f));
                dl.PathStroke(ink,ImDrawFlags.Closed,stroke);L(.5f,.22f,.5f,.75f);break;
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

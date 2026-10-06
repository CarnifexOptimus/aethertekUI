using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public unsafe class ShapedTextNativeTests
{
    private static byte* clipboardText;
    // Independent Windows WPF TextFormatter/Ideal reference, installed Nirmala UI,
    // 32 px. The reference uses no AethertekUI shaping, drawing or hit-test code.
    public static TheoryData<string,float,int[]> HindiSamples => new()
    {
        { "हिन्दी",65.19000f,[0,2,6] },
        { "क्षेत्र",41.32667f,[0,4,7] },
        { "कर्म",49.17333f,[0,1,4] },
        { "प्रार्थना",75.72000f,[0,4,7,9] },
        { "खोजें",59.33000f,[0,2,5] },
        { "सेटिंग्स",82.53333f,[0,2,5,8] },
        { "अनुवाद",92.73667f,[0,1,3,5,6] },
        { "कि",35.94000f,[0,2] },
        { "क्ष",22.95333f,[0,3] },
    };

    [Theory]
    [MemberData(nameof(HindiSamples))]
    public void HindiMeasurementAndHitTestsMatchIndependentWindowsShaping(string text,float expectedWidth,int[] stops)
    {
        using var layout=new MaterialTextLayout(text,32);
        Assert.Equal(text,layout.Text);
        Assert.InRange(layout.Size.X,expectedWidth-.15f,expectedWidth+.15f);
        Assert.InRange(layout.Baseline,34.43f,34.63f);
        Assert.InRange(layout.Size.Y,42.46f,42.67f);
        Assert.Equal(stops,layout.Clusters.Select(cluster=>(int)cluster.Position).Append(text.Length).ToArray());
        Assert.All(layout.Clusters,cluster=>
        {
            Assert.True(cluster.Length>0);
            Assert.True(float.IsFinite(cluster.Width));
            Assert.False(cluster.IsNewline);
        });
        var previousX=-1f;
        for(var i=0;i<stops.Length;i++)
        {
            var caret=layout.HitTestPosition(stops[i]);
            Assert.True(float.IsFinite(caret.X)&&float.IsFinite(caret.Y));
            Assert.True(caret.X>=previousX);
            previousX=caret.X;
            if(i==stops.Length-1) continue;
            var next=layout.HitTestPosition(stops[i+1]);
            Assert.True(next.X>caret.X);
            foreach(var fraction in new[]{.2f,.8f})
            {
                var hit=layout.HitTestPoint(new(caret.X+(next.X-caret.X)*fraction,layout.Baseline*.5f));
                Assert.True(hit.Inside);
                Assert.Equal(stops[i],(int)hit.Position);
                Assert.Equal(stops[i+1]-stops[i],(int)hit.Length);
                Assert.Equal(fraction>.5f,hit.Trailing);
            }
        }
        Assert.False(layout.HitTestPoint(new(-50,-50)).Inside);
        var selection=layout.HitTestRange(0,text.Length);
        Assert.NotEmpty(selection);
        Assert.InRange(selection.Min(rect=>rect.Min.X),-.1f,.1f);
        Assert.InRange(selection.Max(rect=>rect.Max.X),expectedWidth-.15f,expectedWidth+.15f);
    }

    [Fact]
    public void ShapingProducesActualWhiteAlphaInkRatherThanMissingGlyphOrAdvanceOnlyResults()
    {
        foreach(var text in new[]{"कि","क्ष","क्षेत्र","कर्म","प्रार्थना"})
        {
            using var layout=new MaterialTextLayout(text,32);
            var bitmap=layout.Rasterize();
            Assert.InRange(bitmap.Width,1,256);
            Assert.InRange(bitmap.Height,1,96);
            Assert.Equal(checked(bitmap.Width*bitmap.Height*4),bitmap.Rgba.Length);
            Assert.True(float.IsFinite(bitmap.Offset.X)&&float.IsFinite(bitmap.Offset.Y));
            var ink=0;
            for(var index=0;index<bitmap.Rgba.Length;index+=4)
            {
                if(bitmap.Rgba[index+3]==0) continue;
                ink++;
                Assert.Equal(255,bitmap.Rgba[index]);
                Assert.Equal(255,bitmap.Rgba[index+1]);
                Assert.Equal(255,bitmap.Rgba[index+2]);
            }
            Assert.True(ink>80,"Shaped glyph ink was not rasterized.");
        }
    }

    [Fact]
    public void ShapedConjunctWidthRejectsTheUnshapedGlyphAdvanceRegression()
    {
        using var layout=new MaterialTextLayout("क्षेत्र",32);
        // Independent physical-face unshaped sum is 82.25 px; shaped reference 41.32667.
        Assert.True(Math.Abs(layout.Size.X-82.25f)>35);
    }

    [Fact]
    public void PreBaseVowelInkMatchesIndependentWindowsPositionEvenWhenAdvanceWidthsAreEqual()
    {
        using var layout=new MaterialTextLayout("कि",32);
        var bitmap=layout.Rasterize();
        // Independent WPF mask, same physical face and em size: the vowel stem
        // lies at x=3..5 and the consonant stem at x=21..22, y=14..17.
        // A codepoint-ordered rendering has virtually identical advance width.
        Assert.True(NearInk(4,16),"Pre-base vowel stem was not placed before the consonant.");
        Assert.True(NearInk(21,16),"Consonant stem did not retain its shaped position.");
        for(var y=15;y<=17;y++)
            for(var x=9;x<=12;x++) Assert.InRange(Alpha(x,y),0,64);

        bool NearInk(int x,int y)
        {
            for(var oy=-1;oy<=1;oy++)
                for(var ox=-1;ox<=1;ox++) if(Alpha(x+ox,y+oy)>=128) return true;
            return false;
        }
        byte Alpha(int x,int y)
        {
            var px=(int)MathF.Round(x-bitmap.Offset.X);
            var py=(int)MathF.Round(y-bitmap.Offset.Y);
            return px<0||py<0||px>=bitmap.Width||py>=bitmap.Height ? (byte)0 : bitmap.Rgba[(py*bitmap.Width+px)*4+3];
        }
    }

    [Fact]
    public void NativeTextTextureRenderingRetainsTintClipAndDeterministicOwnership()
    {
        using var host=new NativeTestContext(400,180);
        var uploads=0;
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            uploads++;
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var color=ImGui.ColorConvertFloat4ToU32(new Vector4(.5f,.25f,1,.5f));
        ImDrawListPtr list=default;
        void Draw()
        {
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(400,180));
            ImGui.Begin("Shaped texture",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.SetWindowFontScale(32/ImGui.GetFont().FontSize);
            using(var textScope=renderer.Push())
            {
                var drawList=ImGui.GetWindowDrawList();list=drawList;
                drawList.PushClipRect(new(20,20),new(38,80),true);
                MaterialText.AddText(drawList,new(20,20),color,"कि");
                drawList.PopClipRect();
            }
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        Assert.Equal(1,uploads);
        var frame=SoftwarePreview.Capture(host);
        Assert.True(ImGui.GetDrawData().TotalVtxCount>0);
        var textVertices=0;
        for(var commandIndex=0;commandIndex<list.CmdBuffer.Size;commandIndex++)
        {
            var command=list.CmdBuffer[commandIndex];
            if(command.TextureId.Handle==1 || command.ElemCount==0) continue;
            Assert.InRange(command.ClipRect.Z,37.99f,38.01f);
            for(var index=command.IdxOffset;index<command.IdxOffset+command.ElemCount;index++)
            {
                var vertex=list.VtxBuffer[list.IdxBuffer[(int)index]+(int)command.VtxOffset];
                Assert.Equal(color,vertex.Col);
                textVertices++;
            }
        }
        Assert.True(textVertices>0,"Shaped text emitted no texture triangles.");
        Assert.Contains(Enumerable.Range(20,18),x=>Enumerable.Range(20,60).Any(y=>frame.Rgba[(y*frame.Width+x)*4+2]>90));
        renderer.Clear();
        Assert.Equal(uploads,host.ReleasedTextures);
        renderer.Dispose();renderer.Dispose();
        Assert.Equal(uploads,host.ReleasedTextures);
        Assert.Throws<ObjectDisposedException>(()=>renderer.GetLayout("कि",32));
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(false,true)]
    [InlineData(true,true)]
    public void HindiComboPaintsPreviewAndOptionsWhileRetainingNativeIdentityAndSelection(bool appearance,bool leadingIcon)
    {
        foreach(var density in new[]{MaterialDensity.Standard,MaterialDensity.Compact})
        foreach(var scale in new[]{1f,1.5f})
        {
            using var host=new NativeTestContext(700,350,scale);
            var bitmaps=new Dictionary<ulong,MaterialTextBitmap>();
            using var renderer=new MaterialTextRenderer(bitmap=>
            {
                var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
                bitmaps.Add(texture.Id.Handle,bitmap);
                return new MaterialTextTexture(texture.Id,texture);
            });
            var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
            var options=new MaterialOptions<string>([new("hi","hi","हिन्दी"),new("area","area","क्षेत्र")]);
            var selected="hi";
            var changes=0;
            var disabled=false;
            Vector2 fieldCenter=default;
            ImDrawListPtr mainList=default;
            float rowHeight=0,rowSpacing=0;
            void Draw()
            {
                using var scope=MaterialTheme.Push(theme,scale);
                using var textScope=renderer.Push();
                ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
                ImGui.Begin("Hindi combo",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
                ImGui.PushID("retained-combo");var expected=ImGui.GetID("##combo");ImGui.PopID();
                mainList=ImGui.GetWindowDrawList();
                rowHeight=Math.Max(ImGui.GetTextLineHeight(),MaterialText.Measure("हिन्दी").Y);
                rowSpacing=MaterialControls.Metrics.ItemSpacing.Y;
                var layout=MaterialComboLayout.Default with {LeadingIcon=leadingIcon?MaterialIcon.Globe:null};
                var changed=appearance
                    ?MaterialCombo.Draw("retained-combo",options,ref selected,layout,
                        new MaterialControlAppearance(theme.Colors.Surface,theme.Colors.OnSurface,theme.Colors.Outline),
                        width:260,searchable:false,disabled:disabled)
                    :MaterialCombo.Draw("retained-combo",options,ref selected,layout,width:260,searchable:false,disabled:disabled);
                if(changed) changes++;
                if(ImGui.GetCurrentContext().OpenPopupStack.Size==0)
                {
                    Assert.Equal(expected,ImGuiP.GetItemID());
                    fieldCenter=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
                }
                ImGui.End();
            }
            host.Frame(Draw);host.Frame(Draw);
            Assert.True(AssertShapedInk(mainList)>0,"The actual combo preview produced no shaped texture ink.");
            host.Click(fieldCenter,Draw);host.Frame(Draw);host.Frame(Draw);
            Assert.Equal(1,ImGui.GetCurrentContext().OpenPopupStack.Size);
            var popup=new ImGuiWindowPtr(ImGui.GetCurrentContext().OpenPopupStack[0].Window);
            Assert.True(AssertShapedInk(popup.DrawList)>=2,"The actual dropdown did not paint both Hindi options.");
            var secondOption=popup.DC.CursorStartPos+new Vector2(20*scale,rowHeight+rowSpacing+rowHeight*.5f);
            host.Click(secondOption,Draw);host.Frame(Draw);
            Assert.Equal("area",selected);Assert.Equal(1,changes);
            Assert.Equal(0,ImGui.GetCurrentContext().OpenPopupStack.Size);
            Assert.True(AssertShapedInk(mainList)>0);
            disabled=true;host.Frame(Draw);host.Click(fieldCenter,Draw);
            Assert.Equal(0,ImGui.GetCurrentContext().OpenPopupStack.Size);
            Assert.Equal("area",selected);Assert.Equal(1,changes);

            int AssertShapedInk(ImDrawListPtr list)
            {
                var count=0;
                for(var i=0;i<list.CmdBuffer.Size;i++)
                {
                    var command=list.CmdBuffer[i];
                    if(command.ElemCount==0 || !bitmaps.TryGetValue(command.TextureId.Handle,out var bitmap)) continue;
                    count++;
                    var min=new Vector2(float.PositiveInfinity);
                    for(var index=command.IdxOffset;index<command.IdxOffset+command.ElemCount;index++)
                        min=Vector2.Min(min,list.VtxBuffer[list.IdxBuffer[(int)index]+(int)command.VtxOffset].Pos);
                    var hasInk=false;
                    for(var y=0;y<bitmap.Height;y++)
                    for(var x=0;x<bitmap.Width;x++)
                    {
                        if(bitmap.Rgba[(y*bitmap.Width+x)*4+3]==0) continue;
                        hasInk=true;
                        Assert.InRange(min.X+x,command.ClipRect.X-.51f,command.ClipRect.Z+.51f);
                        Assert.InRange(min.Y+y,command.ClipRect.Y-.51f,command.ClipRect.W+.51f);
                    }
                    Assert.True(hasInk);
                }
                return count;
            }
        }
    }

    [Theory]
    [InlineData(MaterialDensity.Standard,1f)]
    [InlineData(MaterialDensity.Compact,1f)]
    [InlineData(MaterialDensity.Standard,1.5f)]
    [InlineData(MaterialDensity.Compact,1.5f)]
    public void HindiSearchKeepsOriginalNativeIdClusterNavigationCodepointBackspacePasteAndUndo(MaterialDensity density,float scale)
    {
        var pasteUndoSteps=NativePasteUndoSteps();
        using var host=new NativeTestContext(700,350,scale);
        ImGui.GetIO().ConfigInputTextCursorBlink=false;
        using var clipboard=new DiagnosticClipboard();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        var value="क्षिx";
        var focus=new MaterialSearchState { FocusRequested=true };
        var following="";
        uint id=0,nextId=0,firstId=0;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Hindi search proof",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.PushID("retained-search");id=ImGui.GetID("##input");ImGui.PopID();
            if(firstId==0) firstId=id; else Assert.Equal(firstId,id);
            MaterialTextField.SearchField("retained-search",ref value,hint:"खोजें",width:350,state:focus);
            nextId=ImGui.GetID("following");
            ImGui.InputText("following",ref following,128);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        var state=ImGuiP.GetInputTextState(id);
        Assert.False(state.IsNull);
        Assert.Equal(id,ImGui.GetCurrentContext().ActiveId);
        Key(ImGuiKey.Home);
        Assert.Equal(0,state.Stb.Cursor);
        Key(ImGuiKey.RightArrow);
        Assert.Equal(4,state.Stb.Cursor);
        Key(ImGuiKey.LeftArrow);
        Assert.Equal(0,state.Stb.Cursor);
        Key(ImGuiKey.RightArrow);
        Key(ImGuiKey.Backspace);
        Assert.Equal("क्षx",value);
        Chord(ImGuiKey.Z);
        Assert.Equal("क्षिx",value);
        Key(ImGuiKey.Home);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.ModShift,true);Key(ImGuiKey.RightArrow);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.ModShift,false);host.Frame(Draw);
        Assert.Equal(0,state.Stb.SelectStart);
        Assert.Equal(4,state.Stb.SelectEnd);
        Key(ImGuiKey.Delete);
        Assert.Equal("x",value);
        Chord(ImGuiKey.Z);
        Assert.Equal("क्षिx",value);
        clipboard.Set("क्षेत्र");
        Chord(ImGuiKey.A);Chord(ImGuiKey.V);
        Assert.Equal("क्षेत्र",value);
        for(var undo=0;undo<pasteUndoSteps;undo++) Chord(ImGuiKey.Z);
        Assert.Equal("क्षिx",value);
        Key(ImGuiKey.Tab);host.Frame(Draw);
        Assert.Equal(nextId,ImGui.GetCurrentContext().ActiveId);

        void Key(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw);
        }
        void Chord(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,true);Key(key);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw);
        }
    }

    [Theory]
    [InlineData(MaterialDensity.Standard,1f)]
    [InlineData(MaterialDensity.Compact,1f)]
    [InlineData(MaterialDensity.Standard,1.5f)]
    [InlineData(MaterialDensity.Compact,1.5f)]
    public void HindiSearchUsesShapedMouseGeometryCompleteInkClippingAndHorizontalScroll(MaterialDensity density,float scale)
    {
        using var host=new NativeTestContext(700,350,scale);
        var offsets=new Dictionary<ulong,Vector2>();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            offsets.Add(texture.Id.Handle,bitmap.Offset);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        var value=string.Concat(Enumerable.Repeat("क्षेत्र ",20));
        var original=value;
        var focus=new MaterialSearchState { FocusRequested=true };
        var disabled=false;
        uint id=0;
        Vector2 textOrigin=default,fieldMin=default,fieldMax=default;
        float fontSize=0;
        uint selectionColor=0;
        ImDrawListPtr list=default;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Hindi search geometry",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            fieldMin=ImGui.GetCursorScreenPos();
            fontSize=ImGui.GetFontSize();
            selectionColor=ImGui.GetColorU32(ImGuiCol.TextSelectedBg);
            ImGui.PushID("retained-search");id=ImGui.GetID("##input");ImGui.PopID();
            MaterialTextField.SearchField("retained-search",ref value,hint:"खोजें",width:200,disabled:disabled,state:focus);
            fieldMax=ImGui.GetItemRectMax();
            list=ImGui.GetWindowDrawList();
            // Native group bounds round fractional extents. Capture the painted
            // text's actual origin so IME/mouse assertions compare real geometry.
            var input=ImGuiP.GetInputTextState(id);
            for(var index=list.CmdBuffer.Size-1;index>=0;index--)
            {
                var command=list.CmdBuffer[index];
                if(command.ElemCount==0 || !offsets.TryGetValue(command.TextureId.Handle,out var offset)) continue;
                var first=list.VtxBuffer[list.IdxBuffer[(int)command.IdxOffset]+(int)command.VtxOffset];
                textOrigin=first.Pos-offset+new Vector2(input.IsNull?0:input.ScrollX,0);
                break;
            }
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        var state=ImGuiP.GetInputTextState(id);
        Assert.False(state.IsNull);
        Key(ImGuiKey.Home);
        var shaped=renderer.GetLayout(value,fontSize);
        var firstEnd=shaped.HitTestPosition(4).X;
        host.Click(textOrigin+new Vector2(firstEnd*.8f,shaped.Baseline*.5f),Draw);
        Assert.Equal(4,state.Stb.Cursor);
        var io=ImGui.GetIO();
        io.DeltaTime=.4f;host.Frame(Draw);io.DeltaTime=1f/60;
        io.AddMousePosEvent(textOrigin.X+firstEnd*.1f,textOrigin.Y+shaped.Baseline*.5f);host.Frame(Draw);
        io.AddMouseButtonEvent(0,true);host.Frame(Draw);
        io.AddMousePosEvent(textOrigin.X+shaped.HitTestPosition(7).X-.1f,textOrigin.Y+shaped.Baseline*.5f);host.Frame(Draw);
        io.AddMouseButtonEvent(0,false);host.Frame(Draw);
        Assert.Equal(0,state.Stb.SelectStart);
        Assert.Equal(7,state.Stb.SelectEnd);
        Assert.Equal(original,value);
        Assert.Contains(Enumerable.Range(0,list.VtxBuffer.Size),index=>list.VtxBuffer[index].Col==selectionColor);
        host.Frame(Draw);
        var imageClip=ImageClip(list);
        var bitmap=renderer.GetLayout(value,fontSize).Rasterize();
        var minInkY=int.MaxValue;var maxInkY=-1;
        for(var y=0;y<bitmap.Height;y++)
            for(var x=0;x<bitmap.Width;x++)
                if(bitmap.Rgba[(y*bitmap.Width+x)*4+3]>=128)
                { minInkY=Math.Min(minInkY,y);maxInkY=Math.Max(maxInkY,y); }
        Assert.True(maxInkY>=minInkY);
        Assert.True(textOrigin.Y+bitmap.Offset.Y+minInkY+.5f>=imageClip.Y-.5f,"Top shaped vowel/conjunct ink was clipped by native font padding.");
        Assert.True(textOrigin.Y+bitmap.Offset.Y+maxInkY+.5f<=imageClip.W+.5f,"Bottom shaped ink was clipped by native font padding.");
        Assert.True(imageClip.X>=fieldMin.X&&imageClip.Z<=fieldMax.X);
        var frame=SoftwarePreview.Capture(host);
        Assert.True(frame.Rgba.Length>0);
        Key(ImGuiKey.End);
        Assert.Equal(value.Length,state.Stb.Cursor);
        Assert.True(state.ScrollX>0,"Long Hindi text did not scroll to the native editing caret.");
        var caret=renderer.GetLayout(value,fontSize).HitTestPosition(value.Length);
        Assert.InRange(textOrigin.X+caret.X-state.ScrollX,ImageClip(list).X-1,ImageClip(list).Z+1);
        Assert.InRange(ImGui.GetCurrentContext().PlatformImeData.InputPos.X,textOrigin.X+caret.X-state.ScrollX-.1f,textOrigin.X+caret.X-state.ScrollX+.1f);
        Assert.InRange(ImGui.GetCurrentContext().PlatformImeData.InputLineHeight,shaped.Size.Y-.1f,shaped.Size.Y+.1f);
        Key(ImGuiKey.Home);
        Assert.InRange(state.ScrollX,0,.01f);
        disabled=true;host.Frame(Draw);
        ImGui.GetIO().AddInputCharacter('x');host.Frame(Draw);
        Assert.Equal(original,value);

        void Key(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw);
        }
        static Vector4 ImageClip(ImDrawListPtr drawList)
        {
            for(var index=drawList.CmdBuffer.Size-1;index>=0;index--)
            {
                var command=drawList.CmdBuffer[index];
                if(command.TextureId.Handle!=1&&command.ElemCount>0) return command.ClipRect;
            }
            throw new Xunit.Sdk.XunitException("Shaped native search emitted no image command.");
        }
    }

    [Theory]
    [InlineData(MaterialDensity.Standard,1f)]
    [InlineData(MaterialDensity.Compact,1f)]
    [InlineData(MaterialDensity.Standard,1.5f)]
    [InlineData(MaterialDensity.Compact,1.5f)]
    public void RawHindiTreeRetainsOriginalNativeIdArrowAndCallerScope(MaterialDensity density,float scale)
    {
        using var host=new NativeTestContext(700,350,scale);
        var bitmaps=new Dictionary<ulong,MaterialTextBitmap>();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            bitmaps.Add(texture.Id.Handle,bitmap);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        const string caption="हिन्दी चेतावनियाँ";
        const string label="Warnings##retained-warning-tree";
        var opened=false;var arrowVertices=0;Vector2 point=default;
        ImDrawListPtr list=default;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            using var shaping=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Raw Hindi tree",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            var expected=ImGui.GetID(label);var after=ImGui.GetID("after");
            var window=ImGuiP.GetCurrentWindow();var depth=window.DC.TreeDepth;
            list=ImGui.GetWindowDrawList();var first=list.VtxBuffer.Size;
            var color=ImGui.GetColorU32(ImGuiCol.Text);var white=ImGui.GetIO().Fonts.TexUvWhitePixel;
            var style=ImGui.GetStyle().FramePadding;
            opened=MaterialText.TreeNode(label,display:caption);
            Assert.Equal(expected,ImGui.GetCurrentContext().LastItemData.ID);
            var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
            Assert.True(max.Y-min.Y>=MaterialText.Measure(caption).Y);
            point=min+new Vector2(ImGui.GetFontSize()*.3f,(max.Y-min.Y)*.5f);
            arrowVertices=Enumerable.Range(first,list.VtxBuffer.Size-first).Count(index=>
                list.VtxBuffer[index].Uv==white&&list.VtxBuffer[index].Col==color
                &&list.VtxBuffer[index].Pos.X<min.X+ImGui.GetFontSize());
            if(opened) ImGui.TreePop();
            Assert.Equal(depth,window.DC.TreeDepth);Assert.Equal(after,ImGui.GetID("after"));
            Assert.Equal(style,ImGui.GetStyle().FramePadding);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        AssertCompleteShapedInk(list,bitmaps);
        Assert.False(opened);Assert.True(arrowVertices>=3,"The retained native tree arrow was hidden.");
        host.Click(point,Draw);Assert.True(opened);Assert.True(arrowVertices>=3);
        AssertCompleteShapedInk(list,bitmaps);
    }

    [Theory]
    [InlineData(MaterialDensity.Standard,1f)]
    [InlineData(MaterialDensity.Compact,1f)]
    [InlineData(MaterialDensity.Standard,1.5f)]
    [InlineData(MaterialDensity.Compact,1.5f)]
    public void HindiTableHeaderRetainsExactNativeSortDecorationIdAndPress(MaterialDensity density,float scale)
    {
        using var host=new NativeTestContext(700,350,scale);
        var bitmaps=new Dictionary<ulong,MaterialTextBitmap>();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            bitmaps.Add(texture.Id.Handle,bitmap);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        const string label="Name##retained-sort-header";
        const string caption="हिन्दी";
        var baseline=true;var nativeDecoration=new List<(Vector2,uint)>();
        Vector2 point=default;ImGuiSortDirection direction=default;var images=0;
        ImDrawListPtr list=default;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            using var shaping=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Hindi sortable header",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            if(ImGui.BeginTable("original-table",1,ImGuiTableFlags.Sortable|ImGuiTableFlags.Borders))
            {
                try
                {
                    ImGui.TableSetupColumn(label,ImGuiTableColumnFlags.DefaultSort|ImGuiTableColumnFlags.WidthFixed,400*scale,7);
                    ImGui.TableNextRow(ImGuiTableRowFlags.Headers,MaterialText.Measure("हिन्दी").Y+2*ImGui.GetStyle().CellPadding.Y);
                    ImGui.TableSetColumnIndex(0);
                    var expected=ImGui.GetID(label);list=ImGui.GetWindowDrawList();var first=list.VtxBuffer.Size;
                    var white=ImGui.GetIO().Fonts.TexUvWhitePixel;
                    if(baseline) ImGui.TableHeader(label);else MaterialText.TableHeader(label,caption);
                    Assert.Equal(expected,ImGui.GetCurrentContext().LastItemData.ID);
                    var decoration=Enumerable.Range(first,list.VtxBuffer.Size-first)
                        .Select(index=>list.VtxBuffer[index]).Where(vertex=>vertex.Uv==white)
                        .Select(vertex=>(vertex.Pos,vertex.Col)).ToList();
                    if(baseline) nativeDecoration=decoration;
                    else if(!ImGui.IsItemHovered()) Assert.Equal(nativeDecoration,decoration);
                    point=ImGui.GetItemRectMin()+(ImGui.GetItemRectMax()-ImGui.GetItemRectMin())*.5f;
                    images=Enumerable.Range(0,list.CmdBuffer.Size).Count(index=>
                        list.CmdBuffer[index].TextureId.Handle!=1&&list.CmdBuffer[index].ElemCount>0);
                    direction=ImGui.TableGetSortSpecs().Specs.SortDirection;
                }
                finally { ImGui.EndTable(); }
            }
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Frame(Draw);
        Assert.True(nativeDecoration.Count>=3,"The native sortable header emitted no sort decoration.");
        baseline=false;host.Frame(Draw);host.Frame(Draw);
        AssertCompleteShapedInk(list,bitmaps);
        Assert.True(images>0,"The retained header emitted no shaped caption.");
        var before=direction;host.Click(point,Draw);Assert.NotEqual(before,direction);
        AssertCompleteShapedInk(list,bitmaps);
    }

    private static void AssertCompleteShapedInk(ImDrawListPtr list,Dictionary<ulong,MaterialTextBitmap> bitmaps)
    {
        var found=false;
        for(var index=0;index<list.CmdBuffer.Size;index++)
        {
            var command=list.CmdBuffer[index];
            if(command.ElemCount==0||!bitmaps.TryGetValue(command.TextureId.Handle,out var bitmap)) continue;
            found=true;
            var origin=list.VtxBuffer[list.IdxBuffer[(int)command.IdxOffset]+(int)command.VtxOffset].Pos;
            var min=new Vector2(float.MaxValue);var max=new Vector2(float.MinValue);
            for(var y=0;y<bitmap.Height;y++)
                for(var x=0;x<bitmap.Width;x++)
                    if(bitmap.Rgba[(y*bitmap.Width+x)*4+3]>0)
                    {
                        min=Vector2.Min(min,origin+new Vector2(x,y));
                        max=Vector2.Max(max,origin+new Vector2(x+1,y+1));
                    }
            Assert.True(min.X>=command.ClipRect.X&&min.Y>=command.ClipRect.Y
                &&max.X<=command.ClipRect.Z&&max.Y<=command.ClipRect.W,
                $"Retained native caption clipped Hindi ink: {min}..{max}, clip {command.ClipRect}.");
        }
        Assert.True(found,"Retained native caption emitted no shaped text.");
    }

    [Fact]
    public void RetainedTreeAndHeaderUploadFailureRestoreNativeScopes()
    {
        using var host=new NativeTestContext(700,350);
        using var renderer=new MaterialTextRenderer(_=>throw new InvalidOperationException("diagnostic upload failure"));
        void Draw()
        {
            using var shaping=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Retained caption failure",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            var window=ImGuiP.GetCurrentWindow();var depth=window.DC.TreeDepth;
            var id=ImGui.GetID("after");var clips=window.DrawList.ClipRectStack.Size;
            var padding=ImGui.GetStyle().FramePadding;
            ImGui.SetNextItemOpen(true,ImGuiCond.Always);
            Assert.Throws<InvalidOperationException>(()=>MaterialText.TreeNode("हिन्दी###tree"));
            Assert.Equal(depth,window.DC.TreeDepth);Assert.Equal(id,ImGui.GetID("after"));
            Assert.Equal(clips,window.DrawList.ClipRectStack.Size);Assert.Equal(padding,ImGui.GetStyle().FramePadding);
            if(ImGui.BeginTable("header-failure",1,ImGuiTableFlags.Sortable))
            {
                try
                {
                    ImGui.TableSetupColumn("हिन्दी###header",ImGuiTableColumnFlags.DefaultSort);
                    ImGui.TableNextRow(ImGuiTableRowFlags.Headers,40);ImGui.TableSetColumnIndex(0);
                    clips=window.DrawList.ClipRectStack.Size;
                    Assert.Throws<InvalidOperationException>(()=>MaterialText.TableHeader("हिन्दी###header"));
                    Assert.Equal(clips,window.DrawList.ClipRectStack.Size);
                }
                finally { ImGui.EndTable(); }
            }
            ImGui.End();
        }
        host.Frame(Draw);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetainedHindiInputPreservesLeadingInkInsideFramePadding(bool showHint)
    {
        var negativeBearings=0;
        foreach(var density in new[]{MaterialDensity.Standard,MaterialDensity.Compact})
        foreach(var scale in new[]{1f,1.5f})
        {
            using var host=new NativeTestContext(700,300,scale);
            var bitmaps=new Dictionary<ulong,MaterialTextBitmap>();
            using var renderer=new MaterialTextRenderer(bitmap=>
            {
                var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
                bitmaps.Add(texture.Id.Handle,bitmap);
                return new MaterialTextTexture(texture.Id,texture);
            });
            var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
            const string caption="ज़ोन का नाम या क्षेत्र ID खोजें";
            var value=showHint?"":caption;
            uint nativeId=0;
            Vector2 min=default,max=default,origin=default;
            ImDrawListPtr list=default;
            void Draw()
            {
                using var themeScope=MaterialTheme.Push(theme,scale);
                using var textScope=renderer.Push();
                ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,300));
                ImGui.Begin("Retained Hindi input bearing",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
                ImGui.SetWindowFontScale(16*scale/ImGui.GetFont().FontSize);
                using var height=MaterialText.PushLineHeight(caption);
                ImGui.SetNextItemWidth(520*scale);
                nativeId=ImGui.GetID("##original-search");
                origin=ImGui.GetCursorScreenPos()+new Vector2(ImGui.GetStyle().FramePadding.X,0);
                Assert.False(MaterialShapedInput.SingleLine("##original-search",caption,ref value,256));
                Assert.Equal(nativeId,ImGui.GetCurrentContext().LastItemData.ID);
                min=ImGui.GetItemRectMin();max=ImGui.GetItemRectMax();list=ImGui.GetWindowDrawList();
                ImGui.End();
            }
            host.Frame(Draw);host.Frame(Draw);
            var found=false;
            foreach(var commandIndex in Enumerable.Range(0,list.CmdBuffer.Size))
            {
                var command=list.CmdBuffer[commandIndex];
                if(command.ElemCount==0||!bitmaps.TryGetValue(command.TextureId.Handle,out var bitmap)) continue;
                found=true;
                var first=list.VtxBuffer[list.IdxBuffer[(int)command.IdxOffset]+(int)command.VtxOffset];
                var minInkX=int.MaxValue;
                for(var y=0;y<bitmap.Height;y++)
                    for(var x=0;x<bitmap.Width;x++)
                        if(bitmap.Rgba[(y*bitmap.Width+x)*4+3]>0) minInkX=Math.Min(minInkX,x);
                Assert.NotEqual(int.MaxValue,minInkX);
                var inkLeft=first.Pos.X+minInkX;
                if(inkLeft<origin.X) negativeBearings++;
                Assert.True(inkLeft>=command.ClipRect.X,"The native padding clipped leading Hindi ink.");
                Assert.True(command.ClipRect.X>=min.X&&command.ClipRect.Z<=max.X);
                Assert.InRange(first.Pos.X-bitmap.Offset.X,origin.X-.01f,origin.X+.01f);
            }
            Assert.True(found,"Retained native field emitted no shaped text.");
            Assert.Equal(showHint?"":caption,value);
        }
        Assert.True(negativeBearings>0,"The regression fixture did not exercise negative leading ink.");
    }

    [Theory]
    [InlineData("A😀क्षB")]
    [InlineData("A😀\uE000क्षB")]
    public void SupplementaryHindiSearchPreservesFocusCopyCutPasteDeleteAndNativeUndo(string original)
    {
        var pasteUndoSteps=NativePasteUndoSteps();
        using var host=new NativeTestContext(700,350);
        using var clipboard=new DiagnosticClipboard();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var value=original;
        var focus=new MaterialSearchState { FocusRequested=true };
        uint id=0;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme);
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Supplementary Hindi search",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.PushID("retained-search");id=ImGui.GetID("##input");ImGui.PopID();
            MaterialTextField.SearchField("retained-search",ref value,hint:"खोजें",width:350,state:focus);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        Assert.Equal(original,value);
        Assert.Equal(id,ImGui.GetCurrentContext().ActiveId);
        Key(ImGuiKey.Home);Key(ImGuiKey.RightArrow);Key(ImGuiKey.RightArrow);
        Key(ImGuiKey.Backspace);
        Assert.Equal(original.Replace("😀","",StringComparison.Ordinal),value);
        Chord(ImGuiKey.Z);
        Assert.Equal(original,value);
        Chord(ImGuiKey.A);Chord(ImGuiKey.C);
        Assert.Equal(original,clipboard.Text);
        Chord(ImGuiKey.X);
        Assert.Equal("",value);
        Assert.Equal(original,clipboard.Text);
        Chord(ImGuiKey.Z);
        Assert.Equal(original,value);
        const string pasted="😀क्षेत्र😀";
        clipboard.Set(pasted);
        Chord(ImGuiKey.A);Chord(ImGuiKey.V);
        Assert.Equal(pasted,value);
        Chord(ImGuiKey.A);Chord(ImGuiKey.C);
        Assert.Equal(pasted,clipboard.Text);
        for(var undo=0;undo<pasteUndoSteps;undo++) Chord(ImGuiKey.Z);
        Assert.Equal(original,value);

        void Key(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw);
        }
        void Chord(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,true);Key(key);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw);
        }
    }

    [Fact]
    public void ReadOnlyShapedFieldRetainsTextAndNativeCopyWithoutAcceptingEdits()
    {
        using var host=new NativeTestContext(700,350);
        using var clipboard=new DiagnosticClipboard();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        const string original="A😀क्षेत्रB";
        var value=original;
        var focus=true;
        uint id=0;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme);
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Read-only shaped field",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.PushID("readonly");id=ImGui.GetID("##input");ImGui.PopID();
            if(focus) { ImGui.SetKeyboardFocusHere();focus=false; }
            MaterialTextField.TextField("readonly",ref value,width:350,flags:ImGuiInputTextFlags.ReadOnly);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        Assert.Equal(id,ImGui.GetCurrentContext().ActiveId);
        Assert.Equal(original,value);
        Key(ImGuiKey.Backspace);Key(ImGuiKey.Delete);
        ImGui.GetIO().AddInputCharacter('x');host.Frame(Draw);
        clipboard.Set("अनुवाद");Chord(ImGuiKey.A);Chord(ImGuiKey.V);
        Assert.Equal(original,value);
        Chord(ImGuiKey.C);
        Assert.Equal(original,clipboard.Text);

        void Key(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw);
        }
        void Chord(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,true);Key(key);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw);
        }
    }

    [Theory]
    [InlineData(MaterialDensity.Standard,1f)]
    [InlineData(MaterialDensity.Compact,1f)]
    [InlineData(MaterialDensity.Standard,1.5f)]
    [InlineData(MaterialDensity.Compact,1.5f)]
    public void HindiTreeButtonsAndMixedTabsReserveNaturalHeightWithoutChangingExplicitSizesOrIds(MaterialDensity density,float scale)
    {
        using var host=new NativeTestContext(900,650,scale);
        var bitmaps=new Dictionary<ulong,MaterialTextBitmap>();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            bitmaps.Add(texture.Id.Handle,bitmap);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        var checkedValue=false;var radio=0;
        float natural=0,automatic=0,explicitHeight=0,english=0,regular=0,treeHeight=0,checkHeight=0,radioHeight=0;
        float englishTab=0,hindiTab=0,hindiWidth=0;
        uint treeId=0,treeExpected=0;
        Vector2 treeCenter=default;
        var open=false;
        ImDrawListPtr list=default;
        int treeFirst=0,treeLast=0;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(900,650));
            ImGui.Begin("Hindi control heights",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.SetWindowFontScale(32/ImGui.GetFont().FontSize);
            using var controls=MaterialControls.Push();
            natural=MaterialText.Measure("हिन्दी").Y;
            regular=MaterialControls.Metrics.Height;
            MaterialButton.Draw("english","English");english=ImGui.GetItemRectSize().Y;
            MaterialButton.Draw("automatic","हिन्दी");automatic=ImGui.GetItemRectSize().Y;
            MaterialButton.Draw("explicit","हिन्दी",size:new(130,10));explicitHeight=ImGui.GetItemRectSize().Y;
            MaterialSelection.Checkbox("checked","हिन्दी",ref checkedValue);checkHeight=ImGui.GetItemRectSize().Y;
            MaterialSelection.Radio("radio","हिन्दी",1,ref radio);radioHeight=ImGui.GetItemRectSize().Y;
            list=ImGui.GetWindowDrawList();treeFirst=list.VtxBuffer.Size;
            var afterId=ImGui.GetID("after-tree");
            ImGui.PushID("retained-tree");treeExpected=ImGui.GetID("##node");ImGui.PopID();
            using(var tree=MaterialTree.Node("retained-tree","हिन्दी"))
            {
                open=tree.Visible;treeId=ImGuiP.GetItemID();
                treeHeight=ImGui.GetItemRectSize().Y;
                treeCenter=ImGui.GetItemRectMin()+new Vector2(ImGui.GetFontSize()*.3f,treeHeight*.5f);
            }
            treeLast=list.VtxBuffer.Size;
            Assert.Equal(afterId,ImGui.GetID("after-tree"));
            var padding=ImGui.GetStyle().FramePadding;
            using(var tabs=MaterialTabs.Begin("retained-tabs",new[]{"English","हिन्दी###retained-hindi"}))
            {
                if(tabs.Visible)
                {
                    using(var tab=MaterialTabs.Item("English")) englishTab=ImGui.GetItemRectSize().Y;
                    ImGui.SetNextItemWidth(140*scale);
                    using(var tab=MaterialTabs.Item("हिन्दी###retained-hindi"))
                    { hindiTab=ImGui.GetItemRectSize().Y;hindiWidth=ImGui.GetItemRectSize().X; }
                }
            }
            Assert.Equal(padding,ImGui.GetStyle().FramePadding);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Frame(Draw);
        Assert.Equal(regular,english);
        Assert.True(automatic>=natural);
        Assert.InRange(explicitHeight,10*scale-.01f,10*scale+.01f);
        Assert.True(checkHeight>=natural&&radioHeight>=natural&&treeHeight>=natural);
        Assert.Equal(treeExpected,treeId);
        Assert.False(open);
        Assert.True(englishTab>=natural&&hindiTab>=natural);
        Assert.InRange(hindiTab,englishTab-.01f,englishTab+.01f);
        Assert.InRange(hindiWidth,140*scale-.01f,140*scale+.01f);
        var nativeArrow=false;var shapedTree=false;
        for(var index=treeFirst;index<treeLast;index++)
            nativeArrow|=(list.VtxBuffer[index].Col>>24)!=0 && list.VtxBuffer[index].Pos.X<treeCenter.X+ImGui.GetFontSize()*.7f;
        for(var index=0;index<list.CmdBuffer.Size;index++)
        {
            var command=list.CmdBuffer[index];
            if(command.ElemCount==0||!bitmaps.TryGetValue(command.TextureId.Handle,out var bitmap))continue;
            var vertexIndex=list.IdxBuffer[(int)command.IdxOffset]+(int)command.VtxOffset;
            if(vertexIndex<treeFirst||vertexIndex>=treeLast)continue;
            shapedTree=true;
            var min=list.VtxBuffer[vertexIndex].Pos;
            for(var y=0;y<bitmap.Height;y++)for(var x=0;x<bitmap.Width;x++)
                if(bitmap.Rgba[(y*bitmap.Width+x)*4+3]!=0)
                {
                    Assert.InRange(min.X+x,command.ClipRect.X-.51f,command.ClipRect.Z+.51f);
                    Assert.InRange(min.Y+y,command.ClipRect.Y-.51f,command.ClipRect.W+.51f);
                }
        }
        Assert.True(nativeArrow&&shapedTree);
        host.Click(treeCenter,Draw);host.Frame(Draw);
        Assert.True(open);
    }

    [Fact]
    public void HindiTreeAndTabUploadFailuresUnwindNativeClipTreeAndIdScopes()
    {
        using var host=new NativeTestContext(700,350);
        using var renderer=new MaterialTextRenderer(_=>throw new InvalidOperationException("Selected upload failure."));
        Exception? error=null;
        var tabFailed=false;
        void Draw()
        {
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Hindi scope exceptions",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            try
            {
            var window=ImGuiP.GetCurrentWindow();
            var expected=ImGui.GetID("after-tree");var depth=window.DC.TreeDepth;var clips=window.DrawList.ClipRectStack.Size;
            ImGui.SetNextItemOpen(true);
            var failed=false;
            try {using var tree=MaterialTree.Node("fault-tree","हिन्दी");}
            catch(InvalidOperationException){failed=true;}
            Assert.True(failed,"Tree upload did not throw.");Assert.Equal(depth,window.DC.TreeDepth);Assert.Equal(clips,window.DrawList.ClipRectStack.Size);
            Assert.Equal(expected,ImGui.GetID("after-tree"));
            using(var tabs=MaterialTabs.Begin("fault-tabs",new[]{"हिन्दी"}))
            {
                Assert.True(tabs.Visible);
                expected=ImGui.GetID("after-tab");clips=window.DrawList.ClipRectStack.Size;
                failed=false;
                try {using var tab=MaterialTabs.Item("हिन्दी",ImGuiTabItemFlags.SetSelected);}
                catch(InvalidOperationException){failed=true;}
                tabFailed|=failed;
                Assert.Equal(clips,window.DrawList.ClipRectStack.Size);Assert.Equal(expected,ImGui.GetID("after-tab"));
            }
            }
            catch(Exception failure){error=failure;}
            finally {ImGui.End();}
        }
        host.Frame(Draw);if(error is not null)throw error;
        host.Frame(Draw);if(error is not null)throw error;
        Assert.True(tabFailed,"A settled native tab did not exercise the upload failure.");
    }

    [Theory]
    [InlineData(MaterialDensity.Standard,1f)]
    [InlineData(MaterialDensity.Compact,1.5f)]
    public void HindiMultilineUsesShapedRowsForNavigationMouseBothScrollAxesAndIme(MaterialDensity density,float scale)
    {
        using var host=new NativeTestContext(750,450,scale);
        ImGui.GetIO().ConfigInputTextCursorBlink=false;
        using var clipboard=new DiagnosticClipboard();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new MaterialTextTexture(texture.Id,texture);
        });
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        var value="क्षि x\nकर्म yz\nक्षेत्र\n"+string.Concat(Enumerable.Repeat("हिन्दी\n",18))+string.Concat(Enumerable.Repeat("क्षेत्र ",30));
        var original=value;
        var focus=true;
        uint id=0;
        float fontSize=0;
        Vector2 origin=default;
        ImGuiWindowPtr child=default;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(750,450));
            ImGui.Begin("Hindi multiline geometry",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.SetWindowFontScale(32/ImGui.GetFont().FontSize);
            using var controls=MaterialControls.Push();
            using var border=new MaterialStyleScope();border.Style(ImGuiStyleVar.FrameBorderSize,0);
            fontSize=ImGui.GetFontSize();
            origin=ImGui.GetCursorScreenPos()+ImGui.GetStyle().FramePadding;
            id=ImGui.GetID("##retained-editor");
            if(focus){ImGui.SetKeyboardFocusHere();focus=false;}
            MaterialShapedInput.Multiline("##retained-editor",ref value,8192,new(260*scale,165*scale));
            var windows=ImGui.GetCurrentContext().Windows;
            for(var index=0;index<windows.Size;index++)
            {
                var candidate=new ImGuiWindowPtr(windows.Data[index]);
                if(candidate.ChildId==id)child=candidate;
            }
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Frame(Draw);
        var state=ImGuiP.GetInputTextState(id);
        Assert.Equal(id,ImGui.GetCurrentContext().ActiveId);
        Chord(ImGuiKey.Home);Key(ImGuiKey.RightArrow);Assert.Equal(4,state.Stb.Cursor);
        Key(ImGuiKey.DownArrow);Assert.InRange(state.Stb.Cursor,7,14);
        Key(ImGuiKey.UpArrow);Assert.Equal(4,state.Stb.Cursor);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.ModShift,true);Key(ImGuiKey.DownArrow);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.ModShift,false);host.Frame(Draw);
        Assert.Equal(4,state.Stb.SelectStart);Assert.True(state.Stb.SelectEnd>7);
        Chord(ImGuiKey.C);Assert.Equal(value[4..state.Stb.SelectEnd],clipboard.Text);
        Key(ImGuiKey.End);var end=state.Stb.Cursor;Key(ImGuiKey.Enter);
        Assert.Equal(original.Insert(end,"\n"),value);Chord(ImGuiKey.Z);Assert.Equal(original,value);
        Chord(ImGuiKey.Home);
        var layout=renderer.GetLayout(value,fontSize);
        var third=layout.HitTestPosition(15);
        var next=layout.HitTestPosition(19);
        ImGui.GetIO().DeltaTime=.4f;host.Frame(Draw);ImGui.GetIO().DeltaTime=1f/60;
        host.Click(origin+new Vector2(third.X+(next.X-third.X)*.8f,third.Y+layout.Baseline*.5f),Draw);
        Assert.Equal(19,state.Stb.Cursor);
        Chord(ImGuiKey.End);
        Assert.True(child.Scroll.Y>0&&state.ScrollX>0);
        var ime=ImGui.GetCurrentContext().PlatformImeData;
        Assert.InRange(ime.InputPos.X,child.InnerClipRect.Min.X-1,child.InnerClipRect.Max.X+1);
        Assert.InRange(ime.InputPos.Y,child.InnerClipRect.Min.Y-1,child.InnerClipRect.Max.Y+1);
        Assert.InRange(ime.InputLineHeight,renderer.GetLayout("हिन्दी",fontSize).Size.Y-.1f,renderer.GetLayout("हिन्दी",fontSize).Size.Y+.1f);
        Assert.True(ime.InputLineHeight<layout.Size.Y/10);
        Assert.True(child.ScrollMax.Y>layout.Size.Y/2);
        Chord(ImGuiKey.Home);Assert.InRange(child.Scroll.Y,0,.01f);Assert.InRange(state.ScrollX,0,.01f);
        Assert.Equal(original,value);

        void Key(ImGuiKey key)
        { ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw); }
        void Chord(ImGuiKey key)
        { ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,true);Key(key);ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw); }
    }

    [Fact]
    public void HindiMultilineRetainsCrLfSupplementaryPrivateUseClipboardAndNativeUndo()
    {
        var undoSteps=NativePasteUndoSteps();
        using var host=new NativeTestContext(700,350);
        using var clipboard=new DiagnosticClipboard();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);return new MaterialTextTexture(texture.Id,texture);});
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        const string original="A😀\uE000क्षि\r\nकर्म\nopaque https://example.test/a?x=1#fragment\rlone CR क्ष";
        const string pasted="😀क्षेत्र\r\n\uE000\r\nकर्म";
        var value=original;var focus=true;var readOnly=false;
        uint id=0;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme);using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Hindi multiline source",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            id=ImGui.GetID("##source");
            if(focus){ImGui.SetKeyboardFocusHere();focus=false;}
            MaterialShapedInput.Multiline("##source",ref value,4096,new(380,130),readOnly?ImGuiInputTextFlags.ReadOnly:ImGuiInputTextFlags.None);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        Assert.Equal(original,value);Assert.Equal(id,ImGui.GetCurrentContext().ActiveId);
        Chord(ImGuiKey.A);Chord(ImGuiKey.C);Assert.Equal(original,clipboard.Text);
        clipboard.Set(pasted);Chord(ImGuiKey.V);Assert.Equal(pasted,value);
        Chord(ImGuiKey.A);Chord(ImGuiKey.C);Assert.Equal(pasted,clipboard.Text);
        for(var index=0;index<undoSteps;index++)Chord(ImGuiKey.Z);Assert.Equal(original,value);
        Chord(ImGuiKey.A);Chord(ImGuiKey.X);Assert.Equal("",value);Assert.Equal(original,clipboard.Text);
        Chord(ImGuiKey.Z);Assert.Equal(original,value);
        readOnly=true;host.Frame(Draw);Chord(ImGuiKey.A);Key(ImGuiKey.Delete);ImGui.GetIO().AddInputCharacter('x');host.Frame(Draw);
        Assert.Equal(original,value);Chord(ImGuiKey.C);Assert.Equal(original,clipboard.Text);

        void Key(ImGuiKey key)
        {ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw);}
        void Chord(ImGuiKey key)
        {ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,true);Key(key);ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw);}
    }

    [Fact]
    public void HindiMultilineRetainsNativeSelectAllWordLineClicksDragAndWheelScrolling()
    {
        using var host=new NativeTestContext(700,350);
        using var clipboard=new DiagnosticClipboard();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);return new MaterialTextTexture(texture.Id,texture);});
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var value="हिन्दी first\nक्षेत्र second\nकर्म third\n"+string.Concat(Enumerable.Repeat("हिन्दी row\n",25));
        var original=value;var flags=ImGuiInputTextFlags.AutoSelectAll;
        uint id=0;float fontSize=0;Vector2 origin=default;
        ImGuiWindowPtr child=default;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme);using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,350));
            ImGui.Begin("Hindi multiline pointer",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.SetWindowFontScale(32/ImGui.GetFont().FontSize);
            using var controls=MaterialControls.Push();
            fontSize=ImGui.GetFontSize();origin=ImGui.GetCursorScreenPos()+ImGui.GetStyle().FramePadding;
            id=ImGui.GetID("##pointer");
            MaterialShapedInput.Multiline("##pointer",ref value,8192,new(430,150),flags);
            var windows=ImGui.GetCurrentContext().Windows;
            for(var index=0;index<windows.Size;index++)
            {var candidate=new ImGuiWindowPtr(windows.Data[index]);if(candidate.ChildId==id)child=candidate;}
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        var layout=renderer.GetLayout(value,fontSize);
        host.Click(origin+new Vector2(20,layout.Baseline*.5f),Draw);
        var state=ImGuiP.GetInputTextState(id);
        // ImGui 1.88 ignores AutoSelectAll for multiline. Ctrl+A remains native.
        Assert.Equal(state.Stb.Cursor,state.Stb.SelectStart);Assert.Equal(state.Stb.Cursor,state.Stb.SelectEnd);
        Chord(ImGuiKey.A);
        Assert.Equal(0,Math.Min(state.Stb.SelectStart,state.Stb.SelectEnd));
        Assert.Equal(value.Length,Math.Max(state.Stb.SelectStart,state.Stb.SelectEnd));
        flags=ImGuiInputTextFlags.None;Chord(ImGuiKey.Home);
        var secondStart=value.IndexOf("क्षेत्र",StringComparison.Ordinal);
        var row=layout.HitTestPosition(secondStart);
        var point=origin+row+new Vector2(15,layout.Baseline*.5f);
        ResetClick();host.Click(point,Draw);host.Click(point,Draw);
        Chord(ImGuiKey.C);Assert.Equal("क्षेत्र",clipboard.Text);
        host.Click(point,Draw);Chord(ImGuiKey.C);Assert.Equal("क्षेत्र second\n",clipboard.Text);
        Chord(ImGuiKey.Home);ResetClick();
        var thirdStart=value.IndexOf("कर्म",StringComparison.Ordinal);
        var third=layout.HitTestPosition(thirdStart);
        var io=ImGui.GetIO();
        io.AddMousePosEvent(origin.X+2,origin.Y+layout.Baseline*.5f);host.Frame(Draw);
        io.AddMouseButtonEvent(0,true);host.Frame(Draw);
        io.AddMousePosEvent(origin.X+third.X+1,origin.Y+third.Y+layout.Baseline*.5f);host.Frame(Draw);
        io.AddMouseButtonEvent(0,false);host.Frame(Draw);
        Assert.Equal(0,state.Stb.SelectStart);Assert.Equal(thirdStart,state.Stb.SelectEnd);
        Chord(ImGuiKey.Home);
        io.AddMousePosEvent(origin.X+50,origin.Y+60);host.Frame(Draw);
        io.AddMouseWheelEvent(0,-2);host.Frame(Draw);host.Frame(Draw);
        Assert.True(child.Scroll.Y>0);Assert.Equal(0,state.Stb.Cursor);
        var scroll=child.Scroll.Y;
        Chord(ImGuiKey.DownArrow);Assert.True(child.Scroll.Y>scroll);Assert.Equal(0,state.Stb.Cursor);
        Assert.Equal(original,value);

        void ResetClick(){ImGui.GetIO().DeltaTime=.4f;host.Frame(Draw);ImGui.GetIO().DeltaTime=1f/60;}
        void Key(ImGuiKey key)
        {ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw);}
        void Chord(ImGuiKey key)
        {ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,true);Key(key);ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw);}
    }

    [Fact]
    public void HindiMultilineCapacityCountsOriginalSupplementaryUtf8AfterHindiIsRemoved()
    {
        using var host=new NativeTestContext(500,240);using var clipboard=new DiagnosticClipboard();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);return new MaterialTextTexture(texture.Id,texture);});
        var value="क";var focus=true;
        void Draw()
        {
            using var textScope=renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(500,240));
            ImGui.Begin("Hindi multiline capacity",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            if(focus){ImGui.SetKeyboardFocusHere();focus=false;}
            MaterialShapedInput.Multiline("##capacity",ref value,7,new(300,100));
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);clipboard.Set("😀😀");Chord(ImGuiKey.A);Chord(ImGuiKey.V);
        Assert.Equal("😀",value);ImGui.GetIO().AddInputCharacter('x');host.Frame(Draw);Assert.Equal("😀x",value);
        Chord(ImGuiKey.A);Chord(ImGuiKey.C);Assert.Equal(value,clipboard.Text);

        void Chord(ImGuiKey key)
        {
            var io=ImGui.GetIO();io.AddKeyEvent(ImGuiKey.ModCtrl,true);io.AddKeyEvent(key,true);host.Frame(Draw);
            io.AddKeyEvent(key,false);host.Frame(Draw);io.AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw);
        }
    }

    [UnmanagedCallersOnly(CallConvs=[typeof(CallConvCdecl)])]
    private static byte* GetDiagnosticClipboard(void* userData) => clipboardText;

    [UnmanagedCallersOnly(CallConvs=[typeof(CallConvCdecl)])]
    private static void SetDiagnosticClipboard(void* userData,byte* text)
        => StoreClipboard(Marshal.PtrToStringUTF8((nint)text) ?? "");

    private static void StoreClipboard(string text)
    {
        var bytes=Encoding.UTF8.GetBytes(text+'\0');
        NativeMemory.Free(clipboardText);
        clipboardText=(byte*)NativeMemory.Alloc((nuint)bytes.Length);
        bytes.CopyTo(new Span<byte>(clipboardText,bytes.Length));
    }

    private static int NativePasteUndoSteps()
    {
        using var host=new NativeTestContext(500,200);
        using var clipboard=new DiagnosticClipboard();
        var value="old";
        var focus=true;
        void Draw()
        {
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(500,200));
            ImGui.Begin("Native paste witness",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            if(focus) { ImGui.SetKeyboardFocusHere();focus=false; }
            ImGui.InputText("vanilla",ref value,128);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        clipboard.Set("new");
        Chord(ImGuiKey.A);Chord(ImGuiKey.V);
        Assert.Equal("new",value);
        Chord(ImGuiKey.Z);
        Assert.Equal("",value);
        Chord(ImGuiKey.Z);
        Assert.Equal("old",value);
        return 2;

        void Chord(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,true);
            ImGui.GetIO().AddKeyEvent(key,true);host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(key,false);host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl,false);host.Frame(Draw);
        }
    }

    private sealed class DiagnosticClipboard : IDisposable
    {
        private readonly void* previousGet;
        private readonly void* previousSet;
        private readonly void* previousData;
        public DiagnosticClipboard()
        {
            var io=ImGui.GetIO();
            previousGet=io.GetClipboardTextFn;
            previousSet=io.SetClipboardTextFn;
            previousData=io.ClipboardUserData;
            io.GetClipboardTextFn=(void*)(delegate* unmanaged[Cdecl]<void*,byte*>)&GetDiagnosticClipboard;
            io.SetClipboardTextFn=(void*)(delegate* unmanaged[Cdecl]<void*,byte*,void>)&SetDiagnosticClipboard;
            io.ClipboardUserData=null;
            StoreClipboard("");
        }
        public void Set(string text)=>StoreClipboard(text);
        public string Text=>Marshal.PtrToStringUTF8((nint)clipboardText) ?? "";
        public void Dispose()
        {
            var io=ImGui.GetIO();
            io.GetClipboardTextFn=previousGet;
            io.SetClipboardTextFn=previousSet;
            io.ClipboardUserData=previousData;
            NativeMemory.Free(clipboardText);
            clipboardText=null;
        }
    }
}

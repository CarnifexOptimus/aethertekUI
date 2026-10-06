using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AethertekUI;

internal static unsafe class WindowsTextInterop
{
    internal const int MaximumTextLength = 65_536;
    internal const float MaximumLayoutDimension = 65_536;
    private const int MaximumNativeItems = 1_048_576;
    private const int MaximumRasterDimension = 16_384;
    private const int MaximumRasterPixels = 16_777_216;
    private const int InsufficientBuffer = unchecked((int)0x8007007A);
    private const int Fail = unchecked((int)0x80004005);
    private const int NoInterface = unchecked((int)0x80004002);
    private const int PointerError = unchecked((int)0x80004003);
    private const int NotImplemented = unchecked((int)0x80004001);
    private static readonly Guid Factory2Id = new("0439fc60-ca44-4994-8dee-3a9af7b732ec");
    private static readonly Guid UnknownId = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid PixelSnappingId = new("eaf3a2da-ecf4-4d24-b644-b34f6842024b");
    private static readonly Guid RendererId = new("ef8a8135-5cc6-45fe-8825-c5a0724eb819");

    [DllImport("dwrite.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int DWriteCreateFactory(int factoryType, in Guid iid, out nint factory);

    internal static nint CreateFactory()
    {
        Check(DWriteCreateFactory(0, in Factory2Id, out var result));
        return result;
    }

    internal static nint CreateLayout(nint factory, string text, float size,
        float maximumWidth, string family, string locale)
    {
        nint fonts = 0, format = 0, result = 0;
        try
        {
            Check(((delegate* unmanaged[Stdcall]<nint, nint*, int, int>)Slot(factory, 3))(
                factory, &fonts, 0));
            uint familyIndex = 0;
            int familyExists = 0;
            fixed (char* familyText = family)
            {
                Check(((delegate* unmanaged[Stdcall]<nint, char*, uint*, int*, int>)Slot(fonts, 5))(
                    fonts, familyText, &familyIndex, &familyExists));
                if (familyExists == 0)
                    throw new InvalidOperationException("The requested Windows text font family is not installed.");
                fixed (char* localeText = locale)
                    Check(((delegate* unmanaged[Stdcall]<nint, char*, nint, int, int, int,
                        float, char*, nint*, int>)Slot(factory, 15))(
                            factory, familyText, fonts, 400, 0, 5, size, localeText, &format));
            }

            var wrap = !float.IsPositiveInfinity(maximumWidth);
            fixed (char* input = text)
                Check(((delegate* unmanaged[Stdcall]<nint, char*, uint, nint, float,
                    float, nint*, int>)Slot(factory, 18))(
                        factory, input, (uint)text.Length, format,
                        wrap ? maximumWidth : MaximumLayoutDimension, MaximumLayoutDimension, &result));
            Check(((delegate* unmanaged[Stdcall]<nint, int, int>)Slot(result, 5))(result, wrap ? 0 : 1));
            return result;
        }
        catch
        {
            Release(ref result);
            throw;
        }
        finally
        {
            Release(ref format);
            Release(ref fonts);
        }
    }

    internal static Vector2 GetSize(nint layout)
    {
        TextMetrics metrics;
        Check(((delegate* unmanaged[Stdcall]<nint, TextMetrics*, int>)Slot(layout, 60))(layout, &metrics));
        if (!float.IsFinite(metrics.WidthIncludingTrailingWhitespace)
            || !float.IsFinite(metrics.Height) || metrics.WidthIncludingTrailingWhitespace < 0 || metrics.Height < 0)
            throw new InvalidOperationException("Windows returned invalid text measurements.");
        return new(metrics.WidthIncludingTrailingWhitespace, metrics.Height);
    }

    internal static float GetBaseline(nint layout, int textLength)
    {
        uint count = 0;
        var function = (delegate* unmanaged[Stdcall]<nint, LineMetrics*, uint, uint*, int>)Slot(layout, 59);
        CheckBufferQuery(function(layout, null, 0, &count), count);
        if (count > textLength + 1) throw new InvalidOperationException("Invalid native line count.");
        var lines = new LineMetrics[count];
        if (count == 0) return 0;
        fixed (LineMetrics* data = lines) Check(function(layout, data, count, &count));
        return lines[0].Baseline;
    }

    internal static MaterialTextCluster[] GetClusters(nint layout, int textLength)
    {
        uint count = 0;
        var function = (delegate* unmanaged[Stdcall]<nint, ClusterMetrics*, uint, uint*, int>)Slot(layout, 62);
        CheckBufferQuery(function(layout, null, 0, &count), count);
        if (count > textLength) throw new InvalidOperationException("Invalid native cluster count.");
        if (count == 0) return [];
        var native = new ClusterMetrics[count];
        fixed (ClusterMetrics* data = native) Check(function(layout, data, count, &count));
        var result = new MaterialTextCluster[count];
        var position = 0;
        for (var index = 0; index < result.Length; index++)
        {
            var cluster = native[index];
            if (cluster.Length == 0 || !float.IsFinite(cluster.Width) || cluster.Width < 0)
                throw new InvalidOperationException("Windows returned invalid cluster measurements.");
            result[index] = new(position, cluster.Length, cluster.Width, (cluster.Flags & 4) != 0);
            position = checked(position + cluster.Length);
        }
        if (position != textLength) throw new InvalidOperationException("Native clusters do not cover the input text.");
        return result;
    }

    internal static MaterialTextHit HitTestPoint(nint layout, Vector2 point)
    {
        int trailing = 0, inside = 0;
        HitMetrics hit;
        Check(((delegate* unmanaged[Stdcall]<nint, float, float, int*, int*, HitMetrics*, int>)Slot(layout, 64))(
            layout, point.X, point.Y, &trailing, &inside, &hit));
        return new(checked((int)hit.Position), checked((int)hit.Length), trailing != 0, inside != 0,
            new(hit.Left, hit.Top), new(hit.Left + hit.Width, hit.Top + hit.Height));
    }

    internal static Vector2 HitTestPosition(nint layout, int position, bool trailing)
    {
        float x = 0, y = 0;
        HitMetrics hit;
        Check(((delegate* unmanaged[Stdcall]<nint, uint, int, float*, float*, HitMetrics*, int>)Slot(layout, 65))(
            layout, (uint)position, trailing ? 1 : 0, &x, &y, &hit));
        return new(x, y);
    }

    internal static MaterialTextRectangle[] HitTestRange(nint layout, int position, int length)
    {
        if (length == 0) return [];
        uint count = 0;
        var function = (delegate* unmanaged[Stdcall]<nint, uint, uint, float, float,
            HitMetrics*, uint, uint*, int>)Slot(layout, 66);
        CheckBufferQuery(function(layout, (uint)position, (uint)length, 0, 0, null, 0, &count), count);
        if (count == 0) return [];
        var native = new HitMetrics[count];
        fixed (HitMetrics* data = native)
            Check(function(layout, (uint)position, (uint)length, 0, 0, data, count, &count));
        var result = new MaterialTextRectangle[count];
        for (var index = 0; index < result.Length; index++)
        {
            var hit = native[index];
            result[index] = new(new(hit.Left, hit.Top), new(hit.Left + hit.Width, hit.Top + hit.Height));
        }
        return result;
    }

    internal static MaterialTextBitmap Rasterize(nint factory, nint layout, bool validateOnly = false)
    {
        var state = new RendererState(factory, validateOnly);
        var handle = GCHandle.Alloc(state);
        NativeRenderer* renderer = null;
        try
        {
            renderer = (NativeRenderer*)NativeMemory.AllocZeroed(
                (nuint)(sizeof(NativeRenderer) + 10 * sizeof(nint)));
            if (renderer == null) throw new OutOfMemoryException();
            InitializeRendererVtable(renderer);
            renderer->Context = GCHandle.ToIntPtr(handle);
            renderer->References = 1;
            var result = ((delegate* unmanaged[Stdcall]<nint, nint, nint, float, float, int>)Slot(layout, 58))(
                layout, 0, (nint)renderer, 0, 0);
            if (state.Failure is { } failure)
                throw new InvalidOperationException(validateOnly ? "Windows shaped glyph coverage failed." : "Windows text rasterization failed.", failure);
            Check(result);
            return state.BuildBitmap();
        }
        finally
        {
            if (renderer != null)
                ((delegate* unmanaged[Stdcall]<nint, uint>)Slot((nint)renderer, 2))((nint)renderer);
            else
                handle.Free();
        }
    }

    internal static void Release(ref nint value)
    {
        var pointer = Interlocked.Exchange(ref value, 0);
        if (pointer != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)Slot(pointer, 2))(pointer);
    }

    private static nint Slot(nint value, int index) => ((nint*)*(nint*)value)[index];

    private static void Check(int result)
    {
        if (result < 0) Marshal.ThrowExceptionForHR(result);
    }

    private static void CheckBufferQuery(int result, uint count)
    {
        if (result != InsufficientBuffer) Check(result);
        if (count > MaximumNativeItems) throw new InvalidOperationException("Native text buffer exceeds its bound.");
    }

    private static void InitializeRendererVtable(NativeRenderer* renderer)
    {
        var table = (nint*)(renderer + 1);
        renderer->Vtable = (nint)table;
        table[0] = (nint)(delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)&QueryInterface;
        table[1] = (nint)(delegate* unmanaged[Stdcall]<nint, uint>)&AddRef;
        table[2] = (nint)(delegate* unmanaged[Stdcall]<nint, uint>)&ReleaseRenderer;
        table[3] = (nint)(delegate* unmanaged[Stdcall]<nint, nint, int*, int>)&IsPixelSnappingDisabled;
        table[4] = (nint)(delegate* unmanaged[Stdcall]<nint, nint, Matrix*, int>)&GetCurrentTransform;
        table[5] = (nint)(delegate* unmanaged[Stdcall]<nint, nint, float*, int>)&GetPixelsPerDip;
        table[6] = (nint)(delegate* unmanaged[Stdcall]<nint, nint, float, float, int, GlyphRun*, nint, nint, int>)&DrawGlyphRun;
        table[7] = (nint)(delegate* unmanaged[Stdcall]<nint, nint, float, float, nint, nint, int>)&DrawDecoration;
        table[8] = table[7];
        table[9] = (nint)(delegate* unmanaged[Stdcall]<nint, nint, float, float, nint, int, int, nint, int>)&DrawInlineObject;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(nint self, Guid* iid, nint* result)
    {
        if (result == null || iid == null) return PointerError;
        *result = 0;
        if (*iid != UnknownId && *iid != PixelSnappingId && *iid != RendererId) return NoInterface;
        *result = self;
        Interlocked.Increment(ref ((NativeRenderer*)self)->References);
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(nint self) => (uint)Interlocked.Increment(ref ((NativeRenderer*)self)->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint ReleaseRenderer(nint self)
    {
        var renderer = (NativeRenderer*)self;
        var references = Interlocked.Decrement(ref renderer->References);
        if (references == 0)
        {
            GCHandle.FromIntPtr(renderer->Context).Free();
            NativeMemory.Free(renderer);
        }
        return (uint)references;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IsPixelSnappingDisabled(nint self, nint context, int* disabled)
    {
        if (disabled == null) return PointerError;
        *disabled = 1;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetCurrentTransform(nint self, nint context, Matrix* transform)
    {
        if (transform == null) return PointerError;
        *transform = new() { M11 = 1, M22 = 1 };
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetPixelsPerDip(nint self, nint context, float* scale)
    {
        if (scale == null) return PointerError;
        *scale = 1;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawGlyphRun(nint self, nint context, float baselineX, float baselineY,
        int measuringMode, GlyphRun* run, nint description, nint effect)
    {
        var state = (RendererState)GCHandle.FromIntPtr(((NativeRenderer*)self)->Context).Target!;
        try
        {
            state.DrawRun(run, baselineX, baselineY, measuringMode);
            return 0;
        }
        catch (Exception error)
        {
            state.Failure = error;
            return Fail;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawDecoration(nint self, nint context, float x, float y, nint decoration, nint effect)
        => NotImplemented;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawInlineObject(nint self, nint context, float x, float y, nint inlineObject,
        int sideways, int rightToLeft, nint effect) => NotImplemented;

    private sealed class RendererState(nint factory, bool validateOnly)
    {
        private readonly List<(NativeRectangle Bounds, byte[] Alpha)> runs = [];
        private int totalPixels;
        public Exception? Failure;

        public void DrawRun(GlyphRun* run, float x, float y, int measuringMode)
        {
            if (run == null || run->FontFace == 0 || run->GlyphCount > MaximumNativeItems)
                throw new InvalidOperationException("Invalid native glyph run.");
            if (validateOnly)
            {
                var glyphs = new ReadOnlySpan<ushort>((void*)run->GlyphIndices, checked((int)run->GlyphCount));
                var advances = new ReadOnlySpan<float>((void*)run->GlyphAdvances, checked((int)run->GlyphCount));
                for (var index = 0; index < glyphs.Length; index++)
                    if (glyphs[index] == 0 && advances[index] > 0)
                        throw new InvalidOperationException("A shaped run contains a missing visible glyph.");
                return;
            }
            nint analysis = 0;
            try
            {
                // Natural symmetric rendering, disabled grid fitting, grayscale antialiasing.
                Check(((delegate* unmanaged[Stdcall]<nint, GlyphRun*, nint, int, int, int,
                    int, float, float, nint*, int>)Slot(factory, 30))(
                        factory, run, 0, 5, measuringMode, 1, 1, x, y, &analysis));
                NativeRectangle bounds;
                Check(((delegate* unmanaged[Stdcall]<nint, int, NativeRectangle*, int>)Slot(analysis, 3))(
                    analysis, 0, &bounds));
                var count = GetPixelCount(bounds);
                if (count == 0) return;
                totalPixels = checked(totalPixels + count);
                if (totalPixels > MaximumRasterPixels)
                    throw new InvalidOperationException("Glyph coverage exceeds the raster memory bound.");
                var alpha = new byte[count];
                fixed (byte* data = alpha)
                    Check(((delegate* unmanaged[Stdcall]<nint, int, NativeRectangle*, byte*, uint, int>)Slot(analysis, 4))(
                        analysis, 0, &bounds, data, (uint)alpha.Length));
                runs.Add((bounds, alpha));
            }
            finally { Release(ref analysis); }
        }

        public MaterialTextBitmap BuildBitmap()
        {
            if (runs.Count == 0) return new(0, 0, Vector2.Zero, []);
            var union = runs[0].Bounds;
            foreach (var (bounds, _) in runs)
            {
                union.Left = Math.Min(union.Left, bounds.Left);
                union.Top = Math.Min(union.Top, bounds.Top);
                union.Right = Math.Max(union.Right, bounds.Right);
                union.Bottom = Math.Max(union.Bottom, bounds.Bottom);
            }
            var count = GetPixelCount(union);
            var width = union.Right - union.Left;
            var height = union.Bottom - union.Top;
            var rgba = new byte[checked(count * 4)];
            for (var pixel = 0; pixel < count; pixel++)
            {
                rgba[pixel * 4] = 255;
                rgba[pixel * 4 + 1] = 255;
                rgba[pixel * 4 + 2] = 255;
            }
            foreach (var (bounds, alpha) in runs)
            {
                var runWidth = bounds.Right - bounds.Left;
                var runHeight = bounds.Bottom - bounds.Top;
                for (var row = 0; row < runHeight; row++)
                    for (var column = 0; column < runWidth; column++)
                    {
                        var coverage = alpha[row * runWidth + column];
                        var target = ((bounds.Top - union.Top + row) * width
                            + bounds.Left - union.Left + column) * 4 + 3;
                        rgba[target] = (byte)(coverage + (rgba[target] * (255 - coverage) + 127) / 255);
                    }
            }
            return new(width, height, new(union.Left, union.Top), rgba);
        }

        private static int GetPixelCount(NativeRectangle bounds)
        {
            var width = (long)bounds.Right - bounds.Left;
            var height = (long)bounds.Bottom - bounds.Top;
            if (width < 0 || height < 0 || width > MaximumRasterDimension
                || height > MaximumRasterDimension)
                throw new InvalidOperationException("Windows text raster exceeds its dimension or memory bound.");
            var count = width * height;
            if (count > MaximumRasterPixels)
                throw new InvalidOperationException("Windows text raster exceeds its dimension or memory bound.");
            return checked((int)count);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRenderer { public nint Vtable, Context; public int References; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Matrix { public float M11, M12, M21, M22, Dx, Dy; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct GlyphRun
    {
        public nint FontFace;
        public float FontEmSize;
        public uint GlyphCount;
        public nint GlyphIndices, GlyphAdvances, GlyphOffsets;
        public int IsSideways;
        public uint BidiLevel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ClusterMetrics { public float Width; public ushort Length, Flags; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LineMetrics
    {
        public uint Length, TrailingWhitespaceLength, NewlineLength;
        public float Height, Baseline;
        public int IsTrimmed;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextMetrics
    {
        public float Left, Top, Width, WidthIncludingTrailingWhitespace, Height, LayoutWidth, LayoutHeight;
        public uint MaximumBidiReorderingDepth, LineCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HitMetrics
    {
        public uint Position, Length;
        public float Left, Top, Width, Height;
        public uint BidiLevel;
        public int IsText, IsTrimmed;
    }
}

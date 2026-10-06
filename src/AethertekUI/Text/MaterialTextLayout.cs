using System.Numerics;

namespace AethertekUI;

/// <summary>A shaped UTF-16 cluster and its advance in layout coordinates.</summary>
public readonly record struct MaterialTextCluster(int Position, int Length, float Width, bool IsNewline);

/// <summary>A native hit-test result identifying one complete shaped cluster.</summary>
public readonly record struct MaterialTextHit(
    int Position, int Length, bool Trailing, bool Inside, Vector2 Min, Vector2 Max);

/// <summary>A selection rectangle in layout coordinates.</summary>
public readonly record struct MaterialTextRectangle(Vector2 Min, Vector2 Max);

/// <summary>Top-down white RGBA pixels with grayscale coverage in alpha.</summary>
public sealed record MaterialTextBitmap(int Width, int Height, Vector2 Offset, byte[] Rgba);

/// <summary>
/// Windows-shaped text, cluster geometry and grayscale rasterization. Font size,
/// wrap width and returned coordinates use already-scaled UI pixels at one DIP per pixel.
/// The caller owns drawing, clipping, textures and native control identity.
/// Dispose only after callers have finished using this instance.
/// </summary>
public sealed class MaterialTextLayout : IDisposable
{
    private nint factory;
    private nint layout;

    public string Text { get; }
    public Vector2 Size { get; }
    public float Baseline { get; }
    public IReadOnlyList<MaterialTextCluster> Clusters { get; }

    public MaterialTextLayout(
        string text,
        float fontSize,
        float maximumWidth = float.PositiveInfinity,
        string fontFamily = "Nirmala UI",
        string locale = "hi-IN")
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        if (text.Length > WindowsTextInterop.MaximumTextLength)
            throw new ArgumentOutOfRangeException(nameof(text), "Text exceeds the shaped-layout bound.");
        if (!float.IsFinite(fontSize) || fontSize <= 0 || fontSize > 512)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be greater than zero and at most 512 pixels.");
        if (!float.IsPositiveInfinity(maximumWidth)
            && (!float.IsFinite(maximumWidth) || maximumWidth <= 0
                || maximumWidth > WindowsTextInterop.MaximumLayoutDimension))
            throw new ArgumentOutOfRangeException(nameof(maximumWidth), "Wrap width must be positive and within the layout bound.");

        Text = text;
        try
        {
            factory = WindowsTextInterop.CreateFactory();
            layout = WindowsTextInterop.CreateLayout(factory, text, fontSize, maximumWidth, fontFamily, locale);
            Size = WindowsTextInterop.GetSize(layout);
            Baseline = WindowsTextInterop.GetBaseline(layout, text.Length);
            Clusters = Array.AsReadOnly(WindowsTextInterop.GetClusters(layout, text.Length));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public MaterialTextHit HitTestPoint(Vector2 point)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            throw new ArgumentOutOfRangeException(nameof(point));
        try { return WindowsTextInterop.HitTestPoint(layout, point); }
        finally { GC.KeepAlive(this); }
    }

    public Vector2 HitTestPosition(int position, bool trailing = false)
    {
        ThrowIfDisposed();
        ValidatePosition(position);
        try { return WindowsTextInterop.HitTestPosition(layout, position, trailing); }
        finally { GC.KeepAlive(this); }
    }

    public IReadOnlyList<MaterialTextRectangle> HitTestRange(int position, int length)
    {
        ThrowIfDisposed();
        ValidatePosition(position);
        if (length < 0 || length > Text.Length - position)
            throw new ArgumentOutOfRangeException(nameof(length));
        try { return Array.AsReadOnly(WindowsTextInterop.HitTestRange(layout, position, length)); }
        finally { GC.KeepAlive(this); }
    }

    public MaterialTextBitmap Rasterize()
    {
        ThrowIfDisposed();
        try { return WindowsTextInterop.Rasterize(factory, layout); }
        finally { GC.KeepAlive(this); }
    }

    /// <summary>Validates actual shaped glyph runs, including Windows fallback, without allocating or uploading a bitmap.</summary>
    public void CheckGlyphs()
    {
        ThrowIfDisposed();
        try { WindowsTextInterop.Rasterize(factory, layout, validateOnly: true); }
        finally { GC.KeepAlive(this); }
    }

    public void Dispose()
    {
        WindowsTextInterop.Release(ref layout);
        WindowsTextInterop.Release(ref factory);
        GC.SuppressFinalize(this);
    }

    ~MaterialTextLayout()
    {
        WindowsTextInterop.Release(ref layout);
        WindowsTextInterop.Release(ref factory);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(layout == 0, this);

    private void ValidatePosition(int position)
    {
        if (position < 0 || position > Text.Length)
            throw new ArgumentOutOfRangeException(nameof(position));
    }
}

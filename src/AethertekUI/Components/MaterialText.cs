using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>A host-created texture. The renderer owns and disposes its supplied resource.</summary>
public sealed class MaterialTextTexture : IDisposable
{
    private IDisposable? owner;
    public ImTextureID Id { get; }
    public MaterialTextTexture(ImTextureID id, IDisposable owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (id.Handle == 0) throw new ArgumentException("A text texture requires a valid handle.", nameof(id));
        Id = id;
        this.owner = owner;
    }
    public void Dispose() => Interlocked.Exchange(ref owner, null)?.Dispose();
}

/// <summary>Owns Windows-shaped layouts and host textures; enter a scope around the owning window tree.</summary>
public sealed unsafe class MaterialTextRenderer : IDisposable
{
    [ThreadStatic] private static Scope? currentScope;
    private readonly Func<MaterialTextBitmap, MaterialTextTexture> upload;
    private readonly Dictionary<Key, Entry> entries = [];
    private nint context;
    private int activeScopes;
    private bool disposed;
    public string FontFamily { get; }
    public string Locale { get; }
    internal static MaterialTextRenderer? Current => currentScope?.Renderer;

    public MaterialTextRenderer(Func<MaterialTextBitmap, MaterialTextTexture> upload,
        string fontFamily = "Nirmala UI", string locale = "hi-IN")
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        this.upload = upload;
        FontFamily = fontFamily;
        Locale = locale;
    }

    public Scope Push()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var nativeContext = (nint)ImGui.GetCurrentContext().Handle;
        if (nativeContext == 0) throw new InvalidOperationException("Enter a text scope in its ImGui context.");
        if (context != 0 && context != nativeContext)
            throw new InvalidOperationException("A text renderer belongs to one ImGui context.");
        context = nativeContext;
        var result = new Scope(this, currentScope, nativeContext);
        currentScope = result;
        activeScopes++;
        return result;
    }

    /// <summary>Borrowed renderer-owned layout. Do not dispose it or retain it beyond the current frame.</summary>
    public MaterialTextLayout GetLayout(string text, float fontSize, float wrapWidth = 0)
        => GetEntry(text, fontSize, wrapWidth).Layout;

    /// <summary>Validates authored Devanagari separately from a consumer's retained native atlas checks.</summary>
    public void CheckGlyphs(IEnumerable<string> text, float fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        ObjectDisposedException.ThrowIf(disposed, this);
        foreach (var value in text.Where(MaterialText.RequiresShaping).Distinct())
        {
            using var layout = new MaterialTextLayout(value, fontSize, fontFamily: FontFamily, locale: Locale);
            layout.CheckGlyphs();
        }
    }

    internal void Draw(ImDrawListPtr drawList, Vector2 position, uint color, string text, float fontSize, float wrapWidth = 0)
    {
        if (text.Length == 0 || (color >> 24) == 0) return;
        var entry = GetEntry(text, fontSize, wrapWidth);
        if (entry.Texture is null)
        {
            var bitmap = entry.Layout.Rasterize();
            if (bitmap.Width == 0 || bitmap.Height == 0) return;
            entry.Texture = upload(bitmap) ?? throw new InvalidOperationException("The text uploader returned no texture.");
            entry.Offset = bitmap.Offset;
            entry.RasterSize = new(bitmap.Width, bitmap.Height);
        }
        var start = position + entry.Offset;
        drawList.AddImage(entry.Texture.Id, start, start + entry.RasterSize,
            Vector2.Zero, Vector2.One, color);
    }

    private Entry GetEntry(string text, float fontSize, float wrapWidth)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(text);
        if (!float.IsFinite(fontSize) || fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
        if (!float.IsFinite(wrapWidth) || wrapWidth < 0) throw new ArgumentOutOfRangeException(nameof(wrapWidth));
        var nativeContext = ImGui.GetCurrentContext();
        if (context != 0 && (nint)nativeContext.Handle != context)
            throw new InvalidOperationException("Use text resources in their original ImGui context.");
        var frame = nativeContext.Handle == null ? 0 : ImGui.GetFrameCount();
        var key = new Key(text, fontSize, wrapWidth);
        if (!entries.TryGetValue(key, out var entry))
        {
            // Never dispose textures already referenced by this frame's draw lists.
            while (entries.Count >= 256)
            {
                var oldest = entries.Where(pair => pair.Value.LastFrame != frame)
                    .OrderBy(pair => pair.Value.LastFrame).FirstOrDefault();
                if (oldest.Value is null) break;
                entries.Remove(oldest.Key);
                oldest.Value.Dispose();
            }
            entry = new(new MaterialTextLayout(text, fontSize,
                wrapWidth == 0 ? float.PositiveInfinity : wrapWidth, FontFamily, Locale));
            entries.Add(key, entry);
        }
        entry.LastFrame = frame;
        return entry;
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (activeScopes != 0) throw new InvalidOperationException("Clear text resources outside drawing scopes.");
        var nativeContext = ImGui.GetCurrentContext();
        if (nativeContext.Handle != null && context != 0)
        {
            if ((nint)nativeContext.Handle != context)
                throw new InvalidOperationException("Release text resources in their original ImGui context.");
            if (nativeContext.WithinFrameScope || (nativeContext.FrameCountRendered != nativeContext.FrameCount
                && entries.Values.Any(entry => entry.Texture is not null && entry.LastFrame == nativeContext.FrameCount)))
                throw new InvalidOperationException("Release text textures after native frame submission.");
        }
        foreach (var entry in entries.Values) entry.Dispose();
        entries.Clear();
    }
    public void Dispose()
    {
        if (disposed) return;
        if (activeScopes != 0) throw new InvalidOperationException("Dispose the text renderer after its drawing scopes.");
        Clear();
        disposed = true;
    }

    private readonly record struct Key(string Text, float Size, float Width);
    private sealed class Entry(MaterialTextLayout layout) : IDisposable
    {
        public MaterialTextLayout Layout { get; } = layout;
        public Vector2 Offset;
        public Vector2 RasterSize;
        public MaterialTextTexture? Texture;
        public int LastFrame;
        public void Dispose() { Texture?.Dispose(); Layout.Dispose(); }
    }

    public sealed class Scope : IDisposable
    {
        private MaterialTextRenderer? owner;
        private readonly Scope? previous;
        private readonly nint nativeContext;
        internal MaterialTextRenderer? Renderer => owner;
        internal Scope(MaterialTextRenderer owner, Scope? previous, nint nativeContext)
        { this.owner = owner; this.previous = previous; this.nativeContext = nativeContext; }
        public void Dispose()
        {
            if (owner is null) return;
            if (currentScope != this || (nint)ImGui.GetCurrentContext().Handle != nativeContext)
                throw new InvalidOperationException("Text scopes must be disposed in their original context and order.");
            currentScope = previous;
            owner.activeScopes--;
            owner = null;
        }
    }
}

/// <summary>Native text for existing scripts; Windows shaping for Devanagari under an owning text scope.</summary>
public static partial class MaterialText
{
    public static string NativeGlyphText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!RequiresShaping(text)) return text;
        var result = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
            if (!IsShapedCodePoint(rune.Value)) result.Append(rune.ToString());
        return result.ToString();
    }

    public static bool RequiresShaping(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (var rune in text.EnumerateRunes())
            if (IsShapedCodePoint(rune.Value))
                return true;
        return false;
    }

    private static bool IsShapedCodePoint(int value) => value is >= 0x0900 and <= 0x097f or >= 0x1cd0 and <= 0x1cff
        or >= 0xa8e0 and <= 0xa8ff or >= 0x11b00 and <= 0x11b5f;

    public static Vector2 Measure(string text, bool hideTextAfterDoubleHash = false, float wrapWidth = 0)
    {
        if (hideTextAfterDoubleHash && text.IndexOf("##", StringComparison.Ordinal) is var end && end >= 0)
            text = text[..end];
        if (MaterialTextRenderer.Current is { } renderer && RequiresShaping(text))
            return renderer.GetLayout(text, ImGui.GetFontSize(), Math.Max(0, wrapWidth)).Size;
        return ImGui.CalcTextSize(text, hideTextAfterDoubleHash, wrapWidth);
    }

    public static void AddText(ImDrawListPtr drawList, Vector2 position, uint color, string text, float wrapWidth = 0)
    {
        if (MaterialTextRenderer.Current is { } renderer && RequiresShaping(text))
            renderer.Draw(drawList, position, color, text, ImGui.GetFontSize(), Math.Max(0, wrapWidth));
        else drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize(), position, color, text, wrapWidth);
    }

    public static void AddText(ImDrawListPtr drawList, ImFontPtr font, float fontSize, Vector2 position,
        uint color, string text, float wrapWidth = 0)
    {
        if (MaterialTextRenderer.Current is { } renderer && RequiresShaping(text))
            renderer.Draw(drawList, position, color, text, fontSize, Math.Max(0, wrapWidth));
        else drawList.AddText(font, fontSize, position, color, text, wrapWidth);
    }

    public static void Text(string text, float wrapWidth = 0)
    {
        if (MaterialTextRenderer.Current is null || !RequiresShaping(text))
        {
            if (wrapWidth > 0) ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
            ImGui.TextUnformatted(text);
            if (wrapWidth > 0) ImGui.PopTextWrapPos();
            return;
        }
        var position = ImGui.GetCursorScreenPos();
        if (wrapWidth <= 0 && ImGuiP.GetCurrentWindow().DC.TextWrapPos >= 0)
            wrapWidth = ImGuiP.CalcWrapWidthForPos(position, ImGuiP.GetCurrentWindow().DC.TextWrapPos);
        var size = Measure(text, wrapWidth: wrapWidth);
        ImGui.Dummy(size);
        if (ImGui.IsItemVisible())
            AddText(ImGui.GetWindowDrawList(), position, ImGui.GetColorU32(ImGuiCol.Text), text, wrapWidth);
    }
}

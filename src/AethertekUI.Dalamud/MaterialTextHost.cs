using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace AethertekUI.Dalamud;

/// <summary>Owns text layouts and host-created textures; no atlas mutation or renderer installation.</summary>
public sealed class MaterialTextHost : IDisposable
{
    public MaterialTextRenderer Renderer { get; }
    public MaterialTextHost(ITextureProvider textures, string fontFamily = "Nirmala UI", string locale = "hi-IN")
    {
        ArgumentNullException.ThrowIfNull(textures);
        Renderer = new(bitmap =>
        {
            var texture = textures.CreateFromRaw(RawImageSpecification.Rgba32(bitmap.Width, bitmap.Height),
                bitmap.Rgba, "AethertekUI shaped text");
            return new(texture.Handle, texture);
        }, fontFamily, locale);
    }
    public MaterialTextRenderer.Scope Push() => Renderer.Push();
    public void Dispose() => Renderer.Dispose();
}

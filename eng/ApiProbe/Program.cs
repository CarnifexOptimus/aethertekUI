using Dalamud.Bindings.ImGui;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
if(args.Length>0 && args[0]=="--refs")
{
    foreach(var path in args.Skip(1))
    {
        Console.WriteLine(Path.GetFileName(path));
        using var stream=File.OpenRead(path);
        using var pe=new PEReader(stream);
        var metadata=pe.GetMetadataReader();
        foreach(var reference in metadata.AssemblyReferences)
            Console.WriteLine("  "+metadata.GetString(metadata.GetAssemblyReference(reference).Name));
    }
    return;
}
var types=new[]{typeof(ImGuiMouseCursor),typeof(ImGuiListClipperPtr),typeof(ImGuiPayloadPtr),typeof(ImFontAtlasPtr),typeof(ImTextureID),typeof(ImDrawDataPtr),typeof(ImDrawListPtr),typeof(ImDrawCmd),typeof(ImGui)};
foreach(var type in types)
{
    Console.WriteLine(type.FullName);
    foreach(var member in type.GetMembers(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static))
        if(args.Length==0||args.Any(a=>member.Name.Contains(a,StringComparison.OrdinalIgnoreCase))) Console.WriteLine(member);
}

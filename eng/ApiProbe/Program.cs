using Dalamud.Bindings.ImGui;
using System.Reflection;
var types=new[]{typeof(ImFontAtlasPtr),typeof(ImTextureID),typeof(ImDrawDataPtr),typeof(ImDrawListPtr),typeof(ImDrawCmd),typeof(ImGui)};
foreach(var type in types)
{
    Console.WriteLine(type.FullName);
    foreach(var member in type.GetMembers(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static))
        if(args.Length==0||args.Any(a=>member.Name.Contains(a,StringComparison.OrdinalIgnoreCase))) Console.WriteLine(member);
}

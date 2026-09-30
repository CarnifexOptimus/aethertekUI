using System.IO.Compression;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Tests;

/// <summary>Diagnostic CPU renderer for the actual ImGui mesh, not a pixel test or production renderer.</summary>
internal static unsafe class SoftwarePreview
{
    public static void Save(string path,NativeTestContext host)
    {
        // The atlas can be rebuilt during frame submission; never retain its old backing pointer.
        host.RefreshAtlas();
        var data=ImGui.GetDrawData();
        if(Environment.GetEnvironmentVariable("MATERIALUI_MESH_DIAGNOSTICS")=="1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var mesh=new BinaryWriter(File.Create(path+".mesh"));
            mesh.Write(data.CmdListsCount);
            for(var index=0;index<data.CmdListsCount;index++)
            {
                var list=new ImDrawListPtr(data.CmdLists[index]);
                mesh.Write(list.CmdBuffer.Size);mesh.Write(list.VtxBuffer.Size);mesh.Write(list.IdxBuffer.Size);
                for(var ci=0;ci<list.CmdBuffer.Size;ci++)
                {
                    var cmd=list.CmdBuffer[ci];
                    mesh.Write(cmd.ClipRect.X);mesh.Write(cmd.ClipRect.Y);mesh.Write(cmd.ClipRect.Z);mesh.Write(cmd.ClipRect.W);
                    mesh.Write(cmd.ElemCount);mesh.Write(cmd.IdxOffset);mesh.Write(cmd.VtxOffset);
                }
                for(var vi=0;vi<list.VtxBuffer.Size;vi++)
                {
                    var vertex=list.VtxBuffer[vi];
                    mesh.Write(vertex.Pos.X);mesh.Write(vertex.Pos.Y);mesh.Write(vertex.Uv.X);mesh.Write(vertex.Uv.Y);mesh.Write(vertex.Col);
                }
                for(var ii=0;ii<list.IdxBuffer.Size;ii++) mesh.Write(list.IdxBuffer[ii]);
            }
        }
        var width=(int)data.DisplaySize.X;var height=(int)data.DisplaySize.Y;
        var rgba=new byte[width*height*4];
        for(var i=0;i<rgba.Length;i+=4) { rgba[i]=26;rgba[i+1]=29;rgba[i+2]=33;rgba[i+3]=255; }
        for(var listIndex=0;listIndex<data.CmdListsCount;listIndex++)
        {
            var list=new ImDrawListPtr(data.CmdLists[listIndex]);
            // Snapshot native vectors before CPU rasterization. Nested generated binding indexers
            // can alias temporary wrapper storage under optimized code.
            var vertices=new ImDrawVert[list.VtxBuffer.Size];
            var indices=new ushort[list.IdxBuffer.Size];
            for(var i=0;i<vertices.Length;i++) vertices[i]=list.VtxBuffer[i];
            for(var i=0;i<indices.Length;i++) indices[i]=list.IdxBuffer[i];
            for(var ci=0;ci<list.CmdBuffer.Size;ci++)
            {
                var cmd=list.CmdBuffer[ci];
                for(uint offset=0;offset<cmd.ElemCount;offset+=3)
                {
                    var a=vertices[(int)(indices[(int)(cmd.IdxOffset+offset)]+cmd.VtxOffset)];
                    var b=vertices[(int)(indices[(int)(cmd.IdxOffset+offset+1)]+cmd.VtxOffset)];
                    var c=vertices[(int)(indices[(int)(cmd.IdxOffset+offset+2)]+cmd.VtxOffset)];
                    Raster(a,b,c,cmd.ClipRect,rgba,width,height,host);
                }
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file=File.Create(path);
        file.Write(new byte[]{137,80,78,71,13,10,26,10});
        var header=new byte[13];
        WriteInt(header,0,width);WriteInt(header,4,height);header[8]=8;header[9]=6;
        Chunk(file,"IHDR",header);
        using var buffer=new MemoryStream();
        using(var zip=new ZLibStream(buffer,CompressionLevel.Fastest,true))
        {
            for(var y=0;y<height;y++) { zip.WriteByte(0);zip.Write(rgba,y*width*4,width*4); }
        }
        Chunk(file,"IDAT",buffer.ToArray());Chunk(file,"IEND",[]);
    }
    private static void Raster(ImDrawVert a,ImDrawVert b,ImDrawVert c,Vector4 clip,byte[] pixels,int width,int height,NativeTestContext host)
    {
        var area=Cross(b.Pos-a.Pos,c.Pos-a.Pos);
        if(Math.Abs(area)<1e-6f)return;
        var minX=Math.Clamp((int)MathF.Floor(Math.Max(clip.X,Math.Min(a.Pos.X,Math.Min(b.Pos.X,c.Pos.X)))),0,width);
        var minY=Math.Clamp((int)MathF.Floor(Math.Max(clip.Y,Math.Min(a.Pos.Y,Math.Min(b.Pos.Y,c.Pos.Y)))),0,height);
        var maxX=Math.Clamp((int)MathF.Ceiling(Math.Min(clip.Z,Math.Max(a.Pos.X,Math.Max(b.Pos.X,c.Pos.X)))),0,width);
        var maxY=Math.Clamp((int)MathF.Ceiling(Math.Min(clip.W,Math.Max(a.Pos.Y,Math.Max(b.Pos.Y,c.Pos.Y)))),0,height);
        var ca=Unpack(a.Col);var cb=Unpack(b.Col);var cc=Unpack(c.Col);
        for(var y=minY;y<maxY;y++) for(var x=minX;x<maxX;x++)
        {
            var p=new Vector2(x+.5f,y+.5f);
            var wa=Cross(b.Pos-p,c.Pos-p)/area;
            var wb=Cross(c.Pos-p,a.Pos-p)/area;
            var wc=1-wa-wb;
            if(wa<0||wb<0||wc<0)continue;
            var uv=a.Uv*wa+b.Uv*wb+c.Uv*wc;
            var tx=Math.Clamp((int)(uv.X*host.AtlasWidth),0,host.AtlasWidth-1);
            var ty=Math.Clamp((int)(uv.Y*host.AtlasHeight),0,host.AtlasHeight-1);
            var color=ca*wa+cb*wb+cc*wc;
            var alpha=color.W*host.Pixels[(ty*host.AtlasWidth+tx)*4+3]/255f;
            var index=(y*width+x)*4;
            pixels[index]=(byte)Math.Clamp(color.X*alpha+pixels[index]*(1-alpha),0,255);
            pixels[index+1]=(byte)Math.Clamp(color.Y*alpha+pixels[index+1]*(1-alpha),0,255);
            pixels[index+2]=(byte)Math.Clamp(color.Z*alpha+pixels[index+2]*(1-alpha),0,255);
        }
    }
    private static float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;
    private static Vector4 Unpack(uint c)=>new(c&255,(c>>8)&255,(c>>16)&255,((c>>24)&255)/255f);
    private static void WriteInt(byte[] bytes,int offset,int value)
    {
        for(var i=0;i<4;i++)bytes[offset+i]=(byte)(value>>(24-i*8));
    }
    private static void Chunk(Stream stream,string name,byte[] bytes)
    {
        var length=new byte[4];WriteInt(length,0,bytes.Length);stream.Write(length);
        var type=System.Text.Encoding.ASCII.GetBytes(name);stream.Write(type);stream.Write(bytes);
        uint crc=0xffffffff;
        foreach(var value in type)crc=Update(crc,value);
        foreach(var value in bytes)crc=Update(crc,value);
        WriteInt(length,0,unchecked((int)~crc));stream.Write(length);
    }
    private static uint Update(uint crc,byte value)
    {
        crc^=value;for(var i=0;i<8;i++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);return crc;
    }
}

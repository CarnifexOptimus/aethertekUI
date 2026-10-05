using System.IO.Compression;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Tests;

/// <summary>Deterministic diagnostic CPU renderer for actual ImGui meshes. Does not verify GPU compositing.</summary>
internal static unsafe class SoftwarePreview
{
    public static void Save(string path,NativeTestContext host) => Write(path,Capture(host));

    public readonly record struct Frame(int Width,int Height,byte[] Rgba);
    public readonly record struct Texture(nint Pixels,int Width,int Height);

    public static Frame Capture(NativeTestContext host)
    {
        // The atlas can be rebuilt during frame submission; never retain its old backing pointer.
        host.RefreshAtlas();
        var data=ImGui.GetDrawData();
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
                var texture=host.Texture(cmd.TextureId);
                for(uint offset=0;offset<cmd.ElemCount;offset+=3)
                {
                    var a=vertices[(int)(indices[(int)(cmd.IdxOffset+offset)]+cmd.VtxOffset)];
                    var b=vertices[(int)(indices[(int)(cmd.IdxOffset+offset+1)]+cmd.VtxOffset)];
                    var c=vertices[(int)(indices[(int)(cmd.IdxOffset+offset+2)]+cmd.VtxOffset)];
                    Raster(a,b,c,cmd.ClipRect,rgba,width,height,texture);
                }
            }
        }
        return new(width,height,rgba);
    }

    public static void Write(string path,Frame frame)
    {
        var (width,height,rgba)=frame;
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

    /// <summary>Reader for exactly the emitted RGBA8/filter-zero format; rejects other encodings.</summary>
    public static Frame Read(string path)
    {
        using var file=File.OpenRead(path);
        Span<byte> signature=stackalloc byte[8]; file.ReadExactly(signature);
        if(!signature.SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) throw new InvalidDataException("Not a PNG.");
        int width=0,height=0; using var compressed=new MemoryStream();
        var ended=false;
        Span<byte> prefix=stackalloc byte[8];
        Span<byte> crc=stackalloc byte[4];
        while(!ended)
        {
            file.ReadExactly(prefix);
            var length=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(prefix[..4]);
            if(length<0 || length>64*1024*1024) throw new InvalidDataException("Invalid chunk length.");
            var type=System.Text.Encoding.ASCII.GetString(prefix[4..]);
            var bytes=new byte[length]; file.ReadExactly(bytes);
            file.ReadExactly(crc);
            uint calculated=0xffffffff;
            foreach(var b in prefix[4..]) calculated=Update(calculated,b);
            foreach(var b in bytes) calculated=Update(calculated,b);
            if(~calculated!=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(crc)) throw new InvalidDataException("Invalid PNG chunk CRC.");
            switch(type)
            {
                case "IHDR":
                    if(length!=13 || bytes[8]!=8 || bytes[9]!=6 || bytes[10]!=0 || bytes[11]!=0 || bytes[12]!=0)
                        throw new InvalidDataException("Expected noninterlaced RGBA8.");
                    width=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(0,4));
                    height=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4,4)); break;
                case "IDAT": compressed.Write(bytes); break;
                case "IEND": ended=true; break;
            }
        }
        if(width<=0 || height<=0 || (long)width*height>32_000_000) throw new InvalidDataException("Invalid frame dimensions.");
        compressed.Position=0; using var zip=new ZLibStream(compressed,CompressionMode.Decompress);
        var rgba=new byte[checked(width*height*4)];
        for(var y=0;y<height;y++)
        {
            if(zip.ReadByte()!=0) throw new InvalidDataException("Expected filter zero.");
            zip.ReadExactly(rgba.AsSpan(y*width*4,width*4));
        }
        if(zip.ReadByte()!=-1) throw new InvalidDataException("Unexpected PNG data.");
        return new(width,height,rgba);
    }

    public static bool Equal(Frame expected,Frame actual,out Frame difference)
    {
        var pixels=new byte[actual.Rgba.Length]; var equal=expected.Width==actual.Width && expected.Height==actual.Height;
        for(var i=0;i<pixels.Length;i+=4)
        {
            pixels[i+3]=255;
            for(var channel=0;channel<4;channel++)
            {
                var delta=i+channel<expected.Rgba.Length?Math.Abs(expected.Rgba[i+channel]-actual.Rgba[i+channel]):255;
                if(delta>2) { equal=false; pixels[i]=255; pixels[i+2]=255; }
            }
        }
        difference=new(actual.Width,actual.Height,pixels);
        return equal;
    }

    public static void Verify(string name,NativeTestContext host,string baselines,string artifacts)
    {
        var frame=Capture(host);
        // Candidate mode is explicit and never writes into the baseline directory.
        if(Environment.GetEnvironmentVariable("AETHERTEKUI_CANDIDATE_DIR") is { Length:>0 } candidates)
        {
            if(Path.GetFullPath(candidates)==Path.GetFullPath(baselines)) throw new InvalidOperationException("Candidates must be separate from baselines.");
            Write(Path.Combine(candidates,name+".png"),frame); return;
        }
        var path=Path.Combine(baselines,name+".png");
        if(!File.Exists(path)) { Write(Path.Combine(artifacts,name+"-actual.png"),frame); throw new FileNotFoundException("Required visual baseline is missing.",path); }
        var expected=Read(path);
        if(Equal(expected,frame,out var difference)) return;
        Write(Path.Combine(artifacts,name+"-expected.png"),expected);
        Write(Path.Combine(artifacts,name+"-actual.png"),frame);
        Write(Path.Combine(artifacts,name+"-difference.png"),difference);
        throw new InvalidOperationException("Visual regression: "+name);
    }

    private static void Raster(ImDrawVert a,ImDrawVert b,ImDrawVert c,Vector4 clip,byte[] pixels,int width,int height,Texture texture)
    {
        var area=Edge(a.Pos,b.Pos,c.Pos);
        if(Math.Abs(area)<1e-6f)return;
        if(area<0) { (b,c)=(c,b);area=-area; }
        var minX=Math.Clamp((int)MathF.Floor(Math.Max(clip.X,Math.Min(a.Pos.X,Math.Min(b.Pos.X,c.Pos.X)))),0,width);
        var minY=Math.Clamp((int)MathF.Floor(Math.Max(clip.Y,Math.Min(a.Pos.Y,Math.Min(b.Pos.Y,c.Pos.Y)))),0,height);
        var maxX=Math.Clamp((int)MathF.Ceiling(Math.Min(clip.Z,Math.Max(a.Pos.X,Math.Max(b.Pos.X,c.Pos.X)))),0,width);
        var maxY=Math.Clamp((int)MathF.Ceiling(Math.Min(clip.W,Math.Max(a.Pos.Y,Math.Max(b.Pos.Y,c.Pos.Y)))),0,height);
        var ca=Unpack(a.Col);var cb=Unpack(b.Col);var cc=Unpack(c.Col);
        for(var y=minY;y<maxY;y++) for(var x=minX;x<maxX;x++)
        {
            var p=new Vector2(x+.5f,y+.5f);
            var ea=Edge(b.Pos,c.Pos,p);var eb=Edge(c.Pos,a.Pos,p);var ec=Edge(a.Pos,b.Pos,p);
            if(!Inside(ea,b.Pos,c.Pos)||!Inside(eb,c.Pos,a.Pos)||!Inside(ec,a.Pos,b.Pos))continue;
            var wa=(float)(ea/area);var wb=(float)(eb/area);var wc=(float)(ec/area);
            var uv=a.Uv*wa+b.Uv*wb+c.Uv*wc;
            var tx=Math.Clamp((int)(uv.X*texture.Width),0,texture.Width-1);
            var ty=Math.Clamp((int)(uv.Y*texture.Height),0,texture.Height-1);
            var color=ca*wa+cb*wb+cc*wc;
            var sample=(byte*)texture.Pixels+(ty*texture.Width+tx)*4;
            var alpha=color.W*sample[3]/255f;
            var index=(y*width+x)*4;
            pixels[index]=(byte)Math.Clamp(color.X*sample[0]/255f*alpha+pixels[index]*(1-alpha),0,255);
            pixels[index+1]=(byte)Math.Clamp(color.Y*sample[1]/255f*alpha+pixels[index+1]*(1-alpha),0,255);
            pixels[index+2]=(byte)Math.Clamp(color.Z*sample[2]/255f*alpha+pixels[index+2]*(1-alpha),0,255);
        }
    }
    // Double precision and a shared-edge ownership rule avoid holes or double blending
    // along the thin triangles used for native borders and antialias fringes.
    private static double Edge(Vector2 a,Vector2 b,Vector2 p)
        => ((double)b.X-a.X)*(p.Y-a.Y)-((double)b.Y-a.Y)*(p.X-a.X);
    private static bool Inside(double edge,Vector2 a,Vector2 b)
        => edge>0 || edge==0 && (b.Y<a.Y || b.Y==a.Y && b.X>a.X);
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

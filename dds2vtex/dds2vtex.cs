using System.Reflection;
using System.Text;
using Pfim;
using Pfim.dds;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

Console.WriteLine("dds2vtex v1.0");

static int PressAnyKeyToContinue(int exitCode = 0)
{
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    return exitCode;
}

if (args.Length == 0)
{
    Console.Error.WriteLine("\tTo use: dds2vtex <dds file>");
    return PressAnyKeyToContinue(-1);
}

// get the dds file from args
var ddsFile = args[0];

if (!Path.GetExtension(ddsFile).Equals(".dds", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Warning, file doesn't have the .dds extension.");
}

using var img = Pfimage.FromFile(ddsFile, new PfimConfig(decompress: false));
var dds = img as Dds;

if (dds == null)
{
    Console.Error.WriteLine($"Error, file is not a DDS, but a {img.GetType()} (format {img.Format}, datalen {img.DataLen})");
    return PressAnyKeyToContinue(-2);
}

var flags = VTexFlags.NO_LOD;
var numMipLevels = (byte)1;

if (dds.Header.MipMapCount != 0)
{
    Console.Error.WriteLine("Warning, DDS has mipmaps, which may not work correctly.");
    flags &= ~VTexFlags.NO_LOD;
    numMipLevels = (byte)dds.Header.MipMapCount;
}

var format = dds switch
{
    Dxt1Dds => VTexFormat.DXT1,
    Dxt5Dds => VTexFormat.DXT5,
    Bc6hDds => VTexFormat.BC6H,
    Bc7Dds => VTexFormat.BC7,
    _ => VTexFormat.UNKNOWN,
};

if (format == VTexFormat.UNKNOWN)
{
    Console.Error.WriteLine($"Error, do not handle DDS with format {dds.GetType()}.");
    return PressAnyKeyToContinue(-3);
}

// TODO: check blocksize

using var stream = File.Open(Path.ChangeExtension(ddsFile, ".vtex_c"), FileMode.Create);
using var writer = new BinaryWriter(stream);
Texture vtex = null!;
var nonDataSize = 0;
var offsetOfDataSize = 0;

using (var resource = new Resource())
{
    var assembly = Assembly.GetExecutingAssembly();
    using var template = assembly.GetManifestResourceStream("vtex.template");
    resource.Read(template);
    vtex = (Texture)resource.DataBlock;

    // Write a copy of the vtex_c up to the DATA block region
    nonDataSize = (int)resource.DataBlock.Offset;

    resource.Reader.BaseStream.Seek(8, SeekOrigin.Begin);
    var blockOffset = resource.Reader.ReadUInt32();
    var blockCount = resource.Reader.ReadUInt32();
    resource.Reader.BaseStream.Seek(blockOffset - 8, SeekOrigin.Current); // 8 is 2 uint32s we just read
    for (var i = 0; i < blockCount; i++)
    {
        var blockType = Encoding.UTF8.GetString(resource.Reader.ReadBytes(4));
        resource.Reader.BaseStream.Position += 8; // Offset, size
        if (blockType == "DATA")
        {
            offsetOfDataSize = (int)resource.Reader.BaseStream.Position - 4;
            break;
        }
    }

    resource.Reader.BaseStream.Position = 0;
    writer.Write(resource.Reader.ReadBytes(nonDataSize).ToArray());
}

// Write the VTEX data
writer.Write(vtex.Version);
writer.Write((ushort)flags);
writer.Write(vtex.Reflectivity[0]);
writer.Write(vtex.Reflectivity[1]);
writer.Write(vtex.Reflectivity[2]);
writer.Write(vtex.Reflectivity[3]);
writer.Write((ushort)dds.Width);
writer.Write((ushort)dds.Height);
writer.Write((ushort)(dds.Header.Depth != 0 ? dds.Header.Depth : 1));
writer.Write((byte)format);
writer.Write((byte)numMipLevels);
writer.Write((uint)0);

// Extra data
writer.Write((uint)0);
writer.Write((uint)0);

var resourceSize = (uint)stream.Length;
var resourceDataSize = (uint)(resourceSize - nonDataSize);

// Dxt data goes here
writer.Write(dds.Data);

// resource: fixup the full and DATA block size
writer.Seek(0, SeekOrigin.Begin);
writer.Write(resourceSize);

writer.Seek(offsetOfDataSize, SeekOrigin.Begin);
writer.Write(resourceDataSize);

/*
writer.Seek(0, SeekOrigin.Begin);
var res2 = new Resource();
res2.Read(writer.BaseStream, false);
*/

return 0;

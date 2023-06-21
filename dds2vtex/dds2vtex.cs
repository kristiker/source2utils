using System.Text;
using Pfim;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

Console.WriteLine("dds2vtex v1.0.0");

static int PressAnyKeyToContinue(int exitCode = 0)
{
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    return exitCode;
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
    return PressAnyKeyToContinue(-1);
}

var format = dds switch
{
    Dxt1Dds => VTexFormat.DXT1,
    Dxt5Dds => VTexFormat.DXT5,
    _ => VTexFormat.UNKNOWN,
};

if (format == VTexFormat.UNKNOWN)
{
    Console.Error.WriteLine($"Error, do not handle DDS with format {dds.GetType()}.");
    return PressAnyKeyToContinue(-2);
}

// TODO: check blocksize

using var stream = File.Open(Path.ChangeExtension(ddsFile, ".vtex_c"), FileMode.Create);
using var writer = new BinaryWriter(stream);
Texture vtex = null!;
var nonDataSize = 0;
var offsetOfDataSize = 0;

using (var resource = new Resource())
{
    resource.Read("D:/Users/kristi/Downloads/dxt1_template.vtex_c");
    vtex = (Texture)resource.DataBlock;

    // Write a copy of the vtex_c up to the DATA block region
    nonDataSize = (int)resource.DataBlock.Offset;

    resource.Reader.BaseStream.Position = 6;
    var blockOffset = resource.Reader.ReadUInt32();
    var blockCount = resource.Reader.ReadUInt32();
    resource.Reader.BaseStream.Position += blockOffset - 8; // 8 is 2 uint32s we just read
    for (var i = 0; i < blockCount; i++)
    {
        if (resource.Reader.ReadBytes(4) == Encoding.UTF8.GetBytes("DATA"))
        {
            offsetOfDataSize = (int)resource.Reader.BaseStream.Position + 4;
            break;
        }
    }

    resource.Reader.BaseStream.Position = 0;
    writer.Write(resource.Reader.ReadBytes(nonDataSize).ToArray());
}

// Write the VTEX data
writer.Write(vtex.Version);
writer.Write((ushort)vtex.Flags);
writer.Write(vtex.Reflectivity[0]);
writer.Write(vtex.Reflectivity[1]);
writer.Write(vtex.Reflectivity[2]);
writer.Write(vtex.Reflectivity[3]);
writer.Write((ushort)dds.Width);
writer.Write((ushort)dds.Height);
writer.Write((ushort)dds.Height + 1);
writer.Write((byte)format);
writer.Write((byte)(dds.MipMaps.Length > 0 ? dds.MipMaps.Length : 1));
writer.Write((uint)0);

// Extra data
writer.Write((uint)0);
writer.Write((uint)0);

// Dxt data goes here
writer.Write(dds.Data);

// Fixup the file and DATA block size
var fileSize = (uint)stream.Length;
var dataSize = (uint)(fileSize - nonDataSize);

// This shit is not overwriting it
writer.Seek(0, SeekOrigin.Begin);
writer.Write(fileSize);

writer.Seek(offsetOfDataSize, SeekOrigin.Begin);
writer.Write(dataSize);
writer.Flush();
stream.Flush();

return 0;

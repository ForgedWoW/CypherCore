namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MovieRecord
{
    public uint Id;
    public byte Volume;
    public byte KeyID;
    public uint AudioFileDataID;
    public uint SubtitleFileDataID;
    public int SubtitleFileFormat;
}
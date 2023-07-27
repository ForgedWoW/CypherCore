using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J;

public sealed record JournalInstanceRecord
{
    public uint Id;
    public LocalizedString Name;
    public LocalizedString Description;
    public ushort MapID;
    public int BackgroundFileDataID;
    public int ButtonFileDataID;
    public int ButtonSmallFileDataID;
    public int LoreFileDataID;
    public int Flags;
    public ushort AreaID;
}
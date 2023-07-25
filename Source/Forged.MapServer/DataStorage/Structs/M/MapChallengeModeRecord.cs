using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MapChallengeModeRecord
{
    public LocalizedString Name;
    public uint Id;
    public ushort MapID;
    public byte Flags;
    public uint ExpansionLevel;
    public int RequiredWorldStateID; // maybe?
    public short[] CriteriaCount = new short[3];
}
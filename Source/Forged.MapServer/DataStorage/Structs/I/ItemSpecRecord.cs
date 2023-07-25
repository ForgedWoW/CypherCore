using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemSpecRecord
{
    public uint Id;
    public byte MinLevel;
    public byte MaxLevel;
    public byte ItemType;
    public ItemSpecStat PrimaryStat;
    public ItemSpecStat SecondaryStat;
    public ushort SpecializationID;
}
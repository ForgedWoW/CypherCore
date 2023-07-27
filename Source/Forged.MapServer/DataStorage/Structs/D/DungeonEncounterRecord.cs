using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed record DungeonEncounterRecord
{
    public LocalizedString Name;
    public uint Id;
    public short MapID;
    public int DifficultyID;
    public int OrderIndex;
    public int CompleteWorldStateID;
    public sbyte Bit;
    public int Flags;
    public int SpellIconFileID;
    public int Faction;
}
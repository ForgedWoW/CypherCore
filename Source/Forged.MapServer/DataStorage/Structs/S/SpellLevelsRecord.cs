namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellLevelsRecord
{
    public uint Id;
    public byte DifficultyID;
    public ushort MaxLevel;
    public byte MaxPassiveAuraLevel;
    public ushort BaseLevel;
    public ushort SpellLevel;
    public uint SpellID;
}
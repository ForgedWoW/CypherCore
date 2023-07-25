namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellAuraOptionsRecord
{
    public uint Id;
    public byte DifficultyID;
    public ushort CumulativeAura;
    public uint ProcCategoryRecovery;
    public byte ProcChance;
    public int ProcCharges;
    public ushort SpellProcsPerMinuteID;
    public int[] ProcTypeMask = new int[2];
    public uint SpellID;
}
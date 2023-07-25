namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellCategoriesRecord
{
    public uint Id;
    public byte DifficultyID;
    public ushort Category;
    public sbyte DefenseType;
    public sbyte DispelType;
    public sbyte Mechanic;
    public sbyte PreventionType;
    public ushort StartRecoveryCategory;
    public ushort ChargeCategory;
    public uint SpellID;
}
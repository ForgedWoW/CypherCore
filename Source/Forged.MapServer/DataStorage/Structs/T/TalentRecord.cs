namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TalentRecord
{
    public uint Id;
    public string Description;
    public byte TierID;
    public byte Flags;
    public byte ColumnIndex;
    public byte ClassID;
    public ushort SpecID;
    public uint SpellID;
    public uint OverridesSpellID;
    public byte[] CategoryMask = new byte[2];
}
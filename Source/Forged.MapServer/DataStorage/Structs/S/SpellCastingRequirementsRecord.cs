namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellCastingRequirementsRecord
{
    public uint Id;
    public uint SpellID;
    public byte FacingCasterFlags;
    public ushort MinFactionID;
    public int MinReputation;
    public ushort RequiredAreasID;
    public byte RequiredAuraVision;
    public ushort RequiresSpellFocus;
}
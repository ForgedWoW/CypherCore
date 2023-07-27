namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellVisualKitRecord
{
    public uint Id;
    public sbyte FallbackPriority;
    public int FallbackSpellVisualKitId;
    public ushort DelayMin;
    public ushort DelayMax;
    public int[] Flags = new int[2];
}
namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemDamageAmmoRecord
{
    public uint Id;
    public ushort ItemLevel;
    public float[] Quality = new float[7];
}
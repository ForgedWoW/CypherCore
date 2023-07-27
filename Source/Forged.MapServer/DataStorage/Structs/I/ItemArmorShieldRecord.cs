namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemArmorShieldRecord
{
    public uint Id;
    public float[] Quality = new float[7];
    public ushort ItemLevel;
}
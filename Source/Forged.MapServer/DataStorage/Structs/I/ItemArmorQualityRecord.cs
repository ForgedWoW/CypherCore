namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemArmorQualityRecord
{
    public uint Id;
    public float[] QualityMod = new float[7];
}
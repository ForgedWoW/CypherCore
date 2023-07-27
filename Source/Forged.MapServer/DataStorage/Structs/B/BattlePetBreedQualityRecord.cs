namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BattlePetBreedQualityRecord
{
    public uint Id;
    public int MaxQualityRoll;
    public float StateMultiplier;
    public sbyte QualityEnum;
}
namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellRadiusRecord
{
    public uint Id;
    public float Radius;
    public float RadiusPerLevel;
    public float RadiusMin;
    public float RadiusMax;
}
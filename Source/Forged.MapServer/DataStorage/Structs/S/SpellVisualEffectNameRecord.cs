namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellVisualEffectNameRecord
{
    public uint Id;
    public int ModelFileDataID;
    public float BaseMissileSpeed;
    public float Scale;
    public float MinAllowedScale;
    public float MaxAllowedScale;
    public float Alpha;
    public uint Flags;
    public int TextureFileDataID;
    public float EffectRadius;
    public uint Type;
    public int GenericID;
    public uint RibbonQualityID;
    public int DissolveEffectID;
    public int ModelPosition;
    public sbyte Unknown901;
}
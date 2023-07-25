namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SoundKitRecord
{
    public uint Id;
    public uint SoundType;
    public float VolumeFloat;
    public int Flags;
    public float MinDistance;
    public float DistanceCutoff;
    public byte EAXDef;
    public uint SoundKitAdvancedID;
    public float VolumeVariationPlus;
    public float VolumeVariationMinus;
    public float PitchVariationPlus;
    public float PitchVariationMinus;
    public sbyte DialogType;
    public float PitchAdjust;
    public ushort BusOverwriteID;
    public byte MaxInstances;
    public uint SoundMixGroupID;
}
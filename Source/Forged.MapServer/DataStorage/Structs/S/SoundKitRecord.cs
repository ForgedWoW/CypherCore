// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SoundKitRecord
{
    public ushort BusOverwriteID;
    public sbyte DialogType;
    public float DistanceCutoff;
    public byte EAXDef;
    public int Flags;
    public uint Id;
    public byte MaxInstances;
    public float MinDistance;
    public float PitchAdjust;
    public float PitchVariationMinus;
    public float PitchVariationPlus;
    public uint SoundKitAdvancedID;
    public uint SoundMixGroupID;
    public uint SoundType;
    public float VolumeFloat;
    public float VolumeVariationMinus;
    public float VolumeVariationPlus;
}
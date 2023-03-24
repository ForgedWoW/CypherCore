// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

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

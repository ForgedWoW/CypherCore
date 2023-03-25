// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CreatureModelDataRecord
{
	public uint Id;
	public float[] GeoBox = new float[6];
	public uint Flags;
	public uint FileDataID;
	public uint BloodID;
	public uint FootprintTextureID;
	public float FootprintTextureLength;
	public float FootprintTextureWidth;
	public float FootprintParticleScale;
	public uint FoleyMaterialID;
	public uint FootstepCameraEffectID;
	public uint DeathThudCameraEffectID;
	public uint SoundID;
	public uint SizeClass;
	public float CollisionWidth;
	public float CollisionHeight;
	public float WorldEffectScale;
	public uint CreatureGeosetDataID;
	public float HoverHeight;
	public float AttachedEffectScale;
	public float ModelScale;
	public float MissileCollisionRadius;
	public float MissileCollisionPush;
	public float MissileCollisionRaise;
	public float MountHeight;
	public float OverrideLootEffectScale;
	public float OverrideNameScale;
	public float OverrideSelectionRadius;
	public float TamedPetBaseScale;
	public sbyte Unknown820_1;                  // scale related
	public float Unknown820_2;                  // scale related
	public float[] Unknown820_3 = new float[2]; // scale related

	public CreatureModelDataFlags GetFlags()
	{
		return (CreatureModelDataFlags)Flags;
	}
}
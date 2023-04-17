// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CreatureModelDataRecord
{
    public float AttachedEffectScale;
    public uint BloodID;
    public float CollisionHeight;
    public float CollisionWidth;
    public uint CreatureGeosetDataID;
    public uint DeathThudCameraEffectID;
    public uint FileDataID;
    public uint Flags;
    public uint FoleyMaterialID;
    public float FootprintParticleScale;
    public uint FootprintTextureID;
    public float FootprintTextureLength;
    public float FootprintTextureWidth;
    public uint FootstepCameraEffectID;
    public float[] GeoBox = new float[6];
    public float HoverHeight;
    public uint Id;
    public float MissileCollisionPush;
    public float MissileCollisionRadius;
    public float MissileCollisionRaise;
    public float ModelScale;
    public float MountHeight;
    public float OverrideLootEffectScale;
    public float OverrideNameScale;
    public float OverrideSelectionRadius;
    public uint SizeClass;
    public uint SoundID;
    public float TamedPetBaseScale;

    public sbyte Unknown820_1;

    // scale related
    public float Unknown820_2;

    // scale related
    public float[] Unknown820_3 = new float[2];

    public float WorldEffectScale;
    // scale related

    public CreatureModelDataFlags GetFlags()
    {
        return (CreatureModelDataFlags)Flags;
    }
}
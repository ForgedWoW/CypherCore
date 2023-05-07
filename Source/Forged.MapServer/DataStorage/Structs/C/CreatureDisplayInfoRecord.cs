// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CreatureDisplayInfoRecord
{
    public ushort AnimReplacementSetID;
    public byte BloodID;
    public byte CreatureModelAlpha;
    public sbyte CreatureModelMinLod;
    public float CreatureModelScale;
    public int DissolveEffectID;
    public int DissolveOutEffectID;
    public int ExtendedDisplayInfoID;
    public byte Flags;
    public sbyte Gender;
    public uint Id;
    public ushort ModelID;
    public int MountPoofSpellVisualKitID;
    public ushort NPCSoundID;
    public ushort ObjectEffectPackageID;
    public ushort ParticleColorID;
    public float PetInstanceScale;
    public float PlayerOverrideScale;
    public int PortraitCreatureDisplayInfoID;
    public int PortraitTextureFileDataID;
    public sbyte SizeClass;
    public ushort SoundID;
    public int StateSpellVisualKitID;
    public int[] TextureVariationFileDataID = new int[4];

    // scale of not own player pets inside dungeons/raids/scenarios
    public sbyte UnarmedWeaponType;
}
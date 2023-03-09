// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class CreatureDisplayInfoRecord
{
	public uint Id;
	public ushort ModelID;
	public ushort SoundID;
	public sbyte SizeClass;
	public float CreatureModelScale;
	public byte CreatureModelAlpha;
	public byte BloodID;
	public int ExtendedDisplayInfoID;
	public ushort NPCSoundID;
	public ushort ParticleColorID;
	public int PortraitCreatureDisplayInfoID;
	public int PortraitTextureFileDataID;
	public ushort ObjectEffectPackageID;
	public ushort AnimReplacementSetID;
	public byte Flags;
	public int StateSpellVisualKitID;
	public float PlayerOverrideScale;
	public float PetInstanceScale; // scale of not own player pets inside dungeons/raids/scenarios
	public sbyte UnarmedWeaponType;
	public int MountPoofSpellVisualKitID;
	public int DissolveEffectID;
	public sbyte Gender;
	public int DissolveOutEffectID;
	public sbyte CreatureModelMinLod;
	public int[] TextureVariationFileDataID = new int[4];
}
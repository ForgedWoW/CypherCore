// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;

namespace Game.Common.DataStorage.Structs.C;

public sealed class ChrClassesRecord
{
	public LocalizedString Name;
	public string Filename;
	public string NameMale;
	public string NameFemale;
	public string PetNameToken;
	public string Description;
	public string RoleInfoString;
	public string DisabledString;
	public string HyphenatedNameMale;
	public string HyphenatedNameFemale;
	public uint Id;
	public uint CreateScreenFileDataID;
	public uint SelectScreenFileDataID;
	public uint IconFileDataID;
	public uint LowResScreenFileDataID;
	public int Flags;
	public uint SpellTextureBlobFileDataID;
	public uint RolesMask;
	public uint ArmorTypeMask;
	public int CharStartKitUnknown901;
	public int MaleCharacterCreationVisualFallback;
	public int MaleCharacterCreationIdleVisualFallback;
	public int FemaleCharacterCreationVisualFallback;
	public int FemaleCharacterCreationIdleVisualFallback;
	public int CharacterCreationIdleGroundVisualFallback;
	public int CharacterCreationGroundVisualFallback;
	public int AlteredFormCharacterCreationIdleVisualFallback;
	public int CharacterCreationAnimLoopWaitTimeMsFallback;
	public ushort CinematicSequenceID;
	public ushort DefaultSpec;
	public byte PrimaryStatPriority;
	public PowerType DisplayPower;
	public byte RangedAttackPowerPerAgility;
	public byte AttackPowerPerAgility;
	public byte AttackPowerPerStrength;
	public byte SpellClassSet;
	public byte ClassColorR;
	public byte ClassColorG;
	public byte ClassColorB;
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChrClassesRecord
{
    public int AlteredFormCharacterCreationIdleVisualFallback;
    public uint ArmorTypeMask;
    public byte AttackPowerPerAgility;
    public byte AttackPowerPerStrength;
    public int CharacterCreationAnimLoopWaitTimeMsFallback;
    public int CharacterCreationGroundVisualFallback;
    public int CharacterCreationIdleGroundVisualFallback;
    public int CharStartKitUnknown901;
    public ushort CinematicSequenceID;
    public byte ClassColorB;
    public byte ClassColorG;
    public byte ClassColorR;
    public uint CreateScreenFileDataID;
    public ushort DefaultSpec;
    public string Description;
    public string DisabledString;
    public PowerType DisplayPower;
    public int FemaleCharacterCreationIdleVisualFallback;
    public int FemaleCharacterCreationVisualFallback;
    public string Filename;
    public int Flags;
    public string HyphenatedNameFemale;
    public string HyphenatedNameMale;
    public uint IconFileDataID;
    public uint Id;
    public uint LowResScreenFileDataID;
    public int MaleCharacterCreationIdleVisualFallback;
    public int MaleCharacterCreationVisualFallback;
    public LocalizedString Name;
    public string NameFemale;
    public string NameMale;
    public string PetNameToken;
    public byte PrimaryStatPriority;
    public byte RangedAttackPowerPerAgility;
    public string RoleInfoString;
    public uint RolesMask;
    public uint SelectScreenFileDataID;
    public byte SpellClassSet;
    public uint SpellTextureBlobFileDataID;
}
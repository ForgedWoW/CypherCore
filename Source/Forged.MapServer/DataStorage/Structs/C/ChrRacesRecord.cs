// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ChrRacesRecord
{
    public int Alliance;
    public float[] AlteredFormCustomizeOffsetFallback = new float[3];
    public float AlteredFormCustomizeRotationFallback;
    public int[] AlteredFormFinishVisualKitID = new int[3];
    public int[] AlteredFormStartVisualKitID = new int[3];
    public sbyte BaseLanguage;
    public uint CinematicSequenceID;
    public string ClientFileString;
    public string ClientPrefix;
    public int CreateScreenFileDataID;
    public sbyte CreatureType;
    public int DefaultClassID;
    public int FactionID;
    public int FemaleModelFallbackRaceID;
    public sbyte FemaleModelFallbackSex;
    public int FemaleTextureFallbackRaceID;
    public sbyte FemaleTextureFallbackSex;
    public int Flags;
    public int HelmetAnimScalingRaceID;
    public int HeritageArmorAchievementID;
    public uint Id;
    public string LoreDescription;
    public string LoreName;
    public string LoreNameFemale;
    public string LoreNameLower;
    public string LoreNameLowerFemale;
    public int LowResScreenFileDataID;
    public int MaleModelFallbackRaceID;
    public sbyte MaleModelFallbackSex;
    public int MaleTextureFallbackRaceID;
    public sbyte MaleTextureFallbackSex;
    public LocalizedString Name;
    public string NameFemale;
    public string NameFemaleLowercase;
    public string NameLowercase;
    public int NeutralRaceID;
    public int PlayableRaceBit;
    public int RaceRelated;
    public uint ResSicknessSpellID;
    public int SelectScreenFileDataID;
    public string ShortName;
    public string ShortNameFemale;
    public string ShortNameLower;
    public string ShortNameLowerFemale;
    public int SplashSoundID;
    public int StartingLevel;
    public int TransmogrifyDisabledSlotMask;
    public int UiDisplayOrder;
    public int UnalteredVisualCustomizationRaceID;
    public int UnalteredVisualRaceID;
    public int Unknown1000;
    public float[] Unknown910_1 = new float[3];
    public float[] Unknown910_2 = new float[3];
    public ChrRacesFlag GetFlags()
    {
        return (ChrRacesFlag)Flags;
    }
}
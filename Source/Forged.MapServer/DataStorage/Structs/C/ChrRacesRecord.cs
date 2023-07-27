using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChrRacesRecord
{
    public uint Id;
    public string ClientPrefix;
    public string ClientFileString;
    public LocalizedString Name;
    public string NameFemale;
    public string NameLowercase;
    public string NameFemaleLowercase;
    public string LoreName;
    public string LoreNameFemale;
    public string LoreNameLower;
    public string LoreNameLowerFemale;
    public string LoreDescription;
    public string ShortName;
    public string ShortNameFemale;
    public string ShortNameLower;
    public string ShortNameLowerFemale;
    public int Flags;
    public int FactionID;
    public uint CinematicSequenceID;
    public uint ResSicknessSpellID;
    public int SplashSoundID;
    public int Alliance;
    public int RaceRelated;
    public int UnalteredVisualRaceID;
    public int DefaultClassID;
    public int CreateScreenFileDataID;
    public int SelectScreenFileDataID;
    public int NeutralRaceID;
    public int LowResScreenFileDataID;
    public int[] AlteredFormStartVisualKitID = new int[3];
    public int[] AlteredFormFinishVisualKitID = new int[3];
    public int HeritageArmorAchievementID;
    public int StartingLevel;
    public int UiDisplayOrder;
    public int MaleModelFallbackRaceID;
    public int FemaleModelFallbackRaceID;
    public int MaleTextureFallbackRaceID;
    public int FemaleTextureFallbackRaceID;
    public int PlayableRaceBit;
    public int HelmetAnimScalingRaceID;
    public int TransmogrifyDisabledSlotMask;
    public int UnalteredVisualCustomizationRaceID;
    public float[] AlteredFormCustomizeOffsetFallback = new float[3];
    public float AlteredFormCustomizeRotationFallback;
    public float[] Unknown910_1 = new float[3];
    public float[] Unknown910_2 = new float[3];
    public int Unknown1000;
    public sbyte BaseLanguage;
    public sbyte CreatureType;
    public sbyte MaleModelFallbackSex;
    public sbyte FemaleModelFallbackSex;
    public sbyte MaleTextureFallbackSex;
    public sbyte FemaleTextureFallbackSex;

    public ChrRacesFlag GetFlags() { return (ChrRacesFlag)Flags; }
}
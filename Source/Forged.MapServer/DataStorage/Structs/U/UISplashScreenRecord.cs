namespace Forged.MapServer.DataStorage.Structs.U;

public sealed record UISplashScreenRecord
{
    public uint Id;
    public string Header;
    public string TopLeftFeatureTitle;
    public string TopLeftFeatureDesc;
    public string BottomLeftFeatureTitle;
    public string BottomLeftFeatureDesc;
    public string RightFeatureTitle;
    public string RightFeatureDesc;
    public int AllianceQuestID;
    public int HordeQuestID;
    public sbyte ScreenType;
    public int TextureKitID;
    public int SoundKitID;
    public int PlayerConditionID;
    public int CharLevelConditionID;
    public int RequiredTimeEventPassed; // serverside TimeEvent table, see ModifierTreeType::HasTimeEventPassed
}
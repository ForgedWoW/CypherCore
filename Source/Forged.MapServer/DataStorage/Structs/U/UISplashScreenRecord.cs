// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UISplashScreenRecord
{
    public int AllianceQuestID;
    public string BottomLeftFeatureDesc;
    public string BottomLeftFeatureTitle;
    public int CharLevelConditionID;
    public string Header;
    public int HordeQuestID;
    public uint Id;
    public int PlayerConditionID;
    public int RequiredTimeEventPassed;
    public string RightFeatureDesc;
    public string RightFeatureTitle;
    public sbyte ScreenType;
    public int SoundKitID;
    public int TextureKitID;
    public string TopLeftFeatureDesc;
    public string TopLeftFeatureTitle;
    // serverside TimeEvent table, see ModifierTreeType::HasTimeEventPassed
}
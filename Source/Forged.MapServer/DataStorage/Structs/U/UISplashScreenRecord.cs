// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UISplashScreenRecord
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
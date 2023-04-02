// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponse
{
    public string Answer;
    public string ButtonTooltip;
    public int ChoiceArtFileId;
    public string Confirmation;
    public string Description;
    public int Flags;
    public byte GroupID;
    public string Header;
    public PlayerChoiceResponseMawPower? MawPower;
    public int ResponseId;
    public ushort ResponseIdentifier;
    public PlayerChoiceResponseReward Reward;
    public uint? RewardQuestID;
    public uint SoundKitID;
    public string SubHeader;
    public uint UiTextureAtlasElementID;
    public int UiTextureKitID;
    public uint WidgetSetID;
}
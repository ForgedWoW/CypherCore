// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponse
{
    public int ResponseId;
    public ushort ResponseIdentifier;
    public int ChoiceArtFileId;
    public int Flags;
    public uint WidgetSetID;
    public uint UiTextureAtlasElementID;
    public uint SoundKitID;
    public byte GroupID;
    public int UiTextureKitID;
    public string Answer;
    public string Header;
    public string SubHeader;
    public string ButtonTooltip;
    public string Description;
    public string Confirmation;
    public PlayerChoiceResponseReward Reward;
    public uint? RewardQuestID;
    public PlayerChoiceResponseMawPower? MawPower;
}
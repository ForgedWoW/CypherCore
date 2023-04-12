// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponse
{
    public string Answer { get; set; }
    public string ButtonTooltip { get; set; }
    public int ChoiceArtFileId { get; set; }
    public string Confirmation { get; set; }
    public string Description { get; set; }
    public int Flags { get; set; }
    public byte GroupID { get; set; }
    public string Header { get; set; }
    public PlayerChoiceResponseMawPower? MawPower { get; set; }
    public int ResponseId { get; set; }
    public ushort ResponseIdentifier { get; set; }
    public PlayerChoiceResponseReward Reward { get; set; }
    public uint? RewardQuestID { get; set; }
    public uint SoundKitID { get; set; }
    public string SubHeader { get; set; }
    public uint UiTextureAtlasElementID { get; set; }
    public int UiTextureKitID { get; set; }
    public uint WidgetSetID { get; set; }
}
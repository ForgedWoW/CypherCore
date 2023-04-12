// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseReward
{
    public uint ArenaPointCount { get; set; }
    public List<PlayerChoiceResponseRewardEntry> Currency { get; set; } = new();
    public List<PlayerChoiceResponseRewardEntry> Faction { get; set; } = new();
    public uint HonorPointCount { get; set; }
    public List<PlayerChoiceResponseRewardItem> ItemChoices { get; set; } = new();
    public List<PlayerChoiceResponseRewardItem> Items { get; set; } = new();
    public ulong Money { get; set; }
    public int PackageId { get; set; }
    public int SkillLineId { get; set; }
    public uint SkillPointCount { get; set; }
    public int TitleId { get; set; }
    public uint Xp { get; set; }
}
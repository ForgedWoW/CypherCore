// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseReward
{
    public uint ArenaPointCount;
    public List<PlayerChoiceResponseRewardEntry> Currency = new();
    public List<PlayerChoiceResponseRewardEntry> Faction = new();
    public uint HonorPointCount;
    public List<PlayerChoiceResponseRewardItem> ItemChoices = new();
    public List<PlayerChoiceResponseRewardItem> Items = new();
    public ulong Money;
    public int PackageId;
    public int SkillLineId;
    public uint SkillPointCount;
    public int TitleId;
    public uint Xp;
}
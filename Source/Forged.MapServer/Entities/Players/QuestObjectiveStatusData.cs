// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Quest;

namespace Forged.MapServer.Entities.Players;

internal struct QuestObjectiveStatusData
{
    public QuestObjective Objective { get; set; }
    public (uint QuestID, QuestStatusData Status) QuestStatusPair { get; set; }
}
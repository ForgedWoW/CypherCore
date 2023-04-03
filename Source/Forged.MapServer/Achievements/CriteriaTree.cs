// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Quest;

namespace Forged.MapServer.Achievements;

public class CriteriaTree
{
    public AchievementRecord Achievement { get; set; }
    public List<CriteriaTree> Children { get; set; } = new();
    public Criteria Criteria { get; set; }
    public CriteriaTreeRecord Entry { get; set; }
    public uint Id { get; set; }
    public QuestObjective QuestObjective { get; set; }
    public ScenarioStepRecord ScenarioStep { get; set; }
}
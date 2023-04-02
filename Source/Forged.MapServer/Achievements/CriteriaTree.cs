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
    public AchievementRecord Achievement;
    public List<CriteriaTree> Children = new();
    public Criteria Criteria;
    public CriteriaTreeRecord Entry;
    public uint Id;
    public QuestObjective QuestObjective;
    public ScenarioStepRecord ScenarioStep;
}
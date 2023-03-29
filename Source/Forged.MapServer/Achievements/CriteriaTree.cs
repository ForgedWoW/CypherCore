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
    public uint Id;
    public CriteriaTreeRecord Entry;
    public AchievementRecord Achievement;
    public ScenarioStepRecord ScenarioStep;
    public QuestObjective QuestObjective;
    public Criteria Criteria;
    public List<CriteriaTree> Children = new();
}
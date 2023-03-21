// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer.Achievements;

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
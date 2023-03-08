using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Achievements;

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
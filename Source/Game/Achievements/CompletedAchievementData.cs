using System.Collections.Generic;
using Game.Entities;

namespace Game.Achievements;

public class CompletedAchievementData
{
	public long Date;
	public List<ObjectGuid> CompletingPlayers = new();
	public bool Changed;
}
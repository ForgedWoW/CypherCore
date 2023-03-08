// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Achievements;
using Game.DataStorage;

namespace Game.Entities;

public partial class Player
{
	public void ResetAchievements()
	{
		_AchievementSys.Reset();
	}

	public void SendRespondInspectAchievements(Player player)
	{
		_AchievementSys.SendAchievementInfo(player);
	}

	public uint GetAchievementPoints()
	{
		return _AchievementSys.GetAchievementPoints();
	}

	public ICollection<uint> GetCompletedAchievementIds()
	{
		return _AchievementSys.GetCompletedAchievementIds();
	}

	public bool HasAchieved(uint achievementId)
	{
		return _AchievementSys.HasAchieved(achievementId);
	}

	public void StartCriteriaTimer(CriteriaStartEvent startEvent, uint entry, uint timeLost = 0)
	{
		_AchievementSys.StartCriteriaTimer(startEvent, entry, timeLost);
	}

	public void RemoveCriteriaTimer(CriteriaStartEvent startEvent, uint entry)
	{
		_AchievementSys.RemoveCriteriaTimer(startEvent, entry);
	}

	public void ResetCriteria(CriteriaFailEvent failEvent, uint failAsset, bool evenIfCriteriaComplete = false)
	{
		_AchievementSys.ResetCriteria(failEvent, failAsset, evenIfCriteriaComplete);
		_questObjectiveCriteriaManager.ResetCriteria(failEvent, failAsset, evenIfCriteriaComplete);
	}

	public void UpdateCriteria(CriteriaType type, double miscValue1, double miscValue2 = 0, double miscValue3 = 0, WorldObject refe = null)
	{
		UpdateCriteria(type, (ulong)miscValue1, (ulong)miscValue2, (ulong)miscValue3, refe);
	}

	public void UpdateCriteria(CriteriaType type, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, WorldObject refe = null)
	{
		_AchievementSys.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);
		_questObjectiveCriteriaManager.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);

		// Update only individual achievement criteria here, otherwise we may get multiple updates
		// from a single boss kill
		if (CriteriaManager.IsGroupCriteriaType(type))
			return;

		var scenario = Scenario;

		if (scenario != null)
			scenario.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);

		var guild = Global.GuildMgr.GetGuildById(GuildId);

		if (guild)
			guild.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);
	}

	public void CompletedAchievement(AchievementRecord entry)
	{
		_AchievementSys.CompletedAchievement(entry, this);
	}

	public bool ModifierTreeSatisfied(uint modifierTreeId)
	{
		return _AchievementSys.ModifierTreeSatisfied(modifierTreeId);
	}
}
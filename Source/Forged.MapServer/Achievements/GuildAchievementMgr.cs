// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Guilds;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Achievements;
using Forged.MapServer.Scripting.Interfaces.IAchievement;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Achievements;

public class GuildAchievementMgr : AchievementManager
{
    private readonly Guild _owner;

	public GuildAchievementMgr(Guild owner)
	{
		_owner = owner;
	}

	public override void Reset()
	{
		base.Reset();

		var guid = _owner.GetGUID();

		foreach (var iter in _completedAchievements)
		{
			GuildAchievementDeleted guildAchievementDeleted = new()
			{
				AchievementID = iter.Key,
				GuildGUID = guid,
				TimeDeleted = GameTime.GetGameTime()
			};

			SendPacket(guildAchievementDeleted);
		}

		_achievementPoints = 0;
		_completedAchievements.Clear();
		DeleteFromDB(guid);
	}

	public static void DeleteFromDB(ObjectGuid guid)
	{
		SQLTransaction trans = new();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENTS);
		stmt.AddValue(0, guid.Counter);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENT_CRITERIA);
		stmt.AddValue(0, guid.Counter);
		trans.Append(stmt);

		DB.Characters.CommitTransaction(trans);
	}

	public void LoadFromDB(SQLResult achievementResult, SQLResult criteriaResult)
	{
		if (!achievementResult.IsEmpty())
			do
			{
				var achievementid = achievementResult.Read<uint>(0);

				// must not happen: cleanup at server startup in sAchievementMgr.LoadCompletedAchievements()
				var achievement = CliDB.AchievementStorage.LookupByKey(achievementid);

				if (achievement == null)
					continue;

				if (_completedAchievements.TryGetValue(achievementid, out var ca))
				{
					ca.Date = achievementResult.Read<long>(1);
					var guids = new StringArray(achievementResult.Read<string>(2), ',');

					if (!guids.IsEmpty())
						for (var i = 0; i < guids.Length; ++i)
							if (ulong.TryParse(guids[i], out var guid))
								ca.CompletingPlayers.Add(ObjectGuid.Create(HighGuid.Player, guid));

					ca.Changed = false;

					_achievementPoints += achievement.Points;
				}
			} while (achievementResult.NextRow());

		if (!criteriaResult.IsEmpty())
		{
			var now = GameTime.GetGameTime();

			do
			{
				var id = criteriaResult.Read<uint>(0);
				var counter = criteriaResult.Read<ulong>(1);
				var date = criteriaResult.Read<long>(2);
				var guidLow = criteriaResult.Read<ulong>(3);

				var criteria = Global.CriteriaMgr.GetCriteria(id);

				if (criteria == null)
				{
					// we will remove not existed criteria for all guilds
					Log.Logger.Error("Non-existing achievement criteria {0} data removed from table `guild_achievement_progress`.", id);

					var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEV_PROGRESS_CRITERIA_GUILD);
					stmt.AddValue(0, id);
					DB.Characters.Execute(stmt);

					continue;
				}

				if (criteria.Entry.StartTimer != 0 && date + criteria.Entry.StartTimer < now)
					continue;

				CriteriaProgress progress = new()
				{
					Counter = counter,
					Date = date,
					PlayerGUID = ObjectGuid.Create(HighGuid.Player, guidLow),
					Changed = false
				};

				_criteriaProgress[id] = progress;
			} while (criteriaResult.NextRow());
		}
	}

	public void SaveToDB(SQLTransaction trans)
	{
		PreparedStatement stmt;
		StringBuilder guidstr = new();

		foreach (var pair in _completedAchievements)
		{
			if (!pair.Value.Changed)
				continue;

			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_ACHIEVEMENT);
			stmt.AddValue(0, _owner.GetId());
			stmt.AddValue(1, pair.Key);
			trans.Append(stmt);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_ACHIEVEMENT);
			stmt.AddValue(0, _owner.GetId());
			stmt.AddValue(1, pair.Key);
			stmt.AddValue(2, pair.Value.Date);

			foreach (var guid in pair.Value.CompletingPlayers)
				guidstr.AppendFormat("{0},", guid.Counter);

			stmt.AddValue(3, guidstr.ToString());
			trans.Append(stmt);

			guidstr.Clear();
		}

		foreach (var pair in _criteriaProgress)
		{
			if (!pair.Value.Changed)
				continue;

			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_ACHIEVEMENT_CRITERIA);
			stmt.AddValue(0, _owner.GetId());
			stmt.AddValue(1, pair.Key);
			trans.Append(stmt);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_ACHIEVEMENT_CRITERIA);
			stmt.AddValue(0, _owner.GetId());
			stmt.AddValue(1, pair.Key);
			stmt.AddValue(2, pair.Value.Counter);
			stmt.AddValue(3, pair.Value.Date);
			stmt.AddValue(4, pair.Value.PlayerGUID.Counter);
			trans.Append(stmt);
		}
	}

	public override void SendAllData(Player receiver)
	{
		AllGuildAchievements allGuildAchievements = new();

		foreach (var pair in _completedAchievements)
		{
			var achievement = VisibleAchievementCheck(pair);

			if (achievement == null)
				continue;

			EarnedAchievement earned = new()
			{
				Id = pair.Key,
				Date = pair.Value.Date
			};

			allGuildAchievements.Earned.Add(earned);
		}

		receiver.SendPacket(allGuildAchievements);
	}

	public void SendAchievementInfo(Player receiver, uint achievementId = 0)
	{
		GuildCriteriaUpdate guildCriteriaUpdate = new();
		var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

		if (achievement != null)
		{
			var tree = Global.CriteriaMgr.GetCriteriaTree(achievement.CriteriaTree);

			if (tree != null)
				CriteriaManager.WalkCriteriaTree(tree,
												node =>
												{
													if (node.Criteria != null)
													{
														var progress = _criteriaProgress.LookupByKey(node.Criteria.Id);

														if (progress != null)
														{
															GuildCriteriaProgress guildCriteriaProgress = new()
															{
																CriteriaID = node.Criteria.Id,
																DateCreated = 0,
																DateStarted = 0,
																DateUpdated = progress.Date,
																Quantity = progress.Counter,
																PlayerGUID = progress.PlayerGUID,
																Flags = 0
															};

															guildCriteriaUpdate.Progress.Add(guildCriteriaProgress);
														}
													}
												});
		}

		receiver.SendPacket(guildCriteriaUpdate);
	}

	public void SendAllTrackedCriterias(Player receiver, List<uint> trackedCriterias)
	{
		GuildCriteriaUpdate guildCriteriaUpdate = new();

		foreach (var criteriaId in trackedCriterias)
		{
			var progress = _criteriaProgress.LookupByKey(criteriaId);

			if (progress == null)
				continue;

			GuildCriteriaProgress guildCriteriaProgress = new()
			{
				CriteriaID = criteriaId,
				DateCreated = 0,
				DateStarted = 0,
				DateUpdated = progress.Date,
				Quantity = progress.Counter,
				PlayerGUID = progress.PlayerGUID,
				Flags = 0
			};

			guildCriteriaUpdate.Progress.Add(guildCriteriaProgress);
		}

		receiver.SendPacket(guildCriteriaUpdate);
	}

	public void SendAchievementMembers(Player receiver, uint achievementId)
	{
		var achievementData = _completedAchievements.LookupByKey(achievementId);

		if (achievementData != null)
		{
			GuildAchievementMembers guildAchievementMembers = new()
			{
				GuildGUID = _owner.GetGUID(),
				AchievementID = achievementId
			};

			foreach (var guid in achievementData.CompletingPlayers)
				guildAchievementMembers.Member.Add(guid);

			receiver.SendPacket(guildAchievementMembers);
		}
	}

	public override void CompletedAchievement(AchievementRecord achievement, Player referencePlayer)
	{
		Log.Logger.Debug("CompletedAchievement({0})", achievement.Id);

		if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter) || HasAchieved(achievement.Id))
			return;

		if (achievement.Flags.HasAnyFlag(AchievementFlags.ShowInGuildNews))
		{
			var guild = referencePlayer.Guild;

			if (guild)
				guild.AddGuildNews(GuildNews.Achievement, ObjectGuid.Empty, (uint)(achievement.Flags & AchievementFlags.ShowInGuildHeader), achievement.Id);
		}

		SendAchievementEarned(achievement);

		CompletedAchievementData ca = new()
		{
			Date = GameTime.GetGameTime(),
			Changed = true
		};

		if (achievement.Flags.HasAnyFlag(AchievementFlags.ShowGuildMembers))
		{
			if (referencePlayer.GuildId == _owner.GetId())
				ca.CompletingPlayers.Add(referencePlayer.GUID);

			var group = referencePlayer.Group;

			if (group)
				for (var refe = group.FirstMember; refe != null; refe = refe.Next())
				{
					var groupMember = refe.Source;

					if (groupMember)
						if (groupMember.GuildId == _owner.GetId())
							ca.CompletingPlayers.Add(groupMember.GUID);
				}
		}

		_completedAchievements[achievement.Id] = ca;

		if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
			Global.AchievementMgr.SetRealmCompleted(achievement);

		if (!achievement.Flags.HasAnyFlag(AchievementFlags.TrackingFlag))
			_achievementPoints += achievement.Points;

		UpdateCriteria(CriteriaType.EarnAchievement, achievement.Id, 0, 0, null, referencePlayer);
		UpdateCriteria(CriteriaType.EarnAchievementPoints, achievement.Points, 0, 0, null, referencePlayer);

		Global.ScriptMgr.RunScript<IAchievementOnCompleted>(p => p.OnCompleted(referencePlayer, achievement), Global.AchievementMgr.GetAchievementScriptId(achievement.Id));
	}

	public override void SendCriteriaUpdate(Criteria entry, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted)
	{
		GuildCriteriaUpdate guildCriteriaUpdate = new();

		GuildCriteriaProgress guildCriteriaProgress = new()
		{
			CriteriaID = entry.Id,
			DateCreated = 0,
			DateStarted = 0,
			DateUpdated = progress.Date,
			Quantity = progress.Counter,
			PlayerGUID = progress.PlayerGUID,
			Flags = 0
		};

		guildCriteriaUpdate.Progress.Add(guildCriteriaProgress);

		_owner.BroadcastPacketIfTrackingAchievement(guildCriteriaUpdate, entry.Id);
	}

	public override void SendCriteriaProgressRemoved(uint criteriaId)
	{
		GuildCriteriaDeleted guildCriteriaDeleted = new()
		{
			GuildGUID = _owner.GetGUID(),
			CriteriaID = criteriaId
		};

		SendPacket(guildCriteriaDeleted);
	}

	public override void SendPacket(ServerPacket data)
	{
		_owner.BroadcastPacket(data);
	}

	public override List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
	{
		return Global.CriteriaMgr.GetGuildCriteriaByType(type);
	}

	public override string GetOwnerInfo()
	{
		return $"Guild ID {_owner.GetId()} {_owner.GetName()}";
	}

    private void SendAchievementEarned(AchievementRecord achievement)
	{
		if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
		{
			// broadcast realm first reached
			BroadcastAchievement serverFirstAchievement = new()
			{
				Name = _owner.GetName(),
				PlayerGUID = _owner.GetGUID(),
				AchievementID = achievement.Id,
				GuildAchievement = true
			};

			Global.WorldMgr.SendGlobalMessage(serverFirstAchievement);
		}

		GuildAchievementEarned guildAchievementEarned = new()
		{
			AchievementID = achievement.Id,
			GuildGUID = _owner.GetGUID(),
			TimeEarned = GameTime.GetGameTime()
		};

		SendPacket(guildAchievementEarned);
	}
}
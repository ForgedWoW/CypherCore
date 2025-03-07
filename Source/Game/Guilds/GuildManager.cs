﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Guilds;

namespace Game;

public sealed class GuildManager : Singleton<GuildManager>
{
	readonly Dictionary<ulong, Guild> GuildStore = new();
	readonly List<GuildReward> guildRewards = new();

	uint NextGuildId;
	GuildManager() { }

	public void AddGuild(Guild guild)
	{
		GuildStore[guild.GetId()] = guild;
	}

	public void RemoveGuild(ulong guildId)
	{
		GuildStore.Remove(guildId);
	}

	public void SaveGuilds()
	{
		foreach (var guild in GuildStore.Values)
			guild.SaveToDB();
	}

	public uint GenerateGuildId()
	{
		if (NextGuildId >= 0xFFFFFFFE)
		{
			Log.outError(LogFilter.Guild, "Guild ids overflow!! Can't continue, shutting down server. ");
			Global.WorldMgr.StopNow();
		}

		return NextGuildId++;
	}

	public Guild GetGuildById(ulong guildId)
	{
		return GuildStore.LookupByKey(guildId);
	}

	public Guild GetGuildByGuid(ObjectGuid guid)
	{
		// Full guids are only used when receiving/sending data to client
		// everywhere else guild id is used
		if (guid.IsGuild)
		{
			var guildId = guid.Counter;

			if (guildId != 0)
				return GetGuildById(guildId);
		}

		return null;
	}

	public Guild GetGuildByName(string guildName)
	{
		foreach (var guild in GuildStore.Values)
			if (guildName == guild.GetName())
				return guild;

		return null;
	}

	public string GetGuildNameById(uint guildId)
	{
		var guild = GetGuildById(guildId);

		if (guild != null)
			return guild.GetName();

		return "";
	}

	public Guild GetGuildByLeader(ObjectGuid guid)
	{
		foreach (var guild in GuildStore.Values)
			if (guild.GetLeaderGUID() == guid)
				return guild;

		return null;
	}

	public void LoadGuilds()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading Guilds Definitions...");

		{
			var oldMSTime = Time.MSTime;

			//          0          1       2             3              4              5              6
			var result = DB.Characters.Query("SELECT g.guildid, g.name, g.leaderguid, g.EmblemStyle, g.EmblemColor, g.BorderStyle, g.BorderColor, " +
											//   7                  8       9       10            11          12
											"g.BackgroundColor, g.info, g.motd, g.createdate, g.BankMoney, COUNT(gbt.guildid) " +
											"FROM guild g LEFT JOIN guild_bank_tab gbt ON g.guildid = gbt.guildid GROUP BY g.guildid ORDER BY g.guildid ASC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.Guild, "Loaded 0 guild definitions. DB table `guild` is empty.");

				return;
			}

			uint count = 0;

			do
			{
				Guild guild = new();

				if (!guild.LoadFromDB(result.GetFields()))
					continue;

				AddGuild(guild);
				count++;
			} while (result.NextRow());

			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}

		Log.outInfo(LogFilter.ServerLoading, "Loading guild ranks...");

		{
			var oldMSTime = Time.MSTime;

			// Delete orphaned guild rank entries before loading the valid ones
			DB.Characters.DirectExecute("DELETE gr FROM guild_rank gr LEFT JOIN guild g ON gr.guildId = g.guildId WHERE g.guildId IS NULL");

			//                                                   0    1      2       3      4       5
			var result = DB.Characters.Query("SELECT guildid, rid, RankOrder, rname, rights, BankMoneyPerDay FROM guild_rank ORDER BY guildid ASC, rid ASC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild ranks. DB table `guild_rank` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<uint>(0);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadRankFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild ranks in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 3. Load all guild members
		Log.outInfo(LogFilter.ServerLoading, "Loading guild members...");

		{
			var oldMSTime = Time.MSTime;

			// Delete orphaned guild member entries before loading the valid ones
			DB.Characters.DirectExecute("DELETE gm FROM guild_member gm LEFT JOIN guild g ON gm.guildId = g.guildId WHERE g.guildId IS NULL");
			DB.Characters.DirectExecute("DELETE gm FROM guild_member_withdraw gm LEFT JOIN guild_member g ON gm.guid = g.guid WHERE g.guid IS NULL");

			//           0           1        2     3      4        5       6       7       8       9       10
			var result = DB.Characters.Query("SELECT gm.guildid, gm.guid, `rank`, pnote, offnote, w.tab0, w.tab1, w.tab2, w.tab3, w.tab4, w.tab5, " +
											//  11      12      13       14      15       16      17       18        19      20         21
											"w.tab6, w.tab7, w.money, c.name, c.level, c.race, c.class, c.gender, c.zone, c.account, c.logout_time " +
											"FROM guild_member gm LEFT JOIN guild_member_withdraw w ON gm.guid = w.guid " +
											"LEFT JOIN characters c ON c.guid = gm.guid ORDER BY gm.guildid ASC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild members. DB table `guild_member` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<uint>(0);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadMemberFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild members in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 4. Load all guild bank tab rights
		Log.outInfo(LogFilter.ServerLoading, "Loading bank tab rights...");

		{
			var oldMSTime = Time.MSTime;

			// Delete orphaned guild bank right entries before loading the valid ones
			DB.Characters.DirectExecute("DELETE gbr FROM guild_bank_right gbr LEFT JOIN guild g ON gbr.guildId = g.guildId WHERE g.guildId IS NULL");

			//      0        1      2    3        4
			var result = DB.Characters.Query("SELECT guildid, TabId, rid, gbright, SlotPerDay FROM guild_bank_right ORDER BY guildid ASC, TabId ASC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild bank tab rights. DB table `guild_bank_right` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<uint>(0);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadBankRightFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} bank tab rights in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 5. Load all event logs
		Log.outInfo(LogFilter.ServerLoading, "Loading guild event logs...");

		{
			var oldMSTime = Time.MSTime;

			DB.Characters.DirectExecute("DELETE FROM guild_eventlog WHERE LogGuid > {0}", GuildConst.EventLogMaxRecords);

			//          0        1        2          3            4            5        6
			var result = DB.Characters.Query("SELECT guildid, LogGuid, EventType, PlayerGuid1, PlayerGuid2, NewRank, TimeStamp FROM guild_eventlog ORDER BY TimeStamp DESC, LogGuid DESC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild event logs. DB table `guild_eventlog` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<uint>(0);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadEventLogFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild event logs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 6. Load all bank event logs
		Log.outInfo(LogFilter.ServerLoading, "Loading guild bank event logs...");

		{
			var oldMSTime = Time.MSTime;

			// Remove log entries that exceed the number of allowed entries per guild
			DB.Characters.DirectExecute("DELETE FROM guild_bank_eventlog WHERE LogGuid > {0}", GuildConst.BankLogMaxRecords);

			//          0        1      2        3          4           5            6               7          8
			var result = DB.Characters.Query("SELECT guildid, TabId, LogGuid, EventType, PlayerGuid, ItemOrMoney, ItemStackCount, DestTabId, TimeStamp FROM guild_bank_eventlog ORDER BY TimeStamp DESC, LogGuid DESC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild bank event logs. DB table `guild_bank_eventlog` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<uint>(0);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadBankEventLogFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild bank event logs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 7. Load all news event logs
		Log.outInfo(LogFilter.ServerLoading, "Loading Guild News...");

		{
			var oldMSTime = Time.MSTime;

			DB.Characters.DirectExecute("DELETE FROM guild_newslog WHERE LogGuid > {0}", GuildConst.NewsLogMaxRecords);

			//      0        1        2          3           4      5      6
			var result = DB.Characters.Query("SELECT guildid, LogGuid, EventType, PlayerGuid, Flags, Value, Timestamp FROM guild_newslog ORDER BY TimeStamp DESC, LogGuid DESC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild event logs. DB table `guild_newslog` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<uint>(0);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadGuildNewsLogFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild new logs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 8. Load all guild bank tabs
		Log.outInfo(LogFilter.ServerLoading, "Loading guild bank tabs...");

		{
			var oldMSTime = Time.MSTime;

			// Delete orphaned guild bank tab entries before loading the valid ones
			DB.Characters.DirectExecute("DELETE gbt FROM guild_bank_tab gbt LEFT JOIN guild g ON gbt.guildId = g.guildId WHERE g.guildId IS NULL");

			//                                              0        1      2        3        4
			var result = DB.Characters.Query("SELECT guildid, TabId, TabName, TabIcon, TabText FROM guild_bank_tab ORDER BY guildid ASC, TabId ASC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild bank tabs. DB table `guild_bank_tab` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<uint>(0);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadBankTabFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild bank tabs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 9. Fill all guild bank tabs
		Log.outInfo(LogFilter.ServerLoading, "Filling bank tabs with items...");

		{
			var oldMSTime = Time.MSTime;

			// Delete orphan guild bank items
			DB.Characters.DirectExecute("DELETE gbi FROM guild_bank_item gbi LEFT JOIN guild g ON gbi.guildId = g.guildId WHERE g.guildId IS NULL");

			var result = DB.Characters.Query(DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_ITEMS));

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild bank tab items. DB table `guild_bank_item` or `item_instance` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guildId = result.Read<ulong>(51);
					var guild = GetGuildById(guildId);

					if (guild)
						guild.LoadBankItemFromDB(result.GetFields());

					++count;
				} while (result.NextRow());

				Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild bank tab items in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// 10. Load guild achievements
		Log.outInfo(LogFilter.ServerLoading, "Loading guild achievements...");

		{
			var oldMSTime = Time.MSTime;

			foreach (var pair in GuildStore)
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_ACHIEVEMENT);
				stmt.AddValue(0, pair.Key);
				var achievementResult = DB.Characters.Query(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_ACHIEVEMENT_CRITERIA);
				stmt.AddValue(0, pair.Key);
				var criteriaResult = DB.Characters.Query(stmt);

				pair.Value.GetAchievementMgr().LoadFromDB(achievementResult, criteriaResult);
			}

			Log.outInfo(LogFilter.ServerLoading, "Loaded guild achievements and criterias in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
		}

		// 11. Validate loaded guild data
		Log.outInfo(LogFilter.Server, "Validating data of loaded guilds...");

		{
			var oldMSTime = Time.MSTime;

			foreach (var guild in GuildStore.ToList())
				if (!guild.Value.Validate())
					GuildStore.Remove(guild.Key);

			Log.outInfo(LogFilter.ServerLoading, "Validated data of loaded guilds in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
		}
	}

	public void LoadGuildRewards()
	{
		var oldMSTime = Time.MSTime;

		//                                            0      1            2         3
		var result = DB.World.Query("SELECT ItemID, MinGuildRep, RaceMask, Cost FROM guild_rewards");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild reward definitions. DB table `guild_rewards` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			GuildReward reward = new();

			reward.ItemID = result.Read<uint>(0);
			reward.MinGuildRep = result.Read<byte>(1);
			reward.RaceMask = result.Read<ulong>(2);
			reward.Cost = result.Read<ulong>(3);

			if (Global.ObjectMgr.GetItemTemplate(reward.ItemID) == null)
			{
				Log.outError(LogFilter.ServerLoading, "Guild rewards constains not existing item entry {0}", reward.ItemID);

				continue;
			}

			if (reward.MinGuildRep >= (int)ReputationRank.Max)
			{
				Log.outError(LogFilter.ServerLoading, "Guild rewards contains wrong reputation standing {0}, max is {1}", reward.MinGuildRep, (int)ReputationRank.Max - 1);

				continue;
			}

			var stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_GUILD_REWARDS_REQ_ACHIEVEMENTS);
			stmt.AddValue(0, reward.ItemID);
			var reqAchievementResult = DB.World.Query(stmt);

			if (!reqAchievementResult.IsEmpty())
				do
				{
					var requiredAchievementId = reqAchievementResult.Read<uint>(0);

					if (!CliDB.AchievementStorage.ContainsKey(requiredAchievementId))
					{
						Log.outError(LogFilter.ServerLoading, "Guild rewards constains not existing achievement entry {0}", requiredAchievementId);

						continue;
					}

					reward.AchievementsRequired.Add(requiredAchievementId);
				} while (reqAchievementResult.NextRow());

			guildRewards.Add(reward);
			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild reward definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void ResetTimes(bool week)
	{
		DB.Characters.Execute(DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBER_WITHDRAW));

		foreach (var guild in GuildStore.Values)
			guild.ResetTimes(week);
	}

	public void SetNextGuildId(uint Id)
	{
		NextGuildId = Id;
	}

	public List<GuildReward> GetGuildRewards()
	{
		return guildRewards;
	}
}

public class GuildReward
{
	public uint ItemID;
	public byte MinGuildRep;
	public ulong RaceMask;
	public ulong Cost;
	public List<uint> AchievementsRequired = new();
}
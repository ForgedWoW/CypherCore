// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals.Caching;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Guilds;

public sealed class GuildManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly CliDB _cliDB;
    private readonly List<GuildReward> _guildRewards = new();
    private readonly Dictionary<ulong, Guild> _guildStore = new();
    private readonly WorldDatabase _worldDatabase;
    private uint _nextGuildId;

    public GuildManager(CliDB cliDB, CharacterDatabase characterDatabase, WorldDatabase worldDatabase, ClassFactory classFactory, ItemTemplateCache itemTemplateCache)
    {
        _cliDB = cliDB;
        _characterDatabase = characterDatabase;
        _worldDatabase = worldDatabase;
        _classFactory = classFactory;
        _itemTemplateCache = itemTemplateCache;
    }

    public void AddGuild(Guild guild)
    {
        _guildStore[guild.GetId()] = guild;
    }

    public uint GenerateGuildId()
    {
        return _nextGuildId++;
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

    public Guild GetGuildById(ulong guildId)
    {
        return _guildStore.LookupByKey(guildId);
    }

    public Guild GetGuildByLeader(ObjectGuid guid)
    {
        return _guildStore.Values.FirstOrDefault(guild => guild.GetLeaderGUID() == guid);
    }

    public Guild GetGuildByName(string guildName)
    {
        return _guildStore.Values.FirstOrDefault(guild => guildName == guild.GetName());
    }

    public string GetGuildNameById(uint guildId)
    {
        var guild = GetGuildById(guildId);

        return guild != null ? guild.GetName() : "";
    }

    public List<GuildReward> GetGuildRewards()
    {
        return _guildRewards;
    }

    public void LoadGuildRewards()
    {
        var oldMSTime = Time.MSTime;

        //                                            0      1            2         3
        var result = _worldDatabase.Query("SELECT ItemID, MinGuildRep, RaceMask, Cost FROM guild_rewards");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 guild reward definitions. DB table `guild_rewards` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            GuildReward reward = new()
            {
                ItemID = result.Read<uint>(0),
                MinGuildRep = result.Read<byte>(1),
                RaceMask = result.Read<ulong>(2),
                Cost = result.Read<ulong>(3)
            };

            if (_itemTemplateCache.GetItemTemplate(reward.ItemID) == null)
            {
                Log.Logger.Error("Guild rewards constains not existing item entry {0}", reward.ItemID);

                continue;
            }

            if (reward.MinGuildRep >= (int)ReputationRank.Max)
            {
                Log.Logger.Error("Guild rewards contains wrong reputation standing {0}, max is {1}", reward.MinGuildRep, (int)ReputationRank.Max - 1);

                continue;
            }

            var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_GUILD_REWARDS_REQ_ACHIEVEMENTS);
            stmt.AddValue(0, reward.ItemID);
            var reqAchievementResult = _worldDatabase.Query(stmt);

            if (!reqAchievementResult.IsEmpty())
                do
                {
                    var requiredAchievementId = reqAchievementResult.Read<uint>(0);

                    if (!_cliDB.AchievementStorage.ContainsKey(requiredAchievementId))
                    {
                        Log.Logger.Error("Guild rewards constains not existing achievement entry {0}", requiredAchievementId);

                        continue;
                    }

                    reward.AchievementsRequired.Add(requiredAchievementId);
                } while (reqAchievementResult.NextRow());

            _guildRewards.Add(reward);
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} guild reward definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGuilds()
    {
        Log.Logger.Information("Loading Guilds Definitions...");

        {
            var oldMSTime = Time.MSTime;

            //          0          1       2             3              4              5              6
            var result = _characterDatabase.Query("SELECT g.guildid, g.name, g.leaderguid, g.EmblemStyle, g.EmblemColor, g.BorderStyle, g.BorderColor, " +
                                                  //   7                  8       9       10            11          12
                                                  "g.BackgroundColor, g.info, g.motd, g.createdate, g.BankMoney, COUNT(gbt.guildid) " +
                                                  "FROM guild g LEFT JOIN guild_bank_tab gbt ON g.guildid = gbt.guildid GROUP BY g.guildid ORDER BY g.guildid ASC");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 guild definitions. DB table `guild` is empty.");

                return;
            }

            uint count = 0;

            do
            {
                var guild = _classFactory.Resolve<Guild>();

                if (!guild.LoadFromDB(result.GetFields()))
                    continue;

                AddGuild(guild);
                count++;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} guild definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        Log.Logger.Information("Loading guild ranks...");

        {
            var oldMSTime = Time.MSTime;

            // Delete orphaned guild rank entries before loading the valid ones
            _characterDatabase.DirectExecute("DELETE gr FROM guild_rank gr LEFT JOIN guild g ON gr.guildId = g.guildId WHERE g.guildId IS NULL");

            //                                                   0    1      2       3      4       5
            var result = _characterDatabase.Query("SELECT guildid, rid, RankOrder, rname, rights, BankMoneyPerDay FROM guild_rank ORDER BY guildid ASC, rid ASC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild ranks. DB table `guild_rank` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<uint>(0);
                    var guild = GetGuildById(guildId);

                    guild?.LoadRankFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} guild ranks in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 3. Load all guild members
        Log.Logger.Information("Loading guild members...");

        {
            var oldMSTime = Time.MSTime;

            // Delete orphaned guild member entries before loading the valid ones
            _characterDatabase.DirectExecute("DELETE gm FROM guild_member gm LEFT JOIN guild g ON gm.guildId = g.guildId WHERE g.guildId IS NULL");
            _characterDatabase.DirectExecute("DELETE gm FROM guild_member_withdraw gm LEFT JOIN guild_member g ON gm.guid = g.guid WHERE g.guid IS NULL");

            //           0           1        2     3      4        5       6       7       8       9       10
            var result = _characterDatabase.Query("SELECT gm.guildid, gm.guid, `rank`, pnote, offnote, w.tab0, w.tab1, w.tab2, w.tab3, w.tab4, w.tab5, " +
                                                  //  11      12      13       14      15       16      17       18        19      20         21
                                                  "w.tab6, w.tab7, w.money, c.name, c.level, c.race, c.class, c.gender, c.zone, c.account, c.logout_time " +
                                                  "FROM guild_member gm LEFT JOIN guild_member_withdraw w ON gm.guid = w.guid " +
                                                  "LEFT JOIN characters c ON c.guid = gm.guid ORDER BY gm.guildid ASC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild members. DB table `guild_member` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<uint>(0);
                    var guild = GetGuildById(guildId);

                    guild?.LoadMemberFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} guild members in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 4. Load all guild bank tab rights
        Log.Logger.Information("Loading bank tab rights...");

        {
            var oldMSTime = Time.MSTime;

            // Delete orphaned guild bank right entries before loading the valid ones
            _characterDatabase.DirectExecute("DELETE gbr FROM guild_bank_right gbr LEFT JOIN guild g ON gbr.guildId = g.guildId WHERE g.guildId IS NULL");

            //      0        1      2    3        4
            var result = _characterDatabase.Query("SELECT guildid, TabId, rid, gbright, SlotPerDay FROM guild_bank_right ORDER BY guildid ASC, TabId ASC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild bank tab rights. DB table `guild_bank_right` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<uint>(0);
                    var guild = GetGuildById(guildId);

                    guild?.LoadBankRightFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} bank tab rights in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 5. Load all event logs
        Log.Logger.Information("Loading guild event logs...");

        {
            var oldMSTime = Time.MSTime;

            _characterDatabase.DirectExecute("DELETE FROM guild_eventlog WHERE LogGuid > {0}", GuildConst.EventLogMaxRecords);

            //          0        1        2          3            4            5        6
            var result = _characterDatabase.Query("SELECT guildid, LogGuid, EventType, PlayerGuid1, PlayerGuid2, NewRank, TimeStamp FROM guild_eventlog ORDER BY TimeStamp DESC, LogGuid DESC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild event logs. DB table `guild_eventlog` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<uint>(0);
                    var guild = GetGuildById(guildId);

                    guild?.LoadEventLogFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} guild event logs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 6. Load all bank event logs
        Log.Logger.Information("Loading guild bank event logs...");

        {
            var oldMSTime = Time.MSTime;

            // Remove log entries that exceed the number of allowed entries per guild
            _characterDatabase.DirectExecute("DELETE FROM guild_bank_eventlog WHERE LogGuid > {0}", GuildConst.BankLogMaxRecords);

            //          0        1      2        3          4           5            6               7          8
            var result = _characterDatabase.Query("SELECT guildid, TabId, LogGuid, EventType, PlayerGuid, ItemOrMoney, ItemStackCount, DestTabId, TimeStamp FROM guild_bank_eventlog ORDER BY TimeStamp DESC, LogGuid DESC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild bank event logs. DB table `guild_bank_eventlog` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<uint>(0);
                    var guild = GetGuildById(guildId);

                    guild?.LoadBankEventLogFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} guild bank event logs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 7. Load all news event logs
        Log.Logger.Information("Loading Guild News...");

        {
            var oldMSTime = Time.MSTime;

            _characterDatabase.DirectExecute("DELETE FROM guild_newslog WHERE LogGuid > {0}", GuildConst.NewsLogMaxRecords);

            //      0        1        2          3           4      5      6
            var result = _characterDatabase.Query("SELECT guildid, LogGuid, EventType, PlayerGuid, Flags, Value, Timestamp FROM guild_newslog ORDER BY TimeStamp DESC, LogGuid DESC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild event logs. DB table `guild_newslog` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<uint>(0);
                    var guild = GetGuildById(guildId);

                    guild?.LoadGuildNewsLogFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} guild new logs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 8. Load all guild bank tabs
        Log.Logger.Information("Loading guild bank tabs...");

        {
            var oldMSTime = Time.MSTime;

            // Delete orphaned guild bank tab entries before loading the valid ones
            _characterDatabase.DirectExecute("DELETE gbt FROM guild_bank_tab gbt LEFT JOIN guild g ON gbt.guildId = g.guildId WHERE g.guildId IS NULL");

            //                                              0        1      2        3        4
            var result = _characterDatabase.Query("SELECT guildid, TabId, TabName, TabIcon, TabText FROM guild_bank_tab ORDER BY guildid ASC, TabId ASC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild bank tabs. DB table `guild_bank_tab` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<uint>(0);
                    var guild = GetGuildById(guildId);

                    guild?.LoadBankTabFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} guild bank tabs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 9. Fill all guild bank tabs
        Log.Logger.Information("Filling bank tabs with items...");

        {
            var oldMSTime = Time.MSTime;

            // Delete orphan guild bank items
            _characterDatabase.DirectExecute("DELETE gbi FROM guild_bank_item gbi LEFT JOIN guild g ON gbi.guildId = g.guildId WHERE g.guildId IS NULL");

            var result = _characterDatabase.Query(_characterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_ITEMS));

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 guild bank tab items. DB table `guild_bank_item` or `item_instance` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guildId = result.Read<ulong>(51);
                    var guild = GetGuildById(guildId);

                    guild?.LoadBankItemFromDB(result.GetFields());

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} guild bank tab items in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // 10. Load guild achievements
        Log.Logger.Information("Loading guild achievements...");

        {
            var oldMSTime = Time.MSTime;

            foreach (var pair in _guildStore)
            {
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_ACHIEVEMENT);
                stmt.AddValue(0, pair.Key);
                var achievementResult = _characterDatabase.Query(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_ACHIEVEMENT_CRITERIA);
                stmt.AddValue(0, pair.Key);
                var criteriaResult = _characterDatabase.Query(stmt);

                pair.Value.GetAchievementMgr().LoadFromDB(achievementResult, criteriaResult);
            }

            Log.Logger.Information("Loaded guild achievements and criterias in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        // 11. Validate loaded guild data
        Log.Logger.Information("Validating data of loaded guilds...");

        {
            var oldMSTime = Time.MSTime;

            foreach (var guild in _guildStore.ToList())
                if (!guild.Value.Validate())
                    _guildStore.Remove(guild.Key);

            Log.Logger.Information("Validated data of loaded guilds in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }
    }

    public void RemoveGuild(ulong guildId)
    {
        _guildStore.Remove(guildId);
    }

    public void ResetTimes(bool week)
    {
        _characterDatabase.Execute(_characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBER_WITHDRAW));

        foreach (var guild in _guildStore.Values)
            guild.ResetTimes(week);
    }

    public void SaveGuilds()
    {
        foreach (var guild in _guildStore.Values)
            guild.SaveToDB();
    }

    public void SetNextGuildId(uint id)
    {
        _nextGuildId = id;
    }
}
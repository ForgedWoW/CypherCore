// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Arenas;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Guilds;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Achievements;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAchievement;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Achievements;

public class GuildAchievementMgr : AchievementManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly Guild _owner;
    private readonly ScriptManager _scriptManager;

    public GuildAchievementMgr(Guild owner, ScriptManager scriptManager, CharacterDatabase characterDatabase, CriteriaManager criteriaManager, WorldManager worldManager, GameObjectManager gameObjectManager, SpellManager spellManager, ArenaTeamManager arenaTeamManager,
                               DisableManager disableManager, WorldStateManager worldStateManager, CliDB cliDB, ConditionManager conditionManager, RealmManager realmManager, IConfiguration configuration,
                               LanguageManager languageManager, DB2Manager db2Manager, MapManager mapManager, AchievementGlobalMgr achievementManager, PhasingHandler phasingHandler, ItemTemplateCache itemTemplateCache) :
        base(criteriaManager, worldManager, gameObjectManager, spellManager, arenaTeamManager, disableManager, worldStateManager, cliDB, conditionManager, realmManager, configuration, languageManager, db2Manager, mapManager, achievementManager, phasingHandler, itemTemplateCache)
    {
        _owner = owner;
        _scriptManager = scriptManager;
        _characterDatabase = characterDatabase;
    }

    public override void CompletedAchievement(AchievementRecord achievement, Player referencePlayer)
    {
        Log.Logger.Debug("CompletedAchievement({0})", achievement.Id);

        if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter) || HasAchieved(achievement.Id))
            return;

        if (achievement.Flags.HasAnyFlag(AchievementFlags.ShowInGuildNews))
            referencePlayer.Guild?.AddGuildNews(GuildNews.Achievement, ObjectGuid.Empty, (uint)(achievement.Flags & AchievementFlags.ShowInGuildHeader), achievement.Id);

        SendAchievementEarned(achievement);

        CompletedAchievementData ca = new()
        {
            Date = GameTime.CurrentTime,
            Changed = true
        };

        if (achievement.Flags.HasAnyFlag(AchievementFlags.ShowGuildMembers))
        {
            if (referencePlayer.GuildId == _owner.GetId())
                ca.CompletingPlayers.Add(referencePlayer.GUID);

            if (referencePlayer.Group != null)
                for (var refe = referencePlayer.Group.FirstMember; refe != null; refe = refe.Next())
                {
                    var groupMember = refe.Source;

                    if (groupMember == null)
                        continue;

                    if (groupMember.GuildId == _owner.GetId())
                        ca.CompletingPlayers.Add(groupMember.GUID);
                }
        }

        CompletedAchievements[achievement.Id] = ca;

        if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            AchievementManager.SetRealmCompleted(achievement);

        if (!achievement.Flags.HasAnyFlag(AchievementFlags.TrackingFlag))
            AchievementPoints += achievement.Points;

        UpdateCriteria(CriteriaType.EarnAchievement, achievement.Id, 0, 0, null, referencePlayer);
        UpdateCriteria(CriteriaType.EarnAchievementPoints, achievement.Points, 0, 0, null, referencePlayer);

        _scriptManager.RunScript<IAchievementOnCompleted>(p => p.OnCompleted(referencePlayer, achievement), AchievementManager.GetAchievementScriptId(achievement.Id));
    }

    public void DeleteFromDB(ObjectGuid guid)
    {
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENTS);
        stmt.AddValue(0, guid.Counter);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENT_CRITERIA);
        stmt.AddValue(0, guid.Counter);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);
    }

    public override List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
    {
        return CriteriaManager.GetGuildCriteriaByType(type);
    }

    public override string GetOwnerInfo()
    {
        return $"Guild ID {_owner.GetId()} {_owner.GetName()}";
    }

    public void LoadFromDB(SQLResult achievementResult, SQLResult criteriaResult)
    {
        if (!achievementResult.IsEmpty())
            do
            {
                var achievementid = achievementResult.Read<uint>(0);

                // must not happen: cleanup at server startup in sAchievementMgr.LoadCompletedAchievements()
                if (!CliDB.AchievementStorage.TryGetValue(achievementid, out var achievement))
                    continue;

                if (CompletedAchievements.TryGetValue(achievementid, out var ca))
                {
                    ca.Date = achievementResult.Read<long>(1);
                    var guids = new StringArray(achievementResult.Read<string>(2), ',');

                    if (!guids.IsEmpty())
                        for (var i = 0; i < guids.Length; ++i)
                            if (ulong.TryParse(guids[i], out var guid))
                                ca.CompletingPlayers.Add(ObjectGuid.Create(HighGuid.Player, guid));

                    ca.Changed = false;

                    AchievementPoints += achievement.Points;
                }
            } while (achievementResult.NextRow());

        if (!criteriaResult.IsEmpty())
        {
            var now = GameTime.CurrentTime;

            do
            {
                var id = criteriaResult.Read<uint>(0);
                var counter = criteriaResult.Read<ulong>(1);
                var date = criteriaResult.Read<long>(2);
                var guidLow = criteriaResult.Read<ulong>(3);

                var criteria = CriteriaManager.GetCriteria(id);

                if (criteria == null)
                {
                    // we will remove not existed criteria for all guilds
                    Log.Logger.Error("Non-existing achievement criteria {0} data removed from table `guild_achievement_progress`.", id);

                    var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEV_PROGRESS_CRITERIA_GUILD);
                    stmt.AddValue(0, id);
                    _characterDatabase.Execute(stmt);

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

                CriteriaProgress[id] = progress;
            } while (criteriaResult.NextRow());
        }
    }

    public override void Reset()
    {
        base.Reset();

        var guid = _owner.GetGUID();

        foreach (var iter in CompletedAchievements)
        {
            GuildAchievementDeleted guildAchievementDeleted = new()
            {
                AchievementID = iter.Key,
                GuildGUID = guid,
                TimeDeleted = GameTime.CurrentTime
            };

            SendPacket(guildAchievementDeleted);
        }

        AchievementPoints = 0;
        CompletedAchievements.Clear();
        DeleteFromDB(guid);
    }

    public void SaveToDB(SQLTransaction trans)
    {
        PreparedStatement stmt;
        StringBuilder guidstr = new();

        foreach (var pair in CompletedAchievements)
        {
            if (!pair.Value.Changed)
                continue;

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_ACHIEVEMENT);
            stmt.AddValue(0, _owner.GetId());
            stmt.AddValue(1, pair.Key);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_ACHIEVEMENT);
            stmt.AddValue(0, _owner.GetId());
            stmt.AddValue(1, pair.Key);
            stmt.AddValue(2, pair.Value.Date);

            foreach (var guid in pair.Value.CompletingPlayers)
                guidstr.Append($"{guid.Counter},");

            stmt.AddValue(3, guidstr.ToString());
            trans.Append(stmt);

            guidstr.Clear();
        }

        foreach (var pair in CriteriaProgress)
        {
            if (!pair.Value.Changed)
                continue;

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_ACHIEVEMENT_CRITERIA);
            stmt.AddValue(0, _owner.GetId());
            stmt.AddValue(1, pair.Key);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_ACHIEVEMENT_CRITERIA);
            stmt.AddValue(0, _owner.GetId());
            stmt.AddValue(1, pair.Key);
            stmt.AddValue(2, pair.Value.Counter);
            stmt.AddValue(3, pair.Value.Date);
            stmt.AddValue(4, pair.Value.PlayerGUID.Counter);
            trans.Append(stmt);
        }
    }

    public void SendAchievementInfo(Player receiver, uint achievementId = 0)
    {
        GuildCriteriaUpdate guildCriteriaUpdate = new();

        if (CliDB.AchievementStorage.TryGetValue(achievementId, out var achievement))
        {
            var tree = CriteriaManager.GetCriteriaTree(achievement.CriteriaTree);

            if (tree != null)
                CriteriaManager.WalkCriteriaTree(tree,
                                                 node =>
                                                 {
                                                     if (node.Criteria == null)
                                                         return;

                                                     if (!CriteriaProgress.TryGetValue(node.Criteria.Id, out var progress))
                                                         return;

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
                                                 });
        }

        receiver.SendPacket(guildCriteriaUpdate);
    }

    public void SendAchievementMembers(Player receiver, uint achievementId)
    {
        if (CompletedAchievements.TryGetValue(achievementId, out var achievementData))
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

    public override void SendAllData(Player receiver)
    {
        AllGuildAchievements allGuildAchievements = new();

        foreach (var pair in CompletedAchievements)
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

    public void SendAllTrackedCriterias(Player receiver, List<uint> trackedCriterias)
    {
        GuildCriteriaUpdate guildCriteriaUpdate = new();

        foreach (var criteriaId in trackedCriterias)
        {
            if (!CriteriaProgress.TryGetValue(criteriaId, out var progress))
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

    public override void SendCriteriaProgressRemoved(uint criteriaId)
    {
        GuildCriteriaDeleted guildCriteriaDeleted = new()
        {
            GuildGUID = _owner.GetGUID(),
            CriteriaID = criteriaId
        };

        SendPacket(guildCriteriaDeleted);
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

    public override void SendPacket(ServerPacket data)
    {
        _owner.BroadcastPacket(data);
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

            WorldManager.SendGlobalMessage(serverFirstAchievement);
        }

        GuildAchievementEarned guildAchievementEarned = new()
        {
            AchievementID = achievement.Id,
            GuildGUID = _owner.GetGUID(),
            TimeEarned = GameTime.CurrentTime
        };

        SendPacket(guildAchievementEarned);
    }
}
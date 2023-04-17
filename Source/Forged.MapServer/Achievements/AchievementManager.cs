﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Arenas;
using Forged.MapServer.Chat;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Achievements;

public class AchievementManager : CriteriaHandler
{
    public Func<KeyValuePair<uint, CompletedAchievementData>, AchievementRecord> VisibleAchievementCheck;

    protected Dictionary<uint, CompletedAchievementData> CompletedAchievements = new();

    public AchievementManager(CriteriaManager criteriaManager, WorldManager worldManager, GameObjectManager gameObjectManager, SpellManager spellManager, ArenaTeamManager arenaTeamManager,
                              DisableManager disableManager, WorldStateManager worldStateManager, CliDB cliDB, ConditionManager conditionManager, RealmManager realmManager, IConfiguration configuration,
                              LanguageManager languageManager, DB2Manager db2Manager, MapManager mapManager, AchievementGlobalMgr achievementManager) :
        base(criteriaManager, worldManager, gameObjectManager, spellManager, arenaTeamManager, disableManager, worldStateManager, cliDB, conditionManager, realmManager, configuration, languageManager, db2Manager, mapManager, achievementManager) { }

    public uint AchievementPoints { get; protected set; }

    public ICollection<uint> CompletedAchievementIds => CompletedAchievements.Keys;

    public override void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer)
    {
        var achievement = tree.Achievement;

        if (achievement == null)
            return;

        // check again the completeness for SUMM and REQ COUNT achievements,
        // as they don't depend on the completed criteria but on the sum of the progress of each individual criteria
        if (achievement.Flags.HasAnyFlag(AchievementFlags.Summ))
            if (IsCompletedAchievement(achievement))
                CompletedAchievement(achievement, referencePlayer);

        var achRefList = AchievementManager.GetAchievementByReferencedId(achievement.Id);

        foreach (var refAchievement in achRefList)
            if (IsCompletedAchievement(refAchievement))
                CompletedAchievement(refAchievement, referencePlayer);
    }

    public override bool CanCompleteCriteriaTree(CriteriaTree tree)
    {
        var achievement = tree.Achievement;

        if (achievement == null)
            return false;

        // counter can never complete
        if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter))
            return false;

        if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            // someone on this realm has already completed that achievement
            if (AchievementManager.IsRealmCompleted(achievement))
                return false;

        return true;
    }

    public override bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
    {
        var achievement = tree.Achievement;

        if (achievement == null)
            return false;

        if (HasAchieved(achievement.Id))
        {
            Log.Logger.Verbose("CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Achievement already earned",
                               criteria.Id,
                               criteria.Entry.Type,
                               achievement.Id);

            return false;
        }

        if (achievement.InstanceID != -1 && referencePlayer.Location.MapId != achievement.InstanceID)
        {
            Log.Logger.Verbose("CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Wrong map",
                               criteria.Id,
                               criteria.Entry.Type,
                               achievement.Id);

            return false;
        }

        if ((achievement.Faction == AchievementFaction.Horde && referencePlayer.Team != TeamFaction.Horde) ||
            (achievement.Faction == AchievementFaction.Alliance && referencePlayer.Team != TeamFaction.Alliance))
        {
            Log.Logger.Verbose("CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Wrong faction",
                               criteria.Id,
                               criteria.Entry.Type,
                               achievement.Id);

            return false;
        }

        // Don't update realm first achievements if the player's account isn't allowed to do so
        if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            if (referencePlayer.Session.HasPermission(RBACPermissions.CannotEarnRealmFirstAchievements))
                return false;

        if (achievement.CovenantID != 0 && referencePlayer.PlayerData.CovenantID != achievement.CovenantID)
        {
            Log.Logger.Verbose($"CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Achievement {achievement.Id}) Wrong covenant");

            return false;
        }

        return base.CanUpdateCriteriaTree(criteria, tree, referencePlayer);
    }

    /// <summary>
    ///     called at player login. The player might have fulfilled some achievements when the achievement system wasn't working yet
    /// </summary>
    /// <param name="referencePlayer"> </param>
    public void CheckAllAchievementCriteria(Player referencePlayer)
    {
        // suppress sending packets
        for (CriteriaType i = 0; i < CriteriaType.Count; ++i)
            UpdateCriteria(i, 0, 0, 0, null, referencePlayer);
    }

    public virtual void CompletedAchievement(AchievementRecord entry, Player referencePlayer) { }

    public override void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer)
    {
        var achievement = tree.Achievement;

        if (achievement == null)
            return;

        // counter can never complete
        if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter))
            return;

        // already completed and stored
        if (HasAchieved(achievement.Id))
            return;

        if (IsCompletedAchievement(achievement))
            CompletedAchievement(achievement, referencePlayer);
    }

    public bool HasAchieved(uint achievementId)
    {
        return CompletedAchievements.ContainsKey(achievementId);
    }

    public override bool RequiredAchievementSatisfied(uint achievementId)
    {
        return HasAchieved(achievementId);
    }

    private bool IsCompletedAchievement(AchievementRecord entry)
    {
        // counter can never complete
        if (entry.Flags.HasAnyFlag(AchievementFlags.Counter))
            return false;

        var tree = Global.CriteriaMgr.GetCriteriaTree(entry.CriteriaTree);

        if (tree == null)
            return false;

        // For SUMM achievements, we have to count the progress of each criteria of the achievement.
        // Oddly, the target count is NOT contained in the achievement, but in each individual criteria
        if (entry.Flags.HasAnyFlag(AchievementFlags.Summ))
        {
            long progress = 0;

            CriteriaManager.WalkCriteriaTree(tree,
                                             criteriaTree =>
                                             {
                                                 if (criteriaTree.Criteria != null)
                                                 {
                                                     var criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);

                                                     if (criteriaProgress != null)
                                                         progress += (long)criteriaProgress.Counter;
                                                 }
                                             });

            return progress >= tree.Entry.Amount;
        }

        return IsCompletedCriteriaTree(tree);
    }
}
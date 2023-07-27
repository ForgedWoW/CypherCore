// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Arenas;
using Forged.MapServer.BattlePets;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Phasing;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using Forged.MapServer.MapWeather;
using Forged.MapServer.Scenarios;

namespace Forged.MapServer.Achievements;

public class CriteriaHandler
{
    public PhasingHandler PhasingHandler { get; }
    protected readonly AchievementGlobalMgr AchievementManager;
    protected readonly ArenaTeamManager ArenaTeamManager;
    protected readonly CliDB CliDB;
    protected readonly ConditionManager ConditionManager;
    protected readonly IConfiguration Configuration;
    protected readonly CriteriaManager CriteriaManager;
    protected readonly DB2Manager DB2Manager;
    protected readonly DisableManager DisableManager;
    protected readonly LanguageManager LanguageManager;
    protected readonly MapManager MapManager;
    protected readonly GameObjectManager GameObjectManager;
    protected readonly RealmManager RealmManager;
    protected readonly SpellManager SpellManager;
    protected readonly Dictionary<uint, uint /*ms time left*/> TimeCriteriaTrees = new();
    protected readonly WorldManager WorldManager;
    protected readonly WorldStateManager WorldStateManager;
    protected Dictionary<uint, CriteriaProgress> CriteriaProgress = new();

    public CriteriaHandler(CriteriaManager criteriaManager, WorldManager worldManager, GameObjectManager gameObjectManager, SpellManager spellManager,
                           ArenaTeamManager arenaTeamManager, DisableManager disableManager, WorldStateManager worldStateManager, CliDB cliDB,
                           ConditionManager conditionManager, RealmManager realmManager, IConfiguration configuration, LanguageManager languageManager,
                           DB2Manager db2Manager, MapManager mapManager, AchievementGlobalMgr achievementManager, PhasingHandler phasingHandler)
    {
        PhasingHandler = phasingHandler;
        AchievementManager = achievementManager;
        CriteriaManager = criteriaManager;
        WorldManager = worldManager;
        GameObjectManager = gameObjectManager;
        SpellManager = spellManager;
        ArenaTeamManager = arenaTeamManager;
        DisableManager = disableManager;
        WorldStateManager = worldStateManager;
        CliDB = cliDB;
        ConditionManager = conditionManager;
        RealmManager = realmManager;
        Configuration = configuration;
        LanguageManager = languageManager;
        DB2Manager = db2Manager;
        MapManager = mapManager;
    }

    public virtual void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer) { }

    public virtual bool CanCompleteCriteriaTree(CriteriaTree tree)
    {
        return true;
    }

    public virtual bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
    {
        if ((tree.Entry.Flags.HasAnyFlag(CriteriaTreeFlags.HordeOnly) && referencePlayer.Team != TeamFaction.Horde) ||
            (tree.Entry.Flags.HasAnyFlag(CriteriaTreeFlags.AllianceOnly) && referencePlayer.Team != TeamFaction.Alliance))
        {
            Log.Logger.Verbose("CriteriaHandler.CanUpdateCriteriaTree: (Id: {0} Type {1} CriteriaTree {2}) Wrong faction",
                               criteria.Id,
                               criteria.Entry.Type,
                               tree.Entry.Id);

            return false;
        }

        return true;
    }

    public virtual void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer) { }

    public virtual List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
    {
        return null;
    }

    public CriteriaProgress GetCriteriaProgress(Criteria entry)
    {
        return CriteriaProgress.LookupByKey(entry.Id);
    }

    public virtual string GetOwnerInfo()
    {
        return "";
    }

    public bool IsCompletedCriteriaTree(CriteriaTree tree)
    {
        if (!CanCompleteCriteriaTree(tree))
            return false;

        ulong requiredCount = tree.Entry.Amount;

        switch ((CriteriaTreeOperator)tree.Entry.Operator)
        {
            case CriteriaTreeOperator.Complete:
                return tree.Criteria != null && IsCompletedCriteria(tree.Criteria, requiredCount);

            case CriteriaTreeOperator.NotComplete:
                return tree.Criteria == null || !IsCompletedCriteria(tree.Criteria, requiredCount);

            case CriteriaTreeOperator.CompleteAll:
                foreach (var node in tree.Children)
                    if (!IsCompletedCriteriaTree(node))
                        return false;

                return true;

            case CriteriaTreeOperator.Sum:
            {
                ulong progress = 0;

                CriteriaManager.WalkCriteriaTree(tree,
                                                 criteriaTree =>
                                                 {
                                                     if (criteriaTree.Criteria == null)
                                                         return;

                                                     var criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);

                                                     if (criteriaProgress != null)
                                                         progress += criteriaProgress.Counter;
                                                 });

                return progress >= requiredCount;
            }
            case CriteriaTreeOperator.Highest:
            {
                ulong progress = 0;

                CriteriaManager.WalkCriteriaTree(tree,
                                                 criteriaTree =>
                                                 {
                                                     if (criteriaTree.Criteria == null)
                                                         return;

                                                     var criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);

                                                     if (criteriaProgress == null)
                                                         return;

                                                     if (criteriaProgress.Counter > progress)
                                                         progress = criteriaProgress.Counter;
                                                 });

                return progress >= requiredCount;
            }
            case CriteriaTreeOperator.StartedAtLeast:
            {
                ulong progress = 0;

                foreach (var node in tree.Children)
                    if (node.Criteria != null)
                    {
                        var criteriaProgress = GetCriteriaProgress(node.Criteria);

                        if (criteriaProgress is { Counter: >= 1 })
                            if (++progress >= requiredCount)
                                return true;
                    }

                return false;
            }
            case CriteriaTreeOperator.CompleteAtLeast:
            {
                ulong progress = 0;

                return tree.Children.Where(IsCompletedCriteriaTree).Any(_ => ++progress >= requiredCount);
            }
            case CriteriaTreeOperator.ProgressBar:
            {
                ulong progress = 0;

                CriteriaManager.WalkCriteriaTree(tree,
                                                 criteriaTree =>
                                                 {
                                                     if (criteriaTree.Criteria == null)
                                                         return;

                                                     var criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);

                                                     if (criteriaProgress != null)
                                                         progress += criteriaProgress.Counter * criteriaTree.Entry.Amount;
                                                 });

                return progress >= requiredCount;
            }
        }

        return false;
    }

    public bool ModifierTreeSatisfied(ModifierTreeNode tree, ulong miscValue1, ulong miscValue2, WorldObject refe, Player referencePlayer)
    {
        switch ((ModifierTreeOperator)tree.Entry.Operator)
        {
            case ModifierTreeOperator.SingleTrue:
                return tree.Entry.Type != 0 && ModifierSatisfied(tree.Entry, miscValue1, miscValue2, refe, referencePlayer);

            case ModifierTreeOperator.SingleFalse:
                return tree.Entry.Type != 0 && !ModifierSatisfied(tree.Entry, miscValue1, miscValue2, refe, referencePlayer);

            case ModifierTreeOperator.All:
                return tree.Children.All(node => ModifierTreeSatisfied(node, miscValue1, miscValue2, refe, referencePlayer));

            case ModifierTreeOperator.Some:
            {
                var requiredAmount = Math.Max(tree.Entry.Amount, (sbyte)1);

                return tree.Children.Where(node => ModifierTreeSatisfied(node, miscValue1, miscValue2, refe, referencePlayer)).Any(_ => --requiredAmount == 0);
            }
        }

        return false;
    }

    public void RemoveCriteriaProgress(Criteria criteria)
    {
        if (criteria == null)
            return;

        if (!CriteriaProgress.ContainsKey(criteria.Id))
            return;

        SendCriteriaProgressRemoved(criteria.Id);

        CriteriaProgress.Remove(criteria.Id);
    }

    public void RemoveCriteriaTimer(CriteriaStartEvent startEvent, uint entry)
    {
        var criteriaList = CriteriaManager.GetTimedCriteriaByType(startEvent);

        foreach (var criteria in criteriaList)
        {
            if (criteria.Entry.StartAsset != entry)
                continue;

            var trees = CriteriaManager.GetCriteriaTreesByCriteria(criteria.Id);

            // Remove the timer from all trees
            foreach (var tree in trees)
                TimeCriteriaTrees.Remove(tree.Id);

            // remove progress
            RemoveCriteriaProgress(criteria);
        }
    }

    public virtual bool RequiredAchievementSatisfied(uint achievementId)
    {
        return false;
    }

    public virtual void Reset()
    {
        foreach (var iter in CriteriaProgress)
            SendCriteriaProgressRemoved(iter.Key);

        CriteriaProgress.Clear();
    }

    public virtual void SendAllData(Player receiver) { }

    public virtual void SendCriteriaProgressRemoved(uint criteriaId) { }

    public virtual void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted) { }

    public virtual void SendPacket(ServerPacket data) { }

    public void SetCriteriaProgress(Criteria criteria, ulong changeValue, Player referencePlayer, ProgressType progressType = ProgressType.Set)
    {
        // Don't allow to cheat - doing timed criteria without timer active
        List<CriteriaTree> trees = null;

        if (criteria.Entry.StartTimer != 0)
        {
            trees = CriteriaManager.GetCriteriaTreesByCriteria(criteria.Id);

            if (trees.Empty())
                return;

            var hasTreeForTimed = trees.Any(tree => TimeCriteriaTrees.ContainsKey(tree.Id));

            if (!hasTreeForTimed)
                return;
        }

        Log.Logger.Debug("SetCriteriaProgress({0}, {1}) for {2}", criteria.Id, changeValue, GetOwnerInfo());

        var progress = GetCriteriaProgress(criteria);

        if (progress == null)
        {
            // not create record for 0 counter but allow it for timed criteria
            // we will need to send 0 progress to client to start the timer
            if (changeValue == 0 && criteria.Entry.StartTimer == 0)
                return;

            progress = new CriteriaProgress
            {
                Counter = changeValue
            };
        }
        else
        {
            ulong newValue = 0;

            switch (progressType)
            {
                case ProgressType.Set:
                    newValue = changeValue;

                    break;

                case ProgressType.Accumulate:
                {
                    // avoid overflow
                    var maxValue = ulong.MaxValue;
                    newValue = maxValue - progress.Counter > changeValue ? progress.Counter + changeValue : maxValue;

                    break;
                }
                case ProgressType.Highest:
                    newValue = progress.Counter < changeValue ? changeValue : progress.Counter;

                    break;
            }

            // not update (not mark as changed) if counter will have same value
            if (progress.Counter == newValue && criteria.Entry.StartTimer == 0)
                return;

            progress.Counter = newValue;
        }

        progress.Changed = true;
        progress.Date = GameTime.CurrentTime; // set the date to the latest update.
        progress.PlayerGUID = referencePlayer?.GUID ?? ObjectGuid.Empty;
        CriteriaProgress[criteria.Id] = progress;

        var timeElapsed = TimeSpan.Zero;

        if (criteria.Entry.StartTimer != 0 && trees != null)
            foreach (var tree in trees)
            {
                var timed = TimeCriteriaTrees.LookupByKey(tree.Id);

                if (timed == 0)
                    continue;

                // Client expects this in packet
                timeElapsed = TimeSpan.FromSeconds(criteria.Entry.StartTimer - timed / Time.IN_MILLISECONDS);

                // Remove the timer, we wont need it anymore
                if (IsCompletedCriteriaTree(tree))
                    TimeCriteriaTrees.Remove(tree.Id);
            }

        SendCriteriaUpdate(criteria, progress, timeElapsed, true);
    }

    public void StartCriteriaTimer(CriteriaStartEvent startEvent, uint entry, uint timeLost = 0)
    {
        var criteriaList = CriteriaManager.GetTimedCriteriaByType(startEvent);

        foreach (var criteria in criteriaList)
        {
            if (criteria.Entry.StartAsset != entry)
                continue;

            var trees = CriteriaManager.GetCriteriaTreesByCriteria(criteria.Id);
            var canStart = false;

            foreach (var tree in trees)
                if ((!TimeCriteriaTrees.ContainsKey(tree.Id) || criteria.Entry.GetFlags().HasFlag(CriteriaFlags.ResetOnStart)) && !IsCompletedCriteriaTree(tree))
                    // Start the timer
                    if (criteria.Entry.StartTimer * Time.IN_MILLISECONDS > timeLost)
                    {
                        TimeCriteriaTrees[tree.Id] = (uint)(criteria.Entry.StartTimer * Time.IN_MILLISECONDS - timeLost);
                        canStart = true;
                    }

            if (!canStart)
                continue;

            // and at client too
            SetCriteriaProgress(criteria, 0, null);
        }
    }

    /// <summary>
    ///     this function will be called whenever the user might have done a criteria relevant action
    /// </summary>
    /// <param name="type"> </param>
    /// <param name="miscValue1"> </param>
    /// <param name="miscValue2"> </param>
    /// <param name="miscValue3"> </param>
    /// <param name="refe"> </param>
    /// <param name="referencePlayer"> </param>
    public void UpdateCriteria(CriteriaType type, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, WorldObject refe = null, Player referencePlayer = null)
    {
        if (type >= CriteriaType.Count)
        {
            Log.Logger.Debug("UpdateCriteria: Wrong criteria type {0}", type);

            return;
        }

        if (referencePlayer == null)
        {
            Log.Logger.Debug("UpdateCriteria: Player is NULL! Cant update criteria");

            return;
        }

        // Disable for GameMasters with GM-mode enabled or for players that don't have the related RBAC permission
        if (referencePlayer.IsGameMaster || referencePlayer.Session.HasPermission(RBACPermissions.CannotEarnAchievements))
        {
            Log.Logger.Debug($"CriteriaHandler::UpdateCriteria: [Player {referencePlayer.GetName()} {(referencePlayer.IsGameMaster ? "GM mode on" : "disallowed by RBAC")}]" +
                             $" {GetOwnerInfo()}, {type} ({(uint)type}), {miscValue1}, {miscValue2}, {miscValue3}");

            return;
        }

        Log.Logger.Debug("UpdateCriteria({0}, {1}, {2}, {3}) {4}. {5}", type, type, miscValue1, miscValue2, miscValue3, GetOwnerInfo());

        var criteriaList = GetCriteriaByType(type, (uint)miscValue1);

        foreach (var criteria in criteriaList)
        {
            var trees = CriteriaManager.GetCriteriaTreesByCriteria(criteria.Id);

            if (!CanUpdateCriteria(criteria, trees, miscValue1, miscValue2, miscValue3, refe, referencePlayer))
                continue;

            // requirements not found in the dbc
            var data = CriteriaManager.GetCriteriaDataSet(criteria);

            if (data != null)
                if (!data.Meets(referencePlayer, refe, (uint)miscValue1, (uint)miscValue2))
                    continue;

            switch (type)
            {
                // std. case: increment at 1
                case CriteriaType.WinBattleground:
                case CriteriaType.TotalRespecs:
                case CriteriaType.LoseDuel:
                case CriteriaType.ItemsPostedAtAuction:
                case CriteriaType.AuctionsWon: /* FIXME: for online player only currently */
                case CriteriaType.RollAnyNeed:
                case CriteriaType.RollAnyGreed:
                case CriteriaType.AbandonAnyQuest:
                case CriteriaType.BuyTaxi:
                case CriteriaType.AcceptSummon:
                case CriteriaType.LootAnyItem:
                case CriteriaType.ObtainAnyItem:
                case CriteriaType.DieAnywhere:
                case CriteriaType.CompleteDailyQuest:
                case CriteriaType.ParticipateInBattleground:
                case CriteriaType.DieOnMap:
                case CriteriaType.DieInInstance:
                case CriteriaType.KilledByCreature:
                case CriteriaType.KilledByPlayer:
                case CriteriaType.DieFromEnviromentalDamage:
                case CriteriaType.BeSpellTarget:
                case CriteriaType.GainAura:
                case CriteriaType.CastSpell:
                case CriteriaType.LandTargetedSpellOnTarget:
                case CriteriaType.WinAnyRankedArena:
                case CriteriaType.UseItem:
                case CriteriaType.RollNeed:
                case CriteriaType.RollGreed:
                case CriteriaType.DoEmote:
                case CriteriaType.UseGameobject:
                case CriteriaType.CatchFishInFishingHole:
                case CriteriaType.WinDuel:
                case CriteriaType.DeliverKillingBlowToClass:
                case CriteriaType.DeliverKillingBlowToRace:
                case CriteriaType.TrackedWorldStateUIModified:
                case CriteriaType.EarnHonorableKill:
                case CriteriaType.KillPlayer:
                case CriteriaType.DeliveredKillingBlow:
                case CriteriaType.PVPKillInArea:
                case CriteriaType.WinArena: // This also behaves like CriteriaType.WinAnyRankedArena
                case CriteriaType.PlayerTriggerGameEvent:
                case CriteriaType.Login:
                case CriteriaType.AnyoneTriggerGameEventScenario:
                case CriteriaType.BattlePetReachLevel:
                case CriteriaType.ActivelyEarnPetLevel:
                case CriteriaType.PlaceGarrisonBuilding:
                case CriteriaType.ActivateAnyGarrisonBuilding:
                case CriteriaType.HonorLevelIncrease:
                case CriteriaType.PrestigeLevelIncrease:
                case CriteriaType.LearnAnyTransmogInSlot:
                case CriteriaType.CollectTransmogSetFromGroup:
                case CriteriaType.CompleteAnyReplayQuest:
                case CriteriaType.BuyItemsFromVendors:
                case CriteriaType.SellItemsToVendors:
                case CriteriaType.EnterTopLevelArea:
                    SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);

                    break;
                // std case: increment at miscValue1
                case CriteriaType.MoneyEarnedFromSales:
                case CriteriaType.MoneySpentOnRespecs:
                case CriteriaType.MoneyEarnedFromQuesting:
                case CriteriaType.MoneySpentOnTaxis:
                case CriteriaType.MoneySpentAtBarberShop:
                case CriteriaType.MoneySpentOnPostage:
                case CriteriaType.MoneyLootedFromCreatures:
                case CriteriaType.MoneyEarnedFromAuctions: /* FIXME: for online player only currently */
                case CriteriaType.TotalDamageTaken:
                case CriteriaType.TotalHealReceived:
                case CriteriaType.CompletedLFGDungeonWithStrangers:
                case CriteriaType.DamageDealt:
                case CriteriaType.HealingDone:
                case CriteriaType.EarnArtifactXPForAzeriteItem:
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Accumulate);

                    break;

                case CriteriaType.KillCreature:
                case CriteriaType.KillAnyCreature:
                case CriteriaType.GetLootByType:
                case CriteriaType.AcquireItem:
                case CriteriaType.LootItem:
                case CriteriaType.CurrencyGained:
                    SetCriteriaProgress(criteria, miscValue2, referencePlayer, ProgressType.Accumulate);

                    break;
                // std case: high value at miscValue1
                case CriteriaType.HighestAuctionBid:
                case CriteriaType.HighestAuctionSale: /* FIXME: for online player only currently */
                case CriteriaType.HighestDamageDone:
                case CriteriaType.HighestDamageTaken:
                case CriteriaType.HighestHealCast:
                case CriteriaType.HighestHealReceived:
                case CriteriaType.AzeriteLevelReached:
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Highest);

                    break;

                case CriteriaType.ReachLevel:
                    SetCriteriaProgress(criteria, referencePlayer.Level, referencePlayer);

                    break;

                case CriteriaType.SkillRaised:
                    uint skillvalue = referencePlayer.GetBaseSkillValue((SkillType)criteria.Entry.Asset);

                    if (skillvalue != 0)
                        SetCriteriaProgress(criteria, skillvalue, referencePlayer);

                    break;

                case CriteriaType.AchieveSkillStep:
                    uint maxSkillvalue = referencePlayer.GetPureMaxSkillValue((SkillType)criteria.Entry.Asset);

                    if (maxSkillvalue != 0)
                        SetCriteriaProgress(criteria, maxSkillvalue, referencePlayer);

                    break;

                case CriteriaType.CompleteQuestsCount:
                    SetCriteriaProgress(criteria, (uint)referencePlayer.GetRewardedQuestCount(), referencePlayer);

                    break;

                case CriteriaType.CompleteAnyDailyQuestPerDay:
                {
                    var nextDailyResetTime = WorldManager.NextDailyQuestsResetTime;
                    var progress = GetCriteriaProgress(criteria);

                    if (miscValue1 == 0) // Login case.
                    {
                        // reset if player missed one day.
                        if (progress != null && progress.Date < nextDailyResetTime - 2 * Time.DAY)
                            SetCriteriaProgress(criteria, 0, referencePlayer);

                        continue;
                    }

                    ProgressType progressType;

                    if (progress == null)
                        // 1st time. Start count.
                        progressType = ProgressType.Set;
                    else if (progress.Date < nextDailyResetTime - 2 * Time.DAY)
                        // last progress is older than 2 days. Player missed 1 day => Restart count.
                        progressType = ProgressType.Set;
                    else if (progress.Date < nextDailyResetTime - Time.DAY)
                        // last progress is between 1 and 2 days. => 1st time of the day.
                        progressType = ProgressType.Accumulate;
                    else
                        // last progress is within the day before the reset => Already counted today.
                        continue;

                    SetCriteriaProgress(criteria, 1, referencePlayer, progressType);

                    break;
                }
                case CriteriaType.CompleteQuestsInZone:
                {
                    if (miscValue1 != 0)
                        SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                    else // login case
                    {
                        uint counter = 0;

                        var rewQuests = referencePlayer.GetRewardedQuests();

                        foreach (var id in rewQuests)
                        {
                            var quest = GameObjectManager.GetQuestTemplate(id);

                            if (quest is { QuestSortID: >= 0 } && quest.QuestSortID == criteria.Entry.Asset)
                                ++counter;
                        }

                        SetCriteriaProgress(criteria, counter, referencePlayer);
                    }

                    break;
                }
                case CriteriaType.MaxDistFallenWithoutDying:
                    // miscValue1 is the ingame fallheight*100 as stored in dbc
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer);

                    break;

                case CriteriaType.CompleteQuest:
                case CriteriaType.LearnOrKnowSpell:
                case CriteriaType.RevealWorldMapOverlay:
                case CriteriaType.GotHaircut:
                case CriteriaType.EquipItemInSlot:
                case CriteriaType.EquipItem:
                case CriteriaType.EarnAchievement:
                case CriteriaType.RecruitGarrisonFollower:
                case CriteriaType.LearnedNewPet:
                case CriteriaType.ActivelyReachLevel:
                    SetCriteriaProgress(criteria, 1, referencePlayer);

                    break;

                case CriteriaType.BankSlotsPurchased:
                    SetCriteriaProgress(criteria, referencePlayer.GetBankBagSlotCount(), referencePlayer);

                    break;

                case CriteriaType.ReputationGained:
                {
                    var reputation = referencePlayer.ReputationMgr.GetReputation(criteria.Entry.Asset);

                    if (reputation > 0)
                        SetCriteriaProgress(criteria, (uint)reputation, referencePlayer);

                    break;
                }
                case CriteriaType.TotalExaltedFactions:
                    SetCriteriaProgress(criteria, referencePlayer.ReputationMgr.ExaltedFactionCount, referencePlayer);

                    break;

                case CriteriaType.LearnSpellFromSkillLine:
                case CriteriaType.LearnTradeskillSkillLine:
                {
                    uint spellCount = 0;

                    foreach (var (spellId, _) in referencePlayer.GetSpellMap())
                    {
                        var bounds = SpellManager.GetSkillLineAbilityMapBounds(spellId);

                        foreach (var skill in bounds)
                            if (skill.SkillLine == criteria.Entry.Asset)
                            {
                                // do not add couter twice if by any chance skill is listed twice in dbc (eg. skill 777 and spell 22717)
                                ++spellCount;

                                break;
                            }
                    }

                    SetCriteriaProgress(criteria, spellCount, referencePlayer);

                    break;
                }
                case CriteriaType.TotalReveredFactions:
                    SetCriteriaProgress(criteria, referencePlayer.ReputationMgr.ReveredFactionCount, referencePlayer);

                    break;

                case CriteriaType.TotalHonoredFactions:
                    SetCriteriaProgress(criteria, referencePlayer.ReputationMgr.HonoredFactionCount, referencePlayer);

                    break;

                case CriteriaType.TotalFactionsEncountered:
                    SetCriteriaProgress(criteria, referencePlayer.ReputationMgr.VisibleFactionCount, referencePlayer);

                    break;

                case CriteriaType.HonorableKills:
                    SetCriteriaProgress(criteria, referencePlayer.ActivePlayerData.LifetimeHonorableKills, referencePlayer);

                    break;

                case CriteriaType.MostMoneyOwned:
                    SetCriteriaProgress(criteria, referencePlayer.Money, referencePlayer, ProgressType.Highest);

                    break;

                case CriteriaType.EarnAchievementPoints:
                    if (miscValue1 == 0)
                        continue;

                    SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Accumulate);

                    break;

                case CriteriaType.EarnPersonalArenaRating:
                {
                    var reqTeamType = criteria.Entry.Asset;

                    if (miscValue1 != 0)
                    {
                        if (miscValue2 != reqTeamType)
                            continue;

                        SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Highest);
                    }
                    else // login case
                        for (byte arenaSlot = 0; arenaSlot < SharedConst.MaxArenaSlot; ++arenaSlot)
                        {
                            var teamId = referencePlayer.GetArenaTeamId(arenaSlot);

                            if (teamId == 0)
                                continue;

                            var team = ArenaTeamManager.GetArenaTeamById(teamId);

                            if (team == null || team.GetArenaType() != reqTeamType)
                                continue;

                            var member = team.GetMember(referencePlayer.GUID);

                            if (member != null)
                            {
                                SetCriteriaProgress(criteria, member.PersonalRating, referencePlayer, ProgressType.Highest);

                                break;
                            }
                        }

                    break;
                }
                case CriteriaType.UniquePetsOwned:
                    SetCriteriaProgress(criteria, referencePlayer.Session.BattlePetMgr.GetPetUniqueSpeciesCount(), referencePlayer);

                    break;

                case CriteriaType.GuildAttainedLevel:
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer);

                    break;
                // FIXME: not triggered in code as result, need to implement
                case CriteriaType.RunInstance:
                case CriteriaType.ParticipateInArena:
                case CriteriaType.EarnTeamArenaRating:
                case CriteriaType.EarnTitle:
                case CriteriaType.MoneySpentOnGuildRepair:
                case CriteriaType.CreatedItemsByCastingSpell:
                case CriteriaType.FishInAnyPool:
                case CriteriaType.GuildBankTabsPurchased:
                case CriteriaType.EarnGuildAchievementPoints:
                case CriteriaType.WinAnyBattleground:
                case CriteriaType.EarnBattlegroundRating:
                case CriteriaType.GuildTabardCreated:
                case CriteriaType.CompleteQuestsCountForGuild:
                case CriteriaType.HonorableKillsForGuild:
                case CriteriaType.KillAnyCreatureForGuild:
                case CriteriaType.CompleteAnyResearchProject:
                case CriteriaType.CompleteGuildChallenge:
                case CriteriaType.CompleteAnyGuildChallenge:
                case CriteriaType.CompletedLFRDungeon:
                case CriteriaType.AbandonedLFRDungeon:
                case CriteriaType.KickInitiatorInLFRDungeon:
                case CriteriaType.KickVoterInLFRDungeon:
                case CriteriaType.KickTargetInLFRDungeon:
                case CriteriaType.GroupedTankLeftEarlyInLFRDungeon:
                case CriteriaType.CompleteAnyScenario:
                case CriteriaType.CompleteScenario:
                case CriteriaType.AccountObtainPetThroughBattle:
                case CriteriaType.WinPetBattle:
                case CriteriaType.PlayerObtainPetThroughBattle:
                case CriteriaType.EnterArea:
                case CriteriaType.LeaveArea:
                case CriteriaType.DefeatDungeonEncounter:
                case CriteriaType.ActivateGarrisonBuilding:
                case CriteriaType.UpgradeGarrison:
                case CriteriaType.StartAnyGarrisonMissionWithFollowerType:
                case CriteriaType.SucceedAnyGarrisonMissionWithFollowerType:
                case CriteriaType.SucceedGarrisonMission:
                case CriteriaType.RecruitAnyGarrisonFollower:
                case CriteriaType.LearnAnyGarrisonBlueprint:
                case CriteriaType.CollectGarrisonShipment:
                case CriteriaType.ItemLevelChangedForGarrisonFollower:
                case CriteriaType.LevelChangedForGarrisonFollower:
                case CriteriaType.LearnToy:
                case CriteriaType.LearnAnyToy:
                case CriteriaType.LearnAnyHeirloom:
                case CriteriaType.FindResearchObject:
                case CriteriaType.ExhaustAnyResearchSite:
                case CriteriaType.CompleteInternalCriteria:
                case CriteriaType.CompleteAnyChallengeMode:
                case CriteriaType.KilledAllUnitsInSpawnRegion:
                case CriteriaType.CompleteChallengeMode:
                case CriteriaType.CreatedItemsByCastingSpellWithLimit:
                case CriteriaType.BattlePetAchievementPointsEarned:
                case CriteriaType.ReleasedSpirit:
                case CriteriaType.AccountKnownPet:
                case CriteriaType.DefeatDungeonEncounterWhileElegibleForLoot:
                case CriteriaType.CompletedLFGDungeon:
                case CriteriaType.KickInitiatorInLFGDungeon:
                case CriteriaType.KickVoterInLFGDungeon:
                case CriteriaType.KickTargetInLFGDungeon:
                case CriteriaType.AbandonedLFGDungeon:
                case CriteriaType.GroupedTankLeftEarlyInLFGDungeon:
                case CriteriaType.EnterAreaTriggerWithActionSet:
                case CriteriaType.StartGarrisonMission:
                case CriteriaType.QualityUpgradedForGarrisonFollower:
                case CriteriaType.EarnArtifactXP:
                case CriteriaType.AnyArtifactPowerRankPurchased:
                case CriteriaType.CompleteResearchGarrisonTalent:
                case CriteriaType.RecruitAnyGarrisonTroop:
                case CriteriaType.CompleteAnyWorldQuest:
                case CriteriaType.ParagonLevelIncreaseWithFaction:
                case CriteriaType.PlayerHasEarnedHonor:
                case CriteriaType.ChooseRelicTalent:
                case CriteriaType.AccountHonorLevelReached:
                case CriteriaType.MythicPlusCompleted:
                case CriteriaType.SocketAnySoulbindConduit:
                case CriteriaType.ObtainAnyItemWithCurrencyValue:
                case CriteriaType.EarnExpansionLevel:
                case CriteriaType.LearnTransmog:
                    break; // Not implemented yet :(
            }

            foreach (var tree in trees)
            {
                if (IsCompletedCriteriaTree(tree))
                    CompletedCriteriaTree(tree, referencePlayer);

                AfterCriteriaTreeUpdate(tree, referencePlayer);
            }
        }
    }

    public void UpdateTimedCriteria(uint timeDiff)
    {
        if (!TimeCriteriaTrees.Empty())
            foreach (var key in TimeCriteriaTrees.Keys.ToList())
            {
                var value = TimeCriteriaTrees[key];

                // Time is up, remove timer and reset progress
                if (value <= timeDiff)
                {
                    var criteriaTree = CriteriaManager.GetCriteriaTree(key);

                    if (criteriaTree.Criteria != null)
                        RemoveCriteriaProgress(criteriaTree.Criteria);

                    TimeCriteriaTrees.Remove(key);
                }
                else
                    TimeCriteriaTrees[key] -= timeDiff;
            }
    }

    private bool CanUpdateCriteria(Criteria criteria, List<CriteriaTree> trees, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player referencePlayer)
    {
        if (DisableManager.IsDisabledFor(DisableType.Criteria, criteria.Id, null))
        {
            Log.Logger.Error("CanUpdateCriteria: (Id: {0} Type {1}) Disabled", criteria.Id, criteria.Entry.Type);

            return false;
        }

        var treeRequirementPassed = false;

        foreach (var tree in trees)
        {
            if (!CanUpdateCriteriaTree(criteria, tree, referencePlayer))
                continue;

            treeRequirementPassed = true;

            break;
        }

        if (!treeRequirementPassed)
            return false;

        if (!RequirementsSatisfied(criteria, miscValue1, miscValue2, miscValue3, refe, referencePlayer))
        {
            Log.Logger.Verbose("CanUpdateCriteria: (Id: {0} Type {1}) Requirements not satisfied", criteria.Id, criteria.Entry.Type);

            return false;
        }

        if (criteria.Modifier != null && !ModifierTreeSatisfied(criteria.Modifier, miscValue1, miscValue2, refe, referencePlayer))
        {
            Log.Logger.Verbose("CanUpdateCriteria: (Id: {0} Type {1}) Requirements have not been satisfied", criteria.Id, criteria.Entry.Type);

            return false;
        }

        if (!ConditionsSatisfied(criteria, referencePlayer))
        {
            Log.Logger.Verbose("CanUpdateCriteria: (Id: {0} Type {1}) Conditions have not been satisfied", criteria.Id, criteria.Entry.Type);

            return false;
        }

        if (criteria.Entry.EligibilityWorldStateID == 0)
            return true;

        return WorldStateManager.GetValue(criteria.Entry.EligibilityWorldStateID, referencePlayer.Location.Map) == criteria.Entry.EligibilityWorldStateValue;
    }

    private bool ConditionsSatisfied(Criteria criteria, Player referencePlayer)
    {
        if (criteria.Entry.FailEvent == 0)
            return true;

        switch ((CriteriaFailEvent)criteria.Entry.FailEvent)
        {
            case CriteriaFailEvent.LeaveBattleground:
                if (!referencePlayer.InBattleground)
                    return false;

                break;

            case CriteriaFailEvent.ModifyPartyStatus:
                if (referencePlayer.Group != null)
                    return false;

                break;
        }

        return true;
    }

    private bool IsCompletedCriteria(Criteria criteria, ulong requiredAmount)
    {
        var progress = GetCriteriaProgress(criteria);

        if (progress == null)
            return false;

        return criteria.Entry.Type switch
        {
            CriteriaType.WinBattleground                  => progress.Counter >= requiredAmount,
            CriteriaType.KillCreature                     => progress.Counter >= requiredAmount,
            CriteriaType.ReachLevel                       => progress.Counter >= requiredAmount,
            CriteriaType.GuildAttainedLevel               => progress.Counter >= requiredAmount,
            CriteriaType.SkillRaised                      => progress.Counter >= requiredAmount,
            CriteriaType.CompleteQuestsCount              => progress.Counter >= requiredAmount,
            CriteriaType.CompleteAnyDailyQuestPerDay      => progress.Counter >= requiredAmount,
            CriteriaType.CompleteQuestsInZone             => progress.Counter >= requiredAmount,
            CriteriaType.DamageDealt                      => progress.Counter >= requiredAmount,
            CriteriaType.HealingDone                      => progress.Counter >= requiredAmount,
            CriteriaType.CompleteDailyQuest               => progress.Counter >= requiredAmount,
            CriteriaType.MaxDistFallenWithoutDying        => progress.Counter >= requiredAmount,
            CriteriaType.BeSpellTarget                    => progress.Counter >= requiredAmount,
            CriteriaType.GainAura                         => progress.Counter >= requiredAmount,
            CriteriaType.CastSpell                        => progress.Counter >= requiredAmount,
            CriteriaType.LandTargetedSpellOnTarget        => progress.Counter >= requiredAmount,
            CriteriaType.TrackedWorldStateUIModified      => progress.Counter >= requiredAmount,
            CriteriaType.PVPKillInArea                    => progress.Counter >= requiredAmount,
            CriteriaType.EarnHonorableKill                => progress.Counter >= requiredAmount,
            CriteriaType.HonorableKills                   => progress.Counter >= requiredAmount,
            CriteriaType.AcquireItem                      => progress.Counter >= requiredAmount,
            CriteriaType.WinAnyRankedArena                => progress.Counter >= requiredAmount,
            CriteriaType.EarnPersonalArenaRating          => progress.Counter >= requiredAmount,
            CriteriaType.UseItem                          => progress.Counter >= requiredAmount,
            CriteriaType.LootItem                         => progress.Counter >= requiredAmount,
            CriteriaType.BankSlotsPurchased               => progress.Counter >= requiredAmount,
            CriteriaType.ReputationGained                 => progress.Counter >= requiredAmount,
            CriteriaType.TotalExaltedFactions             => progress.Counter >= requiredAmount,
            CriteriaType.GotHaircut                       => progress.Counter >= requiredAmount,
            CriteriaType.EquipItemInSlot                  => progress.Counter >= requiredAmount,
            CriteriaType.RollNeed                         => progress.Counter >= requiredAmount,
            CriteriaType.RollGreed                        => progress.Counter >= requiredAmount,
            CriteriaType.DeliverKillingBlowToClass        => progress.Counter >= requiredAmount,
            CriteriaType.DeliverKillingBlowToRace         => progress.Counter >= requiredAmount,
            CriteriaType.DoEmote                          => progress.Counter >= requiredAmount,
            CriteriaType.EquipItem                        => progress.Counter >= requiredAmount,
            CriteriaType.MoneyEarnedFromQuesting          => progress.Counter >= requiredAmount,
            CriteriaType.MoneyLootedFromCreatures         => progress.Counter >= requiredAmount,
            CriteriaType.UseGameobject                    => progress.Counter >= requiredAmount,
            CriteriaType.KillPlayer                       => progress.Counter >= requiredAmount,
            CriteriaType.CatchFishInFishingHole           => progress.Counter >= requiredAmount,
            CriteriaType.LearnSpellFromSkillLine          => progress.Counter >= requiredAmount,
            CriteriaType.WinDuel                          => progress.Counter >= requiredAmount,
            CriteriaType.GetLootByType                    => progress.Counter >= requiredAmount,
            CriteriaType.LearnTradeskillSkillLine         => progress.Counter >= requiredAmount,
            CriteriaType.CompletedLFGDungeonWithStrangers => progress.Counter >= requiredAmount,
            CriteriaType.DeliveredKillingBlow             => progress.Counter >= requiredAmount,
            CriteriaType.CurrencyGained                   => progress.Counter >= requiredAmount,
            CriteriaType.PlaceGarrisonBuilding            => progress.Counter >= requiredAmount,
            CriteriaType.UniquePetsOwned                  => progress.Counter >= requiredAmount,
            CriteriaType.BattlePetReachLevel              => progress.Counter >= requiredAmount,
            CriteriaType.ActivelyEarnPetLevel             => progress.Counter >= requiredAmount,
            CriteriaType.LearnAnyTransmogInSlot           => progress.Counter >= requiredAmount,
            CriteriaType.ParagonLevelIncreaseWithFaction  => progress.Counter >= requiredAmount,
            CriteriaType.PlayerHasEarnedHonor             => progress.Counter >= requiredAmount,
            CriteriaType.ChooseRelicTalent                => progress.Counter >= requiredAmount,
            CriteriaType.AccountHonorLevelReached         => progress.Counter >= requiredAmount,
            CriteriaType.EarnArtifactXPForAzeriteItem     => progress.Counter >= requiredAmount,
            CriteriaType.AzeriteLevelReached              => progress.Counter >= requiredAmount,
            CriteriaType.CompleteAnyReplayQuest           => progress.Counter >= requiredAmount,
            CriteriaType.BuyItemsFromVendors              => progress.Counter >= requiredAmount,
            CriteriaType.SellItemsToVendors               => progress.Counter >= requiredAmount,
            CriteriaType.EnterTopLevelArea                => progress.Counter >= requiredAmount,
            CriteriaType.EarnAchievement                  => progress.Counter >= 1,
            CriteriaType.CompleteQuest                    => progress.Counter >= 1,
            CriteriaType.LearnOrKnowSpell                 => progress.Counter >= 1,
            CriteriaType.RevealWorldMapOverlay            => progress.Counter >= 1,
            CriteriaType.RecruitGarrisonFollower          => progress.Counter >= 1,
            CriteriaType.LearnedNewPet                    => progress.Counter >= 1,
            CriteriaType.HonorLevelIncrease               => progress.Counter >= 1,
            CriteriaType.PrestigeLevelIncrease            => progress.Counter >= 1,
            CriteriaType.ActivelyReachLevel               => progress.Counter >= 1,
            CriteriaType.CollectTransmogSetFromGroup      => progress.Counter >= 1,
            CriteriaType.AchieveSkillStep                 => progress.Counter >= requiredAmount * 75,
            CriteriaType.EarnAchievementPoints            => progress.Counter >= 9000,
            CriteriaType.WinArena                         => requiredAmount != 0 && progress.Counter >= requiredAmount,
            CriteriaType.Login                            => true,
            _                                             => false
        };
    }

    private bool ModifierSatisfied(ModifierTreeRecord modifier, ulong miscValue1, ulong miscValue2, WorldObject refe, Player referencePlayer)
    {
        var reqValue = modifier.Asset;
        var secondaryAsset = modifier.SecondaryAsset;
        var tertiaryAsset = modifier.TertiaryAsset;

        switch ((ModifierTreeType)modifier.Type)
        {
            case ModifierTreeType.PlayerInebriationLevelEqualOrGreaterThan: // 1
            {
                var inebriation = (uint)Math.Min(Math.Max(referencePlayer.DrunkValue, referencePlayer.PlayerData.FakeInebriation), 100);

                if (inebriation < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerMeetsCondition: // 2
            {
                var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(reqValue);

                if (playerCondition == null || !ConditionManager.IsPlayerMeetingCondition(referencePlayer, playerCondition))
                    return false;

                break;
            }
            case ModifierTreeType.MinimumItemLevel: // 3
            {
                // miscValue1 is itemid
                var item = GameObjectManager.ItemTemplateCache.GetItemTemplate((uint)miscValue1);

                if (item == null || item.BaseItemLevel < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.TargetCreatureId: // 4
                if (refe == null || refe.Entry != reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetIsPlayer: // 5
                if (refe == null || !refe.IsTypeId(TypeId.Player))
                    return false;

                break;

            case ModifierTreeType.TargetIsDead: // 6
                if (refe is not { IsUnit: true } || refe.AsUnit.IsAlive)
                    return false;

                break;

            case ModifierTreeType.TargetIsOppositeFaction: // 7
                if (refe == null || !referencePlayer.WorldObjectCombat.IsHostileTo(refe))
                    return false;

                break;

            case ModifierTreeType.PlayerHasAura: // 8
                if (!referencePlayer.HasAura(reqValue))
                    return false;

                break;

            case ModifierTreeType.PlayerHasAuraEffect: // 9
                if (!referencePlayer.HasAuraType((AuraType)reqValue))
                    return false;

                break;

            case ModifierTreeType.TargetHasAura: // 10
                if (refe is not { IsUnit: true } || !refe.AsUnit.HasAura(reqValue))
                    return false;

                break;

            case ModifierTreeType.TargetHasAuraEffect: // 11
                if (refe is not { IsUnit: true } || !refe.AsUnit.HasAuraType((AuraType)reqValue))
                    return false;

                break;

            case ModifierTreeType.TargetHasAuraState: // 12
                if (refe is not { IsUnit: true } || !refe.AsUnit.HasAuraState((AuraStateType)reqValue))
                    return false;

                break;

            case ModifierTreeType.PlayerHasAuraState: // 13
                if (!referencePlayer.HasAuraState((AuraStateType)reqValue))
                    return false;

                break;

            case ModifierTreeType.ItemQualityIsAtLeast: // 14
            {
                // miscValue1 is itemid
                var item = GameObjectManager.ItemTemplateCache.GetItemTemplate((uint)miscValue1);

                if (item == null || (uint)item.Quality < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.ItemQualityIsExactly: // 15
            {
                // miscValue1 is itemid
                var item = GameObjectManager.ItemTemplateCache.GetItemTemplate((uint)miscValue1);

                if (item == null || (uint)item.Quality != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerIsAlive: // 16
                if (referencePlayer.IsDead)
                    return false;

                break;

            case ModifierTreeType.PlayerIsInArea: // 17
            {
                if (referencePlayer.Location.Zone != reqValue && referencePlayer.Location.Area != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.TargetIsInArea: // 18
            {
                if (refe == null)
                    return false;

                if (refe.Location.Zone != reqValue && refe.Location.Area != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.ItemId: // 19
                if (miscValue1 != reqValue)
                    return false;

                break;

            case ModifierTreeType.LegacyDungeonDifficulty: // 20
            {
                var difficulty = CliDB.DifficultyStorage.LookupByKey((uint)referencePlayer.Location.Map.DifficultyID);

                if (difficulty == null || difficulty.OldEnumValue == -1 || difficulty.OldEnumValue != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerToTargetLevelDeltaGreaterThan: // 21
                if (refe is not { IsUnit: true } || referencePlayer.Level < refe.AsUnit.Level + reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetToPlayerLevelDeltaGreaterThan: // 22
                if (refe is not { IsUnit: true } || referencePlayer.Level + reqValue < refe.AsUnit.Level)
                    return false;

                break;

            case ModifierTreeType.PlayerLevelEqualTargetLevel: // 23
                if (refe is not { IsUnit: true } || referencePlayer.Level != refe.AsUnit.Level)
                    return false;

                break;

            case ModifierTreeType.PlayerInArenaWithTeamSize: // 24
            {
                var bg = referencePlayer.Battleground;

                if (bg == null || !bg.IsArena || bg.ArenaType != (ArenaTypes)reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerRace: // 25
                if ((uint)referencePlayer.Race != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerClass: // 26
                if ((uint)referencePlayer.Class != reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetRace: // 27
                if (refe is not { IsUnit: true } || refe.AsUnit.Race != (Race)reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetClass: // 28
                if (refe is not { IsUnit: true } || refe.AsUnit.Class != (PlayerClass)reqValue)
                    return false;

                break;

            case ModifierTreeType.LessThanTappers: // 29
                if (referencePlayer.Group != null && referencePlayer.Group.MembersCount >= reqValue)
                    return false;

                break;

            case ModifierTreeType.CreatureType: // 30
            {
                if (refe == null)
                    return false;

                if (!refe.IsUnit || refe.AsUnit.CreatureType != (CreatureType)reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.CreatureFamily: // 31
            {
                if (refe == null)
                    return false;

                if (!refe.IsCreature || refe.AsCreature.Template.Family != (CreatureFamily)reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerMap: // 32
                if (referencePlayer.Location.MapId != reqValue)
                    return false;

                break;

            case ModifierTreeType.ClientVersionEqualOrLessThan: // 33
                if (reqValue < RealmManager.GetMinorMajorBugfixVersionForBuild(WorldManager.Realm.Build))
                    return false;

                break;

            case ModifierTreeType.BattlePetTeamLevel: // 34
                if (referencePlayer.Session.BattlePetMgr.Slots.Any(slot => slot.Pet.Level < reqValue))
                    return false;

                break;

            case ModifierTreeType.PlayerIsNotInParty: // 35
                if (referencePlayer.Group != null)
                    return false;

                break;

            case ModifierTreeType.PlayerIsInParty: // 36
                if (referencePlayer.Group == null)
                    return false;

                break;

            case ModifierTreeType.HasPersonalRatingEqualOrGreaterThan: // 37
                if (referencePlayer.GetMaxPersonalArenaRatingRequirement(0) < reqValue)
                    return false;

                break;

            case ModifierTreeType.HasTitle: // 38
                if (!referencePlayer.HasTitle(reqValue))
                    return false;

                break;

            case ModifierTreeType.PlayerLevelEqual: // 39
                if (referencePlayer.Level != reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetLevelEqual: // 40
                if (refe == null || refe.GetLevelForTarget(referencePlayer) != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerIsInZone: // 41
            {
                var zoneId = referencePlayer.Location.Area;

                if (CliDB.AreaTableStorage.TryGetValue(zoneId, out var areaEntry))
                    if (areaEntry.HasFlag(AreaFlags.Unk9))
                        zoneId = areaEntry.ParentAreaID;

                if (zoneId != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.TargetIsInZone: // 42
            {
                if (refe == null)
                    return false;

                var zoneId = refe.Location.Area;

                if (CliDB.AreaTableStorage.TryGetValue(zoneId, out var areaEntry))
                    if (areaEntry.HasFlag(AreaFlags.Unk9))
                        zoneId = areaEntry.ParentAreaID;

                if (zoneId != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHealthBelowPercent: // 43
                if (referencePlayer.HealthPct > reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHealthAbovePercent: // 44
                if (referencePlayer.HealthPct < reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHealthEqualsPercent: // 45
                if (referencePlayer.HealthPct != reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetHealthBelowPercent: // 46
                if (refe is not { IsUnit: true } || refe.AsUnit.HealthPct > reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetHealthAbovePercent: // 47
                if (refe is not { IsUnit: true } || refe.AsUnit.HealthPct < reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetHealthEqualsPercent: // 48
                if (refe is not { IsUnit: true } || refe.AsUnit.HealthPct != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHealthBelowValue: // 49
                if (referencePlayer.Health > reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHealthAboveValue: // 50
                if (referencePlayer.Health < reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHealthEqualsValue: // 51
                if (referencePlayer.Health != reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetHealthBelowValue: // 52
                if (refe is not { IsUnit: true } || refe.AsUnit.Health > reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetHealthAboveValue: // 53
                if (refe is not { IsUnit: true } || refe.AsUnit.Health < reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetHealthEqualsValue: // 54
                if (refe is not { IsUnit: true } || refe.AsUnit.Health != reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetIsPlayerAndMeetsCondition: // 55
            {
                if (refe is not { IsPlayer: true })
                    return false;

                var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(reqValue);

                if (playerCondition == null || !ConditionManager.IsPlayerMeetingCondition(refe.AsPlayer, playerCondition))
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasMoreThanAchievementPoints: // 56
                if (referencePlayer.AchievementPoints <= reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerInLfgDungeon: // 57
                if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGDungeon) == 0)
                    return false;

                break;

            case ModifierTreeType.PlayerInRandomLfgDungeon: // 58
                if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGRandomDungeon) == 0)
                    return false;

                break;

            case ModifierTreeType.PlayerInFirstRandomLfgDungeon: // 59
                if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGFirstRandomDungeon) == 0)
                    return false;

                break;

            case ModifierTreeType.PlayerInRankedArenaMatch: // 60
            {
                var bg = referencePlayer.Battleground;

                if (bg == null || !bg.IsArena || !bg.IsRated)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerInGuildParty: // 61 NYI
                return false;

            case ModifierTreeType.PlayerGuildReputationEqualOrGreaterThan: // 62
                if (referencePlayer.ReputationMgr.GetReputation(1168) < reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerInRatedBattleground: // 63
            {
                var bg = referencePlayer.Battleground;

                if (bg == null || !bg.IsBattleground || !bg.IsRated)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerBattlegroundRatingEqualOrGreaterThan: // 64
                if (referencePlayer.GetRbgPersonalRating() < reqValue)
                    return false;

                break;

            case ModifierTreeType.ResearchProjectRarity: // 65 NYI
            case ModifierTreeType.ResearchProjectBranch: // 66 NYI
                return false;

            case ModifierTreeType.WorldStateExpression: // 67
                return CliDB.WorldStateExpressionStorage.TryGetValue(reqValue, out var worldStateExpression) && ConditionManager.IsPlayerMeetingExpression(referencePlayer, worldStateExpression);

            case ModifierTreeType.DungeonDifficulty: // 68
                if (referencePlayer.Location.Map.DifficultyID != (Difficulty)reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerLevelEqualOrGreaterThan: // 69
                if (referencePlayer.Level < reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetLevelEqualOrGreaterThan: // 70
                if (refe is not { IsUnit: true } || refe.AsUnit.Level < reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerLevelEqualOrLessThan: // 71
                if (referencePlayer.Level > reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetLevelEqualOrLessThan: // 72
                if (refe is not { IsUnit: true } || refe.AsUnit.Level > reqValue)
                    return false;

                break;

            case ModifierTreeType.ModifierTree: // 73
                var nextModifierTree = CriteriaManager.GetModifierTree(reqValue);

                return nextModifierTree != null && ModifierTreeSatisfied(nextModifierTree, miscValue1, miscValue2, refe, referencePlayer);

            case ModifierTreeType.PlayerScenario: // 74
            {
                var scenario = referencePlayer.Scenario;

                if (scenario == null || scenario.GetEntry().Id != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.TillersReputationGreaterThan: // 75
                if (referencePlayer.ReputationMgr.GetReputation(1272) < reqValue)
                    return false;

                break;

            case ModifierTreeType.BattlePetAchievementPointsEqualOrGreaterThan: // 76
            {
                short GetRootAchievementCategory(AchievementRecord achievement)
                {
                    var category = (short)achievement.Category;

                    do
                    {
                        var categoryEntry = CliDB.AchievementCategoryStorage.LookupByKey((uint)category);

                        if (categoryEntry?.Parent == -1)
                            break;

                        if (categoryEntry != null)
                            category = categoryEntry.Parent;
                    } while (true);

                    return category;
                }

                uint petAchievementPoints = 0;

                foreach (var achievementId in referencePlayer.CompletedAchievementIds)
                {
                    var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

                    if (GetRootAchievementCategory(achievement) == SharedConst.AchivementCategoryPetBattles)
                        petAchievementPoints += achievement.Points;
                }

                if (petAchievementPoints < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.UniqueBattlePetsEqualOrGreaterThan: // 77
                if (referencePlayer.Session.BattlePetMgr.GetPetUniqueSpeciesCount() < reqValue)
                    return false;

                break;

            case ModifierTreeType.BattlePetType: // 78
            {
                var speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey((uint)miscValue1);

                if (speciesEntry?.PetTypeEnum != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.BattlePetHealthPercentLessThan: // 79 NYI - use target battle pet here, the one we were just battling
                return false;

            case ModifierTreeType.GuildGroupMemberCountEqualOrGreaterThan: // 80
            {
                uint guildMemberCount = 0;
                var group = referencePlayer.Group;

                if (group != null)
                    for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                        if (itr.Source.GuildId == referencePlayer.GuildId)
                            ++guildMemberCount;

                if (guildMemberCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.BattlePetOpponentCreatureId: // 81 NYI
                return false;

            case ModifierTreeType.PlayerScenarioStep: // 82
            {
                var scenario = referencePlayer.Scenario;

                if (scenario == null)
                    return false;

                if (scenario.GetStep().OrderIndex != reqValue - 1)
                    return false;

                break;
            }
            case ModifierTreeType.ChallengeModeMedal: // 83
                return false;                         // OBSOLETE
            case ModifierTreeType.PlayerOnQuest:      // 84
                if (referencePlayer.FindQuestSlot(reqValue) == SharedConst.MaxQuestLogSize)
                    return false;

                break;

            case ModifierTreeType.ExaltedWithFaction: // 85
                if (referencePlayer.ReputationMgr.GetReputation(reqValue) < 42000)
                    return false;

                break;

            case ModifierTreeType.EarnedAchievementOnAccount: // 86
            case ModifierTreeType.EarnedAchievementOnPlayer:  // 87
                if (!referencePlayer.HasAchieved(reqValue))
                    return false;

                break;

            case ModifierTreeType.OrderOfTheCloudSerpentReputationGreaterThan: // 88
                if (referencePlayer.ReputationMgr.GetReputation(1271) < reqValue)
                    return false;

                break;

            case ModifierTreeType.BattlePetQuality:     // 89 NYI
            case ModifierTreeType.BattlePetFightWasPVP: // 90 NYI
                return false;

            case ModifierTreeType.BattlePetSpecies: // 91
                if (miscValue1 != reqValue)
                    return false;

                break;

            case ModifierTreeType.ServerExpansionEqualOrGreaterThan: // 92
                if (Configuration.GetDefaultValue("character:EnforceRaceAndClassExpansions", true) && Configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHasBattlePetJournalLock: // 93
                if (!referencePlayer.Session.BattlePetMgr.HasJournalLock)
                    return false;

                break;

            case ModifierTreeType.FriendshipRepReactionIsMet: // 94
            {
                if (!CliDB.FriendshipRepReactionStorage.TryGetValue(reqValue, out var friendshipRepReaction))
                    return false;

                if (!CliDB.FriendshipReputationStorage.TryGetValue(friendshipRepReaction.FriendshipRepID, out var friendshipReputation))
                    return false;

                if (referencePlayer.GetReputation((uint)friendshipReputation.FactionID) < friendshipRepReaction.ReactionThreshold)
                    return false;

                break;
            }
            case ModifierTreeType.ReputationWithFactionIsEqualOrGreaterThan: // 95
                if (referencePlayer.ReputationMgr.GetReputation(reqValue) < reqValue)
                    return false;

                break;

            case ModifierTreeType.ItemClassAndSubclass: // 96
            {
                var item = GameObjectManager.ItemTemplateCache.GetItemTemplate((uint)miscValue1);

                if (item == null || item.Class != (ItemClass)reqValue || item.SubClass != secondaryAsset)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerGender: // 97
                if ((int)referencePlayer.Gender != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerNativeGender: // 98
                if (referencePlayer.NativeGender != (Gender)reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerSkillEqualOrGreaterThan: // 99
                if (referencePlayer.GetPureSkillValue((SkillType)reqValue) < secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.PlayerLanguageSkillEqualOrGreaterThan: // 100
            {
                var languageDescs = LanguageManager.GetLanguageDescById((Language)reqValue);

                if (!languageDescs.Any(desc => referencePlayer.GetSkillValue((SkillType)desc.SkillId) >= secondaryAsset))
                    return false;

                break;
            }
            case ModifierTreeType.PlayerIsInNormalPhase: // 101
                if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, 0, 0))
                    return false;

                break;

            case ModifierTreeType.PlayerIsInPhase: // 102
                if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, (ushort)reqValue, 0))
                    return false;

                break;

            case ModifierTreeType.PlayerIsInPhaseGroup: // 103
                if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, 0, reqValue))
                    return false;

                break;

            case ModifierTreeType.PlayerKnowsSpell: // 104
                if (!referencePlayer.HasSpell(reqValue))
                    return false;

                break;

            case ModifierTreeType.PlayerHasItemQuantity: // 105
                if (referencePlayer.GetItemCount(reqValue) < secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.PlayerExpansionLevelEqualOrGreaterThan: // 106
                if (referencePlayer.Session.Expansion < (Expansion)reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHasAuraWithLabel: // 107
                if (!referencePlayer.GetAuraQuery().HasLabel(reqValue).Results.Any())
                    return false;

                break;

            case ModifierTreeType.PlayersRealmWorldState: // 108
                if (WorldStateManager.GetValue((int)reqValue, referencePlayer.Location.Map) != secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.TimeBetween: // 109
            {
                var from = Time.GetUnixTimeFromPackedTime(reqValue);
                var to = Time.GetUnixTimeFromPackedTime((uint)secondaryAsset);

                if (GameTime.CurrentTime < from || GameTime.CurrentTime > to)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasCompletedQuest: // 110
                var questBit = DB2Manager.GetQuestUniqueBitFlag(reqValue);

                if (questBit != 0)
                    if ((referencePlayer.ActivePlayerData.QuestCompleted[((int)questBit - 1) >> 6] & (1ul << (((int)questBit - 1) & 63))) == 0)
                        return false;

                break;

            case ModifierTreeType.PlayerIsReadyToTurnInQuest: // 111
                if (referencePlayer.GetQuestStatus(reqValue) != QuestStatus.Complete)
                    return false;

                break;

            case ModifierTreeType.PlayerHasCompletedQuestObjective: // 112
            {
                var objective = GameObjectManager.GetQuestObjective(reqValue);

                if (objective == null)
                    return false;

                var quest = GameObjectManager.GetQuestTemplate(objective.QuestID);

                if (quest == null)
                    return false;

                var slot = referencePlayer.FindQuestSlot(objective.QuestID);

                if (slot >= SharedConst.MaxQuestLogSize || referencePlayer.GetQuestRewardStatus(objective.QuestID) || !referencePlayer.IsQuestObjectiveComplete(slot, quest, objective))
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasExploredArea: // 113
            {
                if (!CliDB.AreaTableStorage.TryGetValue(reqValue, out var areaTable))
                    return false;

                if (areaTable.AreaBit <= 0)
                    break; // success

                var playerIndexOffset = areaTable.AreaBit / ActivePlayerData.ExploredZonesBits;

                if (playerIndexOffset >= PlayerConst.ExploredZonesSize)
                    break;

                if ((referencePlayer.ActivePlayerData.ExploredZones[playerIndexOffset] & (1ul << (areaTable.AreaBit % ActivePlayerData.ExploredZonesBits))) == 0)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasItemQuantityIncludingBank: // 114
                if (referencePlayer.GetItemCount(reqValue, true) < secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.Weather: // 115
                if (referencePlayer.Location.Map.GetZoneWeather(referencePlayer.Location.Zone) != (WeatherState)reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerFaction: // 116
            {
                if (!CliDB.ChrRacesStorage.TryGetValue((uint)referencePlayer.Race, out var race))
                    return false;

                if (!CliDB.FactionTemplateStorage.TryGetValue((uint)race.FactionID, out var faction))
                    return false;

                var factionIndex = -1;

                if (faction.FactionGroup.HasAnyFlag((byte)FactionMasks.Horde))
                    factionIndex = 0;
                else if (faction.FactionGroup.HasAnyFlag((byte)FactionMasks.Alliance))
                    factionIndex = 1;
                else if (faction.FactionGroup.HasAnyFlag((byte)FactionMasks.Player))
                    factionIndex = 0;

                if (factionIndex != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.LfgStatusEqual: // 117
                if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, (PlayerConditionLfgStatus)reqValue) != secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.LFgStatusEqualOrGreaterThan: // 118
                if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, (PlayerConditionLfgStatus)reqValue) < secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.PlayerHasCurrencyEqualOrGreaterThan: // 119
                if (!referencePlayer.HasCurrency(reqValue, (uint)secondaryAsset))
                    return false;

                break;

            case ModifierTreeType.TargetThreatListSizeLessThan: // 120
            {
                var unitRef = refe?.AsUnit;

                if (unitRef is not { CanHaveThreatList: true })
                    return false;

                if (unitRef.GetThreatManager().ThreatListSize >= reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasTrackedCurrencyEqualOrGreaterThan: // 121
                if (referencePlayer.GetCurrencyTrackedQuantity(reqValue) < secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.PlayerMapInstanceType: // 122
                if ((uint)referencePlayer.Location.Map.Entry.InstanceType != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerInTimeWalkerInstance: // 123
                if (!referencePlayer.HasPlayerFlag(PlayerFlags.Timewalking))
                    return false;

                break;

            case ModifierTreeType.PvpSeasonIsActive: // 124
                if (!Configuration.GetDefaultValue("Arena:ArenaSeason:InProgress", false))
                    return false;

                break;

            case ModifierTreeType.PvpSeason: // 125
                if (Configuration.GetDefaultValue("Arena:ArenaSeason:ID", 32) != reqValue)
                    return false;

                break;

            case ModifierTreeType.GarrisonTierEqualOrGreaterThan: // 126
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset || garrison.GetSiteLevel().GarrLevel < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowersWithLevelEqualOrGreaterThan: // 127
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);

                    return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel >= secondaryAsset;
                });

                if (followerCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowersWithQualityEqualOrGreaterThan: // 128
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);

                    return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.Quality >= secondaryAsset;
                });

                if (followerCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerWithAbilityAtLevelEqualOrGreaterThan: // 129
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);

                    return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel >= reqValue && follower.HasAbility((uint)secondaryAsset);
                });

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerWithTraitAtLevelEqualOrGreaterThan: // 130
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var traitEntry = CliDB.GarrAbilityStorage.LookupByKey((uint)secondaryAsset);

                if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);

                    return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel >= reqValue && follower.HasAbility((uint)secondaryAsset);
                });

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerWithAbilityAssignedToBuilding: // 131
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    if (!CliDB.GarrBuildingStorage.TryGetValue(follower.PacketInfo.CurrentBuildingID, out var followerBuilding))
                        return false;

                    return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue);
                });

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerWithTraitAssignedToBuilding: // 132
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                    return false;

                var traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);

                if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    if (!CliDB.GarrBuildingStorage.TryGetValue(follower.PacketInfo.CurrentBuildingID, out var followerBuilding))
                        return false;

                    return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue);
                });

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerWithLevelAssignedToBuilding: // 133
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    if (follower.PacketInfo.FollowerLevel < reqValue)
                        return false;

                    if (!CliDB.GarrBuildingStorage.TryGetValue(follower.PacketInfo.CurrentBuildingID, out var followerBuilding))
                        return false;

                    return followerBuilding.BuildingType == secondaryAsset;
                });

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonBuildingWithLevelEqualOrGreaterThan: // 134
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                    return false;

                foreach (var plot in garrison.GetPlots())
                {
                    if (plot.BuildingInfo.PacketInfo == null)
                        continue;

                    var building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);

                    if (building == null || building.UpgradeLevel < reqValue || building.BuildingType != secondaryAsset)
                        continue;

                    return true;
                }

                return false;
            }
            case ModifierTreeType.HasBlueprintForGarrisonBuilding: // 135
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                    return false;

                if (!garrison.HasBlueprint(reqValue))
                    return false;

                break;
            }
            case ModifierTreeType.HasGarrisonBuildingSpecialization: // 136
                return false;                                        // OBSOLETE
            case ModifierTreeType.AllGarrisonPlotsAreFull:           // 137
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)reqValue)
                    return false;

                foreach (var plot in garrison.GetPlots())
                    if (plot.BuildingInfo.PacketInfo == null)
                        return false;

                break;
            }
            case ModifierTreeType.PlayerIsInOwnGarrison: // 138
                if (!referencePlayer.Location.Map.IsGarrison || referencePlayer.Location.Map.InstanceId != referencePlayer.GUID.Counter)
                    return false;

                break;

            case ModifierTreeType.GarrisonShipmentOfTypeIsPending: // 139 NYI
                return false;

            case ModifierTreeType.GarrisonBuildingIsUnderConstruction: // 140
            {
                if (!CliDB.GarrBuildingStorage.ContainsKey(reqValue))
                    return false;

                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                    return false;

                foreach (var plot in garrison.GetPlots())
                {
                    if (plot.BuildingInfo.PacketInfo == null || plot.BuildingInfo.PacketInfo.GarrBuildingID != reqValue)
                        continue;

                    return !plot.BuildingInfo.PacketInfo.Active;
                }

                return false;
            }
            case ModifierTreeType.GarrisonMissionHasBeenCompleted: // 141 NYI
                return true;

            case ModifierTreeType.GarrisonBuildingLevelEqual: // 142
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                    return false;

                foreach (var plot in garrison.GetPlots())
                {
                    if (plot.BuildingInfo.PacketInfo == null)
                        continue;

                    var building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);

                    if (building == null || building.UpgradeLevel != secondaryAsset || building.BuildingType != reqValue)
                        continue;

                    return true;
                }

                return false;
            }
            case ModifierTreeType.GarrisonFollowerHasAbility: // 143
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                    return false;

                if (miscValue1 != 0)
                {
                    var follower = garrison.GetFollower(miscValue1);

                    if (follower == null)
                        return false;

                    if (!follower.HasAbility(reqValue))
                        return false;
                }
                else
                {
                    var followerCount = garrison.CountFollowers(follower => follower.HasAbility(reqValue));

                    if (followerCount < 1)
                        return false;
                }

                break;
            }
            case ModifierTreeType.GarrisonFollowerHasTrait: // 144
            {
                var traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);

                if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                    return false;

                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                    return false;

                if (miscValue1 != 0)
                {
                    var follower = garrison.GetFollower(miscValue1);

                    if (follower == null || !follower.HasAbility(reqValue))
                        return false;
                }
                else
                {
                    var followerCount = garrison.CountFollowers(follower => follower.HasAbility(reqValue));

                    if (followerCount < 1)
                        return false;
                }

                break;
            }
            case ModifierTreeType.GarrisonFollowerQualityEqual: // 145
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != GarrisonType.Garrison)
                    return false;

                if (miscValue1 != 0)
                {
                    var follower = garrison.GetFollower(miscValue1);

                    if (follower == null || follower.PacketInfo.Quality < reqValue)
                        return false;
                }
                else
                {
                    var followerCount = garrison.CountFollowers(follower => follower.PacketInfo.Quality >= reqValue);

                    if (followerCount < 1)
                        return false;
                }

                break;
            }
            case ModifierTreeType.GarrisonFollowerLevelEqual: // 146
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                    return false;

                if (miscValue1 != 0)
                {
                    var follower = garrison.GetFollower(miscValue1);

                    if (follower == null || follower.PacketInfo.FollowerLevel != reqValue)
                        return false;
                }
                else
                {
                    var followerCount = garrison.CountFollowers(follower => follower.PacketInfo.FollowerLevel == reqValue);

                    if (followerCount < 1)
                        return false;
                }

                break;
            }
            case ModifierTreeType.GarrisonMissionIsRare:  // 147 NYI
            case ModifierTreeType.GarrisonMissionIsElite: // 148 NYI
                return false;

            case ModifierTreeType.CurrentGarrisonBuildingLevelEqual: // 149
            {
                if (miscValue1 == 0)
                    return false;

                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                foreach (var plot in garrison.GetPlots())
                {
                    if (plot.BuildingInfo.PacketInfo == null || plot.BuildingInfo.PacketInfo.GarrBuildingID != miscValue1)
                        continue;

                    var building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);

                    if (building == null || building.UpgradeLevel != reqValue)
                        continue;

                    return true;
                }

                break;
            }
            case ModifierTreeType.GarrisonPlotInstanceHasBuildingThatIsReadyToActivate: // 150
            {
                var garrison = referencePlayer.Garrison;

                var plot = garrison?.GetPlot(reqValue);

                if (plot == null)
                    return false;

                if (!plot.BuildingInfo.CanActivate() || plot.BuildingInfo.PacketInfo == null || plot.BuildingInfo.PacketInfo.Active)
                    return false;

                break;
            }
            case ModifierTreeType.BattlePetTeamWithSpeciesEqualOrGreaterThan: // 151
            {
                var count = referencePlayer.Session.BattlePetMgr.Slots.Count(slot => slot.Pet.Species == secondaryAsset);

                if (count < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.BattlePetTeamWithTypeEqualOrGreaterThan: // 152
            {
                uint count = 0;

                foreach (var slot in referencePlayer.Session.BattlePetMgr.Slots)
                {
                    if (!CliDB.BattlePetSpeciesStorage.TryGetValue(slot.Pet.Species, out var species))
                        continue;

                    if (species.PetTypeEnum == secondaryAsset)
                        ++count;
                }

                if (count < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PetBattleLastAbility:     // 153 NYI
            case ModifierTreeType.PetBattleLastAbilityType: // 154 NYI
                return false;

            case ModifierTreeType.BattlePetTeamWithAliveEqualOrGreaterThan: // 155
            {
                uint count = 0;

                foreach (var slot in referencePlayer.Session.BattlePetMgr.Slots)
                    if (slot.Pet.Health > 0)
                        ++count;

                if (count < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.HasGarrisonBuildingActiveSpecialization: // 156
                return false;                                              // OBSOLETE
            case ModifierTreeType.HasGarrisonFollower:                     // 157
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue);

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerQuestObjectiveProgressEqual: // 158
            {
                var objective = GameObjectManager.GetQuestObjective(reqValue);

                if (objective == null)
                    return false;

                if (referencePlayer.GetQuestObjectiveData(objective) != secondaryAsset)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerQuestObjectiveProgressEqualOrGreaterThan: // 159
            {
                var objective = GameObjectManager.GetQuestObjective(reqValue);

                if (objective == null)
                    return false;

                if (referencePlayer.GetQuestObjectiveData(objective) < secondaryAsset)
                    return false;

                break;
            }
            case ModifierTreeType.IsPTRRealm:                      // 160
            case ModifierTreeType.IsBetaRealm:                     // 161
            case ModifierTreeType.IsQARealm:                       // 162
                return false;                                      // always false
            case ModifierTreeType.GarrisonShipmentContainerIsFull: // 163
                return false;

            case ModifierTreeType.PlayerCountIsValidToStartGarrisonInvasion: // 164
                return true;                                                 // Only 1 player is required and referencePlayer.GetMap() will ALWAYS have at least the referencePlayer on it
            case ModifierTreeType.InstancePlayerCountEqualOrLessThan:        // 165
                if (referencePlayer.Location.Map.PlayersCountExceptGMs > reqValue)
                    return false;

                break;

            case ModifierTreeType.AllGarrisonPlotsFilledWithBuildingsWithLevelEqualOrGreater: // 166
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)reqValue)
                    return false;

                foreach (var plot in garrison.GetPlots())
                {
                    if (plot.BuildingInfo.PacketInfo == null)
                        return false;

                    var building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);

                    if (building == null || building.UpgradeLevel != reqValue)
                        return false;
                }

                break;
            }
            case ModifierTreeType.GarrisonMissionType: // 167 NYI
                return false;

            case ModifierTreeType.GarrisonFollowerItemLevelEqualOrGreaterThan: // 168
            {
                if (miscValue1 == 0)
                    return false;

                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == miscValue1 && follower.GetItemLevel() >= reqValue);

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerCountWithItemLevelEqualOrGreaterThan: // 169
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);

                    return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.GetItemLevel() >= secondaryAsset;
                });

                if (followerCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonTierEqual: // 170
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset || garrison.GetSiteLevel().GarrLevel != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.InstancePlayerCountEqual: // 171
                if (referencePlayer.Location.Map.Players.Count != reqValue)
                    return false;

                break;

            case ModifierTreeType.CurrencyId: // 172
                if (miscValue1 != reqValue)
                    return false;

                break;

            case ModifierTreeType.SelectionIsPlayerCorpse: // 173
                if (referencePlayer.Target.High != HighGuid.Corpse)
                    return false;

                break;

            case ModifierTreeType.PlayerCanAcceptQuest: // 174
            {
                var quest = GameObjectManager.GetQuestTemplate(reqValue);

                if (quest == null)
                    return false;

                if (!referencePlayer.CanTakeQuest(quest, false))
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerCountWithLevelEqualOrGreaterThan: // 175
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);

                    return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel == secondaryAsset;
                });

                if (followerCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerIsInBuilding: // 176
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue && follower.PacketInfo.CurrentBuildingID == secondaryAsset);

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonMissionCountLessThan: // 177 NYI
                return false;

            case ModifierTreeType.GarrisonPlotInstanceCountEqualOrGreaterThan: // 178
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)reqValue)
                    return false;

                uint plotCount = 0;

                foreach (var plot in garrison.GetPlots())
                {
                    var garrPlotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(plot.PacketInfo.GarrPlotInstanceID);

                    if (garrPlotInstance == null || garrPlotInstance.GarrPlotID != secondaryAsset)
                        continue;

                    ++plotCount;
                }

                if (plotCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.CurrencySource: // 179 NYI
                return false;

            case ModifierTreeType.PlayerIsInNotOwnGarrison: // 180
                if (!referencePlayer.Location.Map.IsGarrison || referencePlayer.Location.Map.InstanceId == referencePlayer.GUID.Counter)
                    return false;

                break;

            case ModifierTreeType.HasActiveGarrisonFollower: // 181
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue && (follower.PacketInfo.FollowerStatus & (byte)GarrisonFollowerStatus.Inactive) == 0);

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerDailyRandomValueMod_X_Equals: // 182 NYI
                return false;

            case ModifierTreeType.PlayerHasMount: // 183
            {
                foreach (var pair in referencePlayer.Session.CollectionMgr.AccountMounts)
                {
                    var mount = DB2Manager.GetMount(pair.Key);

                    if (mount == null)
                        continue;

                    if (mount.Id == reqValue)
                        return true;
                }

                return false;
            }
            case ModifierTreeType.GarrisonFollowerCountWithInactiveWithItemLevelEqualOrGreaterThan: // 184
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower =>
                {
                    if (!CliDB.GarrFollowerStorage.TryGetValue(follower.PacketInfo.GarrFollowerID, out var garrFollower))
                        return false;

                    return follower.GetItemLevel() >= secondaryAsset && garrFollower.GarrFollowerTypeID == tertiaryAsset;
                });

                if (followerCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonFollowerIsOnAMission: // 185
            {
                var garrison = referencePlayer.Garrison;

                if (garrison == null)
                    return false;

                var followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue && follower.PacketInfo.CurrentMissionID != 0);

                if (followerCount < 1)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonMissionCountInSetLessThan: // 186 NYI
                return false;

            case ModifierTreeType.GarrisonFollowerType: // 187
            {
                var garrFollower = CliDB.GarrFollowerStorage.LookupByKey((uint)miscValue1);

                if (garrFollower == null || garrFollower.GarrFollowerTypeID != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerUsedBoostLessThanHoursAgoRealTime: // 188 NYI
            case ModifierTreeType.PlayerUsedBoostLessThanHoursAgoGameTime: // 189 NYI
                return false;

            case ModifierTreeType.PlayerIsMercenary: // 190
                if (!referencePlayer.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode))
                    return false;

                break;

            case ModifierTreeType.PlayerEffectiveRace: // 191 NYI
            case ModifierTreeType.TargetEffectiveRace: // 192 NYI
                return false;

            case ModifierTreeType.HonorLevelEqualOrGreaterThan: // 193
                if (referencePlayer.HonorLevel < reqValue)
                    return false;

                break;

            case ModifierTreeType.PrestigeLevelEqualOrGreaterThan: // 194
                return false;                                      // OBSOLOTE
            case ModifierTreeType.GarrisonMissionIsReadyToCollect: // 195 NYI
            case ModifierTreeType.PlayerIsInstanceOwner:           // 196 NYI
                return false;

            case ModifierTreeType.PlayerHasHeirloom: // 197
                if (!referencePlayer.Session.CollectionMgr.AccountHeirlooms.ContainsKey(reqValue))
                    return false;

                break;

            case ModifierTreeType.TeamPoints: // 198 NYI
                return false;

            case ModifierTreeType.PlayerHasToy: // 199
                if (!referencePlayer.Session.CollectionMgr.HasToy(reqValue))
                    return false;

                break;

            case ModifierTreeType.PlayerHasTransmog: // 200
            {
                var (permAppearance, tempAppearance) = referencePlayer.Session.CollectionMgr.HasItemAppearance(reqValue);

                if (!permAppearance || tempAppearance)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonTalentSelected:   // 201 NYI
            case ModifierTreeType.GarrisonTalentResearched: // 202 NYI
                return false;

            case ModifierTreeType.PlayerHasRestriction: // 203
            {
                var restrictionIndex = referencePlayer.ActivePlayerData.CharacterRestrictions.FindIndexIf(restriction => restriction.Type == reqValue);

                if (restrictionIndex < 0)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerCreatedCharacterLessThanHoursAgoRealTime: // 204 NYI
                return false;

            case ModifierTreeType.PlayerCreatedCharacterLessThanHoursAgoGameTime: // 205
                if (TimeSpan.FromHours(reqValue) >= TimeSpan.FromSeconds(referencePlayer.TotalPlayedTime))
                    return false;

                break;

            case ModifierTreeType.QuestHasQuestInfoId: // 206
            {
                var quest = GameObjectManager.GetQuestTemplate((uint)miscValue1);

                if (quest == null || quest.Id != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonTalentResearchInProgress: // 207 NYI
                return false;

            case ModifierTreeType.PlayerEquippedArtifactAppearanceSet: // 208
            {
                var artifactAura = referencePlayer.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

                if (artifactAura != null)
                {
                    var artifact = referencePlayer.GetItemByGuid(artifactAura.CastItemGuid);

                    if (artifact != null)
                        if (CliDB.ArtifactAppearanceStorage.TryGetValue(artifact.GetModifier(ItemModifier.ArtifactAppearanceId), out var artifactAppearance))
                            if (artifactAppearance.ArtifactAppearanceSetID == reqValue)
                                break;
                }

                return false;
            }
            case ModifierTreeType.PlayerHasCurrencyEqual: // 209
                if (referencePlayer.GetCurrencyQuantity(reqValue) != secondaryAsset)
                    return false;

                break;

            case ModifierTreeType.MinimumAverageItemHighWaterMarkForSpec: // 210 NYI
                return false;

            case ModifierTreeType.PlayerScenarioType: // 211
            {
                var scenario = referencePlayer.Scenario;

                if (scenario == null)
                    return false;

                if (scenario.GetEntry().Type != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayersAuthExpansionLevelEqualOrGreaterThan: // 212
                if (referencePlayer.Session.AccountExpansion < (Expansion)reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerLastWeek2v2Rating: // 213 NYI
            case ModifierTreeType.PlayerLastWeek3v3Rating: // 214 NYI
            case ModifierTreeType.PlayerLastWeekRBGRating: // 215 NYI
                return false;

            case ModifierTreeType.GroupMemberCountFromConnectedRealmEqualOrGreaterThan: // 216
            {
                uint memberCount = 0;
                var group = referencePlayer.Group;

                if (group != null)
                    for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                        if (itr.Source != referencePlayer && referencePlayer.PlayerData.VirtualPlayerRealm == itr.Source.PlayerData.VirtualPlayerRealm)
                            ++memberCount;

                if (memberCount < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.ArtifactTraitUnlockedCountEqualOrGreaterThan: // 217
            {
                var artifact = referencePlayer.GetItemByEntry((uint)secondaryAsset, ItemSearchLocation.Everywhere);

                if (artifact == null)
                    return false;

                if (artifact.GetTotalUnlockedArtifactPowers() < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.ParagonReputationLevelEqualOrGreaterThan: // 218
                if (referencePlayer.ReputationMgr.GetParagonLevel((uint)miscValue1) < reqValue)
                    return false;

                return false;

            case ModifierTreeType.GarrisonShipmentIsReady: // 219 NYI
                return false;

            case ModifierTreeType.PlayerIsInPvpBrawl: // 220
            {
                if (!CliDB.BattlemasterListStorage.TryGetValue((uint)referencePlayer.BattlegroundTypeId, out var bg) || !bg.Flags.HasFlag(BattlemasterListFlags.Brawl))
                    return false;

                break;
            }
            case ModifierTreeType.ParagonReputationLevelWithFactionEqualOrGreaterThan: // 221
            {
                if (!CliDB.FactionStorage.TryGetValue((uint)secondaryAsset, out var faction))
                    return false;

                if (referencePlayer.ReputationMgr.GetParagonLevel(faction.ParagonFactionID) < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasItemWithBonusListFromTreeAndQuality: // 222
            {
                var bonusListIDs = DB2Manager.GetAllItemBonusTreeBonuses(reqValue);

                if (bonusListIDs.Empty())
                    return false;

                var bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Everywhere,
                                                                    item =>
                                                                    {
                                                                        var hasBonus = item.BonusListIDs.Any(bonusListID => bonusListIDs.Contains(bonusListID));

                                                                        return !hasBonus;
                                                                    });

                if (bagScanReachedEnd)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasEmptyInventorySlotCountEqualOrGreaterThan: // 223
                if (referencePlayer.GetFreeInventorySlotCount() < reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHasItemInHistoryOfProgressiveEvent: // 224 NYI
                return false;

            case ModifierTreeType.PlayerHasArtifactPowerRankCountPurchasedEqualOrGreaterThan: // 225
            {
                var artifactAura = referencePlayer.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

                if (artifactAura == null)
                    return false;

                var artifact = referencePlayer.GetItemByGuid(artifactAura.CastItemGuid);

                var artifactPower = artifact?.GetArtifactPower((uint)secondaryAsset);

                if (artifactPower == null)
                    return false;

                if (artifactPower.PurchasedRank < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasBoosted: // 226
                if (referencePlayer.HasLevelBoosted())
                    return false;

                break;

            case ModifierTreeType.PlayerHasRaceChanged: // 227
                if (referencePlayer.HasRaceChanged())
                    return false;

                break;

            case ModifierTreeType.PlayerHasBeenGrantedLevelsFromRaF: // 228
                if (referencePlayer.HasBeenGrantedLevelsFromRaF())
                    return false;

                break;

            case ModifierTreeType.IsTournamentRealm: // 229
                return false;

            case ModifierTreeType.PlayerCanAccessAlliedRaces: // 230
                if (!referencePlayer.Session.CanAccessAlliedRaces())
                    return false;

                break;

            case ModifierTreeType.GroupMemberCountWithAchievementEqualOrLessThan: // 231
            {
                var group = referencePlayer.Group;

                if (group != null)
                {
                    uint membersWithAchievement = 0;

                    for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                        if (itr.Source.HasAchieved((uint)secondaryAsset))
                            ++membersWithAchievement;

                    if (membersWithAchievement > reqValue)
                        return false;
                }

                // true if no group
                break;
            }
            case ModifierTreeType.PlayerMainhandWeaponType: // 232
            {
                var visibleItem = referencePlayer.PlayerData.VisibleItems[EquipmentSlot.MainHand];
                var itemSubclass = (uint)ItemSubClassWeapon.Fist;
                var itemTemplate = GameObjectManager.ItemTemplateCache.GetItemTemplate(visibleItem.ItemID);

                if (itemTemplate is { Class: ItemClass.Weapon })
                {
                    itemSubclass = itemTemplate.SubClass;

                    var itemModifiedAppearance = DB2Manager.GetItemModifiedAppearance(visibleItem.ItemID, visibleItem.ItemAppearanceModID);

                    if (itemModifiedAppearance != null)
                    {
                        var itemModifiedAppearaceExtra = CliDB.ItemModifiedAppearanceExtraStorage.LookupByKey(itemModifiedAppearance.Id);

                        if (itemModifiedAppearaceExtra is { DisplayWeaponSubclassID: > 0 })
                            itemSubclass = (uint)itemModifiedAppearaceExtra.DisplayWeaponSubclassID;
                    }
                }

                if (itemSubclass != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerOffhandWeaponType: // 233
            {
                var visibleItem = referencePlayer.PlayerData.VisibleItems[EquipmentSlot.OffHand];
                var itemSubclass = (uint)ItemSubClassWeapon.Fist;
                var itemTemplate = GameObjectManager.ItemTemplateCache.GetItemTemplate(visibleItem.ItemID);

                if (itemTemplate is { Class: ItemClass.Weapon })
                {
                    itemSubclass = itemTemplate.SubClass;

                    var itemModifiedAppearance = DB2Manager.GetItemModifiedAppearance(visibleItem.ItemID, visibleItem.ItemAppearanceModID);

                    if (itemModifiedAppearance != null)
                    {
                        var itemModifiedAppearaceExtra = CliDB.ItemModifiedAppearanceExtraStorage.LookupByKey(itemModifiedAppearance.Id);

                        if (itemModifiedAppearaceExtra is { DisplayWeaponSubclassID: > 0 })
                            itemSubclass = (uint)itemModifiedAppearaceExtra.DisplayWeaponSubclassID;
                    }
                }

                if (itemSubclass != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerPvpTier: // 234
            {
                if (!CliDB.PvpTierStorage.TryGetValue(reqValue, out var pvpTier))
                    return false;

                var pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)pvpTier.BracketID);

                if (pvpInfo == null)
                    return false;

                if (pvpTier.Id != pvpInfo.PvpTierID)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerAzeriteLevelEqualOrGreaterThan: // 235
            {
                var heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                if (heartOfAzeroth == null || heartOfAzeroth.AsAzeriteItem.GetLevel() < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerIsOnQuestInQuestline: // 236
            {
                var isOnQuest = false;
                var questLineQuests = DB2Manager.GetQuestsForQuestLine(reqValue);

                if (!questLineQuests.Empty())
                    isOnQuest = questLineQuests.Any(questLineQuest => referencePlayer.FindQuestSlot(questLineQuest.QuestID) < SharedConst.MaxQuestLogSize);

                if (!isOnQuest)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerIsQnQuestLinkedToScheduledWorldStateGroup: // 237
                return false;                                                      // OBSOLETE (db2 removed)
            case ModifierTreeType.PlayerIsInRaidGroup:                             // 238
            {
                var group = referencePlayer.Group;

                if (group is not { IsRaidGroup: true })
                    return false;

                break;
            }
            case ModifierTreeType.PlayerPvpTierInBracketEqualOrGreaterThan: // 239
            {
                var pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)secondaryAsset);

                if (pvpInfo == null)
                    return false;

                if (!CliDB.PvpTierStorage.TryGetValue(pvpInfo.PvpTierID, out var pvpTier))
                    return false;

                if (pvpTier.Rank < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerCanAcceptQuestInQuestline: // 240
            {
                var questLineQuests = DB2Manager.GetQuestsForQuestLine(reqValue);

                if (questLineQuests.Empty())
                    return false;

                var canTakeQuest = questLineQuests.Any(questLineQuest =>
                {
                    var quest = GameObjectManager.GetQuestTemplate(questLineQuest.QuestID);

                    if (quest != null)
                        return referencePlayer.CanTakeQuest(quest, false);

                    return false;
                });

                if (!canTakeQuest)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasCompletedQuestline: // 241
            {
                var questLineQuests = DB2Manager.GetQuestsForQuestLine(reqValue);

                if (questLineQuests.Empty())
                    return false;

                if (questLineQuests.Any(questLineQuest => !referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID)))
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasCompletedQuestlineQuestCount: // 242
            {
                var questLineQuests = DB2Manager.GetQuestsForQuestLine(reqValue);

                if (questLineQuests.Empty())
                    return false;

                var completedQuests = questLineQuests.Count(questLineQuest => referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID));

                if (completedQuests < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasCompletedPercentageOfQuestline: // 243
            {
                var questLineQuests = DB2Manager.GetQuestsForQuestLine(reqValue);

                if (questLineQuests.Empty())
                    return false;

                var completedQuests = questLineQuests.Count(questLineQuest => referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID));

                if (MathFunctions.GetPctOf(completedQuests, questLineQuests.Count) < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasWarModeEnabled: // 244
                if (!referencePlayer.HasPlayerLocalFlag(PlayerLocalFlags.WarMode))
                    return false;

                break;

            case ModifierTreeType.PlayerIsOnWarModeShard: // 245
                if (!referencePlayer.HasPlayerFlag(PlayerFlags.WarModeActive))
                    return false;

                break;

            case ModifierTreeType.PlayerIsAllowedToToggleWarModeInArea: // 246
                if (!referencePlayer.CanEnableWarModeInArea())
                    return false;

                break;

            case ModifierTreeType.MythicPlusKeystoneLevelEqualOrGreaterThan: // 247 NYI
            case ModifierTreeType.MythicPlusCompletedInTime:                 // 248 NYI
            case ModifierTreeType.MythicPlusMapChallengeMode:                // 249 NYI
            case ModifierTreeType.MythicPlusDisplaySeason:                   // 250 NYI
            case ModifierTreeType.MythicPlusMilestoneSeason:                 // 251 NYI
                return false;

            case ModifierTreeType.PlayerVisibleRace: // 252
            {
                if (!CliDB.CreatureDisplayInfoStorage.TryGetValue(referencePlayer.DisplayId, out var creatureDisplayInfo))
                    return false;

                if (!CliDB.CreatureDisplayInfoExtraStorage.TryGetValue((uint)creatureDisplayInfo.ExtendedDisplayInfoID, out var creatureDisplayInfoExtra))
                    return false;

                if (creatureDisplayInfoExtra.DisplayRaceID != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.TargetVisibleRace: // 253
            {
                if (refe is not { IsUnit: true })
                    return false;

                if (!CliDB.CreatureDisplayInfoStorage.TryGetValue(refe.AsUnit.DisplayId, out var creatureDisplayInfo))
                    return false;

                if (!CliDB.CreatureDisplayInfoExtraStorage.TryGetValue((uint)creatureDisplayInfo.ExtendedDisplayInfoID, out var creatureDisplayInfoExtra))
                    return false;

                if (creatureDisplayInfoExtra.DisplayRaceID != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.FriendshipRepReactionEqual: // 254
            {
                if (!CliDB.FriendshipRepReactionStorage.TryGetValue(reqValue, out var friendshipRepReaction))
                    return false;

                if (!CliDB.FriendshipReputationStorage.TryGetValue(friendshipRepReaction.FriendshipRepID, out var friendshipReputation))
                    return false;

                var friendshipReactions = DB2Manager.GetFriendshipRepReactions(reqValue);

                if (friendshipReactions == null)
                    return false;

                var rank = (int)referencePlayer.GetReputationRank((uint)friendshipReputation.FactionID);

                if (rank >= friendshipReactions.Count)
                    return false;

                if (friendshipReactions[rank].Id != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerAuraStackCountEqual: // 255
                if (referencePlayer.GetAuraCount((uint)secondaryAsset) != reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetAuraStackCountEqual: // 256
                if (refe is not { IsUnit: true } || refe.AsUnit.GetAuraCount((uint)secondaryAsset) != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerAuraStackCountEqualOrGreaterThan: // 257
                if (referencePlayer.GetAuraCount((uint)secondaryAsset) < reqValue)
                    return false;

                break;

            case ModifierTreeType.TargetAuraStackCountEqualOrGreaterThan: // 258
                if (refe is not { IsUnit: true } || refe.AsUnit.GetAuraCount((uint)secondaryAsset) < reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHasAzeriteEssenceRankLessThan: // 259
            {
                var heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                var azeriteItem = heartOfAzeroth?.AsAzeriteItem;

                if (azeriteItem == null)
                    return false;

                foreach (var essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                    if (essence.AzeriteEssenceID == reqValue && essence.Rank < secondaryAsset)
                        return true;

                return false;
            }
            case ModifierTreeType.PlayerHasAzeriteEssenceRankEqual: // 260
            {
                var heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                var azeriteItem = heartOfAzeroth?.AsAzeriteItem;

                if (azeriteItem == null)
                    return false;

                foreach (var essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                    if (essence.AzeriteEssenceID == reqValue && essence.Rank == secondaryAsset)
                        return true;

                return false;
            }
            case ModifierTreeType.PlayerHasAzeriteEssenceRankGreaterThan: // 261
            {
                var heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                var azeriteItem = heartOfAzeroth?.AsAzeriteItem;

                if (azeriteItem == null)
                    return false;

                foreach (var essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                    if (essence.AzeriteEssenceID == reqValue && essence.Rank > secondaryAsset)
                        return true;

                return false;
            }
            case ModifierTreeType.PlayerHasAuraWithEffectIndex: // 262
                if (referencePlayer.GetAuraEffect(reqValue, secondaryAsset) == null)
                    return false;

                break;

            case ModifierTreeType.PlayerLootSpecializationMatchesRole: // 263
            {
                var spec = CliDB.ChrSpecializationStorage.LookupByKey(referencePlayer.GetPrimarySpecialization());

                if (spec == null || spec.Role != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerIsAtMaxExpansionLevel: // 264
                if (!referencePlayer.IsMaxLevel)
                    return false;

                break;

            case ModifierTreeType.TransmogSource: // 265
            {
                if (!CliDB.ItemModifiedAppearanceStorage.TryGetValue((uint)miscValue2, out var itemModifiedAppearance))
                    return false;

                if (itemModifiedAppearance.TransmogSourceTypeEnum != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasAzeriteEssenceInSlotAtRankLessThan: // 266
            {
                var heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                var azeriteItem = heartOfAzeroth?.AsAzeriteItem;

                var selectedEssences = azeriteItem?.GetSelectedAzeriteEssences();

                if (selectedEssences == null)
                    return false;

                foreach (var essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                    if (essence.AzeriteEssenceID == selectedEssences.AzeriteEssenceID[(int)reqValue] && essence.Rank < secondaryAsset)
                        return true;

                return false;
            }
            case ModifierTreeType.PlayerHasAzeriteEssenceInSlotAtRankGreaterThan: // 267
            {
                var heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                var azeriteItem = heartOfAzeroth?.AsAzeriteItem;

                var selectedEssences = azeriteItem?.GetSelectedAzeriteEssences();

                if (selectedEssences == null)
                    return false;

                foreach (var essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                    if (essence.AzeriteEssenceID == selectedEssences.AzeriteEssenceID[(int)reqValue] && essence.Rank > secondaryAsset)
                        return true;

                return false;
            }
            case ModifierTreeType.PlayerLevelWithinContentTuning: // 268
            {
                var level = referencePlayer.Level;
                var levels = DB2Manager.GetContentTuningData(reqValue, 0);

                if (!levels.HasValue)
                    return false;

                if (secondaryAsset != 0)
                    return level >= levels.Value.MinLevelWithDelta && level <= levels.Value.MaxLevelWithDelta;

                return level >= levels.Value.MinLevel && level <= levels.Value.MaxLevel;
            }
            case ModifierTreeType.TargetLevelWithinContentTuning: // 269
            {
                if (refe is not { IsUnit: true })
                    return false;

                var level = refe.AsUnit.Level;
                var levels = DB2Manager.GetContentTuningData(reqValue, 0);

                if (!levels.HasValue)
                    return false;

                if (secondaryAsset != 0)
                    return level >= levels.Value.MinLevelWithDelta && level <= levels.Value.MaxLevelWithDelta;

                return level >= levels.Value.MinLevel && level <= levels.Value.MaxLevel;
            }
            case ModifierTreeType.PlayerIsScenarioInitiator: // 270 NYI
                return false;

            case ModifierTreeType.PlayerHasCompletedQuestOrIsOnQuest: // 271
            {
                var status = referencePlayer.GetQuestStatus(reqValue);

                if (status is QuestStatus.None or QuestStatus.Failed)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerLevelWithinOrAboveContentTuning: // 272
            {
                var level = referencePlayer.Level;
                var levels = DB2Manager.GetContentTuningData(reqValue, 0);

                if (levels.HasValue)
                    return secondaryAsset != 0 ? level >= levels.Value.MinLevelWithDelta : level >= levels.Value.MinLevel;

                return false;
            }
            case ModifierTreeType.TargetLevelWithinOrAboveContentTuning: // 273
            {
                if (refe is not { IsUnit: true })
                    return false;

                var level = refe.AsUnit.Level;
                var levels = DB2Manager.GetContentTuningData(reqValue, 0);

                if (levels.HasValue)
                    return secondaryAsset != 0 ? level >= levels.Value.MinLevelWithDelta : level >= levels.Value.MinLevel;

                return false;
            }
            case ModifierTreeType.PlayerLevelWithinOrAboveLevelRange: // 274 NYI
            case ModifierTreeType.TargetLevelWithinOrAboveLevelRange: // 275 NYI
                return false;

            case ModifierTreeType.MaxJailersTowerLevelEqualOrGreaterThan: // 276
                if (referencePlayer.ActivePlayerData.JailersTowerLevelMax < reqValue)
                    return false;

                break;

            case ModifierTreeType.GroupedWithRaFRecruit: // 277
            {
                var group = referencePlayer.Group;

                if (group == null)
                    return false;

                for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                    if (itr.Source.Session.RecruiterId == referencePlayer.Session.AccountId)
                        return true;

                return false;
            }
            case ModifierTreeType.GroupedWithRaFRecruiter: // 278
            {
                var group = referencePlayer.Group;

                if (group == null)
                    return false;

                for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                    if (itr.Source.Session.AccountId == referencePlayer.Session.RecruiterId)
                        return true;

                return false;
            }
            case ModifierTreeType.PlayerSpecialization: // 279
                if (referencePlayer.GetPrimarySpecialization() != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerMapOrCosmeticChildMap: // 280
            {
                var map = referencePlayer.Location.Map.Entry;

                if (map.Id != reqValue && map.CosmeticParentMapID != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerCanAccessShadowlandsPrepurchaseContent: // 281
                if (referencePlayer.Session.AccountExpansion < Expansion.ShadowLands)
                    return false;

                break;

            case ModifierTreeType.PlayerHasEntitlement:                                // 282 NYI
            case ModifierTreeType.PlayerIsInPartySyncGroup:                            // 283 NYI
            case ModifierTreeType.QuestHasPartySyncRewards:                            // 284 NYI
            case ModifierTreeType.HonorGainSource:                                     // 285 NYI
            case ModifierTreeType.JailersTowerActiveFloorIndexEqualOrGreaterThan:      // 286 NYI
            case ModifierTreeType.JailersTowerActiveFloorDifficultyEqualOrGreaterThan: // 287 NYI
                return false;

            case ModifierTreeType.PlayerCovenant: // 288
                if (referencePlayer.PlayerData.CovenantID != reqValue)
                    return false;

                break;

            case ModifierTreeType.HasTimeEventPassed: // 289
            {
                var eventTimestamp = GameTime.CurrentTime;

                switch (reqValue)
                {
                    case 111:                         // Battle for Azeroth Season 4 Start
                        eventTimestamp = 1579618800L; // January 21, 2020 8:00

                        break;

                    case 120:                         // Patch 9.0.1
                        eventTimestamp = 1602601200L; // October 13, 2020 8:00

                        break;

                    case 121:                         // Shadowlands Season 1 Start
                        eventTimestamp = 1607439600L; // December 8, 2020 8:00

                        break;

                    case 123: // Shadowlands Season 1 End
                        // timestamp = unknown
                        break;

                    case 149: // Shadowlands Season 2 End
                        // timestamp = unknown
                        break;
                }

                if (GameTime.CurrentTime < eventTimestamp)
                    return false;

                break;
            }
            case ModifierTreeType.GarrisonHasPermanentTalent: // 290 NYI
                return false;

            case ModifierTreeType.HasActiveSoulbind: // 291
                if (referencePlayer.PlayerData.SoulbindID != reqValue)
                    return false;

                break;

            case ModifierTreeType.HasMemorizedSpell: // 292 NYI
                return false;

            case ModifierTreeType.PlayerHasAPACSubscriptionReward_2020:     // 293
            case ModifierTreeType.PlayerHasTBCCDEWarpStalker_Mount:         // 294
            case ModifierTreeType.PlayerHasTBCCDEDarkPortal_Toy:            // 295
            case ModifierTreeType.PlayerHasTBCCDEPathOfIllidan_Toy:         // 296
            case ModifierTreeType.PlayerHasImpInABallToySubscriptionReward: // 297
                return false;

            case ModifierTreeType.PlayerIsInAreaGroup: // 298
            {
                var areas = DB2Manager.GetAreasForGroup(reqValue);

                if (!CliDB.AreaTableStorage.TryGetValue(referencePlayer.Location.Area, out var area))
                    return false;

                return areas.Any(areaInGroup => areaInGroup == area.Id || areaInGroup == area.ParentAreaID);
            }
            case ModifierTreeType.TargetIsInAreaGroup: // 299
            {
                if (refe == null)
                    return false;

                var areas = DB2Manager.GetAreasForGroup(reqValue);

                return CliDB.AreaTableStorage.TryGetValue(refe.Location.Area, out var area) && areas.Any(areaInGroup => areaInGroup == area.Id || areaInGroup == area.ParentAreaID);
            }
            case ModifierTreeType.PlayerIsInChromieTime: // 300
                if (referencePlayer.ActivePlayerData.UiChromieTimeExpansionID != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerIsInAnyChromieTime: // 301
                if (referencePlayer.ActivePlayerData.UiChromieTimeExpansionID == 0)
                    return false;

                break;

            case ModifierTreeType.ItemIsAzeriteArmor: // 302
                if (DB2Manager.GetAzeriteEmpoweredItem((uint)miscValue1) == null)
                    return false;

                break;

            case ModifierTreeType.PlayerHasRuneforgePower: // 303
            {
                var block = (int)reqValue / 32;

                if (block >= referencePlayer.ActivePlayerData.RuneforgePowers.Size())
                    return false;

                var bit = reqValue % 32;

                return (referencePlayer.ActivePlayerData.RuneforgePowers[block] & (1u << (int)bit)) != 0;
            }
            case ModifierTreeType.PlayerInChromieTimeForScaling: // 304
                if ((referencePlayer.PlayerData.CtrOptions.Value.ContentTuningConditionMask & 1) == 0)
                    return false;

                break;

            case ModifierTreeType.IsRaFRecruit: // 305
                if (referencePlayer.Session.RecruiterId == 0)
                    return false;

                break;

            case ModifierTreeType.AllPlayersInGroupHaveAchievement: // 306
            {
                var group = referencePlayer.Group;

                if (group != null)
                {
                    for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                        if (!itr.Source.HasAchieved(reqValue))
                            return false;
                }
                else if (!referencePlayer.HasAchieved(reqValue))
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasSoulbindConduitRankEqualOrGreaterThan: // 307 NYI
                return false;

            case ModifierTreeType.PlayerSpellShapeshiftFormCreatureDisplayInfoSelection: // 308
            {
                var formModelData = DB2Manager.GetShapeshiftFormModelData(referencePlayer.Race, referencePlayer.NativeGender, (ShapeShiftForm)secondaryAsset);

                if (formModelData == null)
                    return false;

                var formChoice = referencePlayer.GetCustomizationChoice(formModelData.OptionID);
                var choiceIndex = formModelData.Choices.FindIndex(choice => choice.Id == formChoice);

                if (choiceIndex == -1)
                    return false;

                if (reqValue != formModelData.Displays[choiceIndex].DisplayID)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerSoulbindConduitCountAtRankEqualOrGreaterThan: // 309 NYI
                return false;

            case ModifierTreeType.PlayerIsRestrictedAccount: // 310
                return false;

            case ModifierTreeType.PlayerIsFlying: // 311
                if (!referencePlayer.IsFlying)
                    return false;

                break;

            case ModifierTreeType.PlayerScenarioIsLastStep: // 312
            {
                var scenario = referencePlayer.Scenario;

                if (scenario == null)
                    return false;

                if (scenario.GetStep() != scenario.GetLastStep())
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasWeeklyRewardsAvailable: // 313
                if (referencePlayer.ActivePlayerData.WeeklyRewardsPeriodSinceOrigin == 0)
                    return false;

                break;

            case ModifierTreeType.TargetCovenant: // 314
                if (refe is not { IsPlayer: true })
                    return false;

                if (refe.AsPlayer.PlayerData.CovenantID != reqValue)
                    return false;

                break;

            case ModifierTreeType.PlayerHasTBCCollectorsEdition:   // 315
            case ModifierTreeType.PlayerHasWrathCollectorsEdition: // 316
                return false;

            case ModifierTreeType.GarrisonTalentResearchedAndAtRankEqualOrGreaterThan:          // 317 NYI
            case ModifierTreeType.CurrencySpentOnGarrisonTalentResearchEqualOrGreaterThan:      // 318 NYI
            case ModifierTreeType.RenownCatchupActive:                                          // 319 NYI
            case ModifierTreeType.RapidRenownCatchupActive:                                     // 320 NYI
            case ModifierTreeType.PlayerMythicPlusRatingEqualOrGreaterThan:                     // 321 NYI
            case ModifierTreeType.PlayerMythicPlusRunCountInCurrentExpansionEqualOrGreaterThan: // 322 NYI
                return false;

            case ModifierTreeType.PlayerHasCustomizationChoice: // 323
            {
                var customizationChoiceIndex = referencePlayer.PlayerData.Customizations.FindIndexIf(choice => choice.ChrCustomizationChoiceID == reqValue);

                if (customizationChoiceIndex < 0)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerBestWeeklyWinPvpTier: // 324
            {
                if (!CliDB.PvpTierStorage.TryGetValue(reqValue, out var pvpTier))
                    return false;

                var pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)pvpTier.BracketID);

                if (pvpInfo == null)
                    return false;

                if (pvpTier.Id != pvpInfo.WeeklyBestWinPvpTierID)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerBestWeeklyWinPvpTierInBracketEqualOrGreaterThan: // 325
            {
                var pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)secondaryAsset);

                if (pvpInfo == null)
                    return false;

                if (!CliDB.PvpTierStorage.TryGetValue(pvpInfo.WeeklyBestWinPvpTierID, out var pvpTier))
                    return false;

                if (pvpTier.Rank < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasVanillaCollectorsEdition: // 326
                return false;

            case ModifierTreeType.PlayerHasItemWithKeystoneLevelModifierEqualOrGreaterThan: // 327
            {
                var bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Inventory,
                                                                    item =>
                                                                    {
                                                                        if (item.Entry != reqValue)
                                                                            return true;

                                                                        return item.GetModifier(ItemModifier.ChallengeKeystoneLevel) < secondaryAsset;
                                                                    });

                if (bagScanReachedEnd)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerAuraWithLabelStackCountEqualOrGreaterThan: // 335
            {
                uint count = 0;
                referencePlayer.GetAuraQuery().HasLabel((uint)secondaryAsset).ForEachResult(aura => count += aura.StackAmount);

                if (count < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerAuraWithLabelStackCountEqual: // 336
            {
                uint count = 0;
                referencePlayer.GetAuraQuery().HasLabel((uint)secondaryAsset).ForEachResult(aura => count += aura.StackAmount);

                if (count != reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerAuraWithLabelStackCountEqualOrLessThan: // 337
            {
                uint count = 0;
                referencePlayer.GetAuraQuery().HasLabel((uint)secondaryAsset).ForEachResult(aura => count += aura.StackAmount);

                if (count > reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerIsInCrossFactionGroup: // 338
            {
                var group = referencePlayer.Group;

                if (!group.GroupFlags.HasFlag(GroupFlags.CrossFaction))
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasTraitNodeEntryInActiveConfig: // 340
            {
                bool HasTraitNodeEntry()
                {
                    foreach (var traitConfig in referencePlayer.ActivePlayerData.TraitConfigs)
                    {
                        if ((TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat)
                            if (referencePlayer.ActivePlayerData.ActiveCombatTraitConfigID != traitConfig.ID || !((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
                                continue;

                        foreach (var traitEntry in traitConfig.Entries)
                            if (traitEntry.TraitNodeEntryID == reqValue)
                                return true;
                    }

                    return false;
                }

                if (!HasTraitNodeEntry())
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasTraitNodeEntryInActiveConfigRankGreaterOrEqualThan: // 341
            {
                var traitNodeEntryRank = new Func<short?>(() =>
                {
                    foreach (var traitConfig in referencePlayer.ActivePlayerData.TraitConfigs)
                    {
                        if ((TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat)
                            if (referencePlayer.ActivePlayerData.ActiveCombatTraitConfigID != traitConfig.ID || !((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
                                continue;

                        foreach (var traitEntry in traitConfig.Entries)
                            if (traitEntry.TraitNodeEntryID == secondaryAsset)
                                return (short)traitEntry.Rank;
                    }

                    return null;
                })();

                if (!traitNodeEntryRank.HasValue || traitNodeEntryRank < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerDaysSinceLogout: // 344
                if (GameTime.CurrentTime - referencePlayer.PlayerData.LogoutTime < reqValue * Time.DAY)
                    return false;

                break;

            case ModifierTreeType.PlayerHasPerksProgramPendingReward: // 350
                if (!referencePlayer.ActivePlayerData.HasPerksProgramPendingReward)
                    return false;

                break;

            case ModifierTreeType.PlayerCanUseItem: // 351
            {
                var itemTemplate = GameObjectManager.ItemTemplateCache.GetItemTemplate(reqValue);

                if (itemTemplate == null || referencePlayer.CanUseItem(itemTemplate) != InventoryResult.Ok)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerSummonedBattlePetSpecies: // 352
            {
                if (referencePlayer.PlayerData.CurrentBattlePetBreedQuality != (int)reqValue)
                    return false;
                break;
            }
            case ModifierTreeType.PlayerSummonedBattlePetIsMaxLevel: // 353
            {
                if (referencePlayer.UnitData.WildBattlePetLevel != referencePlayer.ClassFactory.Resolve<BattlePetMgr>().GetMaxPetLevel())
                    return false;
                break;
            }
            case ModifierTreeType.PlayerHasAtLeastProfPathRanks: // 355
            {
                uint ranks = 0;

                foreach (var traitConfig in referencePlayer.ActivePlayerData.TraitConfigs)
                {
                    if ((TraitConfigType)(int)traitConfig.Type != TraitConfigType.Profession)
                        continue;

                    if (traitConfig.SkillLineID != secondaryAsset)
                        continue;

                    foreach (var traitEntry in traitConfig.Entries)
                        if (CliDB.TraitNodeEntryStorage.LookupByKey((uint)traitEntry.TraitNodeEntryID)?.GetNodeEntryType() == TraitNodeEntryType.ProfPath)
                            ranks += (uint)(traitEntry.Rank + traitEntry.GrantedRanks);
                }

                if (ranks < reqValue)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasItemTransmogrifiedToItemModifiedAppearance: // 358
            {
                ItemModifiedAppearanceRecord itemModifiedAppearance = referencePlayer.CliDB.ItemModifiedAppearanceStorage.LookupByKey(reqValue);

                bool bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Inventory, item =>
                {
                    if (item.GetVisibleAppearanceModId(referencePlayer) == itemModifiedAppearance.Id)
                        return false;

                    if ((int)item.Entry == itemModifiedAppearance.ItemID)
                        return false;

                    return true;
                });

                if (bagScanReachedEnd)
                    return false;

                break;
            }
            case ModifierTreeType.PlayerHasCompletedDungeonEncounterInDifficulty: // 366
            {
                if (!referencePlayer.IsLockedToDungeonEncounter(reqValue))
                    return false;

                break;
            }
            case ModifierTreeType.PlayerIsBetweenQuests: // 369
            {
                QuestStatus status = referencePlayer.GetQuestStatus(reqValue);

                if (status == QuestStatus.None || status == QuestStatus.Failed)
                    return false;

                if (referencePlayer.IsQuestRewarded((uint)secondaryAsset))
                    return false;
                break;

            }
            case ModifierTreeType.PlayerScenarioStepID: // 371
                {
                    Scenario scenario = referencePlayer.Scenario;

                    if (scenario == null)
                        return false;

                    if (scenario.GetStep().Id != reqValue)
                        return false;

                    break;
                }
            case ModifierTreeType.PlayerZPositionBelow: // 374
                if (referencePlayer.Location.Z >= reqValue)
                    return false;

                break;
            default:
                return false;
        }

        return true;
    }

    private bool RequirementsSatisfied(Criteria criteria, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player referencePlayer)
    {
        switch (criteria.Entry.Type)
        {
            case CriteriaType.AcceptSummon:
            case CriteriaType.CompleteDailyQuest:
            case CriteriaType.ItemsPostedAtAuction:
            case CriteriaType.MaxDistFallenWithoutDying:
            case CriteriaType.BuyTaxi:
            case CriteriaType.DeliveredKillingBlow:
            case CriteriaType.MoneyEarnedFromAuctions:
            case CriteriaType.MoneySpentAtBarberShop:
            case CriteriaType.MoneySpentOnPostage:
            case CriteriaType.MoneySpentOnRespecs:
            case CriteriaType.MoneySpentOnTaxis:
            case CriteriaType.HighestAuctionBid:
            case CriteriaType.HighestAuctionSale:
            case CriteriaType.HighestHealReceived:
            case CriteriaType.HighestHealCast:
            case CriteriaType.HighestDamageDone:
            case CriteriaType.HighestDamageTaken:
            case CriteriaType.EarnHonorableKill:
            case CriteriaType.LootAnyItem:
            case CriteriaType.MoneyLootedFromCreatures:
            case CriteriaType.LoseDuel:
            case CriteriaType.MoneyEarnedFromQuesting:
            case CriteriaType.MoneyEarnedFromSales:
            case CriteriaType.TotalRespecs:
            case CriteriaType.ObtainAnyItem:
            case CriteriaType.AbandonAnyQuest:
            case CriteriaType.GuildAttainedLevel:
            case CriteriaType.RollAnyGreed:
            case CriteriaType.RollAnyNeed:
            case CriteriaType.KillPlayer:
            case CriteriaType.TotalDamageTaken:
            case CriteriaType.TotalHealReceived:
            case CriteriaType.CompletedLFGDungeonWithStrangers:
            case CriteriaType.GotHaircut:
            case CriteriaType.WinDuel:
            case CriteriaType.WinAnyRankedArena:
            case CriteriaType.AuctionsWon:
            case CriteriaType.CompleteAnyReplayQuest:
            case CriteriaType.BuyItemsFromVendors:
            case CriteriaType.SellItemsToVendors:
                if (miscValue1 == 0)
                    return false;

                break;

            case CriteriaType.BankSlotsPurchased:
            case CriteriaType.CompleteAnyDailyQuestPerDay:
            case CriteriaType.CompleteQuestsCount:
            case CriteriaType.EarnAchievementPoints:
            case CriteriaType.TotalExaltedFactions:
            case CriteriaType.TotalHonoredFactions:
            case CriteriaType.TotalReveredFactions:
            case CriteriaType.MostMoneyOwned:
            case CriteriaType.EarnPersonalArenaRating:
            case CriteriaType.TotalFactionsEncountered:
            case CriteriaType.ReachLevel:
            case CriteriaType.Login:
            case CriteriaType.UniquePetsOwned:
                break;

            case CriteriaType.EarnAchievement:
                if (!RequiredAchievementSatisfied(criteria.Entry.Asset))
                    return false;

                break;

            case CriteriaType.WinBattleground:
            case CriteriaType.ParticipateInBattleground:
            case CriteriaType.DieOnMap:
                if (miscValue1 == 0 || criteria.Entry.Asset != referencePlayer.Location.MapId)
                    return false;

                break;

            case CriteriaType.KillCreature:
            case CriteriaType.KilledByCreature:
                if (miscValue1 == 0 || criteria.Entry.Asset != miscValue1)
                    return false;

                break;

            case CriteriaType.SkillRaised:
            case CriteriaType.AchieveSkillStep:
                // update at loading or specific skill update
                if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.CompleteQuestsInZone:
                if (miscValue1 != 0)
                {
                    var quest = GameObjectManager.GetQuestTemplate((uint)miscValue1);

                    if (quest == null || quest.QuestSortID != criteria.Entry.Asset)
                        return false;
                }

                break;

            case CriteriaType.DieAnywhere:
            {
                if (miscValue1 == 0)
                    return false;

                break;
            }
            case CriteriaType.DieInInstance:
            {
                if (miscValue1 == 0)
                    return false;

                var map = referencePlayer.Location.IsInWorld ? referencePlayer.Location.Map : MapManager.FindMap(referencePlayer.Location.MapId, referencePlayer.InstanceId);

                if (map is not { IsDungeon: true })
                    return false;

                //FIXME: work only for instances where max == min for players
                if (map.ToInstanceMap.MaxPlayers != criteria.Entry.Asset)
                    return false;

                break;
            }
            case CriteriaType.KilledByPlayer:
                if (miscValue1 == 0 || refe == null || !refe.IsTypeId(TypeId.Player))
                    return false;

                break;

            case CriteriaType.DieFromEnviromentalDamage:
                if (miscValue1 == 0 || miscValue2 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.CompleteQuest:
            {
                // if miscValues != 0, it contains the questID.
                if (miscValue1 != 0)
                {
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                }
                else
                {
                    // login case.
                    if (!referencePlayer.GetQuestRewardStatus(criteria.Entry.Asset))
                        return false;
                }

                var data = CriteriaManager.GetCriteriaDataSet(criteria);

                if (data != null)
                    if (!data.Meets(referencePlayer, refe))
                        return false;

                break;
            }
            case CriteriaType.BeSpellTarget:
            case CriteriaType.GainAura:
            case CriteriaType.CastSpell:
            case CriteriaType.LandTargetedSpellOnTarget:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.LearnOrKnowSpell:
                if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                    return false;

                if (!referencePlayer.HasSpell(criteria.Entry.Asset))
                    return false;

                break;

            case CriteriaType.GetLootByType:
                // miscValue1 = itemId - miscValue2 = count of item loot
                // miscValue3 = loot_type (note: 0 = LOOT_CORPSE and then it ignored)
                if (miscValue1 == 0 || miscValue2 == 0 || miscValue3 == 0 || miscValue3 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.AcquireItem:
                if (miscValue1 != 0 && criteria.Entry.Asset != miscValue1)
                    return false;

                break;

            case CriteriaType.UseItem:
            case CriteriaType.LootItem:
            case CriteriaType.EquipItem:
                if (miscValue1 == 0 || criteria.Entry.Asset != miscValue1)
                    return false;

                break;

            case CriteriaType.RevealWorldMapOverlay:
            {
                if (!CliDB.WorldMapOverlayStorage.TryGetValue(criteria.Entry.Asset, out var worldOverlayEntry))
                    break;

                var matchFound = false;

                for (var j = 0; j < SharedConst.MaxWorldMapOverlayArea; ++j)
                {
                    if (!CliDB.AreaTableStorage.TryGetValue(worldOverlayEntry.AreaID[j], out var area))
                        break;

                    if (area.AreaBit < 0)
                        continue;

                    var playerIndexOffset = area.AreaBit / ActivePlayerData.ExploredZonesBits;

                    if (playerIndexOffset >= PlayerConst.ExploredZonesSize)
                        continue;

                    var mask = 1ul << (int)((uint)area.AreaBit % ActivePlayerData.ExploredZonesBits);

                    if (!Convert.ToBoolean(referencePlayer.ActivePlayerData.ExploredZones[playerIndexOffset] & mask))
                        continue;

                    matchFound = true;

                    break;
                }

                if (!matchFound)
                    return false;

                break;
            }
            case CriteriaType.ReputationGained:
                if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.EquipItemInSlot:
            case CriteriaType.LearnAnyTransmogInSlot:
                // miscValue1 = EquipmentSlot miscValue2 = itemid | itemModifiedAppearanceId
                if (miscValue2 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.RollNeed:
            case CriteriaType.RollGreed:
            {
                // miscValue1 = itemid miscValue2 = diced value
                if (miscValue1 == 0 || miscValue2 != criteria.Entry.Asset)
                    return false;

                var proto = GameObjectManager.ItemTemplateCache.GetItemTemplate((uint)miscValue1);

                if (proto == null)
                    return false;

                break;
            }
            case CriteriaType.DoEmote:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.DamageDealt:
            case CriteriaType.HealingDone:
                if (miscValue1 == 0)
                    return false;

                if ((CriteriaFailEvent)criteria.Entry.FailEvent == CriteriaFailEvent.LeaveBattleground)
                {
                    if (!referencePlayer.InBattleground)
                        return false;

                    // map specific case (BG in fact) expected player targeted damage/heal
                    if (refe == null || !refe.IsTypeId(TypeId.Player))
                        return false;
                }

                break;

            case CriteriaType.UseGameobject:
            case CriteriaType.CatchFishInFishingHole:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.LearnSpellFromSkillLine:
            case CriteriaType.LearnTradeskillSkillLine:
                if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.DeliverKillingBlowToClass:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.DeliverKillingBlowToRace:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.TrackedWorldStateUIModified:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.PVPKillInArea:
            case CriteriaType.EnterTopLevelArea:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.CurrencyGained:
                if (miscValue1 == 0 || miscValue2 == 0 || (long)miscValue2 < 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.WinArena:
                if (miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.EarnTeamArenaRating:
                return false;

            case CriteriaType.PlaceGarrisonBuilding:
            case CriteriaType.ActivateGarrisonBuilding:
                if (miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.RecruitGarrisonFollower:
                if (miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.CollectTransmogSetFromGroup:
                if (miscValue1 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.BattlePetReachLevel:
            case CriteriaType.ActivelyEarnPetLevel:
                if (miscValue1 == 0 || miscValue2 == 0 || miscValue2 != criteria.Entry.Asset)
                    return false;

                break;

            case CriteriaType.ActivelyReachLevel:
                if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                    return false;

                break;
        }

        return true;
    }
}
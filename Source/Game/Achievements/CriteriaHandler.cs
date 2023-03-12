// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Configuration;
using Framework.Constants;
using Game.Arenas;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scenarios;
using Game.Spells;

namespace Game.Achievements
{
    public class CriteriaHandler
    {
        protected Dictionary<uint, CriteriaProgress> _criteriaProgress = new();
        readonly Dictionary<uint, uint /*ms time left*/> _timeCriteriaTrees = new();

        public virtual void Reset()
        {
            foreach (var iter in _criteriaProgress)
                SendCriteriaProgressRemoved(iter.Key);

            _criteriaProgress.Clear();
        }

        /// <summary>
        /// this function will be called whenever the user might have done a criteria relevant action
        /// </summary>
        /// <param name="type"></param>
        /// <param name="miscValue1"></param>
        /// <param name="miscValue2"></param>
        /// <param name="miscValue3"></param>
        /// <param name="refe"></param>
        /// <param name="referencePlayer"></param>
        public void UpdateCriteria(CriteriaType type, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, WorldObject refe = null, Player referencePlayer = null)
        {
            if (type >= CriteriaType.Count)
            {
                Log.outDebug(LogFilter.Achievement, "UpdateCriteria: Wrong criteria type {0}", type);
                return;
            }

            if (!referencePlayer)
            {
                Log.outDebug(LogFilter.Achievement, "UpdateCriteria: Player is NULL! Cant update criteria");
                return;
            }

            // Disable for GameMasters with GM-mode enabled or for players that don't have the related RBAC permission
            if (referencePlayer.IsGameMaster || referencePlayer.Session.HasPermission(RBACPermissions.CannotEarnAchievements))
            {
                Log.outDebug(LogFilter.Achievement, $"CriteriaHandler::UpdateCriteria: [Player {referencePlayer.GetName()} {(referencePlayer.IsGameMaster ? "GM mode on" : "disallowed by RBAC")}]" +
                    $" {GetOwnerInfo()}, {type} ({(uint)type}), {miscValue1}, {miscValue2}, {miscValue3}");
                return;
            }

            Log.outDebug(LogFilter.Achievement, "UpdateCriteria({0}, {1}, {2}, {3}) {4}", type, type, miscValue1, miscValue2, miscValue3, GetOwnerInfo());

            List<Criteria> criteriaList = GetCriteriaByType(type, (uint)miscValue1);
            foreach (Criteria criteria in criteriaList)
            {
                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                if (!CanUpdateCriteria(criteria, trees, miscValue1, miscValue2, miscValue3, refe, referencePlayer))
                    continue;

                // requirements not found in the dbc
                CriteriaDataSet data = Global.CriteriaMgr.GetCriteriaDataSet(criteria);
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
                    case CriteriaType.AuctionsWon:    /* FIXME: for online player only currently */
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
                    case CriteriaType.MoneyEarnedFromAuctions:/* FIXME: for online player only currently */
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
                        long nextDailyResetTime = Global.WorldMgr.NextDailyQuestsResetTime;
                        CriteriaProgress progress = GetCriteriaProgress(criteria);

                        if (miscValue1 == 0) // Login case.
                        {
                            // reset if player missed one day.
                            if (progress != null && progress.Date < (nextDailyResetTime - 2 * Time.Day))
                                SetCriteriaProgress(criteria, 0, referencePlayer);
                            continue;
                        }

                        ProgressType progressType;
                        if (progress == null)
                            // 1st time. Start count.
                            progressType = ProgressType.Set;
                        else if (progress.Date < (nextDailyResetTime - 2 * Time.Day))
                            // last progress is older than 2 days. Player missed 1 day => Restart count.
                            progressType = ProgressType.Set;
                        else if (progress.Date < (nextDailyResetTime - Time.Day))
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
                        {
                            SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                        }
                        else // login case
                        {
                            uint counter = 0;

                            var rewQuests = referencePlayer.GetRewardedQuests();
                            foreach (var id in rewQuests)
                            {
                                Quest quest = Global.ObjectMgr.GetQuestTemplate(id);
                                if (quest != null && quest.QuestSortID >= 0 && quest.QuestSortID == criteria.Entry.Asset)
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
                        int reputation = referencePlayer.ReputationMgr.GetReputation(criteria.Entry.Asset);
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
                            var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);
                            foreach (var skill in bounds)
                            {
                                if (skill.SkillLine == criteria.Entry.Asset)
                                {
                                    // do not add couter twice if by any chance skill is listed twice in dbc (eg. skill 777 and spell 22717)
                                    ++spellCount;
                                    break;
                                }
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
                        uint reqTeamType = criteria.Entry.Asset;

                        if (miscValue1 != 0)
                        {
                            if (miscValue2 != reqTeamType)
                                continue;

                            SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Highest);
                        }
                        else // login case
                        {

                            for (byte arena_slot = 0; arena_slot < SharedConst.MaxArenaSlot; ++arena_slot)
                            {
                                uint teamId = referencePlayer.GetArenaTeamId(arena_slot);
                                if (teamId == 0)
                                    continue;

                                ArenaTeam team = Global.ArenaTeamMgr.GetArenaTeamById(teamId);
                                if (team == null || team.GetArenaType() != reqTeamType)
                                    continue;

                                ArenaTeamMember member = team.GetMember(referencePlayer.GUID);
                                if (member != null)
                                {
                                    SetCriteriaProgress(criteria, member.PersonalRating, referencePlayer, ProgressType.Highest);
                                    break;
                                }
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
                        break;                                   // Not implemented yet :(
                }

                foreach (CriteriaTree tree in trees)
                {
                    if (IsCompletedCriteriaTree(tree))
                        CompletedCriteriaTree(tree, referencePlayer);

                    AfterCriteriaTreeUpdate(tree, referencePlayer);
                }
            }
        }

        public void UpdateTimedCriteria(uint timeDiff)
        {
            if (!_timeCriteriaTrees.Empty())
            {
                foreach (var key in _timeCriteriaTrees.Keys.ToList())
                {
                    var value = _timeCriteriaTrees[key];
                    // Time is up, remove timer and reset progress
                    if (value <= timeDiff)
                    {
                        CriteriaTree criteriaTree = Global.CriteriaMgr.GetCriteriaTree(key);
                        if (criteriaTree.Criteria != null)
                            RemoveCriteriaProgress(criteriaTree.Criteria);

                        _timeCriteriaTrees.Remove(key);
                    }
                    else
                    {
                        _timeCriteriaTrees[key] -= timeDiff;
                    }
                }
            }
        }

        public void StartCriteriaTimer(CriteriaStartEvent startEvent, uint entry, uint timeLost = 0)
        {
            List<Criteria> criteriaList = Global.CriteriaMgr.GetTimedCriteriaByType(startEvent);
            foreach (Criteria criteria in criteriaList)
            {
                if (criteria.Entry.StartAsset != entry)
                    continue;

                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                bool canStart = false;
                foreach (CriteriaTree tree in trees)
                {
                    if ((!_timeCriteriaTrees.ContainsKey(tree.Id) || criteria.Entry.GetFlags().HasFlag(CriteriaFlags.ResetOnStart)) && !IsCompletedCriteriaTree(tree))
                    {
                        // Start the timer
                        if (criteria.Entry.StartTimer * Time.InMilliseconds > timeLost)
                        {
                            _timeCriteriaTrees[tree.Id] = (uint)(criteria.Entry.StartTimer * Time.InMilliseconds - timeLost);
                            canStart = true;
                        }
                    }
                }

                if (!canStart)
                    continue;

                // and at client too
                SetCriteriaProgress(criteria, 0, null, ProgressType.Set);
            }
        }

        public void RemoveCriteriaTimer(CriteriaStartEvent startEvent, uint entry)
        {
            List<Criteria> criteriaList = Global.CriteriaMgr.GetTimedCriteriaByType(startEvent);
            foreach (Criteria criteria in criteriaList)
            {
                if (criteria.Entry.StartAsset != entry)
                    continue;

                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                // Remove the timer from all trees
                foreach (CriteriaTree tree in trees)
                    _timeCriteriaTrees.Remove(tree.Id);

                // remove progress
                RemoveCriteriaProgress(criteria);
            }
        }

        public CriteriaProgress GetCriteriaProgress(Criteria entry)
        {
            return _criteriaProgress.LookupByKey(entry.Id);
        }

        public void SetCriteriaProgress(Criteria criteria, ulong changeValue, Player referencePlayer, ProgressType progressType = ProgressType.Set)
        {
            // Don't allow to cheat - doing timed criteria without timer active
            List<CriteriaTree> trees = null;
            if (criteria.Entry.StartTimer != 0)
            {
                trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                if (trees.Empty())
                    return;

                bool hasTreeForTimed = false;
                foreach (CriteriaTree tree in trees)
                {
                    var timedIter = _timeCriteriaTrees.LookupByKey(tree.Id);
                    if (timedIter != 0)
                    {
                        hasTreeForTimed = true;
                        break;
                    }
                }

                if (!hasTreeForTimed)
                    return;
            }

            Log.outDebug(LogFilter.Achievement, "SetCriteriaProgress({0}, {1}) for {2}", criteria.Id, changeValue, GetOwnerInfo());

            CriteriaProgress progress = GetCriteriaProgress(criteria);
            if (progress == null)
            {
                // not create record for 0 counter but allow it for timed criteria
                // we will need to send 0 progress to client to start the timer
                if (changeValue == 0 && criteria.Entry.StartTimer == 0)
                    return;

                progress = new CriteriaProgress();
                progress.Counter = changeValue;

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
                        ulong max_value = ulong.MaxValue;
                        newValue = max_value - progress.Counter > changeValue ? progress.Counter + changeValue : max_value;
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
            progress.Date = GameTime.GetGameTime(); // set the date to the latest update.
            progress.PlayerGUID = referencePlayer ? referencePlayer.GUID : ObjectGuid.Empty;
            _criteriaProgress[criteria.Id] = progress;

            TimeSpan timeElapsed = TimeSpan.Zero;
            if (criteria.Entry.StartTimer != 0)
            {
                Cypher.Assert(trees != null);

                foreach (CriteriaTree tree in trees)
                {
                    var timed = _timeCriteriaTrees.LookupByKey(tree.Id);
                    if (timed != 0)
                    {
                        // Client expects this in packet
                        timeElapsed = TimeSpan.FromSeconds(criteria.Entry.StartTimer - (timed / Time.InMilliseconds));

                        // Remove the timer, we wont need it anymore
                        if (IsCompletedCriteriaTree(tree))
                            _timeCriteriaTrees.Remove(tree.Id);
                    }
                }
            }

            SendCriteriaUpdate(criteria, progress, timeElapsed, true);
        }

        public void RemoveCriteriaProgress(Criteria criteria)
        {
            if (criteria == null)
                return;

            if (!_criteriaProgress.ContainsKey(criteria.Id))
                return;

            SendCriteriaProgressRemoved(criteria.Id);

            _criteriaProgress.Remove(criteria.Id);
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
                    foreach (CriteriaTree node in tree.Children)
                        if (!IsCompletedCriteriaTree(node))
                            return false;
                    return true;
                case CriteriaTreeOperator.Sum:
                {
                    ulong progress = 0;
                    CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
                    {
                        if (criteriaTree.Criteria != null)
                        {
                            CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);
                            if (criteriaProgress != null)
                                progress += criteriaProgress.Counter;
                        }
                    });
                    return progress >= requiredCount;
                }
                case CriteriaTreeOperator.Highest:
                {
                    ulong progress = 0;
                    CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
                    {
                        if (criteriaTree.Criteria != null)
                        {
                            CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);
                            if (criteriaProgress != null)
                                if (criteriaProgress.Counter > progress)
                                    progress = criteriaProgress.Counter;
                        }
                    });
                    return progress >= requiredCount;
                }
                case CriteriaTreeOperator.StartedAtLeast:
                {
                    ulong progress = 0;
                    foreach (CriteriaTree node in tree.Children)
                    {
                        if (node.Criteria != null)
                        {
                            CriteriaProgress criteriaProgress = GetCriteriaProgress(node.Criteria);
                            if (criteriaProgress != null)
                                if (criteriaProgress.Counter >= 1)
                                    if (++progress >= requiredCount)
                                        return true;
                        }
                    }

                    return false;
                }
                case CriteriaTreeOperator.CompleteAtLeast:
                {
                    ulong progress = 0;
                    foreach (CriteriaTree node in tree.Children)
                        if (IsCompletedCriteriaTree(node))
                            if (++progress >= requiredCount)
                                return true;

                    return false;
                }
                case CriteriaTreeOperator.ProgressBar:
                {
                    ulong progress = 0;
                    CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
                    {
                        if (criteriaTree.Criteria != null)
                        {
                            CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);
                            if (criteriaProgress != null)
                                progress += criteriaProgress.Counter * criteriaTree.Entry.Amount;
                        }
                    });
                    return progress >= requiredCount;
                }
                default:
                    break;
            }

            return false;
        }

        public virtual bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
        {
            if ((tree.Entry.Flags.HasAnyFlag(CriteriaTreeFlags.HordeOnly) && referencePlayer.Team != TeamFaction.Horde) ||
                (tree.Entry.Flags.HasAnyFlag(CriteriaTreeFlags.AllianceOnly) && referencePlayer.Team != TeamFaction.Alliance))
            {
                Log.outTrace(LogFilter.Achievement, "CriteriaHandler.CanUpdateCriteriaTree: (Id: {0} Type {1} CriteriaTree {2}) Wrong faction",
                    criteria.Id, criteria.Entry.Type, tree.Entry.Id);
                return false;
            }

            return true;
        }

        public virtual bool CanCompleteCriteriaTree(CriteriaTree tree)
        {
            return true;
        }

        bool IsCompletedCriteria(Criteria criteria, ulong requiredAmount)
        {
            CriteriaProgress progress = GetCriteriaProgress(criteria);
            if (progress == null)
                return false;

            switch (criteria.Entry.Type)
            {
                case CriteriaType.WinBattleground:
                case CriteriaType.KillCreature:
                case CriteriaType.ReachLevel:
                case CriteriaType.GuildAttainedLevel:
                case CriteriaType.SkillRaised:
                case CriteriaType.CompleteQuestsCount:
                case CriteriaType.CompleteAnyDailyQuestPerDay:
                case CriteriaType.CompleteQuestsInZone:
                case CriteriaType.DamageDealt:
                case CriteriaType.HealingDone:
                case CriteriaType.CompleteDailyQuest:
                case CriteriaType.MaxDistFallenWithoutDying:
                case CriteriaType.BeSpellTarget:
                case CriteriaType.GainAura:
                case CriteriaType.CastSpell:
                case CriteriaType.LandTargetedSpellOnTarget:
                case CriteriaType.TrackedWorldStateUIModified:
                case CriteriaType.PVPKillInArea:
                case CriteriaType.EarnHonorableKill:
                case CriteriaType.HonorableKills:
                case CriteriaType.AcquireItem:
                case CriteriaType.WinAnyRankedArena:
                case CriteriaType.EarnPersonalArenaRating:
                case CriteriaType.UseItem:
                case CriteriaType.LootItem:
                case CriteriaType.BankSlotsPurchased:
                case CriteriaType.ReputationGained:
                case CriteriaType.TotalExaltedFactions:
                case CriteriaType.GotHaircut:
                case CriteriaType.EquipItemInSlot:
                case CriteriaType.RollNeed:
                case CriteriaType.RollGreed:
                case CriteriaType.DeliverKillingBlowToClass:
                case CriteriaType.DeliverKillingBlowToRace:
                case CriteriaType.DoEmote:
                case CriteriaType.EquipItem:
                case CriteriaType.MoneyEarnedFromQuesting:
                case CriteriaType.MoneyLootedFromCreatures:
                case CriteriaType.UseGameobject:
                case CriteriaType.KillPlayer:
                case CriteriaType.CatchFishInFishingHole:
                case CriteriaType.LearnSpellFromSkillLine:
                case CriteriaType.WinDuel:
                case CriteriaType.GetLootByType:
                case CriteriaType.LearnTradeskillSkillLine:
                case CriteriaType.CompletedLFGDungeonWithStrangers:
                case CriteriaType.DeliveredKillingBlow:
                case CriteriaType.CurrencyGained:
                case CriteriaType.PlaceGarrisonBuilding:
                case CriteriaType.UniquePetsOwned:
                case CriteriaType.BattlePetReachLevel:
                case CriteriaType.ActivelyEarnPetLevel:
                case CriteriaType.LearnAnyTransmogInSlot:
                case CriteriaType.ParagonLevelIncreaseWithFaction:
                case CriteriaType.PlayerHasEarnedHonor:
                case CriteriaType.ChooseRelicTalent:
                case CriteriaType.AccountHonorLevelReached:
                case CriteriaType.EarnArtifactXPForAzeriteItem:
                case CriteriaType.AzeriteLevelReached:
                case CriteriaType.CompleteAnyReplayQuest:
                case CriteriaType.BuyItemsFromVendors:
                case CriteriaType.SellItemsToVendors:
                case CriteriaType.EnterTopLevelArea:
                    return progress.Counter >= requiredAmount;
                case CriteriaType.EarnAchievement:
                case CriteriaType.CompleteQuest:
                case CriteriaType.LearnOrKnowSpell:
                case CriteriaType.RevealWorldMapOverlay:
                case CriteriaType.RecruitGarrisonFollower:
                case CriteriaType.LearnedNewPet:
                case CriteriaType.HonorLevelIncrease:
                case CriteriaType.PrestigeLevelIncrease:
                case CriteriaType.ActivelyReachLevel:
                case CriteriaType.CollectTransmogSetFromGroup:
                    return progress.Counter >= 1;
                case CriteriaType.AchieveSkillStep:
                    return progress.Counter >= (requiredAmount * 75);
                case CriteriaType.EarnAchievementPoints:
                    return progress.Counter >= 9000;
                case CriteriaType.WinArena:
                    return requiredAmount != 0 && progress.Counter >= requiredAmount;
                case CriteriaType.Login:
                    return true;
                // handle all statistic-only criteria here
                default:
                    break;
            }

            return false;
        }

        bool CanUpdateCriteria(Criteria criteria, List<CriteriaTree> trees, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player referencePlayer)
        {
            if (Global.DisableMgr.IsDisabledFor(DisableType.Criteria, criteria.Id, null))
            {
                Log.outError(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Disabled", criteria.Id, criteria.Entry.Type);
                return false;
            }

            bool treeRequirementPassed = false;
            foreach (CriteriaTree tree in trees)
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
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Requirements not satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (criteria.Modifier != null && !ModifierTreeSatisfied(criteria.Modifier, miscValue1, miscValue2, refe, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Requirements have not been satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (!ConditionsSatisfied(criteria, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Conditions have not been satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (criteria.Entry.EligibilityWorldStateID != 0)
                if (Global.WorldStateMgr.GetValue(criteria.Entry.EligibilityWorldStateID, referencePlayer.Map) != criteria.Entry.EligibilityWorldStateValue)
                    return false;

            return true;
        }

        bool ConditionsSatisfied(Criteria criteria, Player referencePlayer)
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
                    if (referencePlayer.Group)
                        return false;
                    break;
                default:
                    break;
            }

            return true;
        }

        bool RequirementsSatisfied(Criteria criteria, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player referencePlayer)
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
                        Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)miscValue1);
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

                    Map map = referencePlayer.IsInWorld ? referencePlayer.Map : Global.MapMgr.FindMap(referencePlayer.Location.MapId, referencePlayer.InstanceId1);
                    if (!map || !map.IsDungeon)
                        return false;

                    //FIXME: work only for instances where max == min for players
                    if (map.ToInstanceMap.MaxPlayers != criteria.Entry.Asset)
                        return false;
                    break;
                }
                case CriteriaType.KilledByPlayer:
                    if (miscValue1 == 0 || !refe || !refe.IsTypeId(TypeId.Player))
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
                    CriteriaDataSet data = Global.CriteriaMgr.GetCriteriaDataSet(criteria);
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
                    WorldMapOverlayRecord worldOverlayEntry = CliDB.WorldMapOverlayStorage.LookupByKey(criteria.Entry.Asset);
                    if (worldOverlayEntry == null)
                        break;

                    bool matchFound = false;
                    for (int j = 0; j < SharedConst.MaxWorldMapOverlayArea; ++j)
                    {
                        AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(worldOverlayEntry.AreaID[j]);
                        if (area == null)
                            break;

                        if (area.AreaBit < 0)
                            continue;

                        int playerIndexOffset = (int)area.AreaBit / ActivePlayerData.ExploredZonesBits;
                        if (playerIndexOffset >= PlayerConst.ExploredZonesSize)
                            continue;

                        ulong mask = 1ul << (int)((uint)area.AreaBit % ActivePlayerData.ExploredZonesBits);
                        if (Convert.ToBoolean(referencePlayer.ActivePlayerData.ExploredZones[playerIndexOffset] & mask))
                        {
                            matchFound = true;
                            break;
                        }
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

                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
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
                        if (!refe || !refe.IsTypeId(TypeId.Player))
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
                    if (miscValue1 == 0 || miscValue2 == 0 || (long)miscValue2 < 0
                        || miscValue1 != criteria.Entry.Asset)
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
                default:
                    break;
            }
            return true;
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
                    foreach (ModifierTreeNode node in tree.Children)
                        if (!ModifierTreeSatisfied(node, miscValue1, miscValue2, refe, referencePlayer))
                            return false;
                    return true;
                case ModifierTreeOperator.Some:
                {
                    sbyte requiredAmount = Math.Max(tree.Entry.Amount, (sbyte)1);
                    foreach (ModifierTreeNode node in tree.Children)
                        if (ModifierTreeSatisfied(node, miscValue1, miscValue2, refe, referencePlayer))
                            if (--requiredAmount == 0)
                                return true;

                    return false;
                }
                default:
                    break;
            }

            return false;
        }

        bool ModifierSatisfied(ModifierTreeRecord modifier, ulong miscValue1, ulong miscValue2, WorldObject refe, Player referencePlayer)
        {
            uint reqValue = modifier.Asset;
            int secondaryAsset = modifier.SecondaryAsset;
            int tertiaryAsset = modifier.TertiaryAsset;

            switch ((ModifierTreeType)modifier.Type)
            {
                case ModifierTreeType.PlayerInebriationLevelEqualOrGreaterThan: // 1
                {
                    uint inebriation = (uint)Math.Min(Math.Max(referencePlayer.DrunkValue, referencePlayer.PlayerData.FakeInebriation), 100);
                    if (inebriation < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerMeetsCondition: // 2
                {
                    PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(reqValue);
                    if (playerCondition == null || !ConditionManager.IsPlayerMeetingCondition(referencePlayer, playerCondition))
                        return false;
                    break;
                }
                case ModifierTreeType.MinimumItemLevel: // 3
                {
                    // miscValue1 is itemid
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || item.GetBaseItemLevel() < reqValue)
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
                    if (refe == null || !refe.IsUnit || refe.AsUnit.IsAlive)
                        return false;
                    break;
                case ModifierTreeType.TargetIsOppositeFaction: // 7
                    if (refe == null || !referencePlayer.IsHostileTo(refe))
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
                    if (refe == null || !refe.IsUnit || !refe.AsUnit.HasAura(reqValue))
                        return false;
                    break;
                case ModifierTreeType.TargetHasAuraEffect: // 11
                    if (refe == null || !refe.IsUnit || !refe.AsUnit.HasAuraType((AuraType)reqValue))
                        return false;
                    break;
                case ModifierTreeType.TargetHasAuraState: // 12
                    if (refe == null || !refe.IsUnit || !refe.AsUnit.HasAuraState((AuraStateType)reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAuraState: // 13
                    if (!referencePlayer.HasAuraState((AuraStateType)reqValue))
                        return false;
                    break;
                case ModifierTreeType.ItemQualityIsAtLeast: // 14
                {
                    // miscValue1 is itemid
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || (uint)item.GetQuality() < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.ItemQualityIsExactly: // 15
                {
                    // miscValue1 is itemid
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || (uint)item.GetQuality() != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsAlive: // 16
                    if (referencePlayer.IsDead)
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInArea: // 17
                {
                    referencePlayer.GetZoneAndAreaId(out uint zoneId, out uint areaId);
                    if (zoneId != reqValue && areaId != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.TargetIsInArea: // 18
                {
                    if (refe == null)
                        return false;
                    refe.GetZoneAndAreaId(out uint zoneId, out uint areaId);
                    if (zoneId != reqValue && areaId != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.ItemId: // 19
                    if (miscValue1 != reqValue)
                        return false;
                    break;
                case ModifierTreeType.LegacyDungeonDifficulty: // 20
                {
                    DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(referencePlayer.Map.DifficultyID);
                    if (difficulty == null || difficulty.OldEnumValue == -1 || difficulty.OldEnumValue != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerToTargetLevelDeltaGreaterThan: // 21
                    if (refe == null || !refe.IsUnit || referencePlayer.Level < refe.AsUnit.Level + reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetToPlayerLevelDeltaGreaterThan: // 22
                    if (!refe || !refe.IsUnit || referencePlayer.Level + reqValue < refe.AsUnit.Level)
                        return false;
                    break;
                case ModifierTreeType.PlayerLevelEqualTargetLevel: // 23
                    if (!refe || !refe.IsUnit || referencePlayer.Level != refe.AsUnit.Level)
                        return false;
                    break;
                case ModifierTreeType.PlayerInArenaWithTeamSize: // 24
                {
                    Battleground bg = referencePlayer.Battleground;
                    if (!bg || !bg.IsArena() || bg.GetArenaType() != (ArenaTypes)reqValue)
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
                    if (refe == null || !refe.IsUnit || refe.AsUnit.Race != (Race)reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetClass: // 28
                    if (refe == null || !refe.IsUnit || refe.AsUnit.Class != (Class)reqValue)
                        return false;
                    break;
                case ModifierTreeType.LessThanTappers: // 29
                    if (referencePlayer.Group && referencePlayer.Group.MembersCount >= reqValue)
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
                    if (!refe)
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
                    if (reqValue < Global.RealmMgr.GetMinorMajorBugfixVersionForBuild(Global.WorldMgr.Realm.Build))
                        return false;
                    break;
                case ModifierTreeType.BattlePetTeamLevel: // 34
                    foreach (BattlePetSlot slot in referencePlayer.Session.BattlePetMgr.Slots)
                        if (slot.Pet.Level < reqValue)
                            return false;
                    break;
                case ModifierTreeType.PlayerIsNotInParty: // 35
                    if (referencePlayer.Group)
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInParty: // 36
                    if (!referencePlayer.Group)
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
                    uint zoneId = referencePlayer.Area;
                    AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
                    if (areaEntry != null)
                        if (areaEntry.HasFlag(AreaFlags.Unk9))
                            zoneId = areaEntry.ParentAreaID;
                    if (zoneId != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.TargetIsInZone: // 42
                {
                    if (!refe)
                        return false;
                    uint zoneId = refe.Area;
                    AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
                    if (areaEntry != null)
                        if (areaEntry.HasFlag(AreaFlags.Unk9))
                            zoneId = areaEntry.ParentAreaID;
                    if (zoneId != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHealthBelowPercent: // 43
                    if (referencePlayer.HealthPct > (float)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHealthAbovePercent: // 44
                    if (referencePlayer.HealthPct < (float)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHealthEqualsPercent: // 45
                    if (referencePlayer.HealthPct != (float)reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthBelowPercent: // 46
                    if (refe == null || !refe.IsUnit || refe.AsUnit.HealthPct > reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthAbovePercent: // 47
                    if (!refe || !refe.IsUnit || refe.AsUnit.HealthPct < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthEqualsPercent: // 48
                    if (!refe || !refe.IsUnit || refe.AsUnit.HealthPct != reqValue)
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
                    if (!refe || !refe.IsUnit || refe.AsUnit.Health > reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthAboveValue: // 53
                    if (!refe || !refe.IsUnit || refe.AsUnit.Health < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthEqualsValue: // 54
                    if (!refe || !refe.IsUnit || refe.AsUnit.Health != reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetIsPlayerAndMeetsCondition: // 55
                {
                    if (refe == null || !refe.IsPlayer)
                        return false;

                    PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(reqValue);
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
                    Battleground bg = referencePlayer.Battleground;
                    if (bg == null || !bg.IsArena() || !bg.IsRated())
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
                    Battleground bg = referencePlayer.Battleground;
                    if (bg == null || !bg.IsBattleground() || !bg.IsRated())
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
                    WorldStateExpressionRecord worldStateExpression = CliDB.WorldStateExpressionStorage.LookupByKey(reqValue);
                    if (worldStateExpression != null)
                        return ConditionManager.IsPlayerMeetingExpression(referencePlayer, worldStateExpression);
                    return false;
                case ModifierTreeType.DungeonDifficulty: // 68
                    if (referencePlayer.Map.DifficultyID != (Difficulty)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerLevelEqualOrGreaterThan: // 69
                    if (referencePlayer.Level < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetLevelEqualOrGreaterThan: // 70
                    if (!refe || !refe.IsUnit || refe.AsUnit.Level < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerLevelEqualOrLessThan: // 71
                    if (referencePlayer.Level > reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetLevelEqualOrLessThan: // 72
                    if (!refe || !refe.IsUnit || refe.AsUnit.Level > reqValue)
                        return false;
                    break;
                case ModifierTreeType.ModifierTree: // 73
                    ModifierTreeNode nextModifierTree = Global.CriteriaMgr.GetModifierTree(reqValue);
                    if (nextModifierTree != null)
                        return ModifierTreeSatisfied(nextModifierTree, miscValue1, miscValue2, refe, referencePlayer);
                    return false;
                case ModifierTreeType.PlayerScenario: // 74
                {
                    Scenario scenario = referencePlayer.Scenario;
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
                    static short getRootAchievementCategory(AchievementRecord achievement)
                    {
                        short category = (short)achievement.Category;
                        do
                        {
                            var categoryEntry = CliDB.AchievementCategoryStorage.LookupByKey(category);
                            if (categoryEntry?.Parent == -1)
                                break;

                            category = categoryEntry.Parent;
                        } while (true);

                        return category;
                    }

                    uint petAchievementPoints = 0;
                    foreach (uint achievementId in referencePlayer.CompletedAchievementIds)
                    {
                        var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);
                        if (getRootAchievementCategory(achievement) == SharedConst.AchivementCategoryPetBattles)
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
                    var speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(miscValue1);
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
                    {
                        for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                            if (itr.Source.GuildId == referencePlayer.GuildId)
                                ++guildMemberCount;
                    }

                    if (guildMemberCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.BattlePetOpponentCreatureId: // 81 NYI
                    return false;
                case ModifierTreeType.PlayerScenarioStep: // 82
                {
                    Scenario scenario = referencePlayer.Scenario;
                    if (scenario == null)
                        return false;

                    if (scenario.GetStep().OrderIndex != (reqValue - 1))
                        return false;
                    break;
                }
                case ModifierTreeType.ChallengeModeMedal: // 83
                    return false; // OBSOLETE
                case ModifierTreeType.PlayerOnQuest: // 84
                    if (referencePlayer.FindQuestSlot(reqValue) == SharedConst.MaxQuestLogSize)
                        return false;
                    break;
                case ModifierTreeType.ExaltedWithFaction: // 85
                    if (referencePlayer.ReputationMgr.GetReputation(reqValue) < 42000)
                        return false;
                    break;
                case ModifierTreeType.EarnedAchievementOnAccount: // 86
                case ModifierTreeType.EarnedAchievementOnPlayer: // 87
                    if (!referencePlayer.HasAchieved(reqValue))
                        return false;
                    break;
                case ModifierTreeType.OrderOfTheCloudSerpentReputationGreaterThan: // 88
                    if (referencePlayer.ReputationMgr.GetReputation(1271) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.BattlePetQuality: // 89 NYI
                case ModifierTreeType.BattlePetFightWasPVP: // 90 NYI
                    return false;
                case ModifierTreeType.BattlePetSpecies: // 91
                    if (miscValue1 != reqValue)
                        return false;
                    break;
                case ModifierTreeType.ServerExpansionEqualOrGreaterThan: // 92
                    if (ConfigMgr.GetDefaultValue("character.EnforceRaceAndClassExpansions", true) && WorldConfig.GetIntValue(WorldCfg.Expansion) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasBattlePetJournalLock: // 93
                    if (!referencePlayer.Session.BattlePetMgr.HasJournalLock)
                        return false;
                    break;
                case ModifierTreeType.FriendshipRepReactionIsMet: // 94
                {
                    var friendshipRepReaction = CliDB.FriendshipRepReactionStorage.LookupByKey(reqValue);
                    if (friendshipRepReaction == null)
                        return false;

                    var friendshipReputation = CliDB.FriendshipReputationStorage.LookupByKey(friendshipRepReaction.FriendshipRepID);
                    if (friendshipReputation == null)
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
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || item.GetClass() != (ItemClass)reqValue || item.GetSubClass() != secondaryAsset)
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
                    var languageDescs = Global.LanguageMgr.GetLanguageDescById((Language)reqValue);
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
                    if (referencePlayer.GetItemCount(reqValue, false) < secondaryAsset)
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
                    if (Global.WorldStateMgr.GetValue((int)reqValue, referencePlayer.Map) != secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.TimeBetween: // 109
                {
                    long from = Time.GetUnixTimeFromPackedTime(reqValue);
                    long to = Time.GetUnixTimeFromPackedTime((uint)secondaryAsset);
                    if (GameTime.GetGameTime() < from || GameTime.GetGameTime() > to)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedQuest: // 110
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(reqValue);
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
                    QuestObjective objective = Global.ObjectMgr.GetQuestObjective(reqValue);
                    if (objective == null)
                        return false;

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);
                    if (quest == null)
                        return false;

                    ushort slot = referencePlayer.FindQuestSlot(objective.QuestID);
                    if (slot >= SharedConst.MaxQuestLogSize || referencePlayer.GetQuestRewardStatus(objective.QuestID) || !referencePlayer.IsQuestObjectiveComplete(slot, quest, objective))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasExploredArea: // 113
                {
                    AreaTableRecord areaTable = CliDB.AreaTableStorage.LookupByKey(reqValue);
                    if (areaTable == null)
                        return false;

                    if (areaTable.AreaBit <= 0)
                        break; // success

                    int playerIndexOffset = areaTable.AreaBit / ActivePlayerData.ExploredZonesBits;
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
                    if (referencePlayer.Map.GetZoneWeather(referencePlayer.Zone) != (WeatherState)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerFaction: // 116
                {
                    ChrRacesRecord race = CliDB.ChrRacesStorage.LookupByKey(referencePlayer.Race);
                    if (race == null)
                        return false;

                    FactionTemplateRecord faction = CliDB.FactionTemplateStorage.LookupByKey(race.FactionID);
                    if (faction == null)
                        return false;

                    int factionIndex = -1;
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
                    if (refe == null)
                        return false;
                    Unit unitRef = refe.AsUnit;
                    if (unitRef == null || !unitRef.CanHaveThreatList)
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
                    if ((uint)referencePlayer.Map.Entry.InstanceType != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerInTimeWalkerInstance: // 123
                    if (!referencePlayer.HasPlayerFlag(PlayerFlags.Timewalking))
                        return false;
                    break;
                case ModifierTreeType.PvpSeasonIsActive: // 124
                    if (!WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress))
                        return false;
                    break;
                case ModifierTreeType.PvpSeason: // 125
                    if (WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId) != reqValue)
                        return false;
                    break;
                case ModifierTreeType.GarrisonTierEqualOrGreaterThan: // 126
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset || garrison.GetSiteLevel().GarrLevel < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowersWithLevelEqualOrGreaterThan: // 127
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(secondaryAsset);
                    if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                        if (followerBuilding == null)
                            return false;

                        return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue); ;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerWithTraitAssignedToBuilding: // 132
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);
                    if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                        if (followerBuilding == null)
                            return false;

                        return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue); ;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerWithLevelAssignedToBuilding: // 133
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        if (follower.PacketInfo.FollowerLevel < reqValue)
                            return false;

                        GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                        if (followerBuilding == null)
                            return false;

                        return followerBuilding.BuildingType == secondaryAsset;
                    });
                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonBuildingWithLevelEqualOrGreaterThan: // 134
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    foreach (Garrison.Plot plot in garrison.GetPlots())
                    {
                        if (plot.BuildingInfo.PacketInfo == null)
                            continue;

                        GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);
                        if (building == null || building.UpgradeLevel < reqValue || building.BuildingType != secondaryAsset)
                            continue;

                        return true;
                    }
                    return false;
                }
                case ModifierTreeType.HasBlueprintForGarrisonBuilding: // 135
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                        return false;

                    if (!garrison.HasBlueprint(reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.HasGarrisonBuildingSpecialization: // 136
                    return false; // OBSOLETE
                case ModifierTreeType.AllGarrisonPlotsAreFull: // 137
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)reqValue)
                        return false;

                    foreach (var plot in garrison.GetPlots())
                        if (plot.BuildingInfo.PacketInfo == null)
                            return false;
                    break;
                }
                case ModifierTreeType.PlayerIsInOwnGarrison: // 138
                    if (!referencePlayer.Map.IsGarrison || referencePlayer.Map.InstanceId != referencePlayer.GUID.Counter)
                        return false;
                    break;
                case ModifierTreeType.GarrisonShipmentOfTypeIsPending: // 139 NYI
                    return false;
                case ModifierTreeType.GarrisonBuildingIsUnderConstruction: // 140
                {
                    GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(reqValue);
                    if (building == null)
                        return false;

                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    foreach (Garrison.Plot plot in garrison.GetPlots())
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    foreach (Garrison.Plot plot in garrison.GetPlots())
                    {
                        if (plot.BuildingInfo.PacketInfo == null)
                            continue;

                        GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);
                        if (building == null || building.UpgradeLevel != secondaryAsset || building.BuildingType != reqValue)
                            continue;

                        return true;
                    }
                    return false;
                }
                case ModifierTreeType.GarrisonFollowerHasAbility: // 143
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                        return false;

                    if (miscValue1 != 0)
                    {
                        Garrison.Follower follower = garrison.GetFollower(miscValue1);
                        if (follower == null)
                            return false;

                        if (!follower.HasAbility(reqValue))
                            return false;
                    }
                    else
                    {
                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.HasAbility(reqValue);
                        });

                        if (followerCount < 1)
                            return false;
                    }
                    break;
                }
                case ModifierTreeType.GarrisonFollowerHasTrait: // 144
                {
                    GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);
                    if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                        return false;

                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                        return false;

                    if (miscValue1 != 0)
                    {
                        Garrison.Follower follower = garrison.GetFollower(miscValue1);
                        if (follower == null || !follower.HasAbility(reqValue))
                            return false;
                    }
                    else
                    {
                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.HasAbility(reqValue);
                        });

                        if (followerCount < 1)
                            return false;
                    }
                    break;
                }
                case ModifierTreeType.GarrisonFollowerQualityEqual: // 145
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != GarrisonType.Garrison)
                        return false;

                    if (miscValue1 != 0)
                    {
                        Garrison.Follower follower = garrison.GetFollower(miscValue1);
                        if (follower == null || follower.PacketInfo.Quality < reqValue)
                            return false;
                    }
                    else
                    {
                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.Quality >= reqValue;
                        });

                        if (followerCount < 1)
                            return false;
                    }
                    break;
                }
                case ModifierTreeType.GarrisonFollowerLevelEqual: // 146
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                        return false;

                    if (miscValue1 != 0)
                    {
                        Garrison.Follower follower = garrison.GetFollower(miscValue1);
                        if (follower == null || follower.PacketInfo.FollowerLevel != reqValue)
                            return false;
                    }
                    else
                    {
                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.FollowerLevel == reqValue;
                        });

                        if (followerCount < 1)
                            return false;
                    }
                    break;
                }
                case ModifierTreeType.GarrisonMissionIsRare: // 147 NYI
                case ModifierTreeType.GarrisonMissionIsElite: // 148 NYI
                    return false;
                case ModifierTreeType.CurrentGarrisonBuildingLevelEqual: // 149
                {
                    if (miscValue1 == 0)
                        return false;

                    Garrison garrison = referencePlayer.Garrison;
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    var plot = garrison.GetPlot(reqValue);
                    if (plot == null)
                        return false;

                    if (!plot.BuildingInfo.CanActivate() || plot.BuildingInfo.PacketInfo == null || plot.BuildingInfo.PacketInfo.Active)
                        return false;
                    break;
                }
                case ModifierTreeType.BattlePetTeamWithSpeciesEqualOrGreaterThan: // 151
                {
                    uint count = 0;
                    foreach (BattlePetSlot slot in referencePlayer.Session.BattlePetMgr.Slots)
                        if (slot.Pet.Species == secondaryAsset)
                            ++count;

                    if (count < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.BattlePetTeamWithTypeEqualOrGreaterThan: // 152
                {
                    uint count = 0;
                    foreach (BattlePetSlot slot in referencePlayer.Session.BattlePetMgr.Slots)
                    {
                        BattlePetSpeciesRecord species = CliDB.BattlePetSpeciesStorage.LookupByKey(slot.Pet.Species);
                        if (species != null)
                            if (species.PetTypeEnum == secondaryAsset)
                                ++count;
                    }

                    if (count < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PetBattleLastAbility: // 153 NYI
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
                    return false; // OBSOLETE
                case ModifierTreeType.HasGarrisonFollower: // 157
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        return follower.PacketInfo.GarrFollowerID == reqValue;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerQuestObjectiveProgressEqual: // 158
                {
                    QuestObjective objective = Global.ObjectMgr.GetQuestObjective(reqValue);
                    if (objective == null)
                        return false;

                    if (referencePlayer.GetQuestObjectiveData(objective) != secondaryAsset)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerQuestObjectiveProgressEqualOrGreaterThan: // 159
                {
                    QuestObjective objective = Global.ObjectMgr.GetQuestObjective(reqValue);
                    if (objective == null)
                        return false;

                    if (referencePlayer.GetQuestObjectiveData(objective) < secondaryAsset)
                        return false;
                    break;
                }
                case ModifierTreeType.IsPTRRealm: // 160
                case ModifierTreeType.IsBetaRealm: // 161
                case ModifierTreeType.IsQARealm: // 162
                    return false; // always false
                case ModifierTreeType.GarrisonShipmentContainerIsFull: // 163
                    return false;
                case ModifierTreeType.PlayerCountIsValidToStartGarrisonInvasion: // 164
                    return true; // Only 1 player is required and referencePlayer.GetMap() will ALWAYS have at least the referencePlayer on it
                case ModifierTreeType.InstancePlayerCountEqualOrLessThan: // 165
                    if (referencePlayer.Map.GetPlayersCountExceptGMs() > reqValue)
                        return false;
                    break;
                case ModifierTreeType.AllGarrisonPlotsFilledWithBuildingsWithLevelEqualOrGreater: // 166
                {
                    Garrison garrison = referencePlayer.Garrison;
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

                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        return follower.PacketInfo.GarrFollowerID == miscValue1 && follower.GetItemLevel() >= reqValue;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerCountWithItemLevelEqualOrGreaterThan: // 169
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset || garrison.GetSiteLevel().GarrLevel != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.InstancePlayerCountEqual: // 171
                    if (referencePlayer.Map.Players.Count != reqValue)
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
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(reqValue);
                    if (quest == null)
                        return false;

                    if (!referencePlayer.CanTakeQuest(quest, false))
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerCountWithLevelEqualOrGreaterThan: // 175
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
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
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        return follower.PacketInfo.GarrFollowerID == reqValue && follower.PacketInfo.CurrentBuildingID == secondaryAsset;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonMissionCountLessThan: // 177 NYI
                    return false;
                case ModifierTreeType.GarrisonPlotInstanceCountEqualOrGreaterThan: // 178
                {
                    Garrison garrison = referencePlayer.Garrison;
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
                    if (!referencePlayer.Map.IsGarrison || referencePlayer.Map.InstanceId == referencePlayer.GUID.Counter)
                        return false;
                    break;
                case ModifierTreeType.HasActiveGarrisonFollower: // 181
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue && (follower.PacketInfo.FollowerStatus & (byte)GarrisonFollowerStatus.Inactive) == 0);
                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerDailyRandomValueMod_X_Equals: // 182 NYI
                    return false;
                case ModifierTreeType.PlayerHasMount: // 183
                {
                    foreach (var pair in referencePlayer.Session.CollectionMgr.GetAccountMounts())
                    {
                        var mount = Global.DB2Mgr.GetMount(pair.Key);
                        if (mount == null)
                            continue;

                        if (mount.Id == reqValue)
                            return true;
                    }
                    return false;
                }
                case ModifierTreeType.GarrisonFollowerCountWithInactiveWithItemLevelEqualOrGreaterThan: // 184
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        GarrFollowerRecord garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                        if (garrFollower == null)
                            return false;

                        return follower.GetItemLevel() >= secondaryAsset && garrFollower.GarrFollowerTypeID == tertiaryAsset;
                    });

                    if (followerCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerIsOnAMission: // 185
                {
                    Garrison garrison = referencePlayer.Garrison;
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue && follower.PacketInfo.CurrentMissionID != 0);
                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonMissionCountInSetLessThan: // 186 NYI
                    return false;
                case ModifierTreeType.GarrisonFollowerType: // 187
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(miscValue1);
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
                    return false; // OBSOLOTE
                case ModifierTreeType.GarrisonMissionIsReadyToCollect: // 195 NYI
                case ModifierTreeType.PlayerIsInstanceOwner: // 196 NYI
                    return false;
                case ModifierTreeType.PlayerHasHeirloom: // 197
                    if (!referencePlayer.Session.CollectionMgr.GetAccountHeirlooms().ContainsKey(reqValue))
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
                    var (PermAppearance, TempAppearance) = referencePlayer.Session.CollectionMgr.HasItemAppearance(reqValue);
                    if (!PermAppearance || TempAppearance)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonTalentSelected: // 201 NYI
                case ModifierTreeType.GarrisonTalentResearched: // 202 NYI
                    return false;
                case ModifierTreeType.PlayerHasRestriction: // 203
                {
                    int restrictionIndex = referencePlayer.ActivePlayerData.CharacterRestrictions.FindIndexIf(restriction => restriction.Type == reqValue);
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
                    Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)miscValue1);
                    if (quest == null || quest.Id != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonTalentResearchInProgress: // 207 NYI
                    return false;
                case ModifierTreeType.PlayerEquippedArtifactAppearanceSet: // 208
                {
                    Aura artifactAura = referencePlayer.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                    if (artifactAura != null)
                    {
                        Item artifact = referencePlayer.GetItemByGuid(artifactAura.CastItemGuid);
                        if (artifact != null)
                        {
                            ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifact.GetModifier(ItemModifier.ArtifactAppearanceId));
                            if (artifactAppearance != null)
                                if (artifactAppearance.ArtifactAppearanceSetID == reqValue)
                                    break;
                        }
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
                    Scenario scenario = referencePlayer.Scenario;
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
                    {
                        for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                            if (itr.Source != referencePlayer && referencePlayer.PlayerData.VirtualPlayerRealm == itr.Source.PlayerData.VirtualPlayerRealm)
                                ++memberCount;
                    }

                    if (memberCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.ArtifactTraitUnlockedCountEqualOrGreaterThan: // 217
                {
                    Item artifact = referencePlayer.GetItemByEntry((uint)secondaryAsset, ItemSearchLocation.Everywhere);
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
                    var bg = CliDB.BattlemasterListStorage.LookupByKey(referencePlayer.BattlegroundTypeId);
                    if (bg == null || !bg.Flags.HasFlag(BattlemasterListFlags.Brawl))
                        return false;
                    break;
                }
                case ModifierTreeType.ParagonReputationLevelWithFactionEqualOrGreaterThan: // 221
                {
                    var faction = CliDB.FactionStorage.LookupByKey(secondaryAsset);
                    if (faction == null)
                        return false;

                    if (referencePlayer.ReputationMgr.GetParagonLevel(faction.ParagonFactionID) < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasItemWithBonusListFromTreeAndQuality: // 222
                {
                    var bonusListIDs = Global.DB2Mgr.GetAllItemBonusTreeBonuses(reqValue);
                    if (bonusListIDs.Empty())
                        return false;

                    bool bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Everywhere, item =>
                    {
                        bool hasBonus = item.GetBonusListIDs().Any(bonusListID => bonusListIDs.Contains(bonusListID));
                        return !hasBonus;
                    });

                    if (bagScanReachedEnd)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasEmptyInventorySlotCountEqualOrGreaterThan: // 223
                    if (referencePlayer.GetFreeInventorySlotCount(ItemSearchLocation.Inventory) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasItemInHistoryOfProgressiveEvent: // 224 NYI
                    return false;
                case ModifierTreeType.PlayerHasArtifactPowerRankCountPurchasedEqualOrGreaterThan: // 225
                {
                    Aura artifactAura = referencePlayer.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                    if (artifactAura == null)
                        return false;

                    Item artifact = referencePlayer.GetItemByGuid(artifactAura.CastItemGuid);
                    if (!artifact)
                        return false;

                    var artifactPower = artifact.GetArtifactPower((uint)secondaryAsset);
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
                    uint itemSubclass = (uint)ItemSubClassWeapon.Fist;
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(visibleItem.ItemID);
                    if (itemTemplate != null)
                    {
                        if (itemTemplate.GetClass() == ItemClass.Weapon)
                        {
                            itemSubclass = itemTemplate.GetSubClass();

                            var itemModifiedAppearance = Global.DB2Mgr.GetItemModifiedAppearance(visibleItem.ItemID, visibleItem.ItemAppearanceModID);
                            if (itemModifiedAppearance != null)
                            {
                                var itemModifiedAppearaceExtra = CliDB.ItemModifiedAppearanceExtraStorage.LookupByKey(itemModifiedAppearance.Id);
                                if (itemModifiedAppearaceExtra != null)
                                    if (itemModifiedAppearaceExtra.DisplayWeaponSubclassID > 0)
                                        itemSubclass = (uint)itemModifiedAppearaceExtra.DisplayWeaponSubclassID;
                            }
                        }
                    }
                    if (itemSubclass != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerOffhandWeaponType: // 233
                {
                    var visibleItem = referencePlayer.PlayerData.VisibleItems[EquipmentSlot.OffHand];
                    uint itemSubclass = (uint)ItemSubClassWeapon.Fist;
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(visibleItem.ItemID);
                    if (itemTemplate != null)
                    {
                        if (itemTemplate.GetClass() == ItemClass.Weapon)
                        {
                            itemSubclass = itemTemplate.GetSubClass();

                            var itemModifiedAppearance = Global.DB2Mgr.GetItemModifiedAppearance(visibleItem.ItemID, visibleItem.ItemAppearanceModID);
                            if (itemModifiedAppearance != null)
                            {
                                var itemModifiedAppearaceExtra = CliDB.ItemModifiedAppearanceExtraStorage.LookupByKey(itemModifiedAppearance.Id);
                                if (itemModifiedAppearaceExtra != null)
                                    if (itemModifiedAppearaceExtra.DisplayWeaponSubclassID > 0)
                                        itemSubclass = (uint)itemModifiedAppearaceExtra.DisplayWeaponSubclassID;
                            }
                        }
                    }
                    if (itemSubclass != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerPvpTier: // 234
                {
                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(reqValue);
                    if (pvpTier == null)
                        return false;

                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)pvpTier.BracketID);
                    if (pvpInfo == null)
                        return false;

                    if (pvpTier.Id != pvpInfo.PvpTierID)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerAzeriteLevelEqualOrGreaterThan: // 235
                {
                    Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                    if (!heartOfAzeroth || heartOfAzeroth.ToAzeriteItem().GetLevel() < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsOnQuestInQuestline: // 236
                {
                    bool isOnQuest = false;
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (!questLineQuests.Empty())
                        isOnQuest = questLineQuests.Any(questLineQuest => referencePlayer.FindQuestSlot(questLineQuest.QuestID) < SharedConst.MaxQuestLogSize);

                    if (!isOnQuest)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsQnQuestLinkedToScheduledWorldStateGroup: // 237
                    return false; // OBSOLETE (db2 removed)
                case ModifierTreeType.PlayerIsInRaidGroup: // 238
                {
                    var group = referencePlayer.Group;
                    if (group == null || !group.IsRaidGroup)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerPvpTierInBracketEqualOrGreaterThan: // 239
                {
                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)secondaryAsset);
                    if (pvpInfo == null)
                        return false;

                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(pvpInfo.PvpTierID);
                    if (pvpTier == null)
                        return false;

                    if (pvpTier.Rank < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerCanAcceptQuestInQuestline: // 240
                {
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    bool canTakeQuest = questLineQuests.Any(questLineQuest =>
                    {
                        Quest quest = Global.ObjectMgr.GetQuestTemplate(questLineQuest.QuestID);
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
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    foreach (var questLineQuest in questLineQuests)
                        if (!referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID))
                            return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedQuestlineQuestCount: // 242
                {
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    uint completedQuests = 0;
                    foreach (var questLineQuest in questLineQuests)
                        if (referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID))
                            ++completedQuests;

                    if (completedQuests < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedPercentageOfQuestline: // 243
                {
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    int completedQuests = 0;
                    foreach (var questLineQuest in questLineQuests)
                        if (referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID))
                            ++completedQuests;

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
                case ModifierTreeType.MythicPlusCompletedInTime: // 248 NYI
                case ModifierTreeType.MythicPlusMapChallengeMode: // 249 NYI
                case ModifierTreeType.MythicPlusDisplaySeason: // 250 NYI
                case ModifierTreeType.MythicPlusMilestoneSeason: // 251 NYI
                    return false;
                case ModifierTreeType.PlayerVisibleRace: // 252
                {
                    CreatureDisplayInfoRecord creatureDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(referencePlayer.DisplayId);
                    if (creatureDisplayInfo == null)
                        return false;

                    CreatureDisplayInfoExtraRecord creatureDisplayInfoExtra = CliDB.CreatureDisplayInfoExtraStorage.LookupByKey(creatureDisplayInfo.ExtendedDisplayInfoID);
                    if (creatureDisplayInfoExtra == null)
                        return false;

                    if (creatureDisplayInfoExtra.DisplayRaceID != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.TargetVisibleRace: // 253
                {
                    if (refe == null || !refe.IsUnit)
                        return false;
                    CreatureDisplayInfoRecord creatureDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(refe.AsUnit.DisplayId);
                    if (creatureDisplayInfo == null)
                        return false;

                    CreatureDisplayInfoExtraRecord creatureDisplayInfoExtra = CliDB.CreatureDisplayInfoExtraStorage.LookupByKey(creatureDisplayInfo.ExtendedDisplayInfoID);
                    if (creatureDisplayInfoExtra == null)
                        return false;

                    if (creatureDisplayInfoExtra.DisplayRaceID != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.FriendshipRepReactionEqual: // 254
                {
                    var friendshipRepReaction = CliDB.FriendshipRepReactionStorage.LookupByKey(reqValue);
                    if (friendshipRepReaction == null)
                        return false;

                    var friendshipReputation = CliDB.FriendshipReputationStorage.LookupByKey(friendshipRepReaction.FriendshipRepID);
                    if (friendshipReputation == null)
                        return false;

                    var friendshipReactions = Global.DB2Mgr.GetFriendshipRepReactions(reqValue);
                    if (friendshipReactions == null)
                        return false;

                    int rank = (int)referencePlayer.GetReputationRank((uint)friendshipReputation.FactionID);
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
                    if (!refe || !refe.IsUnit || refe.AsUnit.GetAuraCount((uint)secondaryAsset) != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerAuraStackCountEqualOrGreaterThan: // 257
                    if (referencePlayer.GetAuraCount((uint)secondaryAsset) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetAuraStackCountEqualOrGreaterThan: // 258
                    if (!refe || !refe.IsUnit || refe.AsUnit.GetAuraCount((uint)secondaryAsset) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAzeriteEssenceRankLessThan: // 259
                {
                    Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                    if (heartOfAzeroth != null)
                    {
                        AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                        if (azeriteItem != null)
                        {
                            foreach (UnlockedAzeriteEssence essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                                if (essence.AzeriteEssenceID == reqValue && essence.Rank < secondaryAsset)
                                    return true;
                        }
                    }
                    return false;
                }
                case ModifierTreeType.PlayerHasAzeriteEssenceRankEqual: // 260
                {
                    Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                    if (heartOfAzeroth != null)
                    {
                        AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                        if (azeriteItem != null)
                        {
                            foreach (UnlockedAzeriteEssence essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                                if (essence.AzeriteEssenceID == reqValue && essence.Rank == secondaryAsset)
                                    return true;
                        }
                    }
                    return false;
                }
                case ModifierTreeType.PlayerHasAzeriteEssenceRankGreaterThan: // 261
                {
                    Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                    if (heartOfAzeroth != null)
                    {
                        AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                        if (azeriteItem != null)
                        {
                            foreach (UnlockedAzeriteEssence essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                                if (essence.AzeriteEssenceID == reqValue && essence.Rank > secondaryAsset)
                                    return true;
                        }
                    }
                    return false;
                }
                case ModifierTreeType.PlayerHasAuraWithEffectIndex: // 262
                    if (referencePlayer.GetAuraEffect(reqValue, secondaryAsset) == null)
                        return false;
                    break;
                case ModifierTreeType.PlayerLootSpecializationMatchesRole: // 263
                {
                    ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(referencePlayer.GetPrimarySpecialization());
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
                    var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(miscValue2);
                    if (itemModifiedAppearance == null)
                        return false;

                    if (itemModifiedAppearance.TransmogSourceTypeEnum != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasAzeriteEssenceInSlotAtRankLessThan: // 266
                {
                    Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                    if (heartOfAzeroth != null)
                    {
                        AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                        if (azeriteItem != null)
                        {
                            SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
                            if (selectedEssences != null)
                            {
                                foreach (UnlockedAzeriteEssence essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                                    if (essence.AzeriteEssenceID == selectedEssences.AzeriteEssenceID[(int)reqValue] && essence.Rank < secondaryAsset)
                                        return true;
                            }
                        }
                    }
                    return false;
                }
                case ModifierTreeType.PlayerHasAzeriteEssenceInSlotAtRankGreaterThan: // 267
                {
                    Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                    if (heartOfAzeroth != null)
                    {
                        AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                        if (azeriteItem != null)
                        {
                            SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
                            if (selectedEssences != null)
                            {
                                foreach (UnlockedAzeriteEssence essence in azeriteItem.AzeriteItemData.UnlockedEssences)
                                    if (essence.AzeriteEssenceID == selectedEssences.AzeriteEssenceID[(int)reqValue] && essence.Rank > secondaryAsset)
                                        return true;
                            }
                        }
                    }
                    return false;
                }
                case ModifierTreeType.PlayerLevelWithinContentTuning: // 268
                {
                    uint level = referencePlayer.Level;
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
                    if (levels.HasValue)
                    {
                        if (secondaryAsset != 0)
                            return level >= levels.Value.MinLevelWithDelta && level <= levels.Value.MaxLevelWithDelta;
                        return level >= levels.Value.MinLevel && level <= levels.Value.MaxLevel;
                    }
                    return false;
                }
                case ModifierTreeType.TargetLevelWithinContentTuning: // 269
                {
                    if (!refe || !refe.IsUnit)
                        return false;

                    uint level = refe.AsUnit.Level;
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
                    if (levels.HasValue)
                    {
                        if (secondaryAsset != 0)
                            return level >= levels.Value.MinLevelWithDelta && level <= levels.Value.MaxLevelWithDelta;
                        return level >= levels.Value.MinLevel && level <= levels.Value.MaxLevel;
                    }
                    return false;
                }
                case ModifierTreeType.PlayerIsScenarioInitiator: // 270 NYI
                    return false;
                case ModifierTreeType.PlayerHasCompletedQuestOrIsOnQuest: // 271
                {
                    QuestStatus status = referencePlayer.GetQuestStatus(reqValue);
                    if (status == QuestStatus.None || status == QuestStatus.Failed)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerLevelWithinOrAboveContentTuning: // 272
                {
                    uint level = referencePlayer.Level;
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
                    if (levels.HasValue)
                        return secondaryAsset != 0 ? level >= levels.Value.MinLevelWithDelta : level >= levels.Value.MinLevel;
                    return false;
                }
                case ModifierTreeType.TargetLevelWithinOrAboveContentTuning: // 273
                {
                    if (!refe || !refe.IsUnit)
                        return false;

                    uint level = refe.AsUnit.Level;
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
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
                    MapRecord map = referencePlayer.Map.Entry;
                    if (map.Id != reqValue && map.CosmeticParentMapID != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerCanAccessShadowlandsPrepurchaseContent: // 281
                    if (referencePlayer.Session.AccountExpansion < Expansion.ShadowLands)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasEntitlement: // 282 NYI
                case ModifierTreeType.PlayerIsInPartySyncGroup: // 283 NYI
                case ModifierTreeType.QuestHasPartySyncRewards: // 284 NYI
                case ModifierTreeType.HonorGainSource: // 285 NYI
                case ModifierTreeType.JailersTowerActiveFloorIndexEqualOrGreaterThan: // 286 NYI
                case ModifierTreeType.JailersTowerActiveFloorDifficultyEqualOrGreaterThan: // 287 NYI
                    return false;
                case ModifierTreeType.PlayerCovenant: // 288
                    if (referencePlayer.PlayerData.CovenantID != reqValue)
                        return false;
                    break;
                case ModifierTreeType.HasTimeEventPassed: // 289
                {
                    long eventTimestamp = GameTime.GetGameTime();
                    switch (reqValue)
                    {
                        case 111: // Battle for Azeroth Season 4 Start
                            eventTimestamp = 1579618800L; // January 21, 2020 8:00
                            break;
                        case 120: // Patch 9.0.1
                            eventTimestamp = 1602601200L; // October 13, 2020 8:00
                            break;
                        case 121: // Shadowlands Season 1 Start
                            eventTimestamp = 1607439600L; // December 8, 2020 8:00
                            break;
                        case 123: // Shadowlands Season 1 End
                                  // timestamp = unknown
                            break; ;
                        case 149: // Shadowlands Season 2 End
                                  // timestamp = unknown
                            break;
                        default:
                            break;
                    }
                    if (GameTime.GetGameTime() < eventTimestamp)
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
                case ModifierTreeType.PlayerHasAPACSubscriptionReward_2020: // 293
                case ModifierTreeType.PlayerHasTBCCDEWarpStalker_Mount: // 294
                case ModifierTreeType.PlayerHasTBCCDEDarkPortal_Toy: // 295
                case ModifierTreeType.PlayerHasTBCCDEPathOfIllidan_Toy: // 296
                case ModifierTreeType.PlayerHasImpInABallToySubscriptionReward: // 297
                    return false;
                case ModifierTreeType.PlayerIsInAreaGroup: // 298
                {
                    var areas = Global.DB2Mgr.GetAreasForGroup(reqValue);
                    AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(referencePlayer.Area);
                    if (area != null)
                        foreach (uint areaInGroup in areas)
                            if (areaInGroup == area.Id || areaInGroup == area.ParentAreaID)
                                return true;
                    return false;
                }
                case ModifierTreeType.TargetIsInAreaGroup: // 299
                {
                    if (!refe)
                        return false;

                    var areas = Global.DB2Mgr.GetAreasForGroup(reqValue);
                    var area = CliDB.AreaTableStorage.LookupByKey(refe.Area);
                    if (area != null)
                        foreach (uint areaInGroup in areas)
                            if (areaInGroup == area.Id || areaInGroup == area.ParentAreaID)
                                return true;
                    return false;
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
                    if (Global.DB2Mgr.GetAzeriteEmpoweredItem((uint)miscValue1) == null)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasRuneforgePower: // 303
                {
                    int block = (int)reqValue / 32;
                    if (block >= referencePlayer.ActivePlayerData.RuneforgePowers.Size())
                        return false;

                    uint bit = reqValue % 32;
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
                    ShapeshiftFormModelData formModelData = Global.DB2Mgr.GetShapeshiftFormModelData(referencePlayer.Race, referencePlayer.NativeGender, (ShapeShiftForm)secondaryAsset);
                    if (formModelData == null)
                        return false;

                    uint formChoice = referencePlayer.GetCustomizationChoice(formModelData.OptionID);
                    var choiceIndex = formModelData.Choices.FindIndex(choice => { return choice.Id == formChoice; });
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
                    Scenario scenario = referencePlayer.Scenario;
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
                    if (!refe || !refe.IsPlayer)
                        return false;
                    if (refe.AsPlayer.PlayerData.CovenantID != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasTBCCollectorsEdition: // 315
                case ModifierTreeType.PlayerHasWrathCollectorsEdition: // 316
                    return false;
                case ModifierTreeType.GarrisonTalentResearchedAndAtRankEqualOrGreaterThan: // 317 NYI
                case ModifierTreeType.CurrencySpentOnGarrisonTalentResearchEqualOrGreaterThan: // 318 NYI
                case ModifierTreeType.RenownCatchupActive: // 319 NYI
                case ModifierTreeType.RapidRenownCatchupActive: // 320 NYI
                case ModifierTreeType.PlayerMythicPlusRatingEqualOrGreaterThan: // 321 NYI
                case ModifierTreeType.PlayerMythicPlusRunCountInCurrentExpansionEqualOrGreaterThan: // 322 NYI
                    return false;
                case ModifierTreeType.PlayerHasCustomizationChoice: // 323
                {
                    int customizationChoiceIndex = referencePlayer.PlayerData.Customizations.FindIndexIf(choice =>
                    {
                        return choice.ChrCustomizationChoiceID == reqValue;
                    });

                    if (customizationChoiceIndex < 0)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerBestWeeklyWinPvpTier: // 324
                {
                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(reqValue);
                    if (pvpTier == null)
                        return false;

                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)pvpTier.BracketID);
                    if (pvpInfo == null)
                        return false;

                    if (pvpTier.Id != pvpInfo.WeeklyBestWinPvpTierID)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerBestWeeklyWinPvpTierInBracketEqualOrGreaterThan: // 325
                {
                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)secondaryAsset);
                    if (pvpInfo == null)
                        return false;

                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(pvpInfo.WeeklyBestWinPvpTierID);
                    if (pvpTier == null)
                        return false;

                    if (pvpTier.Rank < reqValue)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerHasVanillaCollectorsEdition: // 326
                    return false;
                case ModifierTreeType.PlayerHasItemWithKeystoneLevelModifierEqualOrGreaterThan: // 327
                {
                    bool bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Inventory, item =>
                    {
                        if (item.Entry != reqValue)
                            return true;

                        if (item.GetModifier(ItemModifier.ChallengeKeystoneLevel) < secondaryAsset)
                            return true;

                        return false;
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
                    bool hasTraitNodeEntry()
                    {
                        foreach (var traitConfig in referencePlayer.ActivePlayerData.TraitConfigs)
                        {
                            if ((TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat)
                            {
                                if (referencePlayer.ActivePlayerData.ActiveCombatTraitConfigID != traitConfig.ID
                                    || !((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
                                    continue;
                            }

                            foreach (var traitEntry in traitConfig.Entries)
                                if (traitEntry.TraitNodeEntryID == reqValue)
                                    return true;
                        }
                        return false;
                    }
                    if (!hasTraitNodeEntry())
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
                            {
                                if (referencePlayer.ActivePlayerData.ActiveCombatTraitConfigID != traitConfig.ID
                                    || !((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
                                    continue;
                            }

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
                    if (GameTime.GetGameTime() - referencePlayer.PlayerData.LogoutTime < reqValue * Time.Day)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasPerksProgramPendingReward: // 350
                    if (!referencePlayer.ActivePlayerData.HasPerksProgramPendingReward)
                        return false;
                    break;
                case ModifierTreeType.PlayerCanUseItem: // 351
                {
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(reqValue);
                    if (itemTemplate == null || referencePlayer.CanUseItem(itemTemplate) != InventoryResult.Ok)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasAtLeastProfPathRanks: // 355
                {
                    uint ranks = 0;
                    foreach (TraitConfig traitConfig in referencePlayer.ActivePlayerData.TraitConfigs)
                    {
                        if ((TraitConfigType)(int)traitConfig.Type != TraitConfigType.Profession)
                            continue;

                        if (traitConfig.SkillLineID != secondaryAsset)
                            continue;

                        foreach (TraitEntry traitEntry in traitConfig.Entries)
                            if (CliDB.TraitNodeEntryStorage.LookupByKey(traitEntry.TraitNodeEntryID)?.GetNodeEntryType() == TraitNodeEntryType.ProfPath)
                                ranks += (uint)(traitEntry.Rank + traitEntry.GrantedRanks);
                    }

                    if (ranks < reqValue)
                        return false;
                    break;
                }
                default:
                    return false;
            }
            return true;
        }

        public virtual void SendAllData(Player receiver) { }
        public virtual void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted) { }
        public virtual void SendCriteriaProgressRemoved(uint criteriaId) { }

        public virtual void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer) { }
        public virtual void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer) { }

        public virtual void SendPacket(ServerPacket data) { }

        public virtual bool RequiredAchievementSatisfied(uint achievementId) { return false; }

        public virtual string GetOwnerInfo() { return ""; }
        public virtual List<Criteria> GetCriteriaByType(CriteriaType type, uint asset) { return null; }
    }
}

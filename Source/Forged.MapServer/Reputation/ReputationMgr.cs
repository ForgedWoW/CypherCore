// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Character;
using Forged.MapServer.Networking.Packets.Reputation;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Reputation;

public class ReputationMgr
{
    public static readonly CypherStrings[] ReputationRankStrIndex =
    {
        CypherStrings.RepHated, CypherStrings.RepHostile, CypherStrings.RepUnfriendly, CypherStrings.RepNeutral, CypherStrings.RepFriendly, CypherStrings.RepHonored, CypherStrings.RepRevered, CypherStrings.RepExalted
    };

    public static readonly int[] ReputationRankThresholds =
        {
        -42000,
        // Hated
        -6000,
        // Hostile
        -3000,
        // Unfriendly
        0,
        // Neutral
        3000,
        // Friendly
        9000,
        // Honored
        21000,
        // Revered
        42000
        // Exalted
    };

    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly Dictionary<uint, ReputationRank> _forcedReactions = new();
    private readonly GameObjectManager _gameObjectManager;
    private readonly Player _player;
    private readonly ScriptManager _scriptManager;
    private bool _sendFactionIncreased; //! Play visual effect on next SMSG_SET_FACTION_STANDING sent

    public ReputationMgr(Player owner, CliDB cliDB, CharacterDatabase characterDatabase, IConfiguration configuration, DB2Manager db2Manager, GameObjectManager gameObjectManager,
                         ScriptManager scriptManager)
    {
        _player = owner;
        _cliDB = cliDB;
        _characterDatabase = characterDatabase;
        _configuration = configuration;
        _db2Manager = db2Manager;
        _gameObjectManager = gameObjectManager;
        _scriptManager = scriptManager;
        VisibleFactionCount = 0;
        HonoredFactionCount = 0;
        ReveredFactionCount = 0;
        ExaltedFactionCount = 0;
        _sendFactionIncreased = false;
    }

    public byte ExaltedFactionCount { get; private set; }
    public byte HonoredFactionCount { get; private set; }
    public byte ReveredFactionCount { get; private set; }
    public SortedDictionary<uint, FactionState> StateList { get; } = new();
    public byte VisibleFactionCount { get; private set; }

    // this allows calculating base reputations to offline players, just by race and class
    public static int GetBaseReputationOf(FactionRecord factionEntry, Race race, PlayerClass playerClass)
    {
        if (factionEntry == null)
            return 0;

        var raceMask = SharedConst.GetMaskForRace(race);
        var classMask = 1u << ((int)playerClass - 1);

        for (var i = 0; i < 4; i++)
            if ((factionEntry.ReputationClassMask[i] == 0 || factionEntry.ReputationClassMask[i].HasAnyFlag((short)classMask)) && (factionEntry.ReputationRaceMask[i] == 0 || factionEntry.ReputationRaceMask[i].HasAnyFlag(raceMask)))
                return factionEntry.ReputationBase[i];

        return 0;
    }

    public void ApplyForceReaction(uint factionID, ReputationRank rank, bool apply)
    {
        if (apply)
            _forcedReactions[factionID] = rank;
        else
            _forcedReactions.Remove(factionID);
    }

    public int GetBaseReputation(FactionRecord factionEntry)
    {
        var dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);

        if (dataIndex < 0)
            return 0;

        return factionEntry.ReputationBase[dataIndex];
    }

    public ReputationRank GetForcedRankIfAny(FactionTemplateRecord factionTemplateEntry)
    {
        return GetForcedRankIfAny(factionTemplateEntry.Faction);
    }

    public ReputationRank GetForcedRankIfAny(uint factionId)
    {
        return _forcedReactions.TryGetValue(factionId, out var forced) ? forced : ReputationRank.None;
    }

    public int GetParagonLevel(uint paragonFactionId)
    {
        return GetParagonLevel(_cliDB.FactionStorage.LookupByKey(paragonFactionId));
    }

    public ReputationRank GetRank(FactionRecord factionEntry)
    {
        var reputation = GetReputation(factionEntry);

        return ReputationToRank(factionEntry, reputation);
    }

    public int GetReputation(uint factionID)
    {
        if (!_cliDB.FactionStorage.TryGetValue(factionID, out var factionEntry))
        {
            Log.Logger.Error("ReputationMgr.GetReputation: Can't get reputation of {0} for unknown faction (faction id) #{1}.", _player.GetName(), factionID);

            return 0;
        }

        return GetReputation(factionEntry);
    }

    public int GetReputation(FactionRecord factionEntry)
    {
        // Faction without recorded reputation. Just ignore.
        if (factionEntry == null)
            return 0;

        var state = GetState(factionEntry);

        if (state != null)
            return GetBaseReputation(factionEntry) + state.Standing;

        return 0;
    }

    public uint GetReputationRankStrIndex(FactionRecord factionEntry)
    {
        return (uint)ReputationRankStrIndex[(int)GetRank(factionEntry)];
    }

    public FactionState GetState(FactionRecord factionEntry)
    {
        return factionEntry.CanHaveReputation() ? GetState(factionEntry.ReputationIndex) : null;
    }

    public FactionState GetState(int id)
    {
        return StateList.LookupByKey((uint)id);
    }

    public bool IsAtWar(uint factionId)
    {
        if (!_cliDB.FactionStorage.TryGetValue(factionId, out var factionEntry))
            return false;

        return IsAtWar(factionEntry);
    }

    public bool IsAtWar(FactionRecord factionEntry)
    {
        if (factionEntry == null)
            return false;

        var factionState = GetState(factionEntry);

        if (factionState != null)
            return factionState.Flags.HasFlag(ReputationFlags.AtWar);

        return false;
    }

    public void LoadFromDB(SQLResult result)
    {
        // Set initial reputations (so everything is nifty before DB data load)
        Initialize();

        if (!result.IsEmpty())
            do
            {
                var factionEntry = _cliDB.FactionStorage.LookupByKey(result.Read<uint>(0));

                if (factionEntry != null && factionEntry.CanHaveReputation())
                {
                    if (!StateList.TryGetValue((uint)factionEntry.ReputationIndex, out var faction))
                        continue;

                    // update standing to current
                    faction.Standing = result.Read<int>(1);

                    // update counters
                    if (factionEntry.FriendshipRepID == 0)
                    {
                        var baseRep = GetBaseReputation(factionEntry);
                        var oldRank = ReputationToRank(factionEntry, baseRep);
                        var newRank = ReputationToRank(factionEntry, baseRep + faction.Standing);
                        UpdateRankCounters(oldRank, newRank);
                    }

                    var dbFactionFlags = (ReputationFlags)result.Read<uint>(2);

                    if (dbFactionFlags.HasFlag(ReputationFlags.Visible))
                        SetVisible(faction); // have internal checks for forced invisibility

                    if (dbFactionFlags.HasFlag(ReputationFlags.Inactive))
                        SetInactive(faction, true); // have internal checks for visibility requirement

                    if (dbFactionFlags.HasFlag(ReputationFlags.AtWar)) // DB at war
                    {
                        SetAtWar(faction, true); // have internal checks for FACTION_FLAG_PEACE_FORCED
                    }
                    else // DB not at war
                    {
                        // allow remove if visible (and then not FACTION_FLAG_INVISIBLE_FORCED or FACTION_FLAG_HIDDEN)
                        if (faction.Flags.HasFlag(ReputationFlags.Visible))
                            SetAtWar(faction, false); // have internal checks for FACTION_FLAG_PEACE_FORCED
                    }

                    // set atWar for hostile
                    if (GetRank(factionEntry) <= ReputationRank.Hostile)
                        SetAtWar(faction, true);

                    // reset changed Id if values similar to saved in DB
                    if (faction.Flags == dbFactionFlags)
                    {
                        faction.needSend = false;
                        faction.needSave = false;
                    }
                }
            } while (result.NextRow());
    }

    public bool ModifyReputation(FactionRecord factionEntry, int standing, bool spillOverOnly = false, bool noSpillover = false)
    {
        return SetReputation(factionEntry, standing, true, spillOverOnly, noSpillover);
    }

    public void SaveToDB(SQLTransaction trans)
    {
        foreach (var factionState in StateList.Values)
            if (factionState.needSave)
            {
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION_BY_FACTION);
                stmt.AddValue(0, _player.GUID.Counter);
                stmt.AddValue(1, factionState.Id);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_REPUTATION_BY_FACTION);
                stmt.AddValue(0, _player.GUID.Counter);
                stmt.AddValue(1, factionState.Id);
                stmt.AddValue(2, factionState.Standing);
                stmt.AddValue(3, (ushort)factionState.Flags);
                trans.Append(stmt);

                factionState.needSave = false;
            }
    }

    public void SendForceReactions()
    {
        SetForcedReactions setForcedReactions = new();

        foreach (var pair in _forcedReactions)
        {
            ForcedReaction forcedReaction;
            forcedReaction.Faction = (int)pair.Key;
            forcedReaction.Reaction = (int)pair.Value;

            setForcedReactions.Reactions.Add(forcedReaction);
        }

        _player.SendPacket(setForcedReactions);
    }

    public void SendInitialReputations()
    {
        InitializeFactions initFactions = new();

        foreach (var pair in StateList)
        {
            initFactions.FactionFlags[pair.Key] = pair.Value.Flags;
            initFactions.FactionStandings[pair.Key] = pair.Value.Standing;
            // @todo faction bonus
            pair.Value.needSend = false;
        }

        _player.SendPacket(initFactions);
    }

    public void SendState(FactionState faction)
    {
        SetFactionStanding setFactionStanding = new()
        {
            BonusFromAchievementSystem = 0.0f
        };

        if (faction != null)
        {
            var standing = faction.VisualStandingIncrease != 0 ? faction.VisualStandingIncrease : faction.Standing;
            setFactionStanding.Faction.Add(new FactionStandingData((int)faction.ReputationListID, standing));
        }

        foreach (var state in StateList.Values.Where(state => state.needSend))
        {
            state.needSend = false;

            if (faction != null && state.ReputationListID == faction.ReputationListID)
                continue;

            var standing = state.VisualStandingIncrease != 0 ? state.VisualStandingIncrease : state.Standing;
            setFactionStanding.Faction.Add(new FactionStandingData((int)state.ReputationListID, standing));
        }

        setFactionStanding.ShowVisual = _sendFactionIncreased;
        _player.SendPacket(setFactionStanding);

        _sendFactionIncreased = false; // Reset
    }

    public void SendVisible(FactionState faction, bool visible = true)
    {
        if (_player.Session.PlayerLoading)
            return;

        //make faction visible / not visible in reputation list at client
        SetFactionVisible packet = new(visible)
        {
            FactionIndex = faction.ReputationListID
        };

        _player.SendPacket(packet);
    }

    public void SetAtWar(uint repListID, bool on)
    {
        if (!StateList.TryGetValue(repListID, out var factionState))
            return;

        // always invisible or hidden faction can't change war state
        if (factionState.Flags.HasAnyFlag(ReputationFlags.Hidden | ReputationFlags.Header))
            return;

        SetAtWar(factionState, on);
    }

    public void SetInactive(uint repListID, bool on)
    {
        if (!StateList.TryGetValue(repListID, out var factionState))
            return;

        SetInactive(factionState, on);
    }

    public bool SetOneFactionReputation(FactionRecord factionEntry, int standing, bool incremental)
    {
        if (StateList.TryGetValue((uint)factionEntry.ReputationIndex, out var factionState))
        {
            // Ignore renown reputation already raised to the maximum level
            if (HasMaximumRenownReputation(factionEntry) && standing > 0)
            {
                factionState.needSend = false;
                factionState.needSave = false;

                return false;
            }

            var baseRep = GetBaseReputation(factionEntry);
            var oldStanding = factionState.Standing + baseRep;

            if (incremental || IsRenownReputation(factionEntry))
            {
                // int32 *= float cause one point loss?
                standing = (int)Math.Floor(standing * _configuration.GetDefaultValue("Rate.Reputation.Gain", 1.0f) + 0.5f);
                standing += oldStanding;
            }

            if (standing > GetMaxReputation(factionEntry))
                standing = GetMaxReputation(factionEntry);
            else if (standing < GetMinReputation(factionEntry))
                standing = GetMinReputation(factionEntry);

            // Ignore rank for paragon or renown reputation
            if (!IsParagonReputation(factionEntry) && !IsRenownReputation(factionEntry))
            {
                var oldRank = ReputationToRank(factionEntry, oldStanding);
                var newRank = ReputationToRank(factionEntry, standing);

                if (newRank <= ReputationRank.Hostile)
                    SetAtWar(factionState, true);

                if (newRank > oldRank)
                    _sendFactionIncreased = true;

                if (factionEntry.FriendshipRepID == 0)
                    UpdateRankCounters(oldRank, newRank);
            }
            else
            {
                _sendFactionIncreased = true; // TODO: Check Paragon reputation
            }

            // Calculate new standing and reputation change
            var newStanding = 0;
            var reputationChange = standing - oldStanding;

            if (!IsRenownReputation(factionEntry))
            {
                newStanding = standing - baseRep;
            }
            else
            {
                if (_cliDB.CurrencyTypesStorage.TryGetValue((uint)factionEntry.RenownCurrencyID, out var currency))
                {
                    var renownLevelThreshold = GetRenownLevelThreshold(factionEntry);
                    var oldRenownLevel = GetRenownLevel(factionEntry);

                    var totalReputation = oldRenownLevel * renownLevelThreshold + (standing - baseRep);
                    var newRenownLevel = totalReputation / renownLevelThreshold;
                    newStanding = totalReputation % renownLevelThreshold;

                    if (newRenownLevel >= GetRenownMaxLevel(factionEntry))
                    {
                        newStanding = 0;
                        reputationChange += GetRenownMaxLevel(factionEntry) * renownLevelThreshold - totalReputation;
                    }

                    factionState.VisualStandingIncrease = reputationChange;

                    // If the reputation is decreased by command, we will send CurrencyDestroyReason::Cheat
                    if (oldRenownLevel != newRenownLevel)
                        _player.ModifyCurrency(currency.Id, newRenownLevel - oldRenownLevel, CurrencyGainSource.RenownRepGain);
                }
            }

            _player.ReputationChanged(factionEntry, reputationChange);

            factionState.Standing = newStanding;
            factionState.needSend = true;
            factionState.needSave = true;

            SetVisible(factionState);

            var paragonReputation = _db2Manager.GetParagonReputation(factionEntry.Id);

            if (paragonReputation != null)
            {
                var oldParagonLevel = oldStanding / paragonReputation.LevelThreshold;
                var newParagonLevel = standing / paragonReputation.LevelThreshold;

                if (oldParagonLevel != newParagonLevel)
                {
                    var paragonRewardQuest = _gameObjectManager.GetQuestTemplate((uint)paragonReputation.QuestID);

                    if (paragonRewardQuest != null)
                        _player.AddQuestAndCheckCompletion(paragonRewardQuest, null);
                }
            }

            _player.UpdateCriteria(CriteriaType.TotalFactionsEncountered, factionEntry.Id);
            _player.UpdateCriteria(CriteriaType.ReputationGained, factionEntry.Id);
            _player.UpdateCriteria(CriteriaType.TotalExaltedFactions, factionEntry.Id);
            _player.UpdateCriteria(CriteriaType.TotalReveredFactions, factionEntry.Id);
            _player.UpdateCriteria(CriteriaType.TotalHonoredFactions, factionEntry.Id);

            return true;
        }

        return false;
    }

    public bool SetReputation(FactionRecord factionEntry, double standing)
    {
        return SetReputation(factionEntry, (int)standing);
    }

    public bool SetReputation(FactionRecord factionEntry, int standing)
    {
        return SetReputation(factionEntry, standing, false, false, false);
    }

    public bool SetReputation(FactionRecord factionEntry, int standing, bool incremental, bool spillOverOnly, bool noSpillover)
    {
        _scriptManager.ForEach<IPlayerOnReputationChange>(p => p.OnReputationChange(_player, factionEntry.Id, standing, incremental));
        var res = false;

        if (!noSpillover)
        {
            // if spillover definition exists in DB, override DBC
            var repTemplate = _gameObjectManager.GetRepSpillover(factionEntry.Id);

            if (repTemplate != null)
            {
                for (uint i = 0; i < 5; ++i)
                    if (repTemplate.Faction[i] != 0)
                        if (_player.GetReputationRank(repTemplate.Faction[i]) <= (ReputationRank)repTemplate.FactionRank[i])
                        {
                            // bonuses are already given, so just modify standing by rate
                            var spilloverRep = standing * repTemplate.FactionRate[i];
                            SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(repTemplate.Faction[i]), (int)spilloverRep, incremental);
                        }
            }
            else
            {
                float spillOverRepOut = standing;
                // check for sub-factions that receive spillover
                var flist = _db2Manager.GetFactionTeamList(factionEntry.Id);

                // if has no sub-factions, check for factions with same parent
                if (flist == null && factionEntry.ParentFactionID != 0 && factionEntry.ParentFactionMod[1] != 0.0f)
                {
                    spillOverRepOut *= factionEntry.ParentFactionMod[1];
                    if (_cliDB.FactionStorage.TryGetValue(factionEntry.ParentFactionID, out var parent))
                    {
                        var parentState = StateList.LookupByKey((uint)parent.ReputationIndex);

                        // some team factions have own reputation standing, in this case do not spill to other sub-factions
                        if (parentState != null && parentState.Flags.HasFlag(ReputationFlags.HeaderShowsBar))
                            SetOneFactionReputation(parent, (int)spillOverRepOut, incremental);
                        else // spill to "sister" factions
                            flist = _db2Manager.GetFactionTeamList(factionEntry.ParentFactionID);
                    }
                }

                if (flist != null)
                    // Spillover to affiliated factions
                    foreach (var id in flist)
                    {
                        if (_cliDB.FactionStorage.TryGetValue(id, out var factionEntryCalc))
                        {
                            if (factionEntryCalc == factionEntry || GetRank(factionEntryCalc) > (ReputationRank)factionEntryCalc.ParentFactionMod[0])
                                continue;

                            var spilloverRep = (int)(spillOverRepOut * factionEntryCalc.ParentFactionMod[0]);

                            if (spilloverRep != 0 || !incremental)
                                res = SetOneFactionReputation(factionEntryCalc, spilloverRep, incremental);
                        }
                    }
            }
        }

        // spillover done, update faction itself
        if (StateList.TryGetValue((uint)factionEntry.ReputationIndex, out var faction))
        {
            var primaryFactionToModify = factionEntry;

            if (incremental && standing > 0 && CanGainParagonReputationForFaction(factionEntry))
            {
                primaryFactionToModify = _cliDB.FactionStorage.LookupByKey(factionEntry.ParagonFactionID);
                faction = StateList.LookupByKey((uint)primaryFactionToModify.ReputationIndex);
            }

            if (faction != null)
            {
                // if we update spillover only, do not update main reputation (rank exceeds creature reward rate)
                if (!spillOverOnly)
                    res = SetOneFactionReputation(primaryFactionToModify, standing, incremental);

                // only this faction gets reported to client, even if it has no own visible standing
                SendState(faction);
            }
        }

        return res;
    }

    public void SetVisible(FactionTemplateRecord factionTemplateEntry)
    {
        if (factionTemplateEntry.Faction == 0)
            return;

        if (_cliDB.FactionStorage.TryGetValue(factionTemplateEntry.Faction, out var factionEntry))
            // Never show factions of the opposing team
            if (!Convert.ToBoolean(factionEntry.ReputationRaceMask[1] & SharedConst.GetMaskForRace(_player.Race)) && factionEntry.ReputationBase[1] == SharedConst.ReputationBottom)
                SetVisible(factionEntry);
    }

    public void SetVisible(FactionRecord factionEntry)
    {
        if (!factionEntry.CanHaveReputation())
            return;

        if (!StateList.TryGetValue((uint)factionEntry.ReputationIndex, out var factionState))
            return;

        SetVisible(factionState);
    }

    private bool CanGainParagonReputationForFaction(FactionRecord factionEntry)
    {
        if (!_cliDB.FactionStorage.ContainsKey(factionEntry.ParagonFactionID))
            return false;

        if (GetRank(factionEntry) != ReputationRank.Exalted && !HasMaximumRenownReputation(factionEntry))
            return false;

        var paragonReputation = _db2Manager.GetParagonReputation(factionEntry.ParagonFactionID);

        if (paragonReputation == null)
            return false;

        var quest = _gameObjectManager.GetQuestTemplate((uint)paragonReputation.QuestID);

        if (quest == null)
            return false;

        return _player.Level >= _player.GetQuestMinLevel(quest);
    }

    private ReputationRank GetBaseRank(FactionRecord factionEntry)
    {
        var reputation = GetBaseReputation(factionEntry);

        return ReputationToRank(factionEntry, reputation);
    }

    private ReputationFlags GetDefaultStateFlags(FactionRecord factionEntry)
    {
        var flags = ReputationFlags.None;

        var dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);

        if (dataIndex > 0)
            flags = (ReputationFlags)factionEntry.ReputationFlags[dataIndex];

        if (_db2Manager.GetParagonReputation(factionEntry.Id) != null)
            flags |= ReputationFlags.ShowPropagated;

        return flags;
    }

    private int GetFactionDataIndexForRaceAndClass(FactionRecord factionEntry)
    {
        if (factionEntry == null)
            return -1;

        var raceMask = SharedConst.GetMaskForRace(_player.Race);
        var classMask = (short)_player.ClassMask;

        for (var i = 0; i < 4; i++)
            if ((factionEntry.ReputationRaceMask[i].HasAnyFlag(raceMask) || (factionEntry.ReputationRaceMask[i] == 0 && factionEntry.ReputationClassMask[i] != 0)) && (factionEntry.ReputationClassMask[i].HasAnyFlag(classMask) || factionEntry.ReputationClassMask[i] == 0))

                return i;

        return -1;
    }

    private int GetMaxReputation(FactionRecord factionEntry)
    {
        var paragonReputation = _db2Manager.GetParagonReputation(factionEntry.Id);

        if (paragonReputation != null)
        {
            // has reward quest, cap is just before threshold for another quest reward
            // for example: if current reputation is 12345 and quests are given every 10000 and player has unclaimed reward
            // then cap will be 19999

            // otherwise cap is one theshold level larger
            // if current reputation is 12345 and quests are given every 10000 and player does NOT have unclaimed reward
            // then cap will be 29999

            var reputation = GetReputation(factionEntry);
            var cap = reputation + paragonReputation.LevelThreshold - reputation % paragonReputation.LevelThreshold - 1;

            if (_player.GetQuestStatus((uint)paragonReputation.QuestID) == QuestStatus.None)
                cap += paragonReputation.LevelThreshold;

            return cap;
        }

        if (IsRenownReputation(factionEntry))
            // Compared to a paragon reputation, DF renown reputations
            // have a maximum value of 2500 which resets with each level of renown acquired.
            // We calculate the total reputation necessary to raise the renown to the maximum
            return GetRenownMaxLevel(factionEntry) * GetRenownLevelThreshold(factionEntry);

        var friendshipReactions = _db2Manager.GetFriendshipRepReactions(factionEntry.FriendshipRepID);

        if (!friendshipReactions.Empty())
            return friendshipReactions.Last().ReactionThreshold;

        var dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);

        return dataIndex >= 0 ? factionEntry.ReputationMax[dataIndex] : ReputationRankThresholds.LastOrDefault();
    }

    private int GetMinReputation(FactionRecord factionEntry)
    {
        var friendshipReactions = _db2Manager.GetFriendshipRepReactions(factionEntry.FriendshipRepID);

        return !friendshipReactions.Empty() ? friendshipReactions[0].ReactionThreshold : ReputationRankThresholds[0];
    }

    private int GetParagonLevel(FactionRecord paragonFactionEntry)
    {
        if (paragonFactionEntry == null)
            return 0;

        var paragonReputation = _db2Manager.GetParagonReputation(paragonFactionEntry.Id);

        if (paragonReputation != null)
            return GetReputation(paragonFactionEntry) / paragonReputation.LevelThreshold;

        return 0;
    }

    private int GetRenownLevel(FactionRecord renownFactionEntry)
    {
        if (renownFactionEntry == null)
            return 0;

        if (_cliDB.CurrencyTypesStorage.TryGetValue((uint)renownFactionEntry.RenownCurrencyID, out var currency))
            return (int)_player.GetCurrencyQuantity(currency.Id);

        return 0;
    }

    private int GetRenownLevelThreshold(FactionRecord renownFactionEntry)
    {
        if (renownFactionEntry == null || !IsRenownReputation(renownFactionEntry))
            return 0;

        var dataIndex = GetFactionDataIndexForRaceAndClass(renownFactionEntry);

        return dataIndex >= 0 ? renownFactionEntry.ReputationMax[dataIndex] : 0;
    }

    private int GetRenownMaxLevel(FactionRecord renownFactionEntry)
    {
        if (renownFactionEntry == null)
            return 0;

        if (_cliDB.CurrencyTypesStorage.TryGetValue((uint)renownFactionEntry.RenownCurrencyID, out var currency))
            return (int)_player.GetCurrencyMaxQuantity(currency);

        return 0;
    }

    private bool HasMaximumRenownReputation(FactionRecord factionEntry)
    {
        if (!IsRenownReputation(factionEntry))
            return false;

        return GetRenownLevel(factionEntry) >= GetRenownMaxLevel(factionEntry);
    }

    private void Initialize()
    {
        StateList.Clear();
        VisibleFactionCount = 0;
        HonoredFactionCount = 0;
        ReveredFactionCount = 0;
        ExaltedFactionCount = 0;
        _sendFactionIncreased = false;

        foreach (var factionEntry in _cliDB.FactionStorage.Values)
            if (factionEntry.CanHaveReputation())
            {
                FactionState newFaction = new()
                {
                    Id = factionEntry.Id,
                    ReputationListID = (uint)factionEntry.ReputationIndex,
                    Standing = 0,
                    VisualStandingIncrease = 0,
                    Flags = GetDefaultStateFlags(factionEntry),
                    needSend = true,
                    needSave = true
                };

                if (newFaction.Flags.HasFlag(ReputationFlags.Visible))
                    ++VisibleFactionCount;

                if (factionEntry.FriendshipRepID == 0)
                    UpdateRankCounters(ReputationRank.Hostile, GetBaseRank(factionEntry));

                StateList[newFaction.ReputationListID] = newFaction;
            }
    }

    private bool IsParagonReputation(FactionRecord factionEntry)
    {
        if (_db2Manager.GetParagonReputation(factionEntry.Id) != null)
            return true;

        return false;
    }

    private bool IsRenownReputation(FactionRecord factionEntry)
    {
        return factionEntry.RenownCurrencyID > 0;
    }

    private ReputationRank ReputationToRank(FactionRecord factionEntry, int standing)
    {
        var friendshipReactions = _db2Manager.GetFriendshipRepReactions(factionEntry.FriendshipRepID);

        return !friendshipReactions.Empty() ? ReputationToRankHelper(friendshipReactions, standing, frr => frr.ReactionThreshold) : ReputationToRankHelper(ReputationRankThresholds, standing, threshold => threshold);
    }

    private ReputationRank ReputationToRankHelper<T>(IList<T> thresholds, int standing, Func<T, int> thresholdExtractor)
    {
        var i = 0;
        var rank = -1;

        while (i != thresholds.Count - 1 && standing >= thresholdExtractor(thresholds[i]))
        {
            ++rank;
            ++i;
        }

        return (ReputationRank)rank;
    }

    private void SetAtWar(FactionState faction, bool atWar)
    {
        // Do not allow to declare war to our own faction. But allow for rival factions (eg Aldor vs Scryer).
        if (atWar && faction.Flags.HasFlag(ReputationFlags.Peaceful) && GetRank(_cliDB.FactionStorage.LookupByKey(faction.Id)) > ReputationRank.Hated)
            return;

        // already set
        if (faction.Flags.HasFlag(ReputationFlags.AtWar) == atWar)
            return;

        if (atWar)
            faction.Flags |= ReputationFlags.AtWar;
        else
            faction.Flags &= ~ReputationFlags.AtWar;

        faction.needSend = true;
        faction.needSave = true;
    }

    private void SetInactive(FactionState faction, bool inactive)
    {
        // always invisible or hidden faction can't be inactive
        if (faction.Flags.HasAnyFlag(ReputationFlags.Hidden | ReputationFlags.Header) || !faction.Flags.HasFlag(ReputationFlags.Visible))
            return;

        // already set
        if (faction.Flags.HasFlag(ReputationFlags.Inactive) == inactive)
            return;

        if (inactive)
            faction.Flags |= ReputationFlags.Inactive;
        else
            faction.Flags &= ~ReputationFlags.Inactive;

        faction.needSend = true;
        faction.needSave = true;
    }

    private void SetVisible(FactionState faction)
    {
        // always invisible or hidden faction can't be make visible
        if (faction.Flags.HasFlag(ReputationFlags.Hidden))
            return;

        if (faction.Flags.HasFlag(ReputationFlags.Header) && !faction.Flags.HasFlag(ReputationFlags.HeaderShowsBar))
            return;

        if (_db2Manager.GetParagonReputation(faction.Id) != null)
            return;

        // already set
        if (faction.Flags.HasFlag(ReputationFlags.Visible))
            return;

        faction.Flags |= ReputationFlags.Visible;
        faction.needSend = true;
        faction.needSave = true;

        VisibleFactionCount++;

        SendVisible(faction);
    }

    private void UpdateRankCounters(ReputationRank oldRank, ReputationRank newRank)
    {
        if (oldRank >= ReputationRank.Exalted)
            --ExaltedFactionCount;

        if (oldRank >= ReputationRank.Revered)
            --ReveredFactionCount;

        if (oldRank >= ReputationRank.Honored)
            --HonoredFactionCount;

        if (newRank >= ReputationRank.Exalted)
            ++ExaltedFactionCount;

        if (newRank >= ReputationRank.Revered)
            ++ReveredFactionCount;

        if (newRank >= ReputationRank.Honored)
            ++HonoredFactionCount;
    }
}
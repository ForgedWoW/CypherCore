// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Scripting;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;
using Framework.Database;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forged.RealmServer;

public class ReputationMgr
{
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

	public static readonly CypherStrings[] ReputationRankStrIndex =
	{
		CypherStrings.RepHated, CypherStrings.RepHostile, CypherStrings.RepUnfriendly, CypherStrings.RepNeutral, CypherStrings.RepFriendly, CypherStrings.RepHonored, CypherStrings.RepRevered, CypherStrings.RepExalted
	};


	readonly Player _player;
    private readonly CliDB _cliDb;
    private readonly ScriptManager _scriptManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly DB2Manager _db2Manager;
    private readonly WorldConfig _worldConfig;
    private readonly CharacterDatabase _characterDatabase;
    readonly SortedDictionary<uint, FactionState> _factions = new();
	readonly Dictionary<uint, ReputationRank> _forcedReactions = new();
	byte _visibleFactionCount;
	byte _honoredFactionCount;
	byte _reveredFactionCount;
	byte _exaltedFactionCount;
	bool _sendFactionIncreased; //! Play visual effect on next SMSG_SET_FACTION_STANDING sent

	public byte VisibleFactionCount => _visibleFactionCount;

	public byte HonoredFactionCount => _honoredFactionCount;

	public byte ReveredFactionCount => _reveredFactionCount;

	public byte ExaltedFactionCount => _exaltedFactionCount;

	public SortedDictionary<uint, FactionState> StateList => _factions;

	public ReputationMgr(Player owner,
                            CliDB cliDB,
                            ScriptManager scriptManager,
                            GameObjectManager gameObjectManager,
                            DB2Manager db2Manager,
                            WorldConfig worldConfig,
                            CharacterDatabase characterDatabase)
	{
		_player = owner;
        _cliDb = cliDB;
        _scriptManager = scriptManager;
        _gameObjectManager = gameObjectManager;
        _db2Manager = db2Manager;
        _worldConfig = worldConfig;
        _characterDatabase = characterDatabase;

        _visibleFactionCount = 0;
		_honoredFactionCount = 0;
		_reveredFactionCount = 0;
		_exaltedFactionCount = 0;
		_sendFactionIncreased = false;
	}

	public FactionState GetState(FactionRecord factionEntry)
	{
		return factionEntry.CanHaveReputation() ? GetState(factionEntry.ReputationIndex) : null;
	}

	public bool IsAtWar(uint factionId)
	{
		var factionEntry = _cliDb.FactionStorage.LookupByKey(factionId);

		if (factionEntry == null)
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

	public int GetReputation(uint faction_id)
	{
		var factionEntry = _cliDb.FactionStorage.LookupByKey(faction_id);

		if (factionEntry == null)
		{
			Log.Logger.Error("ReputationMgr.GetReputation: Can't get reputation of {0} for unknown faction (faction id) #{1}.", _player.GetName(), faction_id);

			return 0;
		}

		return GetReputation(factionEntry);
	}

	public int GetBaseReputation(FactionRecord factionEntry)
	{
		var dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);

		if (dataIndex < 0)
			return 0;

		return factionEntry.ReputationBase[dataIndex];
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

	public ReputationRank GetRank(FactionRecord factionEntry)
	{
		var reputation = GetReputation(factionEntry);

		return ReputationToRank(factionEntry, reputation);
	}

	public ReputationRank GetForcedRankIfAny(FactionTemplateRecord factionTemplateEntry)
	{
		return GetForcedRankIfAny(factionTemplateEntry.Faction);
	}

	public int GetParagonLevel(uint paragonFactionId)
	{
		return GetParagonLevel(_cliDb.FactionStorage.LookupByKey(paragonFactionId));
	}

	public void ApplyForceReaction(uint faction_id, ReputationRank rank, bool apply)
	{
		if (apply)
			_forcedReactions[faction_id] = rank;
		else
			_forcedReactions.Remove(faction_id);
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

	public void SendState(FactionState faction)
	{
		SetFactionStanding setFactionStanding = new();
		setFactionStanding.BonusFromAchievementSystem = 0.0f;

		var standing = faction.VisualStandingIncrease != 0 ? faction.VisualStandingIncrease : faction.Standing;

		if (faction != null)
			setFactionStanding.Faction.Add(new FactionStandingData((int)faction.ReputationListID, standing));

		foreach (var state in _factions.Values)
			if (state.needSend)
			{
				state.needSend = false;

				if (faction == null || state.ReputationListID != faction.ReputationListID)
				{
					standing = state.VisualStandingIncrease != 0 ? state.VisualStandingIncrease : state.Standing;
					setFactionStanding.Faction.Add(new FactionStandingData((int)state.ReputationListID, standing));
				}
			}

		setFactionStanding.ShowVisual = _sendFactionIncreased;
		_player.SendPacket(setFactionStanding);

		_sendFactionIncreased = false; // Reset
	}

	public void SendInitialReputations()
	{
		InitializeFactions initFactions = new();

		foreach (var pair in _factions)
		{
			initFactions.FactionFlags[pair.Key] = pair.Value.Flags;
			initFactions.FactionStandings[pair.Key] = pair.Value.Standing;
			// @todo faction bonus
			pair.Value.needSend = false;
		}

		_player.SendPacket(initFactions);
	}

	public void SendVisible(FactionState faction, bool visible = true)
	{
		if (_player.Session.PlayerLoading.IsEmpty)
			return;

		//make faction visible / not visible in reputation list at client
		SetFactionVisible packet = new(visible);
		packet.FactionIndex = faction.ReputationListID;
		_player.SendPacket(packet);
	}

	public bool ModifyReputation(FactionRecord factionEntry, int standing, bool spillOverOnly = false, bool noSpillover = false)
	{
		return SetReputation(factionEntry, standing, true, spillOverOnly, noSpillover);
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
							var spilloverRep = (int)(standing * repTemplate.FactionRate[i]);
							SetOneFactionReputation(_cliDb.FactionStorage.LookupByKey(repTemplate.Faction[i]), spilloverRep, incremental);
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
					var parent = _cliDb.FactionStorage.LookupByKey(factionEntry.ParentFactionID);

					if (parent != null)
					{
						var parentState = _factions.LookupByKey((uint)parent.ReputationIndex);

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
						var factionEntryCalc = _cliDb.FactionStorage.LookupByKey(id);

						if (factionEntryCalc != null)
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
		var faction = _factions.LookupByKey((uint)factionEntry.ReputationIndex);

		if (faction != null)
		{
			var primaryFactionToModify = factionEntry;

			if (incremental && standing > 0 && CanGainParagonReputationForFaction(factionEntry))
			{
				primaryFactionToModify = _cliDb.FactionStorage.LookupByKey(factionEntry.ParagonFactionID);
				faction = _factions.LookupByKey((uint)primaryFactionToModify.ReputationIndex);
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

	public bool SetOneFactionReputation(FactionRecord factionEntry, int standing, bool incremental)
	{
		var factionState = _factions.LookupByKey((uint)factionEntry.ReputationIndex);

		if (factionState != null)
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
				standing = (int)(Math.Floor(standing * _worldConfig.GetFloatValue(WorldCfg.RateReputationGain) + 0.5f));
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
				var currency = _cliDb.CurrencyTypesStorage.LookupByKey(factionEntry.RenownCurrencyID);

				if (currency != null)
				{
					var renownLevelThreshold = GetRenownLevelThreshold(factionEntry);
					var oldRenownLevel = GetRenownLevel(factionEntry);

					var totalReputation = (oldRenownLevel * renownLevelThreshold) + (standing - baseRep);
					var newRenownLevel = totalReputation / renownLevelThreshold;
					newStanding = totalReputation % renownLevelThreshold;

					if (newRenownLevel >= GetRenownMaxLevel(factionEntry))
					{
						newStanding = 0;
						reputationChange += (GetRenownMaxLevel(factionEntry) * renownLevelThreshold) - totalReputation;
					}

					factionState.VisualStandingIncrease = reputationChange;

					// If the reputation is decreased by command, we will send CurrencyDestroyReason::Cheat
					if (oldRenownLevel != newRenownLevel)
						_player.ModifyCurrency(currency.Id, newRenownLevel - oldRenownLevel, CurrencyGainSource.RenownRepGain, CurrencyDestroyReason.Cheat);
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

	public void SetVisible(FactionTemplateRecord factionTemplateEntry)
	{
		if (factionTemplateEntry.Faction == 0)
			return;

		var factionEntry = _cliDb.FactionStorage.LookupByKey(factionTemplateEntry.Faction);

		if (factionEntry.Id != 0)
			// Never show factions of the opposing team
			if (!Convert.ToBoolean(factionEntry.ReputationRaceMask[1] & SharedConst.GetMaskForRace(_player.Race)) && factionEntry.ReputationBase[1] == SharedConst.ReputationBottom)
				SetVisible(factionEntry);
	}

	public void SetVisible(FactionRecord factionEntry)
	{
		if (!factionEntry.CanHaveReputation())
			return;

		var factionState = _factions.LookupByKey((uint)factionEntry.ReputationIndex);

		if (factionState == null)
			return;

		SetVisible(factionState);
	}

	public void SetAtWar(uint repListID, bool on)
	{
		var factionState = _factions.LookupByKey(repListID);

		if (factionState == null)
			return;

		// always invisible or hidden faction can't change war state
		if (factionState.Flags.HasAnyFlag(ReputationFlags.Hidden | ReputationFlags.Header))
			return;

		SetAtWar(factionState, on);
	}

	public void SetInactive(uint repListID, bool on)
	{
		var factionState = _factions.LookupByKey(repListID);

		if (factionState == null)
			return;

		SetInactive(factionState, on);
	}

	public void LoadFromDB(SQLResult result)
	{
		// Set initial reputations (so everything is nifty before DB data load)
		Initialize();

		if (!result.IsEmpty())
			do
			{
				var factionEntry = _cliDb.FactionStorage.LookupByKey(result.Read<uint>(0));

				if (factionEntry != null && factionEntry.CanHaveReputation())
				{
					var faction = _factions.LookupByKey((uint)factionEntry.ReputationIndex);

					if (faction == null)
						continue;

					// update standing to current
					faction.Standing = result.Read<int>(1);

					// update counters
					if (factionEntry.FriendshipRepID == 0)
					{
						var BaseRep = GetBaseReputation(factionEntry);
						var old_rank = ReputationToRank(factionEntry, BaseRep);
						var new_rank = ReputationToRank(factionEntry, BaseRep + faction.Standing);
						UpdateRankCounters(old_rank, new_rank);
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

					// reset changed flag if values similar to saved in DB
					if (faction.Flags == dbFactionFlags)
					{
						faction.needSend = false;
						faction.needSave = false;
					}
				}
			} while (result.NextRow());
	}

	public void SaveToDB(SQLTransaction trans)
	{
		foreach (var factionState in _factions.Values)
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

	public FactionState GetState(int id)
	{
		return _factions.LookupByKey((uint)id);
	}

	public uint GetReputationRankStrIndex(FactionRecord factionEntry)
	{
		return (uint)ReputationRankStrIndex[(int)GetRank(factionEntry)];
	}

	public ReputationRank GetForcedRankIfAny(uint factionId)
	{
		return _forcedReactions.TryGetValue(factionId, out var forced) ? forced : ReputationRank.None;
	}

	// this allows calculating base reputations to offline players, just by race and class
	public static int GetBaseReputationOf(FactionRecord factionEntry, Race race, PlayerClass playerClass)
	{
		if (factionEntry == null)
			return 0;

		var raceMask = SharedConst.GetMaskForRace(race);
		var classMask = (1u << ((int)playerClass - 1));

		for (var i = 0; i < 4; i++)
			if ((factionEntry.ReputationClassMask[i] == 0 || factionEntry.ReputationClassMask[i].HasAnyFlag((short)classMask)) && (factionEntry.ReputationRaceMask[i] == 0 || factionEntry.ReputationRaceMask[i].HasAnyFlag(raceMask)))
				return factionEntry.ReputationBase[i];

		return 0;
	}

	ReputationRank ReputationToRankHelper<T>(IList<T> thresholds, int standing, Func<T, int> thresholdExtractor)
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

	ReputationRank ReputationToRank(FactionRecord factionEntry, int standing)
	{
		var rank = ReputationRank.Min;

		var friendshipReactions = _db2Manager.GetFriendshipRepReactions(factionEntry.FriendshipRepID);

		if (!friendshipReactions.Empty())
			rank = ReputationToRankHelper(friendshipReactions, standing, (FriendshipRepReactionRecord frr) => { return frr.ReactionThreshold; });
		else
			rank = ReputationToRankHelper(ReputationRankThresholds, standing, (int threshold) => { return threshold; });

		return rank;
	}

	int GetMinReputation(FactionRecord factionEntry)
	{
		var friendshipReactions = _db2Manager.GetFriendshipRepReactions(factionEntry.FriendshipRepID);

		if (!friendshipReactions.Empty())
			return friendshipReactions[0].ReactionThreshold;

		return ReputationRankThresholds[0];
	}

	int GetMaxReputation(FactionRecord factionEntry)
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
			return friendshipReactions.LastOrDefault().ReactionThreshold;

		var dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);

		if (dataIndex >= 0)
			return factionEntry.ReputationMax[dataIndex];

		return ReputationRankThresholds.LastOrDefault();
	}

	ReputationRank GetBaseRank(FactionRecord factionEntry)
	{
		var reputation = GetBaseReputation(factionEntry);

		return ReputationToRank(factionEntry, reputation);
	}

	bool IsParagonReputation(FactionRecord factionEntry)
	{
		if (_db2Manager.GetParagonReputation(factionEntry.Id) != null)
			return true;

		return false;
	}

	int GetParagonLevel(FactionRecord paragonFactionEntry)
	{
		if (paragonFactionEntry == null)
			return 0;

		var paragonReputation = _db2Manager.GetParagonReputation(paragonFactionEntry.Id);

		if (paragonReputation != null)
			return GetReputation(paragonFactionEntry) / paragonReputation.LevelThreshold;

		return 0;
	}

	bool HasMaximumRenownReputation(FactionRecord factionEntry)
	{
		if (!IsRenownReputation(factionEntry))
			return false;

		return GetRenownLevel(factionEntry) >= GetRenownMaxLevel(factionEntry);
	}

	bool IsRenownReputation(FactionRecord factionEntry)
	{
		return factionEntry.RenownCurrencyID > 0;
	}

	int GetRenownLevel(FactionRecord renownFactionEntry)
	{
		if (renownFactionEntry == null)
			return 0;

		var currency = _cliDb.CurrencyTypesStorage.LookupByKey((uint)renownFactionEntry.RenownCurrencyID);

		if (currency != null)
			return (int)_player.GetCurrencyQuantity(currency.Id);

		return 0;
	}

	int GetRenownLevelThreshold(FactionRecord renownFactionEntry)
	{
		if (renownFactionEntry == null || !IsRenownReputation(renownFactionEntry))
			return 0;

		var dataIndex = GetFactionDataIndexForRaceAndClass(renownFactionEntry);

		if (dataIndex >= 0)
			return renownFactionEntry.ReputationMax[dataIndex];

		return 0;
	}

	int GetRenownMaxLevel(FactionRecord renownFactionEntry)
	{
		if (renownFactionEntry == null)
			return 0;

		var currency = _cliDb.CurrencyTypesStorage.LookupByKey((uint)renownFactionEntry.RenownCurrencyID);

		if (currency != null)
			return (int)_player.GetCurrencyMaxQuantity(currency);

		return 0;
	}

	ReputationFlags GetDefaultStateFlags(FactionRecord factionEntry)
	{
		var flags = ReputationFlags.None;

		var dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);

		if (dataIndex > 0)
			flags = (ReputationFlags)factionEntry.ReputationFlags[dataIndex];

		if (_db2Manager.GetParagonReputation(factionEntry.Id) != null)
			flags |= ReputationFlags.ShowPropagated;

		return flags;
	}

	void Initialize()
	{
		_factions.Clear();
		_visibleFactionCount = 0;
		_honoredFactionCount = 0;
		_reveredFactionCount = 0;
		_exaltedFactionCount = 0;
		_sendFactionIncreased = false;

		foreach (var factionEntry in _cliDb.FactionStorage.Values)
			if (factionEntry.CanHaveReputation())
			{
				FactionState newFaction = new();
				newFaction.Id = factionEntry.Id;
				newFaction.ReputationListID = (uint)factionEntry.ReputationIndex;
				newFaction.Standing = 0;
				newFaction.VisualStandingIncrease = 0;
				newFaction.Flags = GetDefaultStateFlags(factionEntry);
				newFaction.needSend = true;
				newFaction.needSave = true;

				if (newFaction.Flags.HasFlag(ReputationFlags.Visible))
					++_visibleFactionCount;

				if (factionEntry.FriendshipRepID == 0)
					UpdateRankCounters(ReputationRank.Hostile, GetBaseRank(factionEntry));

				_factions[newFaction.ReputationListID] = newFaction;
			}
	}

	void SetVisible(FactionState faction)
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

		_visibleFactionCount++;

		SendVisible(faction);
	}

	void SetAtWar(FactionState faction, bool atWar)
	{
		// Do not allow to declare war to our own faction. But allow for rival factions (eg Aldor vs Scryer).
		if (atWar && faction.Flags.HasFlag(ReputationFlags.Peaceful) && GetRank(_cliDb.FactionStorage.LookupByKey(faction.Id)) > ReputationRank.Hated)
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

	void SetInactive(FactionState faction, bool inactive)
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

	void UpdateRankCounters(ReputationRank old_rank, ReputationRank new_rank)
	{
		if (old_rank >= ReputationRank.Exalted)
			--_exaltedFactionCount;

		if (old_rank >= ReputationRank.Revered)
			--_reveredFactionCount;

		if (old_rank >= ReputationRank.Honored)
			--_honoredFactionCount;

		if (new_rank >= ReputationRank.Exalted)
			++_exaltedFactionCount;

		if (new_rank >= ReputationRank.Revered)
			++_reveredFactionCount;

		if (new_rank >= ReputationRank.Honored)
			++_honoredFactionCount;
	}

	int GetFactionDataIndexForRaceAndClass(FactionRecord factionEntry)
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

	bool CanGainParagonReputationForFaction(FactionRecord factionEntry)
	{
		if (!_cliDb.FactionStorage.ContainsKey(factionEntry.ParagonFactionID))
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
}
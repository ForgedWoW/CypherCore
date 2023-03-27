// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.Trait;
using Framework.Constants;

namespace Forged.MapServer.Spells;

internal class TraitMgr
{
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    public const uint COMMIT_COMBAT_TRAIT_CONFIG_CHANGES_SPELL_ID = 384255u;
	public const uint MAX_COMBAT_TRAIT_CONFIGS = 10u;

    private readonly Dictionary<int, NodeGroup> _traitGroups = new();
    private readonly Dictionary<int, Node> _traitNodes = new();
    private readonly Dictionary<int, Tree> _traitTrees = new();
    private readonly int[] _skillLinesByClass = new int[(int)PlayerClass.Max];
    private readonly MultiMap<int, Tree> _traitTreesBySkillLine = new();
    private readonly MultiMap<int, Tree> _traitTreesByTraitSystem = new();
    private int _configIdGenerator;
    private readonly MultiMap<int, TraitCurrencySourceRecord> _traitCurrencySourcesByCurrency = new();
    private readonly MultiMap<int, TraitDefinitionEffectPointsRecord> _traitDefinitionEffectPointModifiers = new();
    private readonly MultiMap<int, TraitTreeLoadoutEntryRecord> _traitTreeLoadoutsByChrSpecialization = new();

    public TraitMgr(CliDB cliDB, DB2Manager db2Manager)
    {
        _cliDB = cliDB;
        _db2Manager = db2Manager;
    }

    public void Load()
	{
		_configIdGenerator = int.MaxValue;

		MultiMap<int, TraitCondRecord> nodeEntryConditions = new();

		foreach (var traitNodeEntryXTraitCondEntry in _cliDB.TraitNodeEntryXTraitCondStorage.Values)
		{
			var traitCondEntry = _cliDB.TraitCondStorage.LookupByKey((uint)traitNodeEntryXTraitCondEntry.TraitCondID);

			if (traitCondEntry != null)
				nodeEntryConditions.Add((int)traitNodeEntryXTraitCondEntry.TraitNodeEntryID, traitCondEntry);
		}

		MultiMap<int, TraitCostRecord> nodeEntryCosts = new();

		foreach (var traitNodeEntryXTraitCostEntry in _cliDB.TraitNodeEntryXTraitCostStorage.Values)
		{
			var traitCostEntry = _cliDB.TraitCostStorage.LookupByKey((uint)traitNodeEntryXTraitCostEntry.TraitCostID);

			if (traitCostEntry != null)
				nodeEntryCosts.Add(traitNodeEntryXTraitCostEntry.TraitNodeEntryID, traitCostEntry);
		}

		MultiMap<int, TraitCondRecord> nodeGroupConditions = new();

		foreach (var traitNodeGroupXTraitCondEntry in _cliDB.TraitNodeGroupXTraitCondStorage.Values)
		{
			var traitCondEntry = _cliDB.TraitCondStorage.LookupByKey((uint)traitNodeGroupXTraitCondEntry.TraitCondID);

			if (traitCondEntry != null)
				nodeGroupConditions.Add(traitNodeGroupXTraitCondEntry.TraitNodeGroupID, traitCondEntry);
		}

		MultiMap<int, TraitCostRecord> nodeGroupCosts = new();

		foreach (var traitNodeGroupXTraitCostEntry in _cliDB.TraitNodeGroupXTraitCostStorage.Values)
		{
			var traitCondEntry = _cliDB.TraitCostStorage.LookupByKey((uint)traitNodeGroupXTraitCostEntry.TraitCostID);

			if (traitCondEntry != null)
				nodeGroupCosts.Add(traitNodeGroupXTraitCostEntry.TraitNodeGroupID, traitCondEntry);
		}

		MultiMap<int, int> nodeGroups = new();

		foreach (var traitNodeGroupXTraitNodeEntry in _cliDB.TraitNodeGroupXTraitNodeStorage.Values)
			nodeGroups.Add(traitNodeGroupXTraitNodeEntry.TraitNodeID, traitNodeGroupXTraitNodeEntry.TraitNodeGroupID);

		MultiMap<int, TraitCondRecord> nodeConditions = new();

		foreach (var traitNodeXTraitCondEntry in _cliDB.TraitNodeXTraitCondStorage.Values)
		{
			var traitCondEntry = _cliDB.TraitCondStorage.LookupByKey((uint)traitNodeXTraitCondEntry.TraitCondID);

			if (traitCondEntry != null)
				nodeConditions.Add(traitNodeXTraitCondEntry.TraitNodeID, traitCondEntry);
		}

		MultiMap<uint, TraitCostRecord> nodeCosts = new();

		foreach (var traitNodeXTraitCostEntry in _cliDB.TraitNodeXTraitCostStorage.Values)
		{
			var traitCostEntry = _cliDB.TraitCostStorage.LookupByKey((uint)traitNodeXTraitCostEntry.TraitCostID);

			if (traitCostEntry != null)
				nodeCosts.Add(traitNodeXTraitCostEntry.TraitNodeID, traitCostEntry);
		}

		MultiMap<int, TraitNodeEntryRecord> nodeEntries = new();

		foreach (var traitNodeXTraitNodeEntryEntry in _cliDB.TraitNodeXTraitNodeEntryStorage.Values)
		{
			var traitNodeEntryEntry = _cliDB.TraitNodeEntryStorage.LookupByKey((uint)traitNodeXTraitNodeEntryEntry.TraitNodeEntryID);

			if (traitNodeEntryEntry != null)
				nodeEntries.Add(traitNodeXTraitNodeEntryEntry.TraitNodeID, traitNodeEntryEntry);
		}

		MultiMap<uint, TraitCostRecord> treeCosts = new();

		foreach (var traitTreeXTraitCostEntry in _cliDB.TraitTreeXTraitCostStorage.Values)
		{
			var traitCostEntry = _cliDB.TraitCostStorage.LookupByKey((uint)traitTreeXTraitCostEntry.TraitCostID);

			if (traitCostEntry != null)
				treeCosts.Add(traitTreeXTraitCostEntry.TraitTreeID, traitCostEntry);
		}

		MultiMap<int, TraitCurrencyRecord> treeCurrencies = new();

		foreach (var traitTreeXTraitCurrencyEntry in _cliDB.TraitTreeXTraitCurrencyStorage.Values)
		{
			var traitCurrencyEntry = _cliDB.TraitCurrencyStorage.LookupByKey((uint)traitTreeXTraitCurrencyEntry.TraitCurrencyID);

			if (traitCurrencyEntry != null)
				treeCurrencies.Add(traitTreeXTraitCurrencyEntry.TraitTreeID, traitCurrencyEntry);
		}

		MultiMap<int, int> traitTreesIdsByTraitSystem = new();

		foreach (var traitTree in _cliDB.TraitTreeStorage.Values)
		{
			Tree tree = new()
			{
				Data = traitTree
			};

			var costs = treeCosts.LookupByKey(traitTree.Id);

			if (costs != null)
				tree.Costs = costs;

			var currencies = treeCurrencies.LookupByKey(traitTree.Id);

			if (currencies != null)
				tree.Currencies = currencies;

			if (traitTree.TraitSystemID != 0)
			{
				traitTreesIdsByTraitSystem.Add(traitTree.TraitSystemID, (int)traitTree.Id);
				tree.ConfigType = TraitConfigType.Generic;
			}

			_traitTrees[(int)traitTree.Id] = tree;
		}

		foreach (var traitNodeGroup in _cliDB.TraitNodeGroupStorage.Values)
		{
			NodeGroup nodeGroup = new()
			{
				Data = traitNodeGroup
			};

			var conditions = nodeGroupConditions.LookupByKey(traitNodeGroup.Id);

			if (conditions != null)
				nodeGroup.Conditions = conditions;

			var costs = nodeGroupCosts.LookupByKey(traitNodeGroup.Id);

			if (costs != null)
				nodeGroup.Costs = costs;

			_traitGroups[(int)traitNodeGroup.Id] = nodeGroup;
		}

		foreach (var traitNode in _cliDB.TraitNodeStorage.Values)
		{
			Node node = new()
			{
				Data = traitNode
			};

			var tree = _traitTrees.LookupByKey(traitNode.TraitTreeID);

			if (tree != null)
				tree.Nodes.Add(node);

			foreach (var traitNodeEntry in nodeEntries.LookupByKey(traitNode.Id))
			{
				NodeEntry entry = new()
				{
					Data = traitNodeEntry
				};

				var conditions = nodeEntryConditions.LookupByKey(traitNodeEntry.Id);

				if (conditions != null)
					entry.Conditions = conditions;

				var costs = nodeEntryCosts.LookupByKey(traitNodeEntry.Id);

				if (costs != null)
					entry.Costs = costs;

				node.Entries.Add(entry);
			}

			foreach (var nodeGroupId in nodeGroups.LookupByKey(traitNode.Id))
			{
				var nodeGroup = _traitGroups.LookupByKey(nodeGroupId);

				if (nodeGroup == null)
					continue;

				nodeGroup.Nodes.Add(node);
				node.Groups.Add(nodeGroup);
			}

			var conditions1 = nodeConditions.LookupByKey(traitNode.Id);

			if (conditions1 != null)
				node.Conditions = conditions1;

			var costs1 = nodeCosts.LookupByKey(traitNode.Id);

			if (costs1 != null)
				node.Costs = costs1;

			_traitNodes[(int)traitNode.Id] = node;
		}

		foreach (var traitEdgeEntry in _cliDB.TraitEdgeStorage.Values)
		{
			var left = _traitNodes.LookupByKey(traitEdgeEntry.LeftTraitNodeID);
			var right = _traitNodes.LookupByKey(traitEdgeEntry.RightTraitNodeID);

			if (left == null || right == null)
				continue;

			right.ParentNodes.Add(Tuple.Create(left, (TraitEdgeType)traitEdgeEntry.Type));
		}

		foreach (var skillLineXTraitTreeEntry in _cliDB.SkillLineXTraitTreeStorage.Values)
		{
			var tree = _traitTrees.LookupByKey(skillLineXTraitTreeEntry.TraitTreeID);

			if (tree == null)
				continue;

			var skillLineEntry = _cliDB.SkillLineStorage.LookupByKey((uint)skillLineXTraitTreeEntry.SkillLineID);

			if (skillLineEntry == null)
				continue;

			_traitTreesBySkillLine.Add(skillLineXTraitTreeEntry.SkillLineID, tree);

			if (skillLineEntry.CategoryID == SkillCategory.Class)
			{
				foreach (var skillRaceClassInfo in _db2Manager.GetSkillRaceClassInfo(skillLineEntry.Id))
					for (var i = 1; i < (int)PlayerClass.Max; ++i)
						if ((skillRaceClassInfo.ClassMask & (1 << (i - 1))) != 0)
							_skillLinesByClass[i] = skillLineXTraitTreeEntry.SkillLineID;

				tree.ConfigType = TraitConfigType.Combat;
			}
			else
			{
				tree.ConfigType = TraitConfigType.Profession;
			}
		}

		foreach (var (traitSystemId, traitTreeId) in traitTreesIdsByTraitSystem.KeyValueList)
		{
			var tree = _traitTrees.LookupByKey(traitTreeId);

			if (tree != null)
				_traitTreesByTraitSystem.Add(traitSystemId, tree);
		}

		foreach (var traitCurrencySource in _cliDB.TraitCurrencySourceStorage.Values)
			_traitCurrencySourcesByCurrency.Add(traitCurrencySource.TraitCurrencyID, traitCurrencySource);

		foreach (var traitDefinitionEffectPoints in _cliDB.TraitDefinitionEffectPointsStorage.Values)
			_traitDefinitionEffectPointModifiers.Add(traitDefinitionEffectPoints.TraitDefinitionID, traitDefinitionEffectPoints);

		MultiMap<int, TraitTreeLoadoutEntryRecord> traitTreeLoadoutEntries = new();

		foreach (var traitTreeLoadoutEntry in _cliDB.TraitTreeLoadoutEntryStorage.Values)
			traitTreeLoadoutEntries[traitTreeLoadoutEntry.TraitTreeLoadoutID].Add(traitTreeLoadoutEntry);

		foreach (var traitTreeLoadout in _cliDB.TraitTreeLoadoutStorage.Values)
		{
			var entries = traitTreeLoadoutEntries.LookupByKey(traitTreeLoadout.Id);

			if (entries != null)
			{
				entries.Sort((left, right) => { return left.OrderIndex.CompareTo(right.OrderIndex); });
				// there should be only one loadout per spec, we take last one encountered
				_traitTreeLoadoutsByChrSpecialization[traitTreeLoadout.ChrSpecializationID] = entries;
			}
		}
	}

	/**
	 * Generates new TraitConfig identifier.
	 * Because this only needs to be unique for each character we let it overflow
	 */
	public int GenerateNewTraitConfigId()
	{
		if (_configIdGenerator == int.MaxValue)
			_configIdGenerator = 0;

		return ++_configIdGenerator;
	}

	public TraitConfigType GetConfigTypeForTree(int traitTreeId)
	{
		var tree = _traitTrees.LookupByKey(traitTreeId);

		if (tree == null)
			return TraitConfigType.Invalid;

		return tree.ConfigType;
	}

	/**
	 * @brief Finds relevant TraitTree identifiers
	 * @param traitConfig config data
	 * @return Trait tree data
	 */
	public List<Tree> GetTreesForConfig(TraitConfigPacket traitConfig)
	{
		switch (traitConfig.Type)
		{
			case TraitConfigType.Combat:
				var chrSpecializationEntry = _cliDB.ChrSpecializationStorage.LookupByKey((uint)traitConfig.ChrSpecializationID);

				if (chrSpecializationEntry != null)
					return _traitTreesBySkillLine.LookupByKey(_skillLinesByClass[chrSpecializationEntry.ClassID]);

				break;
			case TraitConfigType.Profession:
				return _traitTreesBySkillLine.LookupByKey(traitConfig.SkillLineID);
			case TraitConfigType.Generic:
				return _traitTreesByTraitSystem.LookupByKey(traitConfig.TraitSystemID);
        }

		return null;
	}

	public bool HasEnoughCurrency(TraitEntryPacket entry, Dictionary<int, int> currencies)
	{
		int GetCurrencyCount(int currencyId)
		{
			return currencies.LookupByKey(currencyId);
		}

		var node = _traitNodes.LookupByKey(entry.TraitNodeID);

		foreach (var group in node.Groups)
			foreach (var cost in group.Costs)
				if (GetCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
					return false;

		var nodeEntryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == entry.TraitNodeEntryID);

		if (nodeEntryItr != null)
			foreach (var cost in nodeEntryItr.Costs)
				if (GetCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
					return false;

		foreach (var cost in node.Costs)
			if (GetCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
				return false;

		var tree = _traitTrees.LookupByKey(node.Data.TraitTreeID);

		if (tree != null)
			foreach (var cost in tree.Costs)
				if (GetCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
					return false;

		return true;
	}

	public void TakeCurrencyCost(TraitEntryPacket entry, Dictionary<int, int> currencies)
	{
		var node = _traitNodes.LookupByKey(entry.TraitNodeID);

		foreach (var group in node.Groups)
			foreach (var cost in group.Costs)
				currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

		var nodeEntryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == entry.TraitNodeEntryID);

		if (nodeEntryItr != null)
			foreach (var cost in nodeEntryItr.Costs)
				currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

		foreach (var cost in node.Costs)
			currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

		var tree = _traitTrees.LookupByKey(node.Data.TraitTreeID);

		if (tree != null)
			foreach (var cost in tree.Costs)
				currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;
	}

	public void FillOwnedCurrenciesMap(TraitConfigPacket traitConfig, Player player, Dictionary<int, int> currencies)
	{
		var trees = GetTreesForConfig(traitConfig);

		if (trees == null)
			return;

		bool HasTraitNodeEntry(int traitNodeEntryId)
		{
			return traitConfig.Entries.Any(traitEntry => traitEntry.Value.TryGetValue(traitNodeEntryId, out var entry) && (entry.Rank > 0 || entry.GrantedRanks > 0));
		}

		foreach (var tree in trees)
		{
			foreach (var currency in tree.Currencies)
				switch (currency.GetCurrencyType())
				{
					case TraitCurrencyType.Gold:
					{
						if (!currencies.ContainsKey((int)currency.Id))
							currencies[(int)currency.Id] = 0;

						// TODO amount is never used.
						//var amount = currencies[(int)currency.Id];

						//if (player.Money > (ulong)(int.MaxValue - amount))
						//	amount = int.MaxValue;
						//else
						//	amount += (int)player.Money;

						break;
					}
					case TraitCurrencyType.CurrencyTypesBased:
						if (!currencies.ContainsKey((int)currency.Id))
							currencies[(int)currency.Id] = 0;

						currencies[(int)currency.Id] += (int)player.GetCurrencyQuantity((uint)currency.CurrencyTypesID);

						break;
					case TraitCurrencyType.TraitSourced:
						var currencySources = _traitCurrencySourcesByCurrency.LookupByKey(currency.Id);

						if (currencySources != null)
							foreach (var currencySource in currencySources)
							{
								if (currencySource.QuestID != 0 && !player.IsQuestRewarded(currencySource.QuestID))
									continue;

								if (currencySource.AchievementID != 0 && !player.HasAchieved(currencySource.AchievementID))
									continue;

								if (currencySource.PlayerLevel != 0 && player.Level < currencySource.PlayerLevel)
									continue;

								if (currencySource.TraitNodeEntryID != 0 && !HasTraitNodeEntry(currencySource.TraitNodeEntryID))
									continue;

								if (!currencies.ContainsKey(currencySource.TraitCurrencyID))
									currencies[currencySource.TraitCurrencyID] = 0;

								currencies[currencySource.TraitCurrencyID] += currencySource.Amount;
							}

						break;
				}
		}
	}

	public void FillSpentCurrenciesMap(TraitEntryPacket entry, Dictionary<int, int> cachedCurrencies)
	{
		var node = _traitNodes.LookupByKey(entry.TraitNodeID);

		foreach (var group in node.Groups)
		{
			foreach (var cost in group.Costs)
			{
				if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
					cachedCurrencies[cost.TraitCurrencyID] = 0;

				cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank;
			}
		}

		var nodeEntryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == entry.TraitNodeEntryID);

		if (nodeEntryItr != null)
			foreach (var cost in nodeEntryItr.Costs)
			{
				if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
					cachedCurrencies[cost.TraitCurrencyID] = 0;

				cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank;
			}

		foreach (var cost in node.Costs)
		{
			if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
				cachedCurrencies[cost.TraitCurrencyID] = 0;

			cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank;
		}

		var tree = _traitTrees.LookupByKey(node.Data.TraitTreeID);

		if (tree != null)
			foreach (var cost in tree.Costs)
			{
				if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
					cachedCurrencies[cost.TraitCurrencyID] = 0;

				cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank;
			}
	}

	public void FillSpentCurrenciesMap(TraitConfigPacket traitConfig, Dictionary<int, int> cachedCurrencies)
	{
		foreach (var kvp in traitConfig.Entries.Values)
			foreach (var entry in kvp.Values)
				FillSpentCurrenciesMap(entry, cachedCurrencies);
	}

	public bool MeetsTraitCondition(TraitConfigPacket traitConfig, Player player, TraitCondRecord condition, Node node)
	{
		if (condition.QuestID != 0 && !player.IsQuestRewarded(condition.QuestID))
			return false;

		if (condition.AchievementID != 0 && !player.HasAchieved(condition.AchievementID))
			return false;

		if (condition.SpecSetID != 0)
		{
			var chrSpecializationId = player.GetPrimarySpecialization();

			if (traitConfig.Type == TraitConfigType.Combat)
				chrSpecializationId = (uint)traitConfig.ChrSpecializationID;

			if (!_db2Manager.IsSpecSetMember(condition.SpecSetID, chrSpecializationId))
				return false;
		}

		if (condition.TraitCurrencyID != 0 && condition.SpentAmountRequired != 0)
		{
			var nodeCost = node.Costs.FirstOrDefault(c => c.TraitCurrencyID == condition.TraitCurrencyID)?.Amount;

			if (!nodeCost.HasValue)
				nodeCost = 1;

			var spent = traitConfig.Entries.Sum(e => e.Value.Values.Sum(i => i.Rank * nodeCost));

			if (node.Data.TraitTreeID == condition.TraitTreeID && spent < condition.SpentAmountRequired)
				return false;
		}

		if (condition.RequiredLevel != 0 && player.Level < condition.RequiredLevel)
			return false;

		return true;
	}

	public List<TraitEntry> GetGrantedTraitEntriesForConfig(TraitConfigPacket traitConfig, Player player)
	{
		List<TraitEntry> entries = new();
		var trees = GetTreesForConfig(traitConfig);

		if (trees == null)
			return entries;

		TraitEntry GetOrCreateEntry(uint nodeId, uint entryId)
		{
			var foundTraitEntry = entries.Find(traitEntry => traitEntry.TraitNodeID == nodeId && traitEntry.TraitNodeEntryID == entryId);

			if (foundTraitEntry == null)
			{
				foundTraitEntry = new TraitEntry
				{
					TraitNodeID = (int)nodeId,
					TraitNodeEntryID = (int)entryId,
					Rank = 0,
					GrantedRanks = 0
				};

				entries.Add(foundTraitEntry);
			}

			return foundTraitEntry;
		}

		foreach (var tree in trees)
		{
			foreach (var node in tree.Nodes)
			{
				foreach (var entry in node.Entries)
					foreach (var condition in entry.Conditions)
						if (condition.GetCondType() == TraitConditionType.Granted && MeetsTraitCondition(traitConfig, player, condition, node))
							GetOrCreateEntry(node.Data.Id, entry.Data.Id).GrantedRanks += condition.GrantedRanks;

				foreach (var condition in node.Conditions)
					if (condition.GetCondType() == TraitConditionType.Granted && MeetsTraitCondition(traitConfig, player, condition, node))
						foreach (var entry in node.Entries)
							GetOrCreateEntry(node.Data.Id, entry.Data.Id).GrantedRanks += condition.GrantedRanks;

				foreach (var group in node.Groups)
					foreach (var condition in group.Conditions)
						if (condition.GetCondType() == TraitConditionType.Granted && MeetsTraitCondition(traitConfig, player, condition, node))
							foreach (var entry in node.Entries)
								GetOrCreateEntry(node.Data.Id, entry.Data.Id).GrantedRanks += condition.GrantedRanks;
			}
		}

		return entries;
	}

	public bool IsValidEntry(TraitEntryPacket traitEntry)
	{
		var node = _traitNodes.LookupByKey(traitEntry.TraitNodeID);

		if (node == null)
			return false;

		var entryItr = node.Entries.Find(entry => entry.Data.Id == traitEntry.TraitNodeEntryID);

		if (entryItr == null)
			return false;

		if (entryItr.Data.MaxRanks < traitEntry.Rank + traitEntry.GrantedRanks)
			return false;

		return true;
	}

	public TalentLearnResult ValidateConfig(TraitConfigPacket traitConfig, Player player, bool requireSpendingAllCurrencies = false)
	{
		int GetNodeEntryCount(int traitNodeId)
		{
			return traitConfig.Entries.Count(traitEntry => traitEntry.Key == traitNodeId);
		}

		TraitEntryPacket GetNodeEntry(uint traitNodeId, uint traitNodeEntryId)
		{
			return traitConfig.Entries.LookupByKey((int)traitNodeId)?.LookupByKey((int)traitNodeEntryId);
		}

		bool IsNodeFullyFilled(Node node)
		{
			if (node.Data.GetNodeType() == TraitNodeType.Selection)
				return node.Entries.Any(nodeEntry =>
				{
					var traitEntry = GetNodeEntry(node.Data.Id, nodeEntry.Data.Id);

					return traitEntry != null && (traitEntry.Rank + traitEntry.GrantedRanks) == nodeEntry.Data.MaxRanks;
				});

			return node.Entries.All(nodeEntry =>
			{
				var traitEntry = GetNodeEntry(node.Data.Id, nodeEntry.Data.Id);

				return traitEntry != null && (traitEntry.Rank + traitEntry.GrantedRanks) == nodeEntry.Data.MaxRanks;
			});
		}
		
		Dictionary<int, int> spentCurrencies = new();
		FillSpentCurrenciesMap(traitConfig, spentCurrencies);

		bool MeetsConditions(List<TraitCondRecord> conditions, Node node)
		{
			var hasConditions = false;

			foreach (var condition in conditions)
				if (condition.GetCondType() == TraitConditionType.Available || condition.GetCondType() == TraitConditionType.Visible)
				{
					if (MeetsTraitCondition(traitConfig, player, condition, node))
						return true;

					hasConditions = true;
				}

			return !hasConditions;
		}

		foreach (var kvp in traitConfig.Entries.Values)
			foreach (var traitEntry in kvp.Values)
			{
				if (!IsValidEntry(traitEntry))
					return TalentLearnResult.FailedUnknown;

				var node = _traitNodes.LookupByKey(traitEntry.TraitNodeID);

				if (node.Data.GetNodeType() == TraitNodeType.Selection)
					if (GetNodeEntryCount(traitEntry.TraitNodeID) != 1)
						return TalentLearnResult.FailedUnknown;

				foreach (var entry in node.Entries)
					if (!MeetsConditions(entry.Conditions, node))
						return TalentLearnResult.FailedUnknown;

				if (!MeetsConditions(node.Conditions, node))
					return TalentLearnResult.FailedUnknown;

				foreach (var group in node.Groups)
					if (!MeetsConditions(group.Conditions, node))
						return TalentLearnResult.FailedUnknown;

				if (!node.ParentNodes.Empty())
				{
					var hasAnyParentTrait = false;

					foreach (var (parentNode, edgeType) in node.ParentNodes)
					{
						if (!IsNodeFullyFilled(parentNode))
						{
							if (edgeType == TraitEdgeType.RequiredForAvailability)
								return TalentLearnResult.FailedNotEnoughTalentsInPrimaryTree;

							continue;
						}

						hasAnyParentTrait = true;
					}

					if (!hasAnyParentTrait)
						return TalentLearnResult.FailedNotEnoughTalentsInPrimaryTree;
				}
			}

		Dictionary<int, int> grantedCurrencies = new();
		FillOwnedCurrenciesMap(traitConfig, player, grantedCurrencies);

		foreach (var (traitCurrencyId, spentAmount) in spentCurrencies)
		{
			if (_cliDB.TraitCurrencyStorage.LookupByKey((uint)traitCurrencyId).GetCurrencyType() != TraitCurrencyType.TraitSourced)
				continue;

			if (spentAmount == 0)
				continue;

			var grantedCount = grantedCurrencies.LookupByKey(traitCurrencyId);

			if (grantedCount == 0 || grantedCount < spentAmount)
				return TalentLearnResult.FailedNotEnoughTalentsInPrimaryTree;
		}

		if (requireSpendingAllCurrencies && traitConfig.Type == TraitConfigType.Combat)
			foreach (var (traitCurrencyId, grantedAmount) in grantedCurrencies)
			{
				if (grantedAmount == 0)
					continue;

				var spentAmount = spentCurrencies.LookupByKey(traitCurrencyId);

				if (spentAmount == 0 || spentAmount != grantedAmount)
					return TalentLearnResult.UnspentTalentPoints;
			}

		return TalentLearnResult.LearnOk;
	}

	public List<TraitDefinitionEffectPointsRecord> GetTraitDefinitionEffectPointModifiers(int traitDefinitionId)
	{
		return _traitDefinitionEffectPointModifiers.LookupByKey(traitDefinitionId);
	}

	public void InitializeStarterBuildTraitConfig(TraitConfigPacket traitConfig, Player player)
	{
		traitConfig.Entries.Clear();
		var trees = GetTreesForConfig(traitConfig);

		if (trees == null)
			return;

		foreach (var grant in GetGrantedTraitEntriesForConfig(traitConfig, player))
		{
			TraitEntryPacket newEntry = new()
			{
				TraitNodeID = grant.TraitNodeID,
				TraitNodeEntryID = grant.TraitNodeEntryID,
				GrantedRanks = grant.GrantedRanks
			};

			traitConfig.AddEntry(newEntry);
		}

		Dictionary<int, int> currencies = new();
		FillOwnedCurrenciesMap(traitConfig, player, currencies);

		var loadoutEntries = _traitTreeLoadoutsByChrSpecialization.LookupByKey(traitConfig.ChrSpecializationID);

		if (loadoutEntries != null)
		{
			TraitEntryPacket FindEntry(TraitConfigPacket config, int traitNodeId, int traitNodeEntryId)
			{
				return config.Entries.LookupByKey(traitNodeId)?.LookupByKey(traitNodeEntryId);
			}

			foreach (var loadoutEntry in loadoutEntries)
			{
				var addedRanks = loadoutEntry.NumPoints;
				var node = _traitNodes.LookupByKey(loadoutEntry.SelectedTraitNodeID);

				TraitEntryPacket newEntry = new()
				{
					TraitNodeID = loadoutEntry.SelectedTraitNodeID,
					TraitNodeEntryID = loadoutEntry.SelectedTraitNodeEntryID
				};

				if (newEntry.TraitNodeEntryID == 0)
					newEntry.TraitNodeEntryID = (int)node.Entries[0].Data.Id;

				var entryInConfig = FindEntry(traitConfig, newEntry.TraitNodeID, newEntry.TraitNodeEntryID);

				if (entryInConfig != null)
					addedRanks -= entryInConfig.Rank;

				newEntry.Rank = addedRanks;

				if (!HasEnoughCurrency(newEntry, currencies))
					continue;

				if (entryInConfig != null)
					entryInConfig.Rank += addedRanks;
				else
					traitConfig.AddEntry(newEntry);

				TakeCurrencyCost(newEntry, currencies);
			}
		}
	}
}
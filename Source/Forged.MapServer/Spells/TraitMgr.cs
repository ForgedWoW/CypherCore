// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;

namespace Game;

class TraitMgr
{
	public const uint COMMIT_COMBAT_TRAIT_CONFIG_CHANGES_SPELL_ID = 384255u;
	public const uint MAX_COMBAT_TRAIT_CONFIGS = 10u;

	static readonly Dictionary<int, NodeGroup> TraitGroups = new();
	static readonly Dictionary<int, Node> TraitNodes = new();
	static readonly Dictionary<int, Tree> TraitTrees = new();
	static readonly int[] SkillLinesByClass = new int[(int)PlayerClass.Max];
	static readonly MultiMap<int, Tree> TraitTreesBySkillLine = new();
	static readonly MultiMap<int, Tree> TraitTreesByTraitSystem = new();
	static int _configIdGenerator;
	static readonly MultiMap<int, TraitCurrencySourceRecord> TraitCurrencySourcesByCurrency = new();
	static readonly MultiMap<int, TraitDefinitionEffectPointsRecord> TraitDefinitionEffectPointModifiers = new();
	static readonly MultiMap<int, TraitTreeLoadoutEntryRecord> TraitTreeLoadoutsByChrSpecialization = new();

	public static void Load()
	{
		_configIdGenerator = int.MaxValue;

		MultiMap<int, TraitCondRecord> nodeEntryConditions = new();

		foreach (var traitNodeEntryXTraitCondEntry in CliDB.TraitNodeEntryXTraitCondStorage.Values)
		{
			var traitCondEntry = CliDB.TraitCondStorage.LookupByKey(traitNodeEntryXTraitCondEntry.TraitCondID);

			if (traitCondEntry != null)
				nodeEntryConditions.Add((int)traitNodeEntryXTraitCondEntry.TraitNodeEntryID, traitCondEntry);
		}

		MultiMap<int, TraitCostRecord> nodeEntryCosts = new();

		foreach (var traitNodeEntryXTraitCostEntry in CliDB.TraitNodeEntryXTraitCostStorage.Values)
		{
			var traitCostEntry = CliDB.TraitCostStorage.LookupByKey(traitNodeEntryXTraitCostEntry.TraitCostID);

			if (traitCostEntry != null)
				nodeEntryCosts.Add(traitNodeEntryXTraitCostEntry.TraitNodeEntryID, traitCostEntry);
		}

		MultiMap<int, TraitCondRecord> nodeGroupConditions = new();

		foreach (var traitNodeGroupXTraitCondEntry in CliDB.TraitNodeGroupXTraitCondStorage.Values)
		{
			var traitCondEntry = CliDB.TraitCondStorage.LookupByKey(traitNodeGroupXTraitCondEntry.TraitCondID);

			if (traitCondEntry != null)
				nodeGroupConditions.Add(traitNodeGroupXTraitCondEntry.TraitNodeGroupID, traitCondEntry);
		}

		MultiMap<int, TraitCostRecord> nodeGroupCosts = new();

		foreach (var traitNodeGroupXTraitCostEntry in CliDB.TraitNodeGroupXTraitCostStorage.Values)
		{
			var traitCondEntry = CliDB.TraitCostStorage.LookupByKey(traitNodeGroupXTraitCostEntry.TraitCostID);

			if (traitCondEntry != null)
				nodeGroupCosts.Add(traitNodeGroupXTraitCostEntry.TraitNodeGroupID, traitCondEntry);
		}

		MultiMap<int, int> nodeGroups = new();

		foreach (var traitNodeGroupXTraitNodeEntry in CliDB.TraitNodeGroupXTraitNodeStorage.Values)
			nodeGroups.Add(traitNodeGroupXTraitNodeEntry.TraitNodeID, traitNodeGroupXTraitNodeEntry.TraitNodeGroupID);

		MultiMap<int, TraitCondRecord> nodeConditions = new();

		foreach (var traitNodeXTraitCondEntry in CliDB.TraitNodeXTraitCondStorage.Values)
		{
			var traitCondEntry = CliDB.TraitCondStorage.LookupByKey(traitNodeXTraitCondEntry.TraitCondID);

			if (traitCondEntry != null)
				nodeConditions.Add(traitNodeXTraitCondEntry.TraitNodeID, traitCondEntry);
		}

		MultiMap<uint, TraitCostRecord> nodeCosts = new();

		foreach (var traitNodeXTraitCostEntry in CliDB.TraitNodeXTraitCostStorage.Values)
		{
			var traitCostEntry = CliDB.TraitCostStorage.LookupByKey(traitNodeXTraitCostEntry.TraitCostID);

			if (traitCostEntry != null)
				nodeCosts.Add(traitNodeXTraitCostEntry.TraitNodeID, traitCostEntry);
		}

		MultiMap<int, TraitNodeEntryRecord> nodeEntries = new();

		foreach (var traitNodeXTraitNodeEntryEntry in CliDB.TraitNodeXTraitNodeEntryStorage.Values)
		{
			var traitNodeEntryEntry = CliDB.TraitNodeEntryStorage.LookupByKey(traitNodeXTraitNodeEntryEntry.TraitNodeEntryID);

			if (traitNodeEntryEntry != null)
				nodeEntries.Add(traitNodeXTraitNodeEntryEntry.TraitNodeID, traitNodeEntryEntry);
		}

		MultiMap<uint, TraitCostRecord> treeCosts = new();

		foreach (var traitTreeXTraitCostEntry in CliDB.TraitTreeXTraitCostStorage.Values)
		{
			var traitCostEntry = CliDB.TraitCostStorage.LookupByKey(traitTreeXTraitCostEntry.TraitCostID);

			if (traitCostEntry != null)
				treeCosts.Add(traitTreeXTraitCostEntry.TraitTreeID, traitCostEntry);
		}

		MultiMap<int, TraitCurrencyRecord> treeCurrencies = new();

		foreach (var traitTreeXTraitCurrencyEntry in CliDB.TraitTreeXTraitCurrencyStorage.Values)
		{
			var traitCurrencyEntry = CliDB.TraitCurrencyStorage.LookupByKey(traitTreeXTraitCurrencyEntry.TraitCurrencyID);

			if (traitCurrencyEntry != null)
				treeCurrencies.Add(traitTreeXTraitCurrencyEntry.TraitTreeID, traitCurrencyEntry);
		}

		MultiMap<int, int> traitTreesIdsByTraitSystem = new();

		foreach (var traitTree in CliDB.TraitTreeStorage.Values)
		{
			Tree tree = new();
			tree.Data = traitTree;

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

			TraitTrees[(int)traitTree.Id] = tree;
		}

		foreach (var traitNodeGroup in CliDB.TraitNodeGroupStorage.Values)
		{
			NodeGroup nodeGroup = new();
			nodeGroup.Data = traitNodeGroup;

			var conditions = nodeGroupConditions.LookupByKey(traitNodeGroup.Id);

			if (conditions != null)
				nodeGroup.Conditions = conditions;

			var costs = nodeGroupCosts.LookupByKey(traitNodeGroup.Id);

			if (costs != null)
				nodeGroup.Costs = costs;

			TraitGroups[(int)traitNodeGroup.Id] = nodeGroup;
		}

		foreach (var traitNode in CliDB.TraitNodeStorage.Values)
		{
			Node node = new();
			node.Data = traitNode;

			var tree = TraitTrees.LookupByKey(traitNode.TraitTreeID);

			if (tree != null)
				tree.Nodes.Add(node);

			foreach (var traitNodeEntry in nodeEntries.LookupByKey(traitNode.Id))
			{
				NodeEntry entry = new();
				entry.Data = traitNodeEntry;

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
				var nodeGroup = TraitGroups.LookupByKey(nodeGroupId);

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

			TraitNodes[(int)traitNode.Id] = node;
		}

		foreach (var traitEdgeEntry in CliDB.TraitEdgeStorage.Values)
		{
			var left = TraitNodes.LookupByKey(traitEdgeEntry.LeftTraitNodeID);
			var right = TraitNodes.LookupByKey(traitEdgeEntry.RightTraitNodeID);

			if (left == null || right == null)
				continue;

			right.ParentNodes.Add(Tuple.Create(left, (TraitEdgeType)traitEdgeEntry.Type));
		}

		foreach (var skillLineXTraitTreeEntry in CliDB.SkillLineXTraitTreeStorage.Values)
		{
			var tree = TraitTrees.LookupByKey(skillLineXTraitTreeEntry.TraitTreeID);

			if (tree == null)
				continue;

			var skillLineEntry = CliDB.SkillLineStorage.LookupByKey(skillLineXTraitTreeEntry.SkillLineID);

			if (skillLineEntry == null)
				continue;

			TraitTreesBySkillLine.Add(skillLineXTraitTreeEntry.SkillLineID, tree);

			if (skillLineEntry.CategoryID == SkillCategory.Class)
			{
				foreach (var skillRaceClassInfo in Global.DB2Mgr.GetSkillRaceClassInfo(skillLineEntry.Id))
					for (var i = 1; i < (int)PlayerClass.Max; ++i)
						if ((skillRaceClassInfo.ClassMask & (1 << (i - 1))) != 0)
							SkillLinesByClass[i] = skillLineXTraitTreeEntry.SkillLineID;

				tree.ConfigType = TraitConfigType.Combat;
			}
			else
			{
				tree.ConfigType = TraitConfigType.Profession;
			}
		}

		foreach (var (traitSystemId, traitTreeId) in traitTreesIdsByTraitSystem.KeyValueList)
		{
			var tree = TraitTrees.LookupByKey(traitTreeId);

			if (tree != null)
				TraitTreesByTraitSystem.Add(traitSystemId, tree);
		}

		foreach (var traitCurrencySource in CliDB.TraitCurrencySourceStorage.Values)
			TraitCurrencySourcesByCurrency.Add(traitCurrencySource.TraitCurrencyID, traitCurrencySource);

		foreach (var traitDefinitionEffectPoints in CliDB.TraitDefinitionEffectPointsStorage.Values)
			TraitDefinitionEffectPointModifiers.Add(traitDefinitionEffectPoints.TraitDefinitionID, traitDefinitionEffectPoints);

		MultiMap<int, TraitTreeLoadoutEntryRecord> traitTreeLoadoutEntries = new();

		foreach (var traitTreeLoadoutEntry in CliDB.TraitTreeLoadoutEntryStorage.Values)
			traitTreeLoadoutEntries[traitTreeLoadoutEntry.TraitTreeLoadoutID].Add(traitTreeLoadoutEntry);

		foreach (var traitTreeLoadout in CliDB.TraitTreeLoadoutStorage.Values)
		{
			var entries = traitTreeLoadoutEntries.LookupByKey(traitTreeLoadout.Id);

			if (entries != null)
			{
				entries.Sort((left, right) => { return left.OrderIndex.CompareTo(right.OrderIndex); });
				// there should be only one loadout per spec, we take last one encountered
				TraitTreeLoadoutsByChrSpecialization[traitTreeLoadout.ChrSpecializationID] = entries;
			}
		}
	}

	/**
	 * Generates new TraitConfig identifier.
	 * Because this only needs to be unique for each character we let it overflow
	 */
	public static int GenerateNewTraitConfigId()
	{
		if (_configIdGenerator == int.MaxValue)
			_configIdGenerator = 0;

		return ++_configIdGenerator;
	}

	public static TraitConfigType GetConfigTypeForTree(int traitTreeId)
	{
		var tree = TraitTrees.LookupByKey(traitTreeId);

		if (tree == null)
			return TraitConfigType.Invalid;

		return tree.ConfigType;
	}

	/**
	 * @brief Finds relevant TraitTree identifiers
	 * @param traitConfig config data
	 * @return Trait tree data
	 */
	public static List<Tree> GetTreesForConfig(TraitConfigPacket traitConfig)
	{
		switch (traitConfig.Type)
		{
			case TraitConfigType.Combat:
				var chrSpecializationEntry = CliDB.ChrSpecializationStorage.LookupByKey(traitConfig.ChrSpecializationID);

				if (chrSpecializationEntry != null)
					return TraitTreesBySkillLine.LookupByKey(SkillLinesByClass[chrSpecializationEntry.ClassID]);

				break;
			case TraitConfigType.Profession:
				return TraitTreesBySkillLine.LookupByKey(traitConfig.SkillLineID);
			case TraitConfigType.Generic:
				return TraitTreesByTraitSystem.LookupByKey(traitConfig.TraitSystemID);
			default:
				break;
		}

		return null;
	}

	public static bool HasEnoughCurrency(TraitEntryPacket entry, Dictionary<int, int> currencies)
	{
		int GetCurrencyCount(int currencyId)
		{
			return currencies.LookupByKey(currencyId);
		}

		var node = TraitNodes.LookupByKey(entry.TraitNodeID);

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

		var tree = TraitTrees.LookupByKey(node.Data.TraitTreeID);

		if (tree != null)
			foreach (var cost in tree.Costs)
				if (GetCurrencyCount(cost.TraitCurrencyID) < cost.Amount * entry.Rank)
					return false;

		return true;
	}

	public static void TakeCurrencyCost(TraitEntryPacket entry, Dictionary<int, int> currencies)
	{
		var node = TraitNodes.LookupByKey(entry.TraitNodeID);

		foreach (var group in node.Groups)
			foreach (var cost in group.Costs)
				currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

		var nodeEntryItr = node.Entries.Find(nodeEntry => nodeEntry.Data.Id == entry.TraitNodeEntryID);

		if (nodeEntryItr != null)
			foreach (var cost in nodeEntryItr.Costs)
				currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

		foreach (var cost in node.Costs)
			currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;

		var tree = TraitTrees.LookupByKey(node.Data.TraitTreeID);

		if (tree != null)
			foreach (var cost in tree.Costs)
				currencies[cost.TraitCurrencyID] -= cost.Amount * entry.Rank;
	}

	public static void FillOwnedCurrenciesMap(TraitConfigPacket traitConfig, Player player, Dictionary<int, int> currencies)
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

						var amount = currencies[(int)currency.Id];

						if (player.Money > (ulong)(int.MaxValue - amount))
							amount = int.MaxValue;
						else
							amount += (int)player.Money;

						break;
					}
					case TraitCurrencyType.CurrencyTypesBased:
						if (!currencies.ContainsKey((int)currency.Id))
							currencies[(int)currency.Id] = 0;

						currencies[(int)currency.Id] += (int)player.GetCurrencyQuantity((uint)currency.CurrencyTypesID);

						break;
					case TraitCurrencyType.TraitSourced:
						var currencySources = TraitCurrencySourcesByCurrency.LookupByKey(currency.Id);

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
					default:
						break;
				}
		}
	}

	public static void FillSpentCurrenciesMap(TraitEntryPacket entry, Dictionary<int, int> cachedCurrencies)
	{
		var node = TraitNodes.LookupByKey(entry.TraitNodeID);

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

		var tree = TraitTrees.LookupByKey(node.Data.TraitTreeID);

		if (tree != null)
			foreach (var cost in tree.Costs)
			{
				if (!cachedCurrencies.ContainsKey(cost.TraitCurrencyID))
					cachedCurrencies[cost.TraitCurrencyID] = 0;

				cachedCurrencies[cost.TraitCurrencyID] += cost.Amount * entry.Rank;
			}
	}

	public static void FillSpentCurrenciesMap(TraitConfigPacket traitConfig, Dictionary<int, int> cachedCurrencies)
	{
		foreach (var kvp in traitConfig.Entries.Values)
			foreach (var entry in kvp.Values)
				FillSpentCurrenciesMap(entry, cachedCurrencies);
	}

	public static bool MeetsTraitCondition(TraitConfigPacket traitConfig, Player player, TraitCondRecord condition, Node node)
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

			if (!Global.DB2Mgr.IsSpecSetMember(condition.SpecSetID, chrSpecializationId))
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

	public static List<TraitEntry> GetGrantedTraitEntriesForConfig(TraitConfigPacket traitConfig, Player player)
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
				foundTraitEntry = new TraitEntry();
				foundTraitEntry.TraitNodeID = (int)nodeId;
				foundTraitEntry.TraitNodeEntryID = (int)entryId;
				foundTraitEntry.Rank = 0;
				foundTraitEntry.GrantedRanks = 0;
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

	public static bool IsValidEntry(TraitEntryPacket traitEntry)
	{
		var node = TraitNodes.LookupByKey(traitEntry.TraitNodeID);

		if (node == null)
			return false;

		var entryItr = node.Entries.Find(entry => entry.Data.Id == traitEntry.TraitNodeEntryID);

		if (entryItr == null)
			return false;

		if (entryItr.Data.MaxRanks < traitEntry.Rank + traitEntry.GrantedRanks)
			return false;

		return true;
	}

	public static TalentLearnResult ValidateConfig(TraitConfigPacket traitConfig, Player player, bool requireSpendingAllCurrencies = false)
	{
		var pTraits = player.GetTraitConfig(traitConfig.ID);

		int GetNodeEntryCount(int traitNodeId)
		{
			return traitConfig.Entries.Count(traitEntry => traitEntry.Key == traitNodeId);
		}

		TraitEntryPacket GetNodeEntry(uint traitNodeId, uint traitNodeEntryId)
		{
			return traitConfig.Entries.LookupByKey(traitNodeId)?.LookupByKey(traitNodeEntryId);
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

		;

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

				var node = TraitNodes.LookupByKey(traitEntry.TraitNodeID);

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
			if (CliDB.TraitCurrencyStorage.LookupByKey(traitCurrencyId).GetCurrencyType() != TraitCurrencyType.TraitSourced)
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

	public static List<TraitDefinitionEffectPointsRecord> GetTraitDefinitionEffectPointModifiers(int traitDefinitionId)
	{
		return TraitDefinitionEffectPointModifiers.LookupByKey(traitDefinitionId);
	}

	public static void InitializeStarterBuildTraitConfig(TraitConfigPacket traitConfig, Player player)
	{
		traitConfig.Entries.Clear();
		var trees = GetTreesForConfig(traitConfig);

		if (trees == null)
			return;

		foreach (var grant in GetGrantedTraitEntriesForConfig(traitConfig, player))
		{
			TraitEntryPacket newEntry = new();
			newEntry.TraitNodeID = grant.TraitNodeID;
			newEntry.TraitNodeEntryID = grant.TraitNodeEntryID;
			newEntry.GrantedRanks = grant.GrantedRanks;
			traitConfig.AddEntry(newEntry);
		}

		Dictionary<int, int> currencies = new();
		FillOwnedCurrenciesMap(traitConfig, player, currencies);

		var loadoutEntries = TraitTreeLoadoutsByChrSpecialization.LookupByKey(traitConfig.ChrSpecializationID);

		if (loadoutEntries != null)
		{
			TraitEntryPacket FindEntry(TraitConfigPacket config, int traitNodeId, int traitNodeEntryId)
			{
				return config.Entries.LookupByKey(traitNodeId)?.LookupByKey(traitNodeEntryId);
			}

			foreach (var loadoutEntry in loadoutEntries)
			{
				var addedRanks = loadoutEntry.NumPoints;
				var node = TraitNodes.LookupByKey(loadoutEntry.SelectedTraitNodeID);

				TraitEntryPacket newEntry = new();
				newEntry.TraitNodeID = loadoutEntry.SelectedTraitNodeID;
				newEntry.TraitNodeEntryID = loadoutEntry.SelectedTraitNodeEntryID;

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
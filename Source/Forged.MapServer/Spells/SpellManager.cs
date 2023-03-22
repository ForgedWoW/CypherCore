﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Game.BattlePets;
using Game.DataStorage;
using Game.Extendability;
using Game.Movement;
using Game.Scripting.Interfaces.ISpellManager;
using Game.Spells;

namespace Game.Entities;

public sealed class SpellManager : Singleton<SpellManager>
{
	public delegate void AuraEffectHandler(AuraEffect effect, AuraApplication aurApp, AuraEffectHandleModes mode, bool apply);

	public delegate void SpellEffectHandler(Spell spell);

	public MultiMap<uint, uint> PetFamilySpellsStorage = new();

	static readonly Dictionary<int, PetAura> DefaultPetAuras = new();

	readonly Dictionary<Difficulty, SpellInfo> _emptyDiffDict = new();


	readonly Dictionary<uint, SpellChainNode> _spellChainNodes = new();
	readonly MultiMap<uint, uint> _spellsReqSpell = new();
	readonly MultiMap<uint, uint> _spellReq = new();
	readonly Dictionary<uint, SpellLearnSkillNode> _spellLearnSkills = new();
	readonly MultiMap<uint, SpellLearnSpellNode> _spellLearnSpells = new();
	readonly Dictionary<KeyValuePair<uint, int>, SpellTargetPosition> _spellTargetPositions = new();
	readonly MultiMap<uint, SpellGroup> _spellSpellGroup = new();
	readonly MultiMap<SpellGroup, int> _spellGroupSpell = new();
	readonly Dictionary<SpellGroup, SpellGroupStackRule> _spellGroupStack = new();
	readonly MultiMap<SpellGroup, AuraType> _spellSameEffectStack = new();
	readonly List<ServersideSpellName> _serversideSpellNames = new();
	readonly Dictionary<uint, Dictionary<Difficulty, SpellProcEntry>> _spellProcMap = new();
	readonly Dictionary<uint, SpellThreatEntry> _spellThreatMap = new();
	readonly Dictionary<uint, Dictionary<int, PetAura>> _spellPetAuraMap = new();
	readonly MultiMap<(SpellLinkedType, uint), int> _spellLinkedMap = new();
	readonly Dictionary<uint, SpellEnchantProcEntry> _spellEnchantProcEventMap = new();
	readonly MultiMap<uint, SpellArea> _spellAreaMap = new();
	readonly MultiMap<uint, SpellArea> _spellAreaForQuestMap = new();
	readonly MultiMap<uint, SpellArea> _spellAreaForQuestEndMap = new();
	readonly MultiMap<uint, SpellArea> _spellAreaForAuraMap = new();
	readonly MultiMap<uint, SpellArea> _spellAreaForAreaMap = new();
	readonly MultiMap<uint, SkillLineAbilityRecord> _skillLineAbilityMap = new();
	readonly Dictionary<uint, MultiMap<uint, uint>> _petLevelupSpellMap = new();
	readonly Dictionary<uint, PetDefaultSpellsEntry> _petDefaultSpellsEntries = new(); // only spells not listed in related mPetLevelupSpellMap entry
	readonly Dictionary<uint, Dictionary<Difficulty, SpellInfo>> _spellInfoMap = new();
	readonly Dictionary<Tuple<uint, byte>, uint> _spellTotemModel = new();

	readonly Dictionary<AuraType, AuraEffectHandler> _effectHandlers = new();

	readonly Dictionary<SpellEffectName, SpellEffectHandler> _spellEffectsHandlers = new();

	SpellManager()
	{
		var currentAsm = Assembly.GetExecutingAssembly();

		foreach (var type in currentAsm.GetTypes())
		{
			foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
			{
				foreach (var auraEffect in methodInfo.GetCustomAttributes<AuraEffectHandlerAttribute>())
				{
					if (auraEffect == null)
						continue;

					var parameters = methodInfo.GetParameters();

					if (parameters.Length < 3)
					{
						Log.outError(LogFilter.ServerLoading, "Method: {0} has wrong parameter count: {1} Should be 3. Can't load AuraEffect.", methodInfo.Name, parameters.Length);

						continue;
					}

					if (parameters[0].ParameterType != typeof(AuraApplication) || parameters[1].ParameterType != typeof(AuraEffectHandleModes) || parameters[2].ParameterType != typeof(bool))
					{
						Log.outError(LogFilter.ServerLoading,
									"Method: {0} has wrong parameter Types: ({1}, {2}, {3}) Should be (AuraApplication, AuraEffectHandleModes, Bool). Can't load AuraEffect.",
									methodInfo.Name,
									parameters[0].ParameterType,
									parameters[1].ParameterType,
									parameters[2].ParameterType);

						continue;
					}

					if (_effectHandlers.ContainsKey(auraEffect.AuraType))
					{
						Log.outError(LogFilter.ServerLoading, "Tried to override AuraEffectHandler of {0} with {1} (AuraType {2}).", _effectHandlers[auraEffect.AuraType].GetMethodInfo().Name, methodInfo.Name, auraEffect.AuraType);

						continue;
					}

					_effectHandlers.Add(auraEffect.AuraType, (AuraEffectHandler)methodInfo.CreateDelegate(typeof(AuraEffectHandler)));
				}

				foreach (var spellEffect in methodInfo.GetCustomAttributes<SpellEffectHandlerAttribute>())
				{
					if (spellEffect == null)
						continue;

					if (_spellEffectsHandlers.ContainsKey(spellEffect.EffectName))
					{
						Log.outError(LogFilter.ServerLoading, "Tried to override SpellEffectsHandler of {0} with {1} (EffectName {2}).", _spellEffectsHandlers[spellEffect.EffectName].ToString(), methodInfo.Name, spellEffect.EffectName);

						continue;
					}

					_spellEffectsHandlers.Add(spellEffect.EffectName, (SpellEffectHandler)methodInfo.CreateDelegate(typeof(SpellEffectHandler)));
				}
			}
		}

		if (_spellEffectsHandlers.Count == 0)
		{
			Log.outFatal(LogFilter.ServerLoading, "Could'nt find any SpellEffectHandlers. Dev needs to check this out.");
			Environment.Exit(1);
		}
	}

	public bool IsSpellValid(uint spellId, Player player = null, bool msg = true)
	{
		var spellInfo = GetSpellInfo(spellId, Difficulty.None);

		return IsSpellValid(spellInfo, player, msg);
	}

	public bool IsSpellValid(SpellInfo spellInfo, Player player = null, bool msg = true)
	{
		// not exist
		if (spellInfo == null)
			return false;

		var needCheckReagents = false;

		// check effects
		foreach (var spellEffectInfo in spellInfo.Effects)
			switch (spellEffectInfo.Effect)
			{
				case 0:
					continue;

				// craft spell for crafting non-existed item (break client recipes list show)
				case SpellEffectName.CreateItem:
				case SpellEffectName.CreateLoot:
				{
					if (spellEffectInfo.ItemType == 0)
					{
						// skip auto-loot crafting spells, its not need explicit item info (but have special fake items sometime)
						if (!spellInfo.IsLootCrafting)
						{
							if (msg)
							{
								if (player)
									player.SendSysMessage("Craft spell {0} not have create item entry.", spellInfo.Id);
								else
									Log.outError(LogFilter.Spells, "Craft spell {0} not have create item entry.", spellInfo.Id);
							}

							return false;
						}
					}
					// also possible IsLootCrafting case but fake item must exist anyway
					else if (Global.ObjectMgr.GetItemTemplate(spellEffectInfo.ItemType) == null)
					{
						if (msg)
						{
							if (player)
								player.SendSysMessage("Craft spell {0} create not-exist in DB item (Entry: {1}) and then...", spellInfo.Id, spellEffectInfo.ItemType);
							else
								Log.outError(LogFilter.Spells, "Craft spell {0} create not-exist in DB item (Entry: {1}) and then...", spellInfo.Id, spellEffectInfo.ItemType);
						}

						return false;
					}

					needCheckReagents = true;

					break;
				}
				case SpellEffectName.LearnSpell:
				{
					var spellInfo2 = GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

					if (!IsSpellValid(spellInfo2, player, msg))
					{
						if (msg)
						{
							if (player != null)
								player.SendSysMessage("Spell {0} learn to broken spell {1}, and then...", spellInfo.Id, spellEffectInfo.TriggerSpell);
							else
								Log.outError(LogFilter.Spells, "Spell {0} learn to invalid spell {1}, and then...", spellInfo.Id, spellEffectInfo.TriggerSpell);
						}

						return false;
					}

					break;
				}
			}

		if (needCheckReagents)
			for (var j = 0; j < SpellConst.MaxReagents; ++j)
				if (spellInfo.Reagent[j] > 0 && Global.ObjectMgr.GetItemTemplate((uint)spellInfo.Reagent[j]) == null)
				{
					if (msg)
					{
						if (player != null)
							player.SendSysMessage("Craft spell {0} have not-exist reagent in DB item (Entry: {1}) and then...", spellInfo.Id, spellInfo.Reagent[j]);
						else
							Log.outError(LogFilter.Spells, "Craft spell {0} have not-exist reagent in DB item (Entry: {1}) and then...", spellInfo.Id, spellInfo.Reagent[j]);
					}

					return false;
				}

		return true;
	}

	public SpellChainNode GetSpellChainNode(uint spell_id)
	{
		return _spellChainNodes.LookupByKey(spell_id);
	}

	public uint GetFirstSpellInChain(uint spell_id)
	{
		var node = GetSpellChainNode(spell_id);

		if (node != null)
			return node.First.Id;

		return spell_id;
	}

	public uint GetLastSpellInChain(uint spell_id)
	{
		var node = GetSpellChainNode(spell_id);

		if (node != null)
			return node.Last.Id;

		return spell_id;
	}

	public uint GetNextSpellInChain(uint spell_id)
	{
		var node = GetSpellChainNode(spell_id);

		if (node != null)
			if (node.Next != null)
				return node.Next.Id;

		return 0;
	}

	public uint GetPrevSpellInChain(uint spell_id)
	{
		var node = GetSpellChainNode(spell_id);

		if (node != null)
			if (node.Prev != null)
				return node.Prev.Id;

		return 0;
	}

	public byte GetSpellRank(uint spell_id)
	{
		var node = GetSpellChainNode(spell_id);

		if (node != null)
			return node.Rank;

		return 0;
	}

	public uint GetSpellWithRank(uint spell_id, uint rank, bool strict = false)
	{
		var node = GetSpellChainNode(spell_id);

		if (node != null)
		{
			if (rank != node.Rank)
				return GetSpellWithRank(node.Rank < rank ? node.Next.Id : node.Prev.Id, rank, strict);
		}
		else if (strict && rank > 1)
		{
			return 0;
		}

		return spell_id;
	}

	public List<uint> GetSpellsRequiredForSpellBounds(uint spell_id)
	{
		return _spellReq.LookupByKey(spell_id);
	}

	public List<uint> GetSpellsRequiringSpellBounds(uint spell_id)
	{
		return _spellsReqSpell.LookupByKey(spell_id);
	}

	public bool IsSpellRequiringSpell(uint spellid, uint req_spellid)
	{
		var spellsRequiringSpell = GetSpellsRequiringSpellBounds(req_spellid);

		foreach (var spell in spellsRequiringSpell)
			if (spell == spellid)
				return true;

		return false;
	}

	public SpellLearnSkillNode GetSpellLearnSkill(uint spell_id)
	{
		return _spellLearnSkills.LookupByKey(spell_id);
	}

	public List<SpellLearnSpellNode> GetSpellLearnSpellMapBounds(uint spell_id)
	{
		return _spellLearnSpells.LookupByKey(spell_id);
	}

	public bool IsSpellLearnToSpell(uint spell_id1, uint spell_id2)
	{
		var bounds = GetSpellLearnSpellMapBounds(spell_id1);

		foreach (var bound in bounds)
			if (bound.Spell == spell_id2)
				return true;

		return false;
	}

	public SpellTargetPosition GetSpellTargetPosition(uint spell_id, int effIndex)
	{
		return _spellTargetPositions.LookupByKey(new KeyValuePair<uint, int>(spell_id, effIndex));
	}

	public List<SpellGroup> GetSpellSpellGroupMapBounds(uint spell_id)
	{
		return _spellSpellGroup.LookupByKey(GetFirstSpellInChain(spell_id));
	}

	public bool IsSpellMemberOfSpellGroup(uint spellid, SpellGroup groupid)
	{
		var spellGroup = GetSpellSpellGroupMapBounds(spellid);

		foreach (var group in spellGroup)
			if (group == groupid)
				return true;

		return false;
	}

	public void GetSetOfSpellsInSpellGroup(SpellGroup group_id, out List<int> foundSpells)
	{
		List<SpellGroup> usedGroups = new();
		GetSetOfSpellsInSpellGroup(group_id, out foundSpells, ref usedGroups);
	}

	public bool AddSameEffectStackRuleSpellGroups(SpellInfo spellInfo, AuraType auraType, double amount, Dictionary<SpellGroup, double> groups)
	{
		var spellId = spellInfo.GetFirstRankSpell().Id;
		var spellGroupList = GetSpellSpellGroupMapBounds(spellId);

		// Find group with SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT if it belongs to one
		foreach (var group in spellGroupList)
		{
			var found = _spellSameEffectStack.LookupByKey(group);

			if (found != null)
			{
				// check auraTypes
				if (!found.Any(p => p == auraType))
					continue;

				// Put the highest amount in the map
				if (!groups.ContainsKey(group))
				{
					groups.Add(group, amount);
				}
				else
				{
					var curr_amount = groups[group];

					// Take absolute value because this also counts for the highest negative aura
					if (Math.Abs(curr_amount) < Math.Abs(amount))
						groups[group] = amount;
				}

				// return because a spell should be in only one SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT group per auraType
				return true;
			}
		}

		// Not in a SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT group, so return false
		return false;
	}

	public SpellGroupStackRule CheckSpellGroupStackRules(SpellInfo spellInfo1, SpellInfo spellInfo2)
	{
		var spellid_1 = spellInfo1.GetFirstRankSpell().Id;
		var spellid_2 = spellInfo2.GetFirstRankSpell().Id;

		// find SpellGroups which are common for both spells
		var spellGroup1 = GetSpellSpellGroupMapBounds(spellid_1);
		List<SpellGroup> groups = new();

		foreach (var group in spellGroup1)
			if (IsSpellMemberOfSpellGroup(spellid_2, group))
			{
				var add = true;
				var groupSpell = GetSpellGroupSpellMapBounds(group);

				foreach (var group2 in groupSpell)
					if (group2 < 0)
					{
						var currGroup = (SpellGroup)Math.Abs(group2);

						if (IsSpellMemberOfSpellGroup(spellid_1, currGroup) && IsSpellMemberOfSpellGroup(spellid_2, currGroup))
						{
							add = false;

							break;
						}
					}

				if (add)
					groups.Add(group);
			}

		var rule = SpellGroupStackRule.Default;

		foreach (var group in groups)
		{
			var found = _spellGroupStack.LookupByKey(group);

			if (found != 0)
				rule = found;

			if (rule != 0)
				break;
		}

		return rule;
	}

	public SpellGroupStackRule GetSpellGroupStackRule(SpellGroup group)
	{
		if (_spellGroupStack.ContainsKey(group))
			return _spellGroupStack.LookupByKey(group);

		return SpellGroupStackRule.Default;
	}

	public SpellProcEntry GetSpellProcEntry(SpellInfo spellInfo)
	{
		if (_spellProcMap.TryGetValue(spellInfo.Id, out var diffdict) && diffdict.TryGetValue(spellInfo.Difficulty, out var procEntry))
			return procEntry;

		var difficulty = CliDB.DifficultyStorage.LookupByKey(spellInfo.Difficulty);

		if (difficulty != null && diffdict != null)
			do
			{
				if (diffdict.TryGetValue((Difficulty)difficulty.FallbackDifficultyID, out procEntry))
					return procEntry;

				difficulty = CliDB.DifficultyStorage.LookupByKey(difficulty.FallbackDifficultyID);
			} while (difficulty != null);

		return null;
	}

	public static bool CanSpellTriggerProcOnEvent(SpellProcEntry procEntry, ProcEventInfo eventInfo)
	{
		// proc type doesn't match
		if (!(eventInfo.TypeMask & procEntry.ProcFlags))
			return false;

		// check XP or honor target requirement
		if (((uint)procEntry.AttributesMask & 0x0000001) != 0)
		{
			var actor = eventInfo.Actor.AsPlayer;

			if (actor)
				if (eventInfo.ActionTarget && !actor.IsHonorOrXPTarget(eventInfo.ActionTarget))
					return false;
		}

		// check power requirement
		if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReqPowerCost))
		{
			if (!eventInfo.ProcSpell)
				return false;

			var costs = eventInfo.ProcSpell.PowerCost;
			var m = costs.Find(cost => cost.Amount > 0);

			if (m == null)
				return false;
		}

		// always trigger for these types
		if (eventInfo.TypeMask.HasFlag(ProcFlags.Heartbeat | ProcFlags.Kill | ProcFlags.Death))
			return true;

		// check school mask (if set) for other trigger types
		if (procEntry.SchoolMask != 0 && !Convert.ToBoolean(eventInfo.SchoolMask & procEntry.SchoolMask))
			return false;

		// check spell family name/flags (if set) for spells
		if (eventInfo.TypeMask.HasFlag(ProcFlags.SpellMask))
		{
			var eventSpellInfo = eventInfo.SpellInfo;

			if (eventSpellInfo != null)
				if (!eventSpellInfo.IsAffected(procEntry.SpellFamilyName, procEntry.SpellFamilyMask))
					return false;

			// check spell type mask (if set)
			if (procEntry.SpellTypeMask != 0 && !Convert.ToBoolean(eventInfo.SpellTypeMask & procEntry.SpellTypeMask))
				return false;
		}

		// check spell phase mask
		if (eventInfo.TypeMask.HasFlag(ProcFlags.ReqSpellPhaseMask))
			if (!Convert.ToBoolean(eventInfo.SpellPhaseMask & procEntry.SpellPhaseMask))
				return false;

		// check hit mask (on taken hit or on done hit, but not on spell cast phase)
		if (eventInfo.TypeMask.HasFlag(ProcFlags.TakenHitMask) || (eventInfo.TypeMask.HasFlag(ProcFlags.DoneHitMask) && !Convert.ToBoolean(eventInfo.SpellPhaseMask & ProcFlagsSpellPhase.Cast)))
		{
			var hitMask = procEntry.HitMask;

			// get default values if hit mask not set
			if (hitMask == 0)
			{
				// for taken procs allow normal + critical hits by default
				if (eventInfo.TypeMask.HasFlag(ProcFlags.TakenHitMask))
					hitMask |= ProcFlagsHit.Normal | ProcFlagsHit.Critical;
				// for done procs allow normal + critical + absorbs by default
				else
					hitMask |= ProcFlagsHit.Normal | ProcFlagsHit.Critical | ProcFlagsHit.Absorb;
			}

			if (!Convert.ToBoolean(eventInfo.HitMask & hitMask))
				return false;
		}

		return true;
	}

	public SpellThreatEntry GetSpellThreatEntry(uint spellID)
	{
		var spellthreat = _spellThreatMap.LookupByKey(spellID);

		if (spellthreat != null)
		{
			return spellthreat;
		}
		else
		{
			var firstSpell = GetFirstSpellInChain(spellID);

			return _spellThreatMap.LookupByKey(firstSpell);
		}
	}

	public List<SkillLineAbilityRecord> GetSkillLineAbilityMapBounds(uint spell_id)
	{
		return _skillLineAbilityMap.LookupByKey(spell_id);
	}

	public PetAura GetPetAura(uint spell_id, byte eff)
	{
		if (_spellPetAuraMap.ContainsKey(spell_id))
			return _spellPetAuraMap[spell_id].LookupByKey(eff);

		return null;
	}

	public Dictionary<int, PetAura> GetPetAuras(uint spell_id)
	{
		if (_spellPetAuraMap.TryGetValue(spell_id, out var auras))
			return auras;

		return DefaultPetAuras;
	}

	public SpellEnchantProcEntry GetSpellEnchantProcEvent(uint enchId)
	{
		return _spellEnchantProcEventMap.LookupByKey(enchId);
	}

	public bool IsArenaAllowedEnchancment(uint ench_id)
	{
		var enchantment = CliDB.SpellItemEnchantmentStorage.LookupByKey(ench_id);

		if (enchantment != null)
			return enchantment.GetFlags().HasFlag(SpellItemEnchantmentFlags.AllowEnteringArena);

		return false;
	}

	public List<int> GetSpellLinked(SpellLinkedType type, uint spellId)
	{
		return _spellLinkedMap.LookupByKey((type, spellId));
	}

	public MultiMap<uint, uint> GetPetLevelupSpellList(CreatureFamily petFamily)
	{
		return _petLevelupSpellMap.LookupByKey(petFamily);
	}

	public PetDefaultSpellsEntry GetPetDefaultSpellsEntry(int id)
	{
		return _petDefaultSpellsEntries.LookupByKey(id);
	}

	public List<SpellArea> GetSpellAreaMapBounds(uint spell_id)
	{
		return _spellAreaMap.LookupByKey(spell_id);
	}

	public List<SpellArea> GetSpellAreaForQuestMapBounds(uint quest_id)
	{
		return _spellAreaForQuestMap.LookupByKey(quest_id);
	}

	public List<SpellArea> GetSpellAreaForQuestEndMapBounds(uint quest_id)
	{
		return _spellAreaForQuestEndMap.LookupByKey(quest_id);
	}

	public List<SpellArea> GetSpellAreaForAuraMapBounds(uint spell_id)
	{
		return _spellAreaForAuraMap.LookupByKey(spell_id);
	}

	public List<SpellArea> GetSpellAreaForAreaMapBounds(uint area_id)
	{
		return _spellAreaForAreaMap.LookupByKey(area_id);
	}

	public SpellInfo GetSpellInfo<T>(T spellId, Difficulty difficulty = Difficulty.None) where T : struct, Enum
	{
		return GetSpellInfo(Convert.ToUInt32(spellId), difficulty);
	}

	public bool TryGetSpellInfo<T>(T spellId, out SpellInfo spellInfo) where T : struct, Enum
	{
		spellInfo = GetSpellInfo(spellId);

		return spellInfo != null;
	}

	public bool TryGetSpellInfo<T>(T spellId, Difficulty difficulty, out SpellInfo spellInfo) where T : struct, Enum
	{
		spellInfo = GetSpellInfo(spellId, difficulty);

		return spellInfo != null;
	}

	public bool TryGetSpellInfo(uint spellId, out SpellInfo spellInfo)
	{
		spellInfo = GetSpellInfo(spellId);

		return spellInfo != null;
	}

	public bool TryGetSpellInfo(uint spellId, Difficulty difficulty, out SpellInfo spellInfo)
	{
		spellInfo = GetSpellInfo(spellId, difficulty);

		return spellInfo != null;
	}

	public SpellInfo GetSpellInfo(uint spellId, Difficulty difficulty = Difficulty.None)
	{
		if (_spellInfoMap.TryGetValue(spellId, out var diffDict) && diffDict.TryGetValue(difficulty, out var spellInfo))
			return spellInfo;

		if (diffDict != null)
		{
			var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);

			if (difficultyEntry != null)
				do
				{
					if (diffDict.TryGetValue((Difficulty)difficultyEntry.FallbackDifficultyID, out spellInfo))
						return spellInfo;

					difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
				} while (difficultyEntry != null);
		}

		return null;
	}

	public SpellInfo AssertSpellInfo(uint spellId, Difficulty difficulty)
	{
		var spellInfo = GetSpellInfo(spellId, difficulty);

		return spellInfo;
	}

	public void ForEachSpellInfo(Action<SpellInfo> callback)
	{
		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
				callback(spellInfo);
	}

	public void ForEachSpellInfoDifficulty(uint spellId, Action<SpellInfo> callback)
	{
		foreach (var spellInfo in _GetSpellInfo(spellId).Values)
			callback(spellInfo);
	}

	// SpellInfo object management
	public bool HasSpellInfo(uint spellId, Difficulty difficulty = Difficulty.None)
	{
		return GetSpellInfo(spellId, difficulty) != null;
	}

	//Extra Shit
	public SpellEffectHandler GetSpellEffectHandler(SpellEffectName eff)
	{
		if (!_spellEffectsHandlers.TryGetValue(eff, out var eh))
		{
			Log.outError(LogFilter.Spells, "No defined handler for SpellEffect {0}", eff);

			return _spellEffectsHandlers[SpellEffectName.None];
		}

		return eh;
	}

	public AuraEffectHandler GetAuraEffectHandler(AuraType type)
	{
		if (!_effectHandlers.TryGetValue(type, out var eh))
		{
			Log.outError(LogFilter.Spells, "No defined handler for AuraEffect {0}", type);

			return _effectHandlers[AuraType.None];
		}

		return eh;
	}

	public SkillRangeType GetSkillRangeType(SkillRaceClassInfoRecord rcEntry)
	{
		var skill = CliDB.SkillLineStorage.LookupByKey(rcEntry.SkillID);

		if (skill == null)
			return SkillRangeType.None;

		if (Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID) != null)
			return SkillRangeType.Rank;

		if (rcEntry.SkillID == (uint)SkillType.Runeforging)
			return SkillRangeType.Mono;

		switch (skill.CategoryID)
		{
			case SkillCategory.Armor:
				return SkillRangeType.Mono;
			case SkillCategory.Languages:
				return SkillRangeType.Language;
		}

		return SkillRangeType.Level;
	}

	public bool IsPrimaryProfessionSkill(uint skill)
	{
		var pSkill = CliDB.SkillLineStorage.LookupByKey(skill);

		return pSkill != null && pSkill.CategoryID == SkillCategory.Profession && pSkill.ParentSkillLineID == 0;
	}

	public bool IsWeaponSkill(uint skill)
	{
		var pSkill = CliDB.SkillLineStorage.LookupByKey(skill);

		return pSkill != null && pSkill.CategoryID == SkillCategory.Weapon;
	}

	public bool IsProfessionOrRidingSkill(uint skill)
	{
		return IsProfessionSkill(skill) || skill == (uint)SkillType.Riding;
	}

	public bool IsProfessionSkill(uint skill)
	{
		return IsPrimaryProfessionSkill(skill) || skill == (uint)SkillType.Fishing || skill == (uint)SkillType.Cooking;
	}

	public bool IsPartOfSkillLine(SkillType skillId, uint spellId)
	{
		var skillBounds = GetSkillLineAbilityMapBounds(spellId);

		if (skillBounds != null)
			foreach (var skill in skillBounds)
				if (skill.SkillLine == (uint)skillId)
					return true;

		return false;
	}

	public SpellSchools GetFirstSchoolInMask(SpellSchoolMask mask)
	{
		for (var i = 0; i < (int)SpellSchools.Max; ++i)
			if (Convert.ToBoolean((int)mask & (1 << i)))
				return (SpellSchools)i;

		return SpellSchools.Normal;
	}

	public uint GetModelForTotem(uint spellId, Race race)
	{
		return _spellTotemModel.LookupByKey(Tuple.Create(spellId, (byte)race));
	}

	bool IsSpellLearnSpell(uint spell_id)
	{
		return _spellLearnSpells.ContainsKey(spell_id);
	}

	List<int> GetSpellGroupSpellMapBounds(SpellGroup group_id)
	{
		return _spellGroupSpell.LookupByKey(group_id);
	}

	void GetSetOfSpellsInSpellGroup(SpellGroup group_id, out List<int> foundSpells, ref List<SpellGroup> usedGroups)
	{
		foundSpells = new List<int>();

		if (usedGroups.Find(p => p == group_id) == 0)
			return;

		usedGroups.Add(group_id);

		var groupSpell = GetSpellGroupSpellMapBounds(group_id);

		foreach (var group in groupSpell)
			if (group < 0)
			{
				var currGroup = (SpellGroup)Math.Abs(group);
				GetSetOfSpellsInSpellGroup(currGroup, out foundSpells, ref usedGroups);
			}
			else
			{
				foundSpells.Add(group);
			}
	}

	Dictionary<Difficulty, SpellInfo> _GetSpellInfo(uint spellId)
	{
		if (_spellInfoMap.TryGetValue(spellId, out var diffDict))
			return diffDict;

		return _emptyDiffDict;
	}

	void UnloadSpellInfoChains()
	{
		foreach (var pair in _spellChainNodes)
			foreach (var spellInfo in _GetSpellInfo(pair.Key).Values)
				spellInfo.ChainEntry = null;

		_spellChainNodes.Clear();
	}

	bool IsTriggerAura(AuraType type)
	{
		switch (type)
		{
			case AuraType.Dummy:
			case AuraType.PeriodicDummy:
			case AuraType.ModConfuse:
			case AuraType.ModThreat:
			case AuraType.ModStun:
			case AuraType.ModDamageDone:
			case AuraType.ModDamageTaken:
			case AuraType.ModResistance:
			case AuraType.ModStealth:
			case AuraType.ModFear:
			case AuraType.ModRoot:
			case AuraType.Transform:
			case AuraType.ReflectSpells:
			case AuraType.DamageImmunity:
			case AuraType.ProcTriggerSpell:
			case AuraType.ProcTriggerDamage:
			case AuraType.ModCastingSpeedNotStack:
			case AuraType.SchoolAbsorb:
			case AuraType.ModPowerCostSchoolPct:
			case AuraType.ModPowerCostSchool:
			case AuraType.ReflectSpellsSchool:
			case AuraType.MechanicImmunity:
			case AuraType.ModDamagePercentTaken:
			case AuraType.SpellMagnet:
			case AuraType.ModAttackPower:
			case AuraType.ModPowerRegenPercent:
			case AuraType.InterceptMeleeRangedAttacks:
			case AuraType.OverrideClassScripts:
			case AuraType.ModMechanicResistance:
			case AuraType.MeleeAttackPowerAttackerBonus:
			case AuraType.ModMeleeHaste:
			case AuraType.ModMeleeHaste3:
			case AuraType.ModAttackerMeleeHitChance:
			case AuraType.ProcTriggerSpellWithValue:
			case AuraType.ModSchoolMaskDamageFromCaster:
			case AuraType.ModSpellDamageFromCaster:
			case AuraType.AbilityIgnoreAurastate:
			case AuraType.ModInvisibility:
			case AuraType.ForceReaction:
			case AuraType.ModTaunt:
			case AuraType.ModDetaunt:
			case AuraType.ModDamagePercentDone:
			case AuraType.ModAttackPowerPct:
			case AuraType.ModHitChance:
			case AuraType.ModWeaponCritPercent:
			case AuraType.ModBlockPercent:
			case AuraType.ModRoot2:
				return true;
		}

		return false;
	}

	bool IsAlwaysTriggeredAura(AuraType type)
	{
		switch (type)
		{
			case AuraType.OverrideClassScripts:
			case AuraType.ModStealth:
			case AuraType.ModConfuse:
			case AuraType.ModFear:
			case AuraType.ModRoot:
			case AuraType.ModStun:
			case AuraType.Transform:
			case AuraType.ModInvisibility:
			case AuraType.SpellMagnet:
			case AuraType.SchoolAbsorb:
			case AuraType.ModRoot2:
				return true;
		}

		return false;
	}

	ProcFlagsSpellType GetSpellTypeMask(AuraType type)
	{
		switch (type)
		{
			case AuraType.ModStealth:
				return ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
			case AuraType.ModConfuse:
			case AuraType.ModFear:
			case AuraType.ModRoot:
			case AuraType.ModRoot2:
			case AuraType.ModStun:
			case AuraType.Transform:
			case AuraType.ModInvisibility:
				return ProcFlagsSpellType.Damage;
			default:
				return ProcFlagsSpellType.MaskAll;
		}
	}

	void AddSpellInfo(SpellInfo spellInfo)
	{
		if (!_spellInfoMap.TryGetValue(spellInfo.Id, out var diffDict))
		{
			diffDict = new Dictionary<Difficulty, SpellInfo>();
			_spellInfoMap[spellInfo.Id] = diffDict;
		}

		diffDict[spellInfo.Difficulty] = spellInfo;
	}

	#region Loads

	public void LoadSpellRanks()
	{
		var oldMSTime = Time.MSTime;

		Dictionary<uint /*spell*/, uint /*next*/> chains = new();
		List<uint> hasPrev = new();

		foreach (var skillAbility in CliDB.SkillLineAbilityStorage.Values)
		{
			if (skillAbility.SupercedesSpell == 0)
				continue;

			if (!HasSpellInfo(skillAbility.SupercedesSpell, Difficulty.None) || !HasSpellInfo(skillAbility.Spell, Difficulty.None))
				continue;

			chains[skillAbility.SupercedesSpell] = skillAbility.Spell;
			hasPrev.Add(skillAbility.Spell);
		}

		// each key in chains that isn't present in hasPrev is a first rank
		foreach (var pair in chains)
		{
			if (hasPrev.Contains(pair.Key))
				continue;

			var first = GetSpellInfo(pair.Key, Difficulty.None);
			var next = GetSpellInfo(pair.Value, Difficulty.None);

			if (!_spellChainNodes.ContainsKey(pair.Key))
				_spellChainNodes[pair.Key] = new SpellChainNode();

			_spellChainNodes[pair.Key].First = first;
			_spellChainNodes[pair.Key].Prev = null;
			_spellChainNodes[pair.Key].Next = next;
			_spellChainNodes[pair.Key].Last = next;
			_spellChainNodes[pair.Key].Rank = 1;

			foreach (var difficultyInfo in _GetSpellInfo(pair.Key).Values)
				difficultyInfo.ChainEntry = _spellChainNodes[pair.Key];

			if (!_spellChainNodes.ContainsKey(pair.Value))
				_spellChainNodes[pair.Value] = new SpellChainNode();

			_spellChainNodes[pair.Value].First = first;
			_spellChainNodes[pair.Value].Prev = first;
			_spellChainNodes[pair.Value].Next = null;
			_spellChainNodes[pair.Value].Last = next;
			_spellChainNodes[pair.Value].Rank = 2;

			foreach (var difficultyInfo in _GetSpellInfo(pair.Value).Values)
				difficultyInfo.ChainEntry = _spellChainNodes[pair.Value];

			byte rank = 3;
			var nextPair = chains.Find(pair.Value);

			while (nextPair.Key != 0)
			{
				var prev = GetSpellInfo(nextPair.Key, Difficulty.None); // already checked in previous iteration (or above, in case this is the first one)
				var last = GetSpellInfo(nextPair.Value, Difficulty.None);

				if (last == null)
					break;

				if (!_spellChainNodes.ContainsKey(nextPair.Key))
					_spellChainNodes[nextPair.Key] = new SpellChainNode();

				_spellChainNodes[nextPair.Key].Next = last;

				if (!_spellChainNodes.ContainsKey(nextPair.Value))
					_spellChainNodes[nextPair.Value] = new SpellChainNode();

				_spellChainNodes[nextPair.Value].First = first;
				_spellChainNodes[nextPair.Value].Prev = prev;
				_spellChainNodes[nextPair.Value].Next = null;
				_spellChainNodes[nextPair.Value].Last = last;
				_spellChainNodes[nextPair.Value].Rank = rank++;

				foreach (var difficultyInfo in _GetSpellInfo(nextPair.Value).Values)
					difficultyInfo.ChainEntry = _spellChainNodes[nextPair.Value];

				// fill 'last'
				do
				{
					_spellChainNodes[prev.Id].Last = last;
					prev = _spellChainNodes[prev.Id].Prev;
				} while (prev != null);

				nextPair = chains.Find(nextPair.Value);
			}
		}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell rank records in {1}ms", _spellChainNodes.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellRequired()
	{
		var oldMSTime = Time.MSTime;

		_spellsReqSpell.Clear(); // need for reload case
		_spellReq.Clear();       // need for reload case

		//                                                   0        1
		var result = DB.World.Query("SELECT spell_id, req_spell from spell_required");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell required records. DB table `spell_required` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var spell_id = result.Read<uint>(0);
			var spell_req = result.Read<uint>(1);

			// check if chain is made with valid first spell
			var spell = GetSpellInfo(spell_id, Difficulty.None);

			if (spell == null)
			{
				Log.outError(LogFilter.Sql, "spell_id {0} in `spell_required` table is not found in dbcs, skipped", spell_id);

				continue;
			}

			var req_spell = GetSpellInfo(spell_req, Difficulty.None);

			if (req_spell == null)
			{
				Log.outError(LogFilter.Sql, "req_spell {0} in `spell_required` table is not found in dbcs, skipped", spell_req);

				continue;
			}

			if (spell.IsRankOf(req_spell))
			{
				Log.outError(LogFilter.Sql, "req_spell {0} and spell_id {1} in `spell_required` table are ranks of the same spell, entry not needed, skipped", spell_req, spell_id);

				continue;
			}

			if (IsSpellRequiringSpell(spell_id, spell_req))
			{
				Log.outError(LogFilter.Sql, "duplicated entry of req_spell {0} and spell_id {1} in `spell_required`, skipped", spell_req, spell_id);

				continue;
			}

			_spellReq.Add(spell_id, spell_req);
			_spellsReqSpell.Add(spell_req, spell_id);
			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell required records in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellLearnSkills()
	{
		_spellLearnSkills.Clear();

		// search auto-learned skills and add its to map also for use in unlearn spells/talents
		uint dbc_count = 0;

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var entry in kvp.Values)
			{
				if (entry.Difficulty != Difficulty.None)
					continue;

				foreach (var spellEffectInfo in entry.Effects)
				{
					SpellLearnSkillNode dbc_node = new();

					switch (spellEffectInfo.Effect)
					{
						case SpellEffectName.Skill:
							dbc_node.Skill = (SkillType)spellEffectInfo.MiscValue;
							dbc_node.Step = (ushort)spellEffectInfo.CalcValue();

							if (dbc_node.Skill != SkillType.Riding)
								dbc_node.Value = 1;
							else
								dbc_node.Value = (ushort)(dbc_node.Step * 75);

							dbc_node.Maxvalue = (ushort)(dbc_node.Step * 75);

							break;
						case SpellEffectName.DualWield:
							dbc_node.Skill = SkillType.DualWield;
							dbc_node.Step = 1;
							dbc_node.Value = 1;
							dbc_node.Maxvalue = 1;

							break;
						default:
							continue;
					}

					_spellLearnSkills.Add(entry.Id, dbc_node);
					++dbc_count;

					break;
				}
			}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Spell Learn Skills from DBC", dbc_count);
	}

	public void LoadSpellLearnSpells()
	{
		var oldMSTime = Time.MSTime;

		_spellLearnSpells.Clear();

		//                                         0      1        2
		var result = DB.World.Query("SELECT entry, SpellID, Active FROM spell_learn_spell");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell learn spells. DB table `spell_learn_spell` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var spell_id = result.Read<uint>(0);

			var node = new SpellLearnSpellNode();
			node.Spell = result.Read<uint>(1);
			node.OverridesSpell = 0;
			node.Active = result.Read<bool>(2);
			node.AutoLearned = false;

			var spellInfo = GetSpellInfo(spell_id, Difficulty.None);

			if (spellInfo == null)
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_learn_spell` does not exist", spell_id);

				continue;
			}

			if (!HasSpellInfo(node.Spell, Difficulty.None))
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_learn_spell` learning not existed spell {1}", spell_id, node.Spell);

				continue;
			}

			if (spellInfo.HasAttribute(SpellCustomAttributes.IsTalent))
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_learn_spell` attempt learning talent spell {1}, skipped", spell_id, node.Spell);

				continue;
			}

			_spellLearnSpells.Add(spell_id, node);
			++count;
		} while (result.NextRow());

		// search auto-learned spells and add its to map also for use in unlearn spells/talents
		uint dbc_count = 0;

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var entry in kvp.Values)
			{
				if (entry.Difficulty != Difficulty.None)
					continue;

				foreach (var spellEffectInfo in entry.Effects)
					if (spellEffectInfo.Effect == SpellEffectName.LearnSpell)
					{
						var dbc_node = new SpellLearnSpellNode();
						dbc_node.Spell = spellEffectInfo.TriggerSpell;
						dbc_node.Active = true; // all dbc based learned spells is active (show in spell book or hide by client itself)
						dbc_node.OverridesSpell = 0;

						// ignore learning not existed spells (broken/outdated/or generic learnig spell 483
						if (GetSpellInfo(dbc_node.Spell, Difficulty.None) == null)
							continue;

						// talent or passive spells or skill-step spells auto-cast and not need dependent learning,
						// pet teaching spells must not be dependent learning (cast)
						// other required explicit dependent learning
						dbc_node.AutoLearned = spellEffectInfo.TargetA.Target == Targets.UnitPet || entry.HasAttribute(SpellCustomAttributes.IsTalent) || entry.IsPassive || entry.HasEffect(SpellEffectName.SkillStep);

						var db_node_bounds = GetSpellLearnSpellMapBounds(entry.Id);

						var found = false;

						foreach (var bound in db_node_bounds)
							if (bound.Spell == dbc_node.Spell)
							{
								Log.outError(LogFilter.Sql,
											"Spell {0} auto-learn spell {1} in spell.dbc then the record in `spell_learn_spell` is redundant, please fix DB.",
											entry.Id,
											dbc_node.Spell);

								found = true;

								break;
							}

						if (!found) // add new spell-spell pair if not found
						{
							_spellLearnSpells.Add(entry.Id, dbc_node);
							++dbc_count;
						}
					}
			}

		foreach (var spellLearnSpell in CliDB.SpellLearnSpellStorage.Values)
		{
			if (!HasSpellInfo(spellLearnSpell.SpellID, Difficulty.None) || !HasSpellInfo(spellLearnSpell.LearnSpellID, Difficulty.None))
				continue;

			var db_node_bounds = _spellLearnSpells.LookupByKey(spellLearnSpell.SpellID);
			var found = false;

			foreach (var spellNode in db_node_bounds)
				if (spellNode.Spell == spellLearnSpell.LearnSpellID)
				{
					Log.outError(LogFilter.Sql, $"Found redundant record (entry: {spellLearnSpell.SpellID}, SpellID: {spellLearnSpell.LearnSpellID}) in `spell_learn_spell`, spell added automatically from SpellLearnSpell.db2");
					found = true;

					break;
				}

			if (found)
				continue;

			// Check if it is already found in Spell.dbc, ignore silently if yes
			var dbc_node_bounds = GetSpellLearnSpellMapBounds(spellLearnSpell.SpellID);
			found = false;

			foreach (var spellNode in dbc_node_bounds)
				if (spellNode.Spell == spellLearnSpell.LearnSpellID)
				{
					found = true;

					break;
				}

			if (found)
				continue;

			SpellLearnSpellNode dbcLearnNode = new();
			dbcLearnNode.Spell = spellLearnSpell.LearnSpellID;
			dbcLearnNode.OverridesSpell = spellLearnSpell.OverridesSpellID;
			dbcLearnNode.Active = true;
			dbcLearnNode.AutoLearned = false;

			_spellLearnSpells.Add(spellLearnSpell.SpellID, dbcLearnNode);
			++dbc_count;
		}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell learn spells, {1} found in Spell.dbc in {2} ms", count, dbc_count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellTargetPositions()
	{
		var oldMSTime = Time.MSTime;

		_spellTargetPositions.Clear(); // need for reload case

		//                                         0   1         2           3                  4                  5
		var result = DB.World.Query("SELECT ID, EffectIndex, MapID, PositionX, PositionY, PositionZ FROM spell_target_position");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell target coordinates. DB table `spell_target_position` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var spellId = result.Read<uint>(0);
			int effIndex = result.Read<byte>(1);

			SpellTargetPosition st = new();
			st.TargetMapId = result.Read<uint>(2);
			st.X = result.Read<float>(3);
			st.Y = result.Read<float>(4);
			st.Z = result.Read<float>(5);

			var mapEntry = CliDB.MapStorage.LookupByKey(st.TargetMapId);

			if (mapEntry == null)
			{
				Log.outError(LogFilter.Sql, "Spell (ID: {0}, EffectIndex: {1}) is using a non-existant MapID (ID: {2})", spellId, effIndex, st.TargetMapId);

				continue;
			}

			if (st.X == 0 && st.Y == 0 && st.Z == 0)
			{
				Log.outError(LogFilter.Sql, "Spell (ID: {0}, EffectIndex: {1}) target coordinates not provided.", spellId, effIndex);

				continue;
			}

			var spellInfo = GetSpellInfo(spellId, Difficulty.None);

			if (spellInfo == null)
			{
				Log.outError(LogFilter.Sql, "Spell (ID: {0}) listed in `spell_target_position` does not exist.", spellId);

				continue;
			}

			if (effIndex >= spellInfo.Effects.Count)
			{
				Log.outError(LogFilter.Sql, "Spell (Id: {0}, effIndex: {1}) listed in `spell_target_position` does not have an effect at index {2}.", spellId, effIndex, effIndex);

				continue;
			}

			// target facing is in degrees for 6484 & 9268... (blizz sucks)
			if (spellInfo.GetEffect(effIndex).PositionFacing > 2 * Math.PI)
				st.Orientation = spellInfo.GetEffect(effIndex).PositionFacing * (float)Math.PI / 180;
			else
				st.Orientation = spellInfo.GetEffect(effIndex).PositionFacing;

			if (spellInfo.GetEffect(effIndex).TargetA.Target == Targets.DestDb || spellInfo.GetEffect(effIndex).TargetB.Target == Targets.DestDb)
			{
				var key = new KeyValuePair<uint, int>(spellId, effIndex);
				_spellTargetPositions[key] = st;
				++count;
			}
			else
			{
				Log.outError(LogFilter.Sql, "Spell (Id: {0}, effIndex: {1}) listed in `spell_target_position` does not have target TARGET_DEST_DB (17).", spellId, effIndex);

				continue;
			}
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell teleport coordinates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellGroups()
	{
		var oldMSTime = Time.MSTime;

		_spellSpellGroup.Clear(); // need for reload case
		_spellGroupSpell.Clear();

		//                                                0     1
		var result = DB.World.Query("SELECT id, spell_id FROM spell_group");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell group definitions. DB table `spell_group` is empty.");

			return;
		}

		List<uint> groups = new();
		uint count = 0;

		do
		{
			var group_id = result.Read<uint>(0);

			if (group_id <= 1000 && group_id >= (uint)SpellGroup.CoreRangeMax)
			{
				Log.outError(LogFilter.Sql, "SpellGroup id {0} listed in `spell_group` is in core range, but is not defined in core!", group_id);

				continue;
			}

			var spell_id = result.Read<int>(1);

			groups.Add(group_id);
			_spellGroupSpell.Add((SpellGroup)group_id, spell_id);
		} while (result.NextRow());

		_spellGroupSpell.RemoveIfMatching((group) =>
		{
			if (group.Value < 0)
			{
				if (!groups.Contains((uint)Math.Abs(group.Value)))
				{
					Log.outError(LogFilter.Sql, "SpellGroup id {0} listed in `spell_group` does not exist", Math.Abs(group.Value));

					return true;
				}
			}
			else
			{
				var spellInfo = GetSpellInfo((uint)group.Value, Difficulty.None);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_group` does not exist", group.Value);

					return true;
				}
				else if (spellInfo.Rank > 1)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_group` is not first rank of spell", group.Value);

					return true;
				}
			}

			return false;
		});

		foreach (var group in groups)
		{
			GetSetOfSpellsInSpellGroup((SpellGroup)group, out var spells);

			foreach (var spell in spells)
			{
				++count;
				_spellSpellGroup.Add((uint)spell, (SpellGroup)group);
			}
		}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell group definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellGroupStackRules()
	{
		var oldMSTime = Time.MSTime;

		_spellGroupStack.Clear(); // need for reload case
		_spellSameEffectStack.Clear();

		List<SpellGroup> sameEffectGroups = new();

		//                                         0         1
		var result = DB.World.Query("SELECT group_id, stack_rule FROM spell_group_stack_rules");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell group stack rules. DB table `spell_group_stack_rules` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var group_id = (SpellGroup)result.Read<uint>(0);
			var stack_rule = (SpellGroupStackRule)result.Read<byte>(1);

			if (stack_rule >= SpellGroupStackRule.Max)
			{
				Log.outError(LogFilter.Sql, "SpellGroupStackRule {0} listed in `spell_group_stack_rules` does not exist", stack_rule);

				continue;
			}

			var spellGroup = GetSpellGroupSpellMapBounds(group_id);

			if (spellGroup == null)
			{
				Log.outError(LogFilter.Sql, "SpellGroup id {0} listed in `spell_group_stack_rules` does not exist", group_id);

				continue;
			}

			_spellGroupStack.Add(group_id, stack_rule);

			// different container for same effect stack rules, need to check effect types
			if (stack_rule == SpellGroupStackRule.ExclusiveSameEffect)
				sameEffectGroups.Add(group_id);

			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell group stack rules in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

		count = 0;
		oldMSTime = Time.MSTime;
		Log.outInfo(LogFilter.ServerLoading, "Parsing SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT stack rules...");

		foreach (var group_id in sameEffectGroups)
		{
			GetSetOfSpellsInSpellGroup(group_id, out var spellIds);

			List<AuraType> auraTypes = new();

			// we have to 'guess' what effect this group corresponds to
			{
				List<AuraType> frequencyContainer = new();

				// only waylay for the moment (shared group)
				AuraType[] SubGroups =
				{
					AuraType.ModMeleeHaste, AuraType.ModMeleeRangedHaste, AuraType.ModRangedHaste
				};

				foreach (uint spellId in spellIds)
				{
					var spellInfo = GetSpellInfo(spellId, Difficulty.None);

					foreach (var spellEffectInfo in spellInfo.Effects)
					{
						if (!spellEffectInfo.IsAura())
							continue;

						var auraName = spellEffectInfo.ApplyAuraName;

						if (SubGroups.Contains(auraName))
							// count as first aura
							auraName = SubGroups[0];

						frequencyContainer.Add(auraName);
					}
				}

				AuraType auraType = 0;
				var auraTypeCount = 0;

				foreach (var auraName in frequencyContainer)
				{
					var currentCount = frequencyContainer.Count(p => p == auraName);

					if (currentCount > auraTypeCount)
					{
						auraType = auraName;
						auraTypeCount = currentCount;
					}
				}

				if (auraType == SubGroups[0])
				{
					auraTypes.AddRange(SubGroups);

					break;
				}

				if (auraTypes.Empty())
					auraTypes.Add(auraType);
			}

			// re-check spells against guessed group
			foreach (uint spellId in spellIds)
			{
				var spellInfo = GetSpellInfo(spellId, Difficulty.None);

				var found = false;

				while (spellInfo != null)
				{
					foreach (var auraType in auraTypes)
						if (spellInfo.HasAura(auraType))
						{
							found = true;

							break;
						}

					if (found)
						break;

					spellInfo = spellInfo.GetNextRankSpell();
				}

				// not found either, log error
				if (!found)
					Log.outError(LogFilter.Sql, $"SpellId {spellId} listed in `spell_group` with stack rule 3 does not share aura assigned for group {group_id}");
			}

			_spellSameEffectStack[group_id] = auraTypes;
			++count;
		}

		Log.outInfo(LogFilter.ServerLoading, $"Parsed {count} SPELL_GROUP_STACK_RULE_EXCLUSIVE_SAME_EFFECT stack rules in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void LoadSpellProcs()
	{
		var oldMSTime = Time.MSTime;

		_spellProcMap.Clear(); // need for reload case

		//                                         0        1           2                3                 4                 5                 6
		var result = DB.World.Query("SELECT SpellId, SchoolMask, SpellFamilyName, SpellFamilyMask0, SpellFamilyMask1, SpellFamilyMask2, SpellFamilyMask3, " +
									//7          8           9              10              11       12              13                  14              15      16        17
									"ProcFlags, ProcFlags2, SpellTypeMask, SpellPhaseMask, HitMask, AttributesMask, DisableEffectsMask, ProcsPerMinute, Chance, Cooldown, Charges FROM spell_proc");

		uint count = 0;

		if (!result.IsEmpty())
		{
			do
			{
				var spellId = result.Read<int>(0);

				var allRanks = false;

				if (spellId < 0)
				{
					allRanks = true;
					spellId = -spellId;
				}

				var spellInfo = GetSpellInfo((uint)spellId, Difficulty.None);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_proc` does not exist", spellId);

					continue;
				}

				if (allRanks)
					if (spellInfo.GetFirstRankSpell().Id != (uint)spellId)
					{
						Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_proc` is not first rank of spell.", spellId);

						continue;
					}

				SpellProcEntry baseProcEntry = new();

				baseProcEntry.SchoolMask = (SpellSchoolMask)result.Read<uint>(1);
				baseProcEntry.SpellFamilyName = (SpellFamilyNames)result.Read<uint>(2);
				baseProcEntry.SpellFamilyMask = new FlagArray128(result.Read<uint>(3), result.Read<uint>(4), result.Read<uint>(5), result.Read<uint>(6));
				baseProcEntry.ProcFlags = new ProcFlagsInit(result.Read<int>(7), result.Read<int>(8), 2);
				baseProcEntry.SpellTypeMask = (ProcFlagsSpellType)result.Read<uint>(9);
				baseProcEntry.SpellPhaseMask = (ProcFlagsSpellPhase)result.Read<uint>(10);
				baseProcEntry.HitMask = (ProcFlagsHit)result.Read<uint>(11);
				baseProcEntry.AttributesMask = (ProcAttributes)result.Read<uint>(12);
				baseProcEntry.DisableEffectsMask = result.Read<uint>(13);
				baseProcEntry.ProcsPerMinute = result.Read<float>(14);
				baseProcEntry.Chance = result.Read<float>(15);
				baseProcEntry.Cooldown = result.Read<uint>(16);
				baseProcEntry.Charges = result.Read<uint>(17);

				while (spellInfo != null)
				{
					if (!_spellProcMap.ContainsKey(spellInfo.Id, spellInfo.Difficulty))
					{
						Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_proc` has duplicate entry in the table", spellInfo.Id);

						break;
					}

					var procEntry = baseProcEntry;

					// take defaults from dbcs
					if (!procEntry.ProcFlags)
						procEntry.ProcFlags = spellInfo.ProcFlags;

					if (procEntry.Charges == 0)
						procEntry.Charges = spellInfo.ProcCharges;

					if (procEntry.Chance == 0 && procEntry.ProcsPerMinute == 0)
						procEntry.Chance = spellInfo.ProcChance;

					if (procEntry.Cooldown == 0)
						procEntry.Cooldown = spellInfo.ProcCooldown;

					// validate data
					if (Convert.ToBoolean(procEntry.SchoolMask & ~SpellSchoolMask.All))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SchoolMask` set: {1}", spellInfo.Id, procEntry.SchoolMask);

					if (procEntry.SpellFamilyName != 0 && !Global.DB2Mgr.IsValidSpellFamiliyName(procEntry.SpellFamilyName))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SpellFamilyName` set: {1}", spellInfo.Id, procEntry.SpellFamilyName);

					if (procEntry.Chance < 0)
					{
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has negative value in `Chance` field", spellInfo.Id);
						procEntry.Chance = 0;
					}

					if (procEntry.ProcsPerMinute < 0)
					{
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has negative value in `ProcsPerMinute` field", spellInfo.Id);
						procEntry.ProcsPerMinute = 0;
					}

					if (!procEntry.ProcFlags)
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} doesn't have `ProcFlags` value defined, proc will not be triggered", spellInfo.Id);

					if (Convert.ToBoolean(procEntry.SpellTypeMask & ~ProcFlagsSpellType.MaskAll))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SpellTypeMask` set: {1}", spellInfo.Id, procEntry.SpellTypeMask);

					if (procEntry.SpellTypeMask != 0 && !procEntry.ProcFlags.HasFlag(ProcFlags.SpellMask))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has `SpellTypeMask` value defined, but it won't be used for defined `ProcFlags` value", spellInfo.Id);

					if (procEntry.SpellPhaseMask == 0 && procEntry.ProcFlags.HasFlag(ProcFlags.ReqSpellPhaseMask))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} doesn't have `SpellPhaseMask` value defined, but it's required for defined `ProcFlags` value, proc will not be triggered", spellInfo.Id);

					if (Convert.ToBoolean(procEntry.SpellPhaseMask & ~ProcFlagsSpellPhase.MaskAll))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `SpellPhaseMask` set: {1}", spellInfo.Id, procEntry.SpellPhaseMask);

					if (procEntry.SpellPhaseMask != 0 && !procEntry.ProcFlags.HasFlag(ProcFlags.ReqSpellPhaseMask))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has `SpellPhaseMask` value defined, but it won't be used for defined `ProcFlags` value", spellInfo.Id);

					if (Convert.ToBoolean(procEntry.HitMask & ~ProcFlagsHit.MaskAll))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has wrong `HitMask` set: {1}", spellInfo.Id, procEntry.HitMask);

					if (procEntry.HitMask != 0 && !(procEntry.ProcFlags.HasFlag(ProcFlags.TakenHitMask) || (procEntry.ProcFlags.HasFlag(ProcFlags.DoneHitMask) && (procEntry.SpellPhaseMask == 0 || Convert.ToBoolean(procEntry.SpellPhaseMask & (ProcFlagsSpellPhase.Hit | ProcFlagsSpellPhase.Finish))))))
						Log.outError(LogFilter.Sql, "`spell_proc` table entry for spellId {0} has `HitMask` value defined, but it won't be used for defined `ProcFlags` and `SpellPhaseMask` values", spellInfo.Id);

					foreach (var spellEffectInfo in spellInfo.Effects)
						if ((procEntry.DisableEffectsMask & (1u << (int)spellEffectInfo.EffectIndex)) != 0 && !spellEffectInfo.IsAura())
							Log.outError(LogFilter.Sql, $"The `spell_proc` table entry for spellId {spellInfo.Id} has DisableEffectsMask with effect {spellEffectInfo.EffectIndex}, but effect {spellEffectInfo.EffectIndex} is not an aura effect");

					if (procEntry.AttributesMask.HasFlag(ProcAttributes.ReqSpellmod))
					{
						var found = false;

						foreach (var spellEffectInfo in spellInfo.Effects)
						{
							if (!spellEffectInfo.IsAura())
								continue;

							if (spellEffectInfo.ApplyAuraName == AuraType.AddPctModifier || spellEffectInfo.ApplyAuraName == AuraType.AddFlatModifier || spellEffectInfo.ApplyAuraName == AuraType.AddPctModifierBySpellLabel || spellEffectInfo.ApplyAuraName == AuraType.AddFlatModifierBySpellLabel)
							{
								found = true;

								break;
							}
						}

						if (!found)
							Log.outError(LogFilter.Sql, $"The `spell_proc` table entry for spellId {spellInfo.Id} has Attribute PROC_ATTR_REQ_SPELLMOD, but spell has no spell mods. Proc will not be triggered");
					}

					if ((procEntry.AttributesMask & ~ProcAttributes.AllAllowed) != 0)
					{
						Log.outError(LogFilter.Sql, $"The `spell_proc` table entry for spellId {spellInfo.Id} has `AttributesMask` value specifying invalid attributes 0x{(procEntry.AttributesMask & ~ProcAttributes.AllAllowed):X}.");
						procEntry.AttributesMask &= ProcAttributes.AllAllowed;
					}

					_spellProcMap.Add(spellInfo.Id, spellInfo.Difficulty, procEntry);
					++count;

					if (allRanks)
						spellInfo = spellInfo.GetNextRankSpell();
					else
						break;
				}
			} while (result.NextRow());

			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell proc conditions and data in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}
		else
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell proc conditions and data. DB table `spell_proc` is empty.");
		}

		// This generates default procs to retain compatibility with previous proc system
		Log.outInfo(LogFilter.ServerLoading, "Generating spell proc data from SpellMap...");
		count = 0;
		oldMSTime = Time.MSTime;

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
			{
				// Data already present in DB, overwrites default proc
				if (_spellProcMap.ContainsKey(spellInfo.Id, spellInfo.Difficulty))
					continue;

				// Nothing to do if no flags set
				if (spellInfo.ProcFlags == null)
					continue;

				var addTriggerFlag = false;
				var procSpellTypeMask = ProcFlagsSpellType.None;
				uint nonProcMask = 0;

				foreach (var spellEffectInfo in spellInfo.Effects)
				{
					if (!spellEffectInfo.IsEffect())
						continue;

					var auraName = spellEffectInfo.ApplyAuraName;

					if (auraName == 0)
						continue;

					if (!IsTriggerAura(auraName))
					{
						// explicitly disable non proccing auras to avoid losing charges on self proc
						nonProcMask |= 1u << (int)spellEffectInfo.EffectIndex;

						continue;
					}

					procSpellTypeMask |= GetSpellTypeMask(auraName);

					if (IsAlwaysTriggeredAura(auraName))
						addTriggerFlag = true;

					// many proc auras with taken procFlag mask don't have attribute "can proc with triggered"
					// they should proc nevertheless (example mage armor spells with judgement)
					if (!addTriggerFlag && spellInfo.ProcFlags.HasFlag(ProcFlags.TakenHitMask))
						switch (auraName)
						{
							case AuraType.ProcTriggerSpell:
							case AuraType.ProcTriggerDamage:
								addTriggerFlag = true;

								break;
							default:
								break;
						}
				}

				if (procSpellTypeMask == 0)
				{
					foreach (var spellEffectInfo in spellInfo.Effects)
						if (spellEffectInfo.IsAura())
						{
							Log.outDebug(LogFilter.Sql, $"Spell Id {spellInfo.Id} has DBC ProcFlags 0x{spellInfo.ProcFlags[0]:X} 0x{spellInfo.ProcFlags[1]:X}, but it's of non-proc aura type, it probably needs an entry in `spell_proc` table to be handled correctly.");

							break;
						}

					continue;
				}

				SpellProcEntry procEntry = new();
				procEntry.SchoolMask = 0;
				procEntry.ProcFlags = spellInfo.ProcFlags;
				procEntry.SpellFamilyName = 0;

				foreach (var spellEffectInfo in spellInfo.Effects)
					if (spellEffectInfo.IsEffect() && IsTriggerAura(spellEffectInfo.ApplyAuraName))
						procEntry.SpellFamilyMask |= spellEffectInfo.SpellClassMask;

				if (procEntry.SpellFamilyMask)
					procEntry.SpellFamilyName = spellInfo.SpellFamilyName;

				procEntry.SpellTypeMask = procSpellTypeMask;
				procEntry.SpellPhaseMask = ProcFlagsSpellPhase.Hit;
				procEntry.HitMask = ProcFlagsHit.None; // uses default proc @see SpellMgr::CanSpellTriggerProcOnEvent

				foreach (var spellEffectInfo in spellInfo.Effects)
				{
					if (!spellEffectInfo.IsAura())
						continue;

					switch (spellEffectInfo.ApplyAuraName)
					{
						// Reflect auras should only proc off reflects
						case AuraType.ReflectSpells:
						case AuraType.ReflectSpellsSchool:
							procEntry.HitMask = ProcFlagsHit.Reflect;

							break;
						// Only drop charge on crit
						case AuraType.ModWeaponCritPercent:
							procEntry.HitMask = ProcFlagsHit.Critical;

							break;
						// Only drop charge on block
						case AuraType.ModBlockPercent:
							procEntry.HitMask = ProcFlagsHit.Block;

							break;
						// proc auras with another aura reducing hit chance (eg 63767) only proc on missed attack
						case AuraType.ModHitChance:
							if (spellEffectInfo.CalcValue() <= -100)
								procEntry.HitMask = ProcFlagsHit.Miss;

							break;
						default:
							continue;
					}

					break;
				}

				procEntry.AttributesMask = 0;
				procEntry.DisableEffectsMask = nonProcMask;

				if (spellInfo.ProcFlags.HasFlag(ProcFlags.Kill))
					procEntry.AttributesMask |= ProcAttributes.ReqExpOrHonor;

				if (addTriggerFlag)
					procEntry.AttributesMask |= ProcAttributes.TriggeredCanProc;

				procEntry.ProcsPerMinute = 0;
				procEntry.Chance = spellInfo.ProcChance;
				procEntry.Cooldown = spellInfo.ProcCooldown;
				procEntry.Charges = spellInfo.ProcCharges;

				_spellProcMap.Add(spellInfo.Id, spellInfo.Difficulty, procEntry);
				++count;
			}

		Log.outInfo(LogFilter.ServerLoading, "Generated spell proc data for {0} spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellThreats()
	{
		var oldMSTime = Time.MSTime;

		_spellThreatMap.Clear(); // need for reload case

		//                                           0      1        2       3
		var result = DB.World.Query("SELECT entry, flatMod, pctMod, apPctMod FROM spell_threat");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 aggro generating spells. DB table `spell_threat` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var entry = result.Read<uint>(0);

			if (!HasSpellInfo(entry, Difficulty.None))
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_threat` does not exist", entry);

				continue;
			}

			SpellThreatEntry ste = new();
			ste.FlatMod = result.Read<int>(1);
			ste.PctMod = result.Read<float>(2);
			ste.ApPctMod = result.Read<float>(3);

			_spellThreatMap[entry] = ste;
			count++;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SpellThreatEntries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSkillLineAbilityMap()
	{
		var oldMSTime = Time.MSTime;

		_skillLineAbilityMap.Clear();

		foreach (var skill in CliDB.SkillLineAbilityStorage.Values)
			_skillLineAbilityMap.Add(skill.Spell, skill);

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SkillLineAbility MultiMap Data in {1} ms", _skillLineAbilityMap.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellPetAuras()
	{
		var oldMSTime = Time.MSTime;

		_spellPetAuraMap.Clear(); // need for reload case

		//                                                  0       1       2    3
		var result = DB.World.Query("SELECT spell, effectId, pet, aura FROM spell_pet_auras");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell pet auras. DB table `spell_pet_auras` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var spell = result.Read<uint>(0);
			var eff = result.Read<byte>(1);
			var pet = result.Read<uint>(2);
			var aura = result.Read<uint>(3);

			var petAura = GetPetAura(spell, eff);

			if (petAura != null)
			{
				petAura.AddAura(pet, aura);
			}
			else
			{
				var spellInfo = GetSpellInfo(spell, Difficulty.None);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_pet_auras` does not exist", spell);

					continue;
				}

				if (eff >= spellInfo.Effects.Count)
				{
					Log.outError(LogFilter.Spells, "Spell {0} listed in `spell_pet_auras` does not have effect at index {1}", spell, eff);

					continue;
				}

				if (spellInfo.GetEffect(eff).Effect != SpellEffectName.Dummy && (spellInfo.GetEffect(eff).Effect != SpellEffectName.ApplyAura || spellInfo.GetEffect(eff).ApplyAuraName != AuraType.Dummy))
				{
					Log.outError(LogFilter.Spells, "Spell {0} listed in `spell_pet_auras` does not have dummy aura or dummy effect", spell);

					continue;
				}

				var spellInfo2 = GetSpellInfo(aura, Difficulty.None);

				if (spellInfo2 == null)
				{
					Log.outError(LogFilter.Sql, "Aura {0} listed in `spell_pet_auras` does not exist", aura);

					continue;
				}

				PetAura pa = new(pet, aura, spellInfo.GetEffect(eff).TargetA.Target == Targets.UnitPet, spellInfo.GetEffect(eff).CalcValue());

				if (!_spellPetAuraMap.ContainsKey(spell))
					_spellPetAuraMap[spell] = new Dictionary<int, PetAura>();

				_spellPetAuraMap[spell][eff] = pa;
			}

			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell pet auras in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellEnchantProcData()
	{
		var oldMSTime = Time.MSTime;

		_spellEnchantProcEventMap.Clear(); // need for reload case

		//                                         0          1       2               3        4
		var result = DB.World.Query("SELECT EnchantID, Chance, ProcsPerMinute, HitMask, AttributesMask FROM spell_enchant_proc_data");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell enchant proc event conditions. DB table `spell_enchant_proc_data` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var enchantId = result.Read<uint>(0);

			var ench = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);

			if (ench == null)
			{
				Log.outError(LogFilter.Sql, "Enchancment {0} listed in `spell_enchant_proc_data` does not exist", enchantId);

				continue;
			}

			SpellEnchantProcEntry spe = new();
			spe.Chance = result.Read<uint>(1);
			spe.ProcsPerMinute = result.Read<float>(2);
			spe.HitMask = result.Read<uint>(3);
			spe.AttributesMask = (EnchantProcAttributes)result.Read<uint>(4);

			_spellEnchantProcEventMap[enchantId] = spe;

			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} enchant proc data definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellLinked()
	{
		var oldMSTime = Time.MSTime;

		_spellLinkedMap.Clear(); // need for reload case

		//                                                0              1             2
		var result = DB.World.Query("SELECT spell_trigger, spell_effect, type FROM spell_linked_spell");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 linked spells. DB table `spell_linked_spell` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var trigger = result.Read<int>(0);
			var effect = result.Read<int>(1);
			var type = (SpellLinkedType)result.Read<byte>(2);

			var spellInfo = GetSpellInfo((uint)Math.Abs(trigger), Difficulty.None);

			if (spellInfo == null)
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_linked_spell` does not exist", Math.Abs(trigger));

				continue;
			}

			if (effect >= 0)
				foreach (var spellEffectInfo in spellInfo.Effects)
					if (spellEffectInfo.CalcValue() == Math.Abs(effect))
						Log.outError(LogFilter.Sql, $"The spell {Math.Abs(trigger)} Effect: {Math.Abs(effect)} listed in `spell_linked_spell` has same bp{spellEffectInfo.EffectIndex} like effect (possible hack)");

			if (!HasSpellInfo((uint)Math.Abs(effect), Difficulty.None))
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_linked_spell` does not exist", Math.Abs(effect));

				continue;
			}

			if (type < SpellLinkedType.Cast || type > SpellLinkedType.Remove)
			{
				Log.outError(LogFilter.Sql, $"The spell trigger {trigger}, effect {effect} listed in `spell_linked_spell` has invalid link type {type}, skipped.");

				continue;
			}

			if (trigger < 0)
			{
				if (type != SpellLinkedType.Cast)
					Log.outError(LogFilter.Sql, $"The spell trigger {trigger} listed in `spell_linked_spell` has invalid link type {type}, changed to 0.");

				trigger = -trigger;
				type = SpellLinkedType.Remove;
			}


			if (type != SpellLinkedType.Aura)
				if (trigger == effect)
				{
					Log.outError(LogFilter.Sql, $"The spell trigger {trigger}, effect {effect} listed in `spell_linked_spell` triggers itself (infinite loop), skipped.");

					continue;
				}

			_spellLinkedMap.Add((type, (uint)trigger), effect);

			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} linked spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadPetLevelupSpellMap()
	{
		var oldMSTime = Time.MSTime;

		_petLevelupSpellMap.Clear(); // need for reload case

		uint count = 0;
		uint family_count = 0;

		foreach (var creatureFamily in CliDB.CreatureFamilyStorage.Values)
			for (byte j = 0; j < 2; ++j)
			{
				if (creatureFamily.SkillLine[j] == 0)
					continue;

				var skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill((uint)creatureFamily.SkillLine[j]);

				if (skillLineAbilities == null)
					continue;

				foreach (var skillLine in skillLineAbilities)
				{
					if (skillLine.AcquireMethod != AbilityLearnType.OnSkillLearn)
						continue;

					var spell = GetSpellInfo(skillLine.Spell, Difficulty.None);

					if (spell == null) // not exist or triggered or talent
						continue;

					if (spell.SpellLevel == 0)
						continue;

					if (!_petLevelupSpellMap.ContainsKey(creatureFamily.Id))
						_petLevelupSpellMap.Add(creatureFamily.Id, new MultiMap<uint, uint>());

					var spellSet = _petLevelupSpellMap.LookupByKey(creatureFamily.Id);

					if (spellSet.Count == 0)
						++family_count;

					spellSet.Add(spell.SpellLevel, spell.Id);
					++count;
				}
			}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pet levelup and default spells for {1} families in {2} ms", count, family_count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadPetDefaultSpells()
	{
		var oldMSTime = Time.MSTime;

		_petDefaultSpellsEntries.Clear();

		uint countCreature = 0;

		Log.outInfo(LogFilter.ServerLoading, "Loading summonable creature templates...");

		// different summon spells
		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellEntry in kvp.Values)
				if (spellEntry.Difficulty != Difficulty.None)
					foreach (var spellEffectInfo in spellEntry.Effects)
						if (spellEffectInfo.Effect == SpellEffectName.Summon || spellEffectInfo.Effect == SpellEffectName.SummonPet)
						{
							var creature_id = spellEffectInfo.MiscValue;
							var cInfo = Global.ObjectMgr.GetCreatureTemplate((uint)creature_id);

							if (cInfo == null)
								continue;

							// get default pet spells from creature_template
							var petSpellsId = cInfo.Entry;

							if (_petDefaultSpellsEntries.LookupByKey(cInfo.Entry) != null)
								continue;

							PetDefaultSpellsEntry petDefSpells = new();

							for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
								petDefSpells.Spellid[j] = cInfo.Spells[j];

							if (LoadPetDefaultSpells_helper(cInfo, petDefSpells))
							{
								_petDefaultSpellsEntries[petSpellsId] = petDefSpells;
								++countCreature;
							}
						}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} summonable creature templates in {1} ms", countCreature, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	bool LoadPetDefaultSpells_helper(CreatureTemplate cInfo, PetDefaultSpellsEntry petDefSpells)
	{
		// skip empty list;
		var have_spell = false;

		for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
			if (petDefSpells.Spellid[j] != 0)
			{
				have_spell = true;

				break;
			}

		if (!have_spell)
			return false;

		// remove duplicates with levelupSpells if any
		var levelupSpells = cInfo.Family != 0 ? GetPetLevelupSpellList(cInfo.Family) : null;

		if (levelupSpells != null)
			for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
			{
				if (petDefSpells.Spellid[j] == 0)
					continue;

				foreach (var pair in levelupSpells.KeyValueList)
					if (pair.Value == petDefSpells.Spellid[j])
					{
						petDefSpells.Spellid[j] = 0;

						break;
					}
			}

		// skip empty list;
		have_spell = false;

		for (byte j = 0; j < SharedConst.MaxCreatureSpellDataSlots; ++j)
			if (petDefSpells.Spellid[j] != 0)
			{
				have_spell = true;

				break;
			}

		return have_spell;
	}

	public void LoadSpellAreas()
	{
		var oldMSTime = Time.MSTime;

		_spellAreaMap.Clear(); // need for reload case
		_spellAreaForAreaMap.Clear();
		_spellAreaForQuestMap.Clear();
		_spellAreaForQuestEndMap.Clear();
		_spellAreaForAuraMap.Clear();

		//                                            0     1         2              3               4                 5          6          7       8      9
		var result = DB.World.Query("SELECT spell, area, quest_start, quest_start_status, quest_end_status, quest_end, aura_spell, racemask, gender, flags FROM spell_area");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell area requirements. DB table `spell_area` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var spell = result.Read<uint>(0);

			SpellArea spellArea = new();
			spellArea.SpellId = spell;
			spellArea.AreaId = result.Read<uint>(1);
			spellArea.QuestStart = result.Read<uint>(2);
			spellArea.QuestStartStatus = result.Read<uint>(3);
			spellArea.QuestEndStatus = result.Read<uint>(4);
			spellArea.QuestEnd = result.Read<uint>(5);
			spellArea.AuraSpell = result.Read<int>(6);
			spellArea.RaceMask = result.Read<ulong>(7);
			spellArea.Gender = (Gender)result.Read<uint>(8);
			spellArea.Flags = (SpellAreaFlag)result.Read<byte>(9);

			var spellInfo = GetSpellInfo(spell, Difficulty.None);

			if (spellInfo != null)
			{
				if (spellArea.Flags.HasAnyFlag(SpellAreaFlag.AutoCast))
					spellInfo.Attributes |= SpellAttr0.NoAuraCancel;
			}
			else
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` does not exist", spell);

				continue;
			}

			{
				var ok = true;
				var sa_bounds = GetSpellAreaMapBounds(spellArea.SpellId);

				foreach (var bound in sa_bounds)
				{
					if (spellArea.SpellId != bound.SpellId)
						continue;

					if (spellArea.AreaId != bound.AreaId)
						continue;

					if (spellArea.QuestStart != bound.QuestStart)
						continue;

					if (spellArea.AuraSpell != bound.AuraSpell)
						continue;

					if ((spellArea.RaceMask & bound.RaceMask) == 0)
						continue;

					if (spellArea.Gender != bound.Gender)
						continue;

					// duplicate by requirements
					ok = false;

					break;
				}

				if (!ok)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` already listed with similar requirements.", spell);

					continue;
				}
			}

			if (spellArea.AreaId != 0 && !CliDB.AreaTableStorage.ContainsKey(spellArea.AreaId))
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong area ({1}) requirement", spell, spellArea.AreaId);

				continue;
			}

			if (spellArea.QuestStart != 0 && Global.ObjectMgr.GetQuestTemplate(spellArea.QuestStart) == null)
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong start quest ({1}) requirement", spell, spellArea.QuestStart);

				continue;
			}

			if (spellArea.QuestEnd != 0)
				if (Global.ObjectMgr.GetQuestTemplate(spellArea.QuestEnd) == null)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong end quest ({1}) requirement", spell, spellArea.QuestEnd);

					continue;
				}

			if (spellArea.AuraSpell != 0)
			{
				var info = GetSpellInfo((uint)Math.Abs(spellArea.AuraSpell), Difficulty.None);

				if (info == null)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong aura spell ({1}) requirement", spell, Math.Abs(spellArea.AuraSpell));

					continue;
				}

				if (Math.Abs(spellArea.AuraSpell) == spellArea.SpellId)
				{
					Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have aura spell ({1}) requirement for itself", spell, Math.Abs(spellArea.AuraSpell));

					continue;
				}

				// not allow autocast chains by auraSpell field (but allow use as alternative if not present)
				if (spellArea.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spellArea.AuraSpell > 0)
				{
					var chain = false;
					var saBound = GetSpellAreaForAuraMapBounds(spellArea.SpellId);

					foreach (var bound in saBound)
						if (bound.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && bound.AuraSpell > 0)
						{
							chain = true;

							break;
						}

					if (chain)
					{
						Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have aura spell ({1}) requirement that itself autocast from aura", spell, spellArea.AuraSpell);

						continue;
					}

					var saBound2 = GetSpellAreaMapBounds((uint)spellArea.AuraSpell);

					foreach (var bound in saBound2)
						if (bound.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && bound.AuraSpell > 0)
						{
							chain = true;

							break;
						}

					if (chain)
					{
						Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have aura spell ({1}) requirement that itself autocast from aura", spell, spellArea.AuraSpell);

						continue;
					}
				}
			}

			if (spellArea.RaceMask != 0 && (spellArea.RaceMask & SharedConst.RaceMaskAllPlayable) == 0)
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong race mask ({1}) requirement", spell, spellArea.RaceMask);

				continue;
			}

			if (spellArea.Gender != Gender.None && spellArea.Gender != Gender.Female && spellArea.Gender != Gender.Male)
			{
				Log.outError(LogFilter.Sql, "Spell {0} listed in `spell_area` have wrong gender ({1}) requirement", spell, spellArea.Gender);

				continue;
			}

			_spellAreaMap.Add(spell, spellArea);
			var sa = _spellAreaMap[spell];

			// for search by current zone/subzone at zone/subzone change
			if (spellArea.AreaId != 0)
				_spellAreaForAreaMap.AddRange(spellArea.AreaId, sa);

			// for search at quest update checks
			if (spellArea.QuestStart != 0 || spellArea.QuestEnd != 0)
			{
				if (spellArea.QuestStart == spellArea.QuestEnd)
				{
					_spellAreaForQuestMap.AddRange(spellArea.QuestStart, sa);
				}
				else
				{
					if (spellArea.QuestStart != 0)
						_spellAreaForQuestMap.AddRange(spellArea.QuestStart, sa);

					if (spellArea.QuestEnd != 0)
						_spellAreaForQuestMap.AddRange(spellArea.QuestEnd, sa);
				}
			}

			// for search at quest start/reward
			if (spellArea.QuestEnd != 0)
				_spellAreaForQuestEndMap.AddRange(spellArea.QuestEnd, sa);

			// for search at aura apply
			if (spellArea.AuraSpell != 0)
				_spellAreaForAuraMap.AddRange((uint)Math.Abs(spellArea.AuraSpell), sa);

			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell area requirements in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellInfoStore()
	{
		var oldMSTime = Time.MSTime;

		_spellInfoMap.Clear();
		var loadData = new Dictionary<(uint Id, Difficulty difficulty), SpellInfoLoadHelper>();

		Dictionary<uint, BattlePetSpeciesRecord> battlePetSpeciesByCreature = new();

		foreach (var battlePetSpecies in CliDB.BattlePetSpeciesStorage.Values)
			if (battlePetSpecies.CreatureID != 0)
				battlePetSpeciesByCreature[battlePetSpecies.CreatureID] = battlePetSpecies;

		SpellInfoLoadHelper GetLoadHelper(uint spellId, uint difficulty)
		{
			var key = (spellId, (Difficulty)difficulty);

			if (!loadData.ContainsKey(key))
				loadData[key] = new SpellInfoLoadHelper();

			return loadData[key];
		}

		foreach (var effect in CliDB.SpellEffectStorage.Values)
		{
			GetLoadHelper(effect.SpellID, effect.DifficultyID).Effects[effect.EffectIndex] = effect;

			if (effect.Effect == (int)SpellEffectName.Summon)
			{
				var summonProperties = CliDB.SummonPropertiesStorage.LookupByKey(effect.EffectMiscValue[1]);

				if (summonProperties != null)
					if (summonProperties.Slot == (int)SummonSlot.MiniPet && summonProperties.GetFlags().HasFlag(SummonPropertiesFlags.SummonFromBattlePetJournal))
					{
						var battlePetSpecies = battlePetSpeciesByCreature.LookupByKey(effect.EffectMiscValue[0]);

						if (battlePetSpecies != null)
							BattlePetMgr.AddBattlePetSpeciesBySpell(effect.SpellID, battlePetSpecies);
					}
			}

			if (effect.Effect == (int)SpellEffectName.Language)
				Global.LanguageMgr.LoadSpellEffectLanguage(effect);
		}

		foreach (var auraOptions in CliDB.SpellAuraOptionsStorage.Values)
			GetLoadHelper(auraOptions.SpellID, auraOptions.DifficultyID).AuraOptions = auraOptions;

		foreach (var auraRestrictions in CliDB.SpellAuraRestrictionsStorage.Values)
			GetLoadHelper(auraRestrictions.SpellID, auraRestrictions.DifficultyID).AuraRestrictions = auraRestrictions;

		foreach (var castingRequirements in CliDB.SpellCastingRequirementsStorage.Values)
			GetLoadHelper(castingRequirements.SpellID, 0).CastingRequirements = castingRequirements;

		foreach (var categories in CliDB.SpellCategoriesStorage.Values)
			GetLoadHelper(categories.SpellID, categories.DifficultyID).Categories = categories;

		foreach (var classOptions in CliDB.SpellClassOptionsStorage.Values)
			GetLoadHelper(classOptions.SpellID, 0).ClassOptions = classOptions;

		foreach (var cooldowns in CliDB.SpellCooldownsStorage.Values)
			GetLoadHelper(cooldowns.SpellID, cooldowns.DifficultyID).Cooldowns = cooldowns;

		foreach (var equippedItems in CliDB.SpellEquippedItemsStorage.Values)
			GetLoadHelper(equippedItems.SpellID, 0).EquippedItems = equippedItems;

		foreach (var interrupts in CliDB.SpellInterruptsStorage.Values)
			GetLoadHelper(interrupts.SpellID, interrupts.DifficultyID).Interrupts = interrupts;

		foreach (var label in CliDB.SpellLabelStorage.Values)
			GetLoadHelper(label.SpellID, 0).Labels.Add(label);

		foreach (var levels in CliDB.SpellLevelsStorage.Values)
			GetLoadHelper(levels.SpellID, levels.DifficultyID).Levels = levels;

		foreach (var misc in CliDB.SpellMiscStorage.Values)
			if (misc.DifficultyID == 0)
				GetLoadHelper(misc.SpellID, misc.DifficultyID).Misc = misc;

		foreach (var power in CliDB.SpellPowerStorage.Values)
		{
			uint difficulty = 0;
			var index = power.OrderIndex;

			var powerDifficulty = CliDB.SpellPowerDifficultyStorage.LookupByKey(power.Id);

			if (powerDifficulty != null)
			{
				difficulty = powerDifficulty.DifficultyID;
				index = powerDifficulty.OrderIndex;
			}

			GetLoadHelper(power.SpellID, difficulty).Powers[index] = power;
		}

		foreach (var reagents in CliDB.SpellReagentsStorage.Values)
			GetLoadHelper(reagents.SpellID, 0).Reagents = reagents;

		foreach (var reagentsCurrency in CliDB.SpellReagentsCurrencyStorage.Values)
			GetLoadHelper((uint)reagentsCurrency.SpellID, 0).ReagentsCurrency.Add(reagentsCurrency);

		foreach (var scaling in CliDB.SpellScalingStorage.Values)
			GetLoadHelper(scaling.SpellID, 0).Scaling = scaling;

		foreach (var shapeshift in CliDB.SpellShapeshiftStorage.Values)
			GetLoadHelper(shapeshift.SpellID, 0).Shapeshift = shapeshift;

		foreach (var targetRestrictions in CliDB.SpellTargetRestrictionsStorage.Values)
			GetLoadHelper(targetRestrictions.SpellID, targetRestrictions.DifficultyID).TargetRestrictions = targetRestrictions;

		foreach (var totems in CliDB.SpellTotemsStorage.Values)
			GetLoadHelper(totems.SpellID, 0).Totems = totems;

		foreach (var visual in CliDB.SpellXSpellVisualStorage.Values)
		{
			var visuals = GetLoadHelper(visual.SpellID, visual.DifficultyID).Visuals;
			visuals.Add(visual);
		}

		// sorted with unconditional visuals being last
		foreach (var data in loadData)
			data.Value.Visuals.Sort((left, right) => { return right.CasterPlayerConditionID.CompareTo(left.CasterPlayerConditionID); });

		foreach (var empwerRank in CliDB.SpellEmpowerStageStorage)
			if (CliDB.SpellEmpowerStorage.TryGetValue(empwerRank.Value.SpellEmpowerID, out var empowerRecord))
				GetLoadHelper(empowerRecord.SpellID, 0).EmpowerStages.Add(empwerRank.Value);
			else
				Log.outWarn(LogFilter.ServerLoading, $"SpellEmpowerStageStorage contains SpellEmpowerID: {empwerRank.Value.SpellEmpowerID} that is not found in SpellEmpowerStorage.");

		foreach (var data in loadData)
		{
			var spellNameEntry = CliDB.SpellNameStorage.LookupByKey(data.Key.Id);

			if (spellNameEntry == null)
				continue;

			// fill blanks
			var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(data.Key.difficulty);

			if (difficultyEntry != null)
				do
				{
					var fallbackData = loadData.LookupByKey((data.Key.Id, (Difficulty)difficultyEntry.FallbackDifficultyID));

					if (fallbackData != null)
					{
						if (data.Value.AuraOptions == null)
							data.Value.AuraOptions = fallbackData.AuraOptions;

						if (data.Value.AuraRestrictions == null)
							data.Value.AuraRestrictions = fallbackData.AuraRestrictions;

						if (data.Value.CastingRequirements == null)
							data.Value.CastingRequirements = fallbackData.CastingRequirements;

						if (data.Value.Categories == null)
							data.Value.Categories = fallbackData.Categories;

						if (data.Value.ClassOptions == null)
							data.Value.ClassOptions = fallbackData.ClassOptions;

						if (data.Value.Cooldowns == null)
							data.Value.Cooldowns = fallbackData.Cooldowns;

						foreach (var fallbackEff in fallbackData.Effects)
							if (!data.Value.Effects.ContainsKey(fallbackEff.Key))
								data.Value.Effects[fallbackEff.Key] = fallbackEff.Value;

						if (data.Value.EquippedItems == null)
							data.Value.EquippedItems = fallbackData.EquippedItems;

						if (data.Value.Interrupts == null)
							data.Value.Interrupts = fallbackData.Interrupts;

						if (data.Value.Labels.Empty())
							data.Value.Labels = fallbackData.Labels;

						if (data.Value.Levels == null)
							data.Value.Levels = fallbackData.Levels;

						if (data.Value.Misc == null)
							data.Value.Misc = fallbackData.Misc;

						for (var i = 0; i < fallbackData.Powers.Length; ++i)
							if (data.Value.Powers[i] == null)
								data.Value.Powers[i] = fallbackData.Powers[i];

						if (data.Value.Reagents == null)
							data.Value.Reagents = fallbackData.Reagents;

						if (data.Value.ReagentsCurrency.Empty())
							data.Value.ReagentsCurrency = fallbackData.ReagentsCurrency;

						if (data.Value.Scaling == null)
							data.Value.Scaling = fallbackData.Scaling;

						if (data.Value.Shapeshift == null)
							data.Value.Shapeshift = fallbackData.Shapeshift;

						if (data.Value.TargetRestrictions == null)
							data.Value.TargetRestrictions = fallbackData.TargetRestrictions;

						if (data.Value.Totems == null)
							data.Value.Totems = fallbackData.Totems;

						// visuals fall back only to first difficulty that defines any visual
						// they do not stack all difficulties in fallback chain
						if (data.Value.Visuals.Empty())
							data.Value.Visuals = fallbackData.Visuals;
					}

					difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
				} while (difficultyEntry != null);

			//first key = id, difficulty
			//second key = id


			AddSpellInfo(new SpellInfo(spellNameEntry, data.Key.difficulty, data.Value));
		}

		Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo store in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void UnloadSpellInfoImplicitTargetConditionLists()
	{
		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spell in kvp.Values)
				spell.UnloadImplicitTargetConditionLists();
	}

	public void LoadSpellInfoServerside()
	{
		var oldMSTime = Time.MSTime;

		MultiMap<(uint spellId, Difficulty difficulty), SpellEffectRecord> spellEffects = new();

		//                                                0        1            2             3       4           5                6
		var effectsResult = DB.World.Query("SELECT SpellID, EffectIndex, DifficultyID, Effect, EffectAura, EffectAmplitude, EffectAttributes, " +
											//7                 8                       9                     10                  11              12              13
											"EffectAuraPeriod, EffectBonusCoefficient, EffectChainAmplitude, EffectChainTargets, EffectItemType, EffectMechanic, EffectPointsPerResource, " +
											//14               15                        16                  17                      18             19           20
											"EffectPosFacing, EffectRealPointsPerLevel, EffectTriggerSpell, BonusCoefficientFromAP, PvpMultiplier, Coefficient, Variance, " +
											//21                   22                              23                24                25                26
											"ResourceCoefficient, GroupSizeBasePointsCoefficient, EffectBasePoints, EffectMiscValue1, EffectMiscValue2, EffectRadiusIndex1, " +
											//27                  28                     29                     30                     31                     32
											"EffectRadiusIndex2, EffectSpellClassMask1, EffectSpellClassMask2, EffectSpellClassMask3, EffectSpellClassMask4, ImplicitTarget1, " +
											//33
											"ImplicitTarget2 FROM serverside_spell_effect");

		if (!effectsResult.IsEmpty())
			do
			{
				var spellId = effectsResult.Read<uint>(0);
				var difficulty = (Difficulty)effectsResult.Read<uint>(2);
				SpellEffectRecord effect = new();
				effect.EffectIndex = effectsResult.Read<int>(1);
				effect.Effect = effectsResult.Read<uint>(3);
				effect.EffectAura = effectsResult.Read<short>(4);
				effect.EffectAmplitude = effectsResult.Read<float>(5);
				effect.EffectAttributes = (SpellEffectAttributes)effectsResult.Read<int>(6);
				effect.EffectAuraPeriod = effectsResult.Read<uint>(7);
				effect.EffectBonusCoefficient = effectsResult.Read<float>(8);
				effect.EffectChainAmplitude = effectsResult.Read<float>(9);
				effect.EffectChainTargets = effectsResult.Read<int>(10);
				effect.EffectItemType = effectsResult.Read<uint>(11);
				effect.EffectMechanic = effectsResult.Read<int>(12);
				effect.EffectPointsPerResource = effectsResult.Read<float>(13);
				effect.EffectPosFacing = effectsResult.Read<float>(14);
				effect.EffectRealPointsPerLevel = effectsResult.Read<float>(15);
				effect.EffectTriggerSpell = effectsResult.Read<uint>(16);
				effect.BonusCoefficientFromAP = effectsResult.Read<float>(17);
				effect.PvpMultiplier = effectsResult.Read<float>(18);
				effect.Coefficient = effectsResult.Read<float>(19);
				effect.Variance = effectsResult.Read<float>(20);
				effect.ResourceCoefficient = effectsResult.Read<float>(21);
				effect.GroupSizeBasePointsCoefficient = effectsResult.Read<float>(22);
				effect.EffectBasePoints = effectsResult.Read<float>(23);
				effect.EffectMiscValue[0] = effectsResult.Read<int>(24);
				effect.EffectMiscValue[1] = effectsResult.Read<int>(25);
				effect.EffectRadiusIndex[0] = effectsResult.Read<uint>(26);
				effect.EffectRadiusIndex[1] = effectsResult.Read<uint>(27);
				effect.EffectSpellClassMask = new FlagArray128(effectsResult.Read<uint>(28), effectsResult.Read<uint>(29), effectsResult.Read<uint>(30), effectsResult.Read<uint>(31));
				effect.ImplicitTarget[0] = effectsResult.Read<short>(32);
				effect.ImplicitTarget[1] = effectsResult.Read<short>(33);

				var existingSpellBounds = _GetSpellInfo(spellId);

				if (existingSpellBounds.Empty())
				{
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} effext index {effect.EffectIndex} references a regular spell loaded from file. Adding serverside effects to existing spells is not allowed.");

					continue;
				}

				if (difficulty != Difficulty.None && !CliDB.DifficultyStorage.HasRecord((uint)difficulty))
				{
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} effect index {effect.EffectIndex} references non-existing difficulty {difficulty}, skipped");

					continue;
				}

				if (effect.Effect >= (uint)SpellEffectName.TotalSpellEffects)
				{
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} has invalid effect type {effect.Effect} at index {effect.EffectIndex}, skipped");

					continue;
				}

				if (effect.EffectAura >= (uint)AuraType.Total)
				{
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} has invalid aura type {effect.EffectAura} at index {effect.EffectIndex}, skipped");

					continue;
				}

				if (effect.ImplicitTarget[0] >= (uint)Targets.TotalSpellTargets)
				{
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} has invalid targetA type {effect.ImplicitTarget[0]} at index {effect.EffectIndex}, skipped");

					continue;
				}

				if (effect.ImplicitTarget[1] >= (uint)Targets.TotalSpellTargets)
				{
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} has invalid targetB type {effect.ImplicitTarget[1]} at index {effect.EffectIndex}, skipped");

					continue;
				}

				if (effect.EffectRadiusIndex[0] != 0 && !CliDB.SpellRadiusStorage.HasRecord(effect.EffectRadiusIndex[0]))
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} has invalid radius id {effect.EffectRadiusIndex[0]} at index {effect.EffectIndex}, set to 0");

				if (effect.EffectRadiusIndex[1] != 0 && !CliDB.SpellRadiusStorage.HasRecord(effect.EffectRadiusIndex[1]))
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} has invalid max radius id {effect.EffectRadiusIndex[1]} at index {effect.EffectIndex}, set to 0");

				spellEffects.Add((spellId, difficulty), effect);
			} while (effectsResult.NextRow());

		//                                               0   1             2           3       4         5           6             7              8
		var spellsResult = DB.World.Query("SELECT Id, DifficultyID, CategoryId, Dispel, Mechanic, Attributes, AttributesEx, AttributesEx2, AttributesEx3, " +
										//9              10             11             12             13             14             15              16              17              18
										"AttributesEx4, AttributesEx5, AttributesEx6, AttributesEx7, AttributesEx8, AttributesEx9, AttributesEx10, AttributesEx11, AttributesEx12, AttributesEx13, " +
										//19              20       21          22       23                  24                  25                 26               27
										"AttributesEx14, Stances, StancesNot, Targets, TargetCreatureType, RequiresSpellFocus, FacingCasterFlags, CasterAuraState, TargetAuraState, " +
										//28                      29                      30               31               32                      33
										"ExcludeCasterAuraState, ExcludeTargetAuraState, CasterAuraSpell, TargetAuraSpell, ExcludeCasterAuraSpell, ExcludeTargetAuraSpell, " +
										//34              35              36                     37                     38
										"CasterAuraType, TargetAuraType, ExcludeCasterAuraType, ExcludeTargetAuraType, CastingTimeIndex, " +
										//39            40                    41                     42                 43              44                   45
										"RecoveryTime, CategoryRecoveryTime, StartRecoveryCategory, StartRecoveryTime, InterruptFlags, AuraInterruptFlags1, AuraInterruptFlags2, " +
										//46                      47                      48         49          50          51           52            53           54        55         56
										"ChannelInterruptFlags1, ChannelInterruptFlags2, ProcFlags, ProcFlags2, ProcChance, ProcCharges, ProcCooldown, ProcBasePPM, MaxLevel, BaseLevel, SpellLevel, " +
										//57             58          59     60           61           62                 63                        64                             65
										"DurationIndex, RangeIndex, Speed, LaunchDelay, StackAmount, EquippedItemClass, EquippedItemSubClassMask, EquippedItemInventoryTypeMask, ContentTuningId, " +
										//66         67         68         69              70                  71               72                 73                 74                 75
										"SpellName, ConeAngle, ConeWidth, MaxTargetLevel, MaxAffectedTargets, SpellFamilyName, SpellFamilyFlags1, SpellFamilyFlags2, SpellFamilyFlags3, SpellFamilyFlags4, " +
										//76        77              78           79          80
										"DmgClass, PreventionType, AreaGroupId, SchoolMask, ChargeCategoryId FROM serverside_spell");

		if (!spellsResult.IsEmpty())
			do
			{
				var spellId = spellsResult.Read<uint>(0);
				var difficulty = (Difficulty)spellsResult.Read<uint>(1);

				if (CliDB.SpellNameStorage.HasRecord(spellId))
				{
					Log.outError(LogFilter.Sql, $"Serverside spell {spellId} difficulty {difficulty} is already loaded from file. Overriding existing spells is not allowed.");

					continue;
				}

				_serversideSpellNames.Add(new ServersideSpellName(spellId, spellsResult.Read<string>(66)));

				SpellInfo spellInfo = new(_serversideSpellNames.Last().Name, difficulty, spellEffects[(spellId, difficulty)]);
				spellInfo.CategoryId = spellsResult.Read<uint>(2);
				spellInfo.Dispel = (DispelType)spellsResult.Read<uint>(3);
				spellInfo.Mechanic = (Mechanics)spellsResult.Read<uint>(4);
				spellInfo.Attributes = (SpellAttr0)spellsResult.Read<uint>(5);
				spellInfo.AttributesEx = (SpellAttr1)spellsResult.Read<uint>(6);
				spellInfo.AttributesEx2 = (SpellAttr2)spellsResult.Read<uint>(7);
				spellInfo.AttributesEx3 = (SpellAttr3)spellsResult.Read<uint>(8);
				spellInfo.AttributesEx4 = (SpellAttr4)spellsResult.Read<uint>(9);
				spellInfo.AttributesEx5 = (SpellAttr5)spellsResult.Read<uint>(10);
				spellInfo.AttributesEx6 = (SpellAttr6)spellsResult.Read<uint>(11);
				spellInfo.AttributesEx7 = (SpellAttr7)spellsResult.Read<uint>(12);
				spellInfo.AttributesEx8 = (SpellAttr8)spellsResult.Read<uint>(13);
				spellInfo.AttributesEx9 = (SpellAttr9)spellsResult.Read<uint>(14);
				spellInfo.AttributesEx10 = (SpellAttr10)spellsResult.Read<uint>(15);
				spellInfo.AttributesEx11 = (SpellAttr11)spellsResult.Read<uint>(16);
				spellInfo.AttributesEx12 = (SpellAttr12)spellsResult.Read<uint>(17);
				spellInfo.AttributesEx13 = (SpellAttr13)spellsResult.Read<uint>(18);
				spellInfo.AttributesEx14 = (SpellAttr14)spellsResult.Read<uint>(19);
				spellInfo.Stances = spellsResult.Read<ulong>(20);
				spellInfo.StancesNot = spellsResult.Read<ulong>(21);
				spellInfo.Targets = (SpellCastTargetFlags)spellsResult.Read<uint>(22);
				spellInfo.TargetCreatureType = spellsResult.Read<uint>(23);
				spellInfo.RequiresSpellFocus = spellsResult.Read<uint>(24);
				spellInfo.FacingCasterFlags = spellsResult.Read<uint>(25);
				spellInfo.CasterAuraState = (AuraStateType)spellsResult.Read<uint>(26);
				spellInfo.TargetAuraState = (AuraStateType)spellsResult.Read<uint>(27);
				spellInfo.ExcludeCasterAuraState = (AuraStateType)spellsResult.Read<uint>(28);
				spellInfo.ExcludeTargetAuraState = (AuraStateType)spellsResult.Read<uint>(29);
				spellInfo.CasterAuraSpell = spellsResult.Read<uint>(30);
				spellInfo.TargetAuraSpell = spellsResult.Read<uint>(31);
				spellInfo.ExcludeCasterAuraSpell = spellsResult.Read<uint>(32);
				spellInfo.ExcludeTargetAuraSpell = spellsResult.Read<uint>(33);
				spellInfo.CasterAuraType = (AuraType)spellsResult.Read<int>(34);
				spellInfo.TargetAuraType = (AuraType)spellsResult.Read<int>(35);
				spellInfo.ExcludeCasterAuraType = (AuraType)spellsResult.Read<int>(36);
				spellInfo.ExcludeTargetAuraType = (AuraType)spellsResult.Read<int>(37);
				spellInfo.CastTimeEntry = CliDB.SpellCastTimesStorage.LookupByKey(spellsResult.Read<uint>(38));
				spellInfo.RecoveryTime = spellsResult.Read<uint>(39);
				spellInfo.CategoryRecoveryTime = spellsResult.Read<uint>(40);
				spellInfo.StartRecoveryCategory = spellsResult.Read<uint>(41);
				spellInfo.StartRecoveryTime = spellsResult.Read<uint>(42);
				spellInfo.InterruptFlags = (SpellInterruptFlags)spellsResult.Read<uint>(43);
				spellInfo.AuraInterruptFlags = (SpellAuraInterruptFlags)spellsResult.Read<uint>(44);
				spellInfo.AuraInterruptFlags2 = (SpellAuraInterruptFlags2)spellsResult.Read<uint>(45);
				spellInfo.ChannelInterruptFlags = (SpellAuraInterruptFlags)spellsResult.Read<uint>(46);
				spellInfo.ChannelInterruptFlags2 = (SpellAuraInterruptFlags2)spellsResult.Read<uint>(47);
				spellInfo.ProcFlags = new ProcFlagsInit(spellsResult.Read<int>(48), spellsResult.Read<int>(49));
				spellInfo.ProcChance = spellsResult.Read<uint>(50);
				spellInfo.ProcCharges = spellsResult.Read<uint>(51);
				spellInfo.ProcCooldown = spellsResult.Read<uint>(52);
				spellInfo.ProcBasePpm = spellsResult.Read<float>(53);
				spellInfo.MaxLevel = spellsResult.Read<uint>(54);
				spellInfo.BaseLevel = spellsResult.Read<uint>(55);
				spellInfo.SpellLevel = spellsResult.Read<uint>(56);
				spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(spellsResult.Read<uint>(57));
				spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(spellsResult.Read<uint>(58));
				spellInfo.Speed = spellsResult.Read<float>(59);
				spellInfo.LaunchDelay = spellsResult.Read<float>(60);
				spellInfo.StackAmount = spellsResult.Read<uint>(61);
				spellInfo.EquippedItemClass = (ItemClass)spellsResult.Read<int>(62);
				spellInfo.EquippedItemSubClassMask = spellsResult.Read<int>(63);
				spellInfo.EquippedItemInventoryTypeMask = spellsResult.Read<int>(64);
				spellInfo.ContentTuningId = spellsResult.Read<uint>(65);
				spellInfo.ConeAngle = spellsResult.Read<float>(67);
				spellInfo.Width = spellsResult.Read<float>(68);
				spellInfo.MaxTargetLevel = spellsResult.Read<uint>(69);
				spellInfo.MaxAffectedTargets = spellsResult.Read<uint>(70);
				spellInfo.SpellFamilyName = (SpellFamilyNames)spellsResult.Read<uint>(71);
				spellInfo.SpellFamilyFlags = new FlagArray128(spellsResult.Read<uint>(72), spellsResult.Read<uint>(73), spellsResult.Read<uint>(74), spellsResult.Read<uint>(75));
				spellInfo.DmgClass = (SpellDmgClass)spellsResult.Read<uint>(76);
				spellInfo.PreventionType = (SpellPreventionType)spellsResult.Read<uint>(77);
				spellInfo.RequiredAreasId = spellsResult.Read<int>(78);
				spellInfo.SchoolMask = (SpellSchoolMask)spellsResult.Read<uint>(79);
				spellInfo.ChargeCategoryId = spellsResult.Read<uint>(80);

				AddSpellInfo(spellInfo);
			} while (spellsResult.NextRow());

		Log.outInfo(LogFilter.ServerLoading, $"Loaded {_serversideSpellNames.Count} serverside spells {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void LoadSpellInfoCustomAttributes()
	{
		var oldMSTime = Time.MSTime;
		var oldMSTime2 = oldMSTime;

		var result = DB.World.Query("SELECT entry, attributes FROM spell_custom_attr");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell custom attributes from DB. DB table `spell_custom_attr` is empty.");
		}
		else
		{
			uint count = 0;

			do
			{
				var spellId = result.Read<uint>(0);
				var attributes = result.Read<uint>(1);

				var spells = _GetSpellInfo(spellId);

				if (spells.Empty())
				{
					Log.outError(LogFilter.Sql, "Table `spell_custom_attr` has wrong spell (entry: {0}), ignored.", spellId);

					continue;
				}

				foreach (var spellInfo in spells.Values)
				{
					if (attributes.HasAnyFlag((uint)SpellCustomAttributes.ShareDamage))
						if (!spellInfo.HasEffect(SpellEffectName.SchoolDamage))
						{
							Log.outError(LogFilter.Sql, "Spell {0} listed in table `spell_custom_attr` with SPELL_ATTR0_CU_SHARE_DAMAGE has no SPELL_EFFECT_SCHOOL_DAMAGE, ignored.", spellId);

							continue;
						}

					spellInfo.AttributesCu |= (SpellCustomAttributes)attributes;
				}

				++count;
			} while (result.NextRow());

			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell custom attributes from DB in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime2));
		}

		List<uint> talentSpells = new();

		foreach (var talentInfo in CliDB.TalentStorage.Values)
			talentSpells.Add(talentInfo.SpellID);

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
			{
				foreach (var spellEffectInfo in spellInfo.Effects)
				{
					// all bleed effects and spells ignore armor
					if ((spellInfo.GetEffectMechanicMask(spellEffectInfo.EffectIndex) & (1ul << (int)Mechanics.Bleed)) != 0)
						spellInfo.AttributesCu |= SpellCustomAttributes.IgnoreArmor;

					switch (spellEffectInfo.ApplyAuraName)
					{
						case AuraType.ModPossess:
						case AuraType.ModConfuse:
						case AuraType.ModCharm:
						case AuraType.AoeCharm:
						case AuraType.ModFear:
						case AuraType.ModStun:
							spellInfo.AttributesCu |= SpellCustomAttributes.AuraCC;

							break;
					}

					switch (spellEffectInfo.ApplyAuraName)
					{
						case AuraType.OpenStable: // No point in saving this, since the stable dialog can't be open on aura load anyway.
						// Auras that require both caster & target to be in world cannot be saved
						case AuraType.ControlVehicle:
						case AuraType.BindSight:
						case AuraType.ModPossess:
						case AuraType.ModPossessPet:
						case AuraType.ModCharm:
						case AuraType.AoeCharm:
						// Controlled by Battleground
						case AuraType.BattleGroundPlayerPosition:
						case AuraType.BattleGroundPlayerPositionFactional:
							spellInfo.AttributesCu |= SpellCustomAttributes.AuraCannotBeSaved;

							break;
					}

					switch (spellEffectInfo.Effect)
					{
						case SpellEffectName.SchoolDamage:
						case SpellEffectName.HealthLeech:
						case SpellEffectName.Heal:
						case SpellEffectName.WeaponDamageNoSchool:
						case SpellEffectName.WeaponPercentDamage:
						case SpellEffectName.WeaponDamage:
						case SpellEffectName.PowerBurn:
						case SpellEffectName.HealMechanical:
						case SpellEffectName.NormalizedWeaponDmg:
						case SpellEffectName.HealPct:
						case SpellEffectName.DamageFromMaxHealthPCT:
							spellInfo.AttributesCu |= SpellCustomAttributes.CanCrit;

							break;
					}

					switch (spellEffectInfo.Effect)
					{
						case SpellEffectName.SchoolDamage:
						case SpellEffectName.WeaponDamage:
						case SpellEffectName.WeaponDamageNoSchool:
						case SpellEffectName.NormalizedWeaponDmg:
						case SpellEffectName.WeaponPercentDamage:
						case SpellEffectName.Heal:
							spellInfo.AttributesCu |= SpellCustomAttributes.DirectDamage;

							break;
						case SpellEffectName.PowerDrain:
						case SpellEffectName.PowerBurn:
						case SpellEffectName.HealMaxHealth:
						case SpellEffectName.HealthLeech:
						case SpellEffectName.HealPct:
						case SpellEffectName.EnergizePct:
						case SpellEffectName.Energize:
						case SpellEffectName.HealMechanical:
							spellInfo.AttributesCu |= SpellCustomAttributes.NoInitialThreat;

							break;
						case SpellEffectName.Charge:
						case SpellEffectName.ChargeDest:
						case SpellEffectName.Jump:
						case SpellEffectName.JumpDest:
						case SpellEffectName.LeapBack:
							spellInfo.AttributesCu |= SpellCustomAttributes.Charge;

							break;
						case SpellEffectName.Pickpocket:
							spellInfo.AttributesCu |= SpellCustomAttributes.PickPocket;

							break;
						case SpellEffectName.EnchantItem:
						case SpellEffectName.EnchantItemTemporary:
						case SpellEffectName.EnchantItemPrismatic:
						case SpellEffectName.EnchantHeldItem:
						{
							// only enchanting profession enchantments procs can stack
							if (IsPartOfSkillLine(SkillType.Enchanting, spellInfo.Id))
							{
								var enchantId = (uint)spellEffectInfo.MiscValue;
								var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);

								if (enchant == null)
									break;

								for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
								{
									if (enchant.Effect[s] != ItemEnchantmentType.CombatSpell)
										continue;

									foreach (var procInfo in _GetSpellInfo(enchant.EffectArg[s]).Values)
									{
										// if proced directly from enchantment, not via proc aura
										// NOTE: Enchant Weapon - Blade Ward also has proc aura spell and is proced directly
										// however its not expected to stack so this check is good
										if (procInfo.HasAura(AuraType.ProcTriggerSpell))
											continue;

										procInfo.AttributesCu |= SpellCustomAttributes.EnchantProc;
									}
								}
							}

							break;
						}
					}
				}

				// spells ignoring hit result should not be binary
				if (!spellInfo.HasAttribute(SpellAttr3.AlwaysHit))
				{
					var setFlag = false;

					foreach (var spellEffectInfo in spellInfo.Effects)
						if (spellEffectInfo.IsEffect())
						{
							switch (spellEffectInfo.Effect)
							{
								case SpellEffectName.SchoolDamage:
								case SpellEffectName.WeaponDamage:
								case SpellEffectName.WeaponDamageNoSchool:
								case SpellEffectName.NormalizedWeaponDmg:
								case SpellEffectName.WeaponPercentDamage:
								case SpellEffectName.TriggerSpell:
								case SpellEffectName.TriggerSpellWithValue:
									break;
								case SpellEffectName.PersistentAreaAura:
								case SpellEffectName.ApplyAura:
								case SpellEffectName.ApplyAreaAuraParty:
								case SpellEffectName.ApplyAreaAuraRaid:
								case SpellEffectName.ApplyAreaAuraFriend:
								case SpellEffectName.ApplyAreaAuraEnemy:
								case SpellEffectName.ApplyAreaAuraPet:
								case SpellEffectName.ApplyAreaAuraOwner:
								case SpellEffectName.ApplyAuraOnPet:
								case SpellEffectName.ApplyAreaAuraSummons:
								case SpellEffectName.ApplyAreaAuraPartyNonrandom:
								{
									if (spellEffectInfo.ApplyAuraName == AuraType.PeriodicDamage ||
										spellEffectInfo.ApplyAuraName == AuraType.PeriodicDamagePercent ||
										spellEffectInfo.ApplyAuraName == AuraType.PeriodicDummy ||
										spellEffectInfo.ApplyAuraName == AuraType.PeriodicLeech ||
										spellEffectInfo.ApplyAuraName == AuraType.PeriodicHealthFunnel ||
										spellEffectInfo.ApplyAuraName == AuraType.PeriodicDummy)
										break;

									goto default;
								}
								default:
								{
									// No value and not interrupt cast or crowd control without SPELL_ATTR0_UNAFFECTED_BY_INVULNERABILITY flag
									if (spellEffectInfo.CalcValue() == 0 && !((spellEffectInfo.Effect == SpellEffectName.InterruptCast || spellInfo.HasAttribute(SpellCustomAttributes.AuraCC)) && !spellInfo.HasAttribute(SpellAttr0.NoImmunities)))
										break;

									// Sindragosa Frost Breath
									if (spellInfo.Id == 69649 || spellInfo.Id == 71056 || spellInfo.Id == 71057 || spellInfo.Id == 71058 || spellInfo.Id == 73061 || spellInfo.Id == 73062 || spellInfo.Id == 73063 || spellInfo.Id == 73064)
										break;

									// Frostbolt
									if (spellInfo.SpellFamilyName == SpellFamilyNames.Mage && spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x20u))
										break;

									// Frost Fever
									if (spellInfo.Id == 55095)
										break;

									// Haunt
									if (spellInfo.SpellFamilyName == SpellFamilyNames.Warlock && spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x40000u))
										break;

									setFlag = true;

									break;
								}
							}

							if (setFlag)
							{
								spellInfo.AttributesCu |= SpellCustomAttributes.BinarySpell;

								break;
							}
						}
				}

				// Remove normal school mask to properly calculate damage
				if (spellInfo.SchoolMask.HasAnyFlag(SpellSchoolMask.Normal) && spellInfo.SchoolMask.HasAnyFlag(SpellSchoolMask.Magic))
				{
					spellInfo.SchoolMask &= ~SpellSchoolMask.Normal;
					spellInfo.AttributesCu |= SpellCustomAttributes.SchoolmaskNormalWithMagic;
				}

				spellInfo.InitializeSpellPositivity();

				if (talentSpells.Contains(spellInfo.Id))
					spellInfo.AttributesCu |= SpellCustomAttributes.IsTalent;

				if (MathFunctions.fuzzyNe(spellInfo.Width, 0.0f))
					spellInfo.AttributesCu |= SpellCustomAttributes.ConeLine;

				switch (spellInfo.SpellFamilyName)
				{
					case SpellFamilyNames.Warrior:
						// Shout / Piercing Howl
						if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x20000u) /* || spellInfo.SpellFamilyFlags[1] & 0x20*/)
							spellInfo.AttributesCu |= SpellCustomAttributes.AuraCC;

						break;
					case SpellFamilyNames.Druid:
						// Roar
						if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x8u))
							spellInfo.AttributesCu |= SpellCustomAttributes.AuraCC;

						break;
					case SpellFamilyNames.Generic:
						// Stoneclaw Totem effect
						if (spellInfo.Id == 5729)
							spellInfo.AttributesCu |= SpellCustomAttributes.AuraCC;

						break;
					default:
						break;
				}

				spellInfo.InitializeExplicitTargetMask();

				if (spellInfo.Speed > 0.0f)
				{
					bool visualNeedsAmmo(SpellXSpellVisualRecord spellXspellVisual)
					{
						var spellVisual = CliDB.SpellVisualStorage.LookupByKey(spellXspellVisual.SpellVisualID);

						if (spellVisual == null)
							return false;

						var spellVisualMissiles = Global.DB2Mgr.GetSpellVisualMissiles(spellVisual.SpellVisualMissileSetID);

						if (spellVisualMissiles.Empty())
							return false;

						foreach (var spellVisualMissile in spellVisualMissiles)
						{
							var spellVisualEffectName = CliDB.SpellVisualEffectNameStorage.LookupByKey(spellVisualMissile.SpellVisualEffectNameID);

							if (spellVisualEffectName == null)
								continue;

							var type = (SpellVisualEffectNameType)spellVisualEffectName.Type;

							if (type == SpellVisualEffectNameType.UnitAmmoBasic || type == SpellVisualEffectNameType.UnitAmmoPreferred)
								return true;
						}

						return false;
					}

					foreach (var spellXspellVisual in spellInfo.SpellVisuals)
						if (visualNeedsAmmo(spellXspellVisual))
						{
							spellInfo.AttributesCu |= SpellCustomAttributes.NeedsAmmoData;

							break;
						}
				}

				// Saving to DB happens before removing from world - skip saving these auras
				if (spellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.LeaveWorld))
					spellInfo.AttributesCu |= SpellCustomAttributes.AuraCannotBeSaved;
			}

		// addition for binary spells, omit spells triggering other spells
		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
			{
				if (!spellInfo.HasAttribute(SpellCustomAttributes.BinarySpell))
				{
					var allNonBinary = true;
					var overrideAttr = false;

					foreach (var spellEffectInfo in spellInfo.Effects)
						if (spellEffectInfo.IsAura() && spellEffectInfo.TriggerSpell != 0)
							switch (spellEffectInfo.ApplyAuraName)
							{
								case AuraType.PeriodicTriggerSpell:
								case AuraType.PeriodicTriggerSpellFromClient:
								case AuraType.PeriodicTriggerSpellWithValue:
									var triggerSpell = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

									if (triggerSpell != null)
									{
										overrideAttr = true;

										if (triggerSpell.HasAttribute(SpellCustomAttributes.BinarySpell))
											allNonBinary = false;
									}

									break;
								default:
									break;
							}

					if (overrideAttr && allNonBinary)
						spellInfo.AttributesCu &= ~SpellCustomAttributes.BinarySpell;
				}

				// remove attribute from spells that can't crit
				if (spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
					if (spellInfo.HasAttribute(SpellAttr2.CantCrit))
						spellInfo.AttributesCu &= ~SpellCustomAttributes.CanCrit;
			}

		// add custom attribute to liquid auras
		foreach (var liquid in CliDB.LiquidTypeStorage.Values)
			if (liquid.SpellID != 0)
				foreach (var spellInfo in _GetSpellInfo(liquid.SpellID).Values)
					spellInfo.AttributesCu |= SpellCustomAttributes.AuraCannotBeSaved;

		Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo custom attributes in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
	}

	void ApplySpellFix(int[] spellIds, Action<SpellInfo> fix)
	{
		foreach (uint spellId in spellIds)
		{
			var range = _GetSpellInfo(spellId);

			if (range.Empty())
			{
				Log.outError(LogFilter.ServerLoading, $"Spell info correction specified for non-existing spell {spellId}");

				continue;
			}

			foreach (var spellInfo in range.Values)
				fix(spellInfo);
		}
	}

	void ApplySpellEffectFix(SpellInfo spellInfo, int effectIndex, Action<SpellEffectInfo> fix)
	{
		if (spellInfo.Effects.Count <= effectIndex)
		{
			Log.outError(LogFilter.ServerLoading, $"Spell effect info correction specified for non-existing effect {effectIndex} of spell {spellInfo.Id}");

			return;
		}

		fix(spellInfo.GetEffect(effectIndex));
	}

	public void LoadSpellInfosLateFix()
	{
		foreach (var fix in IOHelpers.GetAllObjectsFromAssemblies<ISpellManagerSpellLateFix>(Path.Combine(AppContext.BaseDirectory, "Scripts")))
			ApplySpellFix(fix.SpellIds, fix.ApplySpellFix);
	}

	public void LoadSpellInfoCorrections()
	{
		var oldMSTime = Time.MSTime;

		foreach (var fix in IOHelpers.GetAllObjectsFromAssemblies<ISpellManagerSpellFix>(Path.Combine(AppContext.BaseDirectory, "Scripts")))
			ApplySpellFix(fix.SpellIds, fix.ApplySpellFix);

		// Some spells have no amplitude set
		{
			ApplySpellFix(new[]
						{
							6727,     // Poison Mushroom
							7331,     // Healing Aura (TEST) (Rank 1)
							/*
							30400, // Nether Beam - Perseverance
								Blizzlike to have it disabled? DBC says:
								"This is currently turned off to increase performance. Enable this to make it fire more frequently."
							*/ 34589, // Dangerous Water
							52562,    // Arthas Zombie Catcher
							57550,    // Tirion Aggro
							65755
						},
						spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.ApplyAuraPeriod = 1 * Time.InMilliseconds; }); });

			ApplySpellFix(new[]
						{
							24707, // Food
							26263, // Dim Sum
							29055  // Refreshing Red Apple
						},
						spellInfo =>
						{
							ApplySpellEffectFix(spellInfo,
												1,
												spellEffectInfo =>
												{
													spellEffectInfo.ApplyAuraPeriod = 1 * Time.InMilliseconds;
													;
												});
						});

			// Karazhan - Chess NPC AI, action timer
			ApplySpellFix(new[]
						{
							37504
						},
						spellInfo =>
						{
							ApplySpellEffectFix(spellInfo,
												1,
												spellEffectInfo =>
												{
													spellEffectInfo.ApplyAuraPeriod = 5 * Time.InMilliseconds;
													;
												});
						});

			// Vomit
			ApplySpellFix(new[]
						{
							43327
						},
						spellInfo =>
						{
							ApplySpellEffectFix(spellInfo,
												1,
												spellEffectInfo =>
												{
													spellEffectInfo.ApplyAuraPeriod = 1 * Time.InMilliseconds;
													;
												});
						});
		}

		// specific code for cases with no trigger spell provided in field
		{
			// Brood Affliction: Bronze
			ApplySpellFix(new[]
						{
							23170
						},
						spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TriggerSpell = 23171; }); });

			// Feed Captured Animal
			ApplySpellFix(new[]
						{
							29917
						},
						spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TriggerSpell = 29916; }); });

			// Remote Toy
			ApplySpellFix(new[]
						{
							37027
						},
						spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TriggerSpell = 37029; }); });

			// Eye of Grillok
			ApplySpellFix(new[]
						{
							38495
						},
						spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TriggerSpell = 38530; }); });

			// Tear of Azzinoth Summon Channel - it's not really supposed to do anything, and this only prevents the console spam
			ApplySpellFix(new[]
						{
							39857
						},
						spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TriggerSpell = 39856; }); });

			// Personalized Weather
			ApplySpellFix(new[]
						{
							46736
						},
						spellInfo =>
						{
							ApplySpellEffectFix(spellInfo,
												0,
												spellEffectInfo =>
												{
													spellEffectInfo.TriggerSpell = 46737;
													spellEffectInfo.ApplyAuraName = AuraType.PeriodicTriggerSpell;
												});
						});
		}

		// Allows those to crit
		ApplySpellFix(new[]
					{
						379,   // Earth Shield
						71607, // Item - Bauble of True Blood 10m
						71646, // Item - Bauble of True Blood 25m
						71610, // Item - Althor's Abacus trigger 10m
						71641  // Item - Althor's Abacus trigger 25m
					},
					spellInfo =>
					{
						// We need more spells to find a general way (if there is any)
						spellInfo.DmgClass = SpellDmgClass.Magic;
					});

		ApplySpellFix(new[]
					{
						63026, // Summon Aspirant Test NPC (HACK: Target shouldn't be changed)
						63137  // Summon Valiant Test (HACK: Target shouldn't be changed; summon position should be untied from spell destination)
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.DestDb); }); });

		// Summon Skeletons
		ApplySpellFix(new[]
					{
						52611, 52612
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.MiscValueB = 64; }); });

		ApplySpellFix(new[]
					{
						40244, // Simon Game Visual
						40245, // Simon Game Visual
						40246, // Simon Game Visual
						40247, // Simon Game Visual
						42835  // Spout, remove damage effect, only anim is needed
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.None; }); });

		ApplySpellFix(new[]
					{
						63665, // Charge (Argent Tournament emote on riders)
						31298, // Sleep (needs target selection script)
						51904, // Summon Ghouls On Scarlet Crusade (this should use conditions table, script for this spell needs to be fixed)
						68933, // Wrath of Air Totem rank 2 (Aura)
						29200  // Purify Helboar Meat
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitCaster);
												spellEffectInfo.TargetB = new SpellImplicitTargetInfo();
											});
					});

		ApplySpellFix(new[]
					{
						56690, // Thrust Spear
						60586, // Mighty Spear Thrust
						60776, // Claw Swipe
						60881, // Fatal Strike
						60864  // Jaws of Death
					},
					spellInfo => { spellInfo.AttributesEx4 |= SpellAttr4.IgnoreDamageTakenModifiers; });

		// Howl of Azgalor
		ApplySpellFix(new[]
					{
						31344
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards100); // 100yards instead of 50000?!
											});
					});

		ApplySpellFix(new[]
					{
						42818, // Headless Horseman - Wisp Flight Port
						42821  // Headless Horseman - Wisp Flight Missile
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(6); // 100 yards
					});

		// They Must Burn Bomb Aura (self)
		ApplySpellFix(new[]
					{
						36350
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.TriggerSpell = 36325; // They Must Burn Bomb Drop (DND)
											});
					});

		ApplySpellFix(new[]
					{
						31347, // Doom
						36327, // Shoot Arcane Explosion Arrow
						39365, // Thundering Storm
						41071, // Raise Dead (HACK)
						42442, // Vengeance Landing Cannonfire
						42611, // Shoot
						44978, // Wild Magic
						45001, // Wild Magic
						45002, // Wild Magic
						45004, // Wild Magic
						45006, // Wild Magic
						45010, // Wild Magic
						45761, // Shoot Gun
						45863, // Cosmetic - Incinerate to Random Target
						48246, // Ball of Flame
						41635, // Prayer of Mending
						44869, // Spectral Blast
						45027, // Revitalize
						45976, // Muru Portal Channel
						52124, // Sky Darkener Assault
						52479, // Gift of the Harvester
						61588, // Blazing Harpoon
						55479, // Force Obedience
						28560, // Summon Blizzard (Sapphiron)
						53096, // Quetz'lun's Judgment
						70743, // AoD Special
						70614, // AoD Special - Vegard
						4020,  // Safirdrang's Chill
						52438, // Summon Skittering Swarmer (Force Cast)
						52449, // Summon Skittering Infector (Force Cast)
						53609, // Summon Anub'ar Assassin (Force Cast)
						53457, // Summon Impale Trigger (AoE)
						45907, // Torch Target Picker
						52953, // Torch
						58121, // Torch
						43109, // Throw Torch
						58552, // Return to Orgrimmar
						58533, // Return to Stormwind
						21855, // Challenge Flag
						38762, // Force of Neltharaku
						51122, // Fierce Lightning Stike
						71848, // Toxic Wasteling Find Target
						36146, // Chains of Naberius
						33711, // Murmur's Touch
						38794  // Murmur's Touch
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 1; });

		ApplySpellFix(new[]
					{
						36384, // Skartax Purple Beam
						47731  // Critter
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 2; });

		ApplySpellFix(new[]
					{
						28542, // Life Drain - Sapphiron
						29213, // Curse of the Plaguebringer - Noth
						29576, // Multi-Shot
						37790, // Spread Shot
						39992, // Needle Spine
						40816, // Saber Lash
						41303, // Soul Drain
						41376, // Spite
						45248, // Shadow Blades
						46771, // Flame Sear
						66588  // Flaming Spear
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 3; });

		ApplySpellFix(new[]
					{
						38310, // Multi-Shot
						53385  // Divine Storm (Damage)
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 4; });

		ApplySpellFix(new[]
					{
						42005, // Bloodboil
						38296, // Spitfire Totem
						37676, // Insidious Whisper
						46008, // Negative Energy
						45641, // Fire Bloom
						55665, // Life Drain - Sapphiron (H)
						28796, // Poison Bolt Volly - Faerlina
						37135  // Domination
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 5; });

		ApplySpellFix(new[]
					{
						40827, // Sinful Beam
						40859, // Sinister Beam
						40860, // Vile Beam
						40861, // Wicked Beam
						54098, // Poison Bolt Volly - Faerlina (H)
						54835  // Curse of the Plaguebringer - Noth (H)
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 10; });

		// Unholy Frenzy
		ApplySpellFix(new[]
					{
						50312
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 15; });

		// Fingers of Frost
		ApplySpellFix(new[]
					{
						44544
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.SpellClassMask[0] |= 0x20000; }); });

		ApplySpellFix(new[]
					{
						52212, // Death and Decay
						41485, // Deadly Poison - Black Temple
						41487  // Envenom - Black Temple
					},
					spellInfo => { spellInfo.AttributesEx6 |= SpellAttr6.IgnorePhaseShift; });

		// Oscillation Field
		ApplySpellFix(new[]
					{
						37408
					},
					spellInfo => { spellInfo.AttributesEx3 |= SpellAttr3.DotStackingRule; });

		// Crafty's Ultra-Advanced Proto-Typical Shortening Blaster
		ApplySpellFix(new[]
					{
						51912
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.ApplyAuraPeriod = 3000; }); });

		// Nether Portal - Perseverence
		ApplySpellFix(new[]
					{
						30421
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 2, spellEffectInfo => { spellEffectInfo.BasePoints += 30000; }); });

		// Parasitic Shadowfiend Passive
		ApplySpellFix(new[]
					{
						41913
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.ApplyAuraName = AuraType.Dummy; // proc debuff, and summon infinite fiends
											});
					});

		ApplySpellFix(new[]
					{
						27892, // To Anchor 1
						27928, // To Anchor 1
						27935, // To Anchor 1
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards10); }); });

		// Wrath of the Plaguebringer
		ApplySpellFix(new[]
					{
						29214, 54836
					},
					spellInfo =>
					{
						// target allys instead of enemies, target A is src_caster, spells with effect like that have ally target
						// this is the only known exception, probably just wrong data
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetB = new SpellImplicitTargetInfo(Targets.UnitSrcAreaAlly); });
						ApplySpellEffectFix(spellInfo, 1, spellEffectInfo => { spellEffectInfo.TargetB = new SpellImplicitTargetInfo(Targets.UnitSrcAreaAlly); });
					});

		// Earthbind Totem (instant pulse)
		ApplySpellFix(new[]
					{
						6474
					},
					spellInfo => { spellInfo.AttributesEx5 |= SpellAttr5.ExtraInitialPeriod; });

		ApplySpellFix(new[]
					{
						70728, // Exploit Weakness (needs target selection script)
						70840  // Devious Minds (needs target selection script)
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitCaster);
												spellEffectInfo.TargetB = new SpellImplicitTargetInfo(Targets.UnitPet);
											});
					});

		// Ride Carpet
		ApplySpellFix(new[]
					{
						45602
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.BasePoints = 0; // force seat 0, vehicle doesn't have the required seat flags for "no seat specified (-1)"
											});
					});

		// Easter Lay Noblegarden Egg Aura - Interrupt flags copied from aura which this aura is linked with
		ApplySpellFix(new[]
					{
						61719
					},
					spellInfo => { spellInfo.AuraInterruptFlags = SpellAuraInterruptFlags.HostileActionReceived | SpellAuraInterruptFlags.Damage; });

		ApplySpellFix(new[]
					{
						71838, // Drain Life - Bryntroll Normal
						71839  // Drain Life - Bryntroll Heroic
					},
					spellInfo => { spellInfo.AttributesEx2 |= SpellAttr2.CantCrit; });

		ApplySpellFix(new[]
					{
						51597, // Summon Scourged Captive
						56606, // Ride Jokkum
						61791  // Ride Vehicle (Yogg-Saron)
					},
					spellInfo =>
					{
						/// @todo: remove this when basepoints of all Ride Vehicle auras are calculated correctly
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.BasePoints = 1; });
					});

		// Summon Scourged Captive
		ApplySpellFix(new[]
					{
						51597
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.Scaling.Variance = 0.0f; }); });

		// Black Magic
		ApplySpellFix(new[]
					{
						59630
					},
					spellInfo => { spellInfo.Attributes |= SpellAttr0.Passive; });

		// Paralyze
		ApplySpellFix(new[]
					{
						48278
					},
					spellInfo => { spellInfo.AttributesEx3 |= SpellAttr3.DotStackingRule; });

		ApplySpellFix(new[]
					{
						51798, // Brewfest - Relay Race - Intro - Quest Complete
						47134  // Quest Complete
					},
					spellInfo =>
					{
						//! HACK: This spell break quest complete for alliance and on retail not used
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.None; });
					});

		// Siege Cannon (Tol Barad)
		ApplySpellFix(new[]
					{
						85123
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards200);
												spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitSrcAreaEntry);
											});
					});

		// Gathering Storms
		ApplySpellFix(new[]
					{
						198300
					},
					spellInfo =>
					{
						spellInfo.ProcCharges = 1; // override proc charges, has 0 (unlimited) in db2
					});

		ApplySpellFix(new[]
					{
						15538, // Gout of Flame
						42490, // Energized!
						42492, // Cast Energized
						43115  // Plague Vial
					},
					spellInfo => { spellInfo.AttributesEx |= SpellAttr1.NoThreat; });

		// Test Ribbon Pole Channel
		ApplySpellFix(new[]
					{
						29726
					},
					spellInfo => { spellInfo.ChannelInterruptFlags &= ~SpellAuraInterruptFlags.Action; });

		// Sic'em
		ApplySpellFix(new[]
					{
						42767
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitNearbyEntry); }); });

		// Burn Body
		ApplySpellFix(new[]
					{
						42793
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											2,
											spellEffectInfo =>
											{
												spellEffectInfo.MiscValue = 24008; // Fallen Combatant
											});
					});

		// Gift of the Naaru (priest and monk variants)
		ApplySpellFix(new[]
					{
						59544, 121093
					},
					spellInfo => { spellInfo.SpellFamilyFlags[2] = 0x80000000; });

		ApplySpellFix(new[]
					{
						50661, // Weakened Resolve
						68979, // Unleashed Souls
						48714, // Compelled
						7853,  // The Art of Being a Water Terror: Force Cast on Player
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(13); // 50000yd
					});

		ApplySpellFix(new[]
					{
						44327, // Trained Rock Falcon/Hawk Hunting
						44408  // Trained Rock Falcon/Hawk Hunting
					},
					spellInfo => { spellInfo.Speed = 0.0f; });

		// Summon Corpse Scarabs
		ApplySpellFix(new[]
					{
						28864, 29105
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards10); }); });

		// Tag Greater Felfire Diemetradon
		ApplySpellFix(new[]
					{
						37851, // Tag Greater Felfire Diemetradon
						37918  // Arcano-pince
					},
					spellInfo => { spellInfo.RecoveryTime = 3000; });

		// Jormungar Strike
		ApplySpellFix(new[]
					{
						56513
					},
					spellInfo => { spellInfo.RecoveryTime = 2000; });

		ApplySpellFix(new[]
					{
						54997, // Cast Net (tooltip says 10s but sniffs say 6s)
						56524  // Acid Breath
					},
					spellInfo => { spellInfo.RecoveryTime = 6000; });

		ApplySpellFix(new[]
					{
						47911, // EMP
						48620, // Wing Buffet
						51752  // Stampy's Stompy-Stomp
					},
					spellInfo => { spellInfo.RecoveryTime = 10000; });

		ApplySpellFix(new[]
					{
						37727, // Touch of Darkness
						54996  // Ice Slick (tooltip says 20s but sniffs say 12s)
					},
					spellInfo => { spellInfo.RecoveryTime = 12000; });

		// Signal Helmet to Attack
		ApplySpellFix(new[]
					{
						51748
					},
					spellInfo => { spellInfo.RecoveryTime = 15000; });

		// Charge
		ApplySpellFix(new[]
					{
						51756, // Charge
						37919, //Arcano-dismantle
						37917  //Arcano-Cloak
					},
					spellInfo => { spellInfo.RecoveryTime = 20000; });

		// Summon Frigid Bones
		ApplySpellFix(new[]
					{
						53525
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(4); // 2 minutes
					});

		// Dark Conclave Ritualist Channel
		ApplySpellFix(new[]
					{
						38469
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(6); // 100yd
					});

		// Chrono Shift (enemy slow part)
		ApplySpellFix(new[]
					{
						236299
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(6); // 100yd
					});

		//
		// VIOLET HOLD SPELLS
		//
		// Water Globule (Ichoron)
		ApplySpellFix(new[]
					{
						54258, 54264, 54265, 54266, 54267
					},
					spellInfo =>
					{
						// in 3.3.5 there is only one radius in dbc which is 0 yards in this case
						// use max radius from 4.3.4
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards25); });
					});
		// ENDOF VIOLET HOLD

		//
		// ULDUAR SPELLS
		//
		// Pursued (Flame Leviathan)
		ApplySpellFix(new[]
					{
						62374
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards50000); // 50000yd
											});
					});

		// Focused Eyebeam Summon Trigger (Kologarn)
		ApplySpellFix(new[]
					{
						63342
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 1; });

		ApplySpellFix(new[]
					{
						65584, // Growth of Nature (Freya)
						64381  // Strength of the Pack (Auriaya)
					},
					spellInfo => { spellInfo.AttributesEx3 |= SpellAttr3.DotStackingRule; });

		ApplySpellFix(new[]
					{
						63018, // Searing Light (XT-002)
						65121, // Searing Light (25m) (XT-002)
						63024, // Gravity Bomb (XT-002)
						64234  // Gravity Bomb (25m) (XT-002)
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 1; });

		ApplySpellFix(new[]
					{
						64386, // Terrifying Screech (Auriaya)
						64389, // Sentinel Blast (Auriaya)
						64678  // Sentinel Blast (Auriaya)
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(28); // 5 seconds, wrong DBC data?
					});

		// Potent Pheromones (Freya)
		ApplySpellFix(new[]
					{
						64321
					},
					spellInfo =>
					{
						// spell should dispel area aura, but doesn't have the attribute
						// may be db data bug, or blizz may keep reapplying area auras every update with checking immunity
						// that will be clear if we get more spells with problem like this
						spellInfo.AttributesEx |= SpellAttr1.ImmunityPurgesEffect;
					});

		// Blizzard (Thorim)
		ApplySpellFix(new[]
					{
						62576, 62602
					},
					spellInfo =>
					{
						// DBC data is wrong for 0, it's a different dynobject target than 1
						// Both effects should be shared by the same DynObject
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.DestCasterLeft); });
					});

		// Spinning Up (Mimiron)
		ApplySpellFix(new[]
					{
						63414
					},
					spellInfo =>
					{
						spellInfo.ChannelInterruptFlags = SpellAuraInterruptFlags.None;
						spellInfo.ChannelInterruptFlags2 = SpellAuraInterruptFlags2.None;
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetB = new SpellImplicitTargetInfo(Targets.UnitCaster); });
					});

		// Rocket Strike (Mimiron)
		ApplySpellFix(new[]
					{
						63036
					},
					spellInfo => { spellInfo.Speed = 0; });

		// Magnetic Field (Mimiron)
		ApplySpellFix(new[]
					{
						64668
					},
					spellInfo => { spellInfo.Mechanic = Mechanics.None; });

		// Empowering Shadows (Yogg-Saron)
		ApplySpellFix(new[]
					{
						64468, 64486
					},
					spellInfo =>
					{
						spellInfo.MaxAffectedTargets = 3; // same for both modes?
					});

		// Cosmic Smash (Algalon the Observer)
		ApplySpellFix(new[]
					{
						62301
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 1; });

		// Cosmic Smash (Algalon the Observer)
		ApplySpellFix(new[]
					{
						64598
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 3; });

		// Cosmic Smash (Algalon the Observer)
		ApplySpellFix(new[]
					{
						62293
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetB = new SpellImplicitTargetInfo(Targets.DestCaster); }); });

		// Cosmic Smash (Algalon the Observer)
		ApplySpellFix(new[]
					{
						62311, 64596
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(6); // 100yd
					});

		ApplySpellFix(new[]
					{
						64014, // Expedition Base Camp Teleport
						64024, // Conservatory Teleport
						64025, // Halls of Invention Teleport
						64028, // Colossal Forge Teleport
						64029, // Shattered Walkway Teleport
						64030, // Antechamber Teleport
						64031, // Scrapyard Teleport
						64032, // Formation Grounds Teleport
						65042  // Prison of Yogg-Saron Teleport
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.DestDb); }); });
		// ENDOF ULDUAR SPELLS

		//
		// TRIAL OF THE CRUSADER SPELLS
		//
		// Infernal Eruption
		ApplySpellFix(new[]
					{
						66258
					},
					spellInfo =>
					{
						// increase duration from 15 to 18 seconds because caster is already
						// unsummoned when spell missile hits the ground so nothing happen in result
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(85);
					});
		// ENDOF TRIAL OF THE CRUSADER SPELLS

		//
		// ICECROWN CITADEL SPELLS
		//
		ApplySpellFix(new[]
					{
						70781, // Light's Hammer Teleport
						70856, // Oratory of the Damned Teleport
						70857, // Rampart of Skulls Teleport
						70858, // Deathbringer's Rise Teleport
						70859, // Upper Spire Teleport
						70860, // Frozen Throne Teleport
						70861  // Sindragosa's Lair Teleport
					},
					spellInfo =>
					{
						// THESE SPELLS ARE WORKING CORRECTLY EVEN WITHOUT THIS HACK
						// THE ONLY REASON ITS HERE IS THAT CURRENT GRID SYSTEM
						// DOES NOT ALLOW FAR OBJECT SELECTION (dist > 333)
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.DestDb); });
					});

		// Shadow's Fate
		ApplySpellFix(new[]
					{
						71169
					},
					spellInfo => { spellInfo.AttributesEx3 |= SpellAttr3.DotStackingRule; });

		// Resistant Skin (Deathbringer Saurfang adds)
		ApplySpellFix(new[]
					{
						72723
					},
					spellInfo =>
					{
						// this spell initially granted Shadow damage immunity, however it was removed but the data was left in client
						ApplySpellEffectFix(spellInfo, 2, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.None; });
					});

		// Coldflame Jets (Traps after Saurfang)
		ApplySpellFix(new[]
					{
						70460
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(1); // 10 seconds
					});

		ApplySpellFix(new[]
					{
						71412, // Green Ooze Summon (Professor Putricide)
						71415  // Orange Ooze Summon (Professor Putricide)
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitTargetAny); }); });

		// Awaken Plagued Zombies
		ApplySpellFix(new[]
					{
						71159
					},
					spellInfo => { spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(21); });

		// Volatile Ooze Beam Protection (Professor Putricide)
		ApplySpellFix(new[]
					{
						70530
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.Effect = SpellEffectName.ApplyAura; // for an unknown reason this was SPELL_EFFECT_APPLY_AREA_AURA_RAID
											});
					});

		// Mutated Strength (Professor Putricide)
		ApplySpellFix(new[]
					{
						71604
					},
					spellInfo =>
					{
						// THIS IS HERE BECAUSE COOLDOWN ON CREATURE PROCS WERE NOT IMPLEMENTED WHEN THE SCRIPT WAS WRITTEN
						ApplySpellEffectFix(spellInfo, 1, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.None; });
					});

		// Unbound Plague (Professor Putricide) (needs target selection script)
		ApplySpellFix(new[]
					{
						70911
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetB = new SpellImplicitTargetInfo(Targets.UnitTargetEnemy); }); });

		// Empowered Flare (Blood Prince Council)
		ApplySpellFix(new[]
					{
						71708
					},
					spellInfo => { spellInfo.AttributesEx3 |= SpellAttr3.IgnoreCasterModifiers; });

		// Swarming Shadows
		ApplySpellFix(new[]
					{
						71266
					},
					spellInfo =>
					{
						spellInfo.RequiredAreasId = 0; // originally, these require area 4522, which is... outside of Icecrown Citadel
					});

		// Corruption
		ApplySpellFix(new[]
					{
						70602
					},
					spellInfo => { spellInfo.AttributesEx3 |= SpellAttr3.DotStackingRule; });

		// Column of Frost (visual marker)
		ApplySpellFix(new[]
					{
						70715
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(32); // 6 seconds (missing)
					});

		// Mana Void (periodic aura)
		ApplySpellFix(new[]
					{
						71085
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(9); // 30 seconds (missing)
					});

		// Summon Suppressor (needs target selection script)
		ApplySpellFix(new[]
					{
						70936
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(157); // 90yd

						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitTargetAny);
												spellEffectInfo.TargetB = new SpellImplicitTargetInfo();
											});
					});

		// Sindragosa's Fury
		ApplySpellFix(new[]
					{
						70598
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.DestDest); }); });

		// Frost Bomb
		ApplySpellFix(new[]
					{
						69846
					},
					spellInfo =>
					{
						spellInfo.Speed = 0.0f; // This spell's summon happens instantly
					});

		// Chilled to the Bone
		ApplySpellFix(new[]
					{
						70106
					},
					spellInfo =>
					{
						spellInfo.AttributesEx3 |= SpellAttr3.IgnoreCasterModifiers;
						spellInfo.AttributesEx6 |= SpellAttr6.IgnoreCasterDamageModifiers;
					});

		// Ice Lock
		ApplySpellFix(new[]
					{
						71614
					},
					spellInfo => { spellInfo.Mechanic = Mechanics.Stun; });

		// Defile
		ApplySpellFix(new[]
					{
						72762
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(559); // 53 seconds
					});

		// Defile
		ApplySpellFix(new[]
					{
						72743
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(22); // 45 seconds
					});

		// Defile
		ApplySpellFix(new[]
					{
						72754
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards200); // 200yd
											});

						ApplySpellEffectFix(spellInfo,
											1,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards200); // 200yd
											});
					});

		// Val'kyr Target Search
		ApplySpellFix(new[]
					{
						69030
					},
					spellInfo => { spellInfo.Attributes |= SpellAttr0.NoImmunities; });

		// Raging Spirit Visual
		ApplySpellFix(new[]
					{
						69198
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(13); // 50000yd
					});

		// Harvest Soul
		ApplySpellFix(new[]
					{
						73655
					},
					spellInfo => { spellInfo.AttributesEx3 |= SpellAttr3.IgnoreCasterModifiers; });

		// Summon Shadow Trap
		ApplySpellFix(new[]
					{
						73540
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(3); // 60 seconds
					});

		// Shadow Trap (visual)
		ApplySpellFix(new[]
					{
						73530
					},
					spellInfo =>
					{
						spellInfo.DurationEntry = CliDB.SpellDurationStorage.LookupByKey(27); // 3 seconds
					});

		// Summon Spirit Bomb
		ApplySpellFix(new[]
					{
						74302
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 2; });

		// Summon Spirit Bomb
		ApplySpellFix(new[]
					{
						73579
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards25); // 25yd
											});
					});

		// Raise Dead
		ApplySpellFix(new[]
					{
						72376
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 3; });

		// Jump
		ApplySpellFix(new[]
					{
						71809
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(5); // 40yd

						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards10); // 10yd
												spellEffectInfo.MiscValue = 190;
											});
					});

		// Broken Frostmourne
		ApplySpellFix(new[]
					{
						72405
					},
					spellInfo =>
					{
						spellInfo.AttributesEx |= SpellAttr1.NoThreat;

						ApplySpellEffectFix(spellInfo,
											1,
											spellEffectInfo =>
											{
												spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards20); // 20yd
											});
					});
		// ENDOF ICECROWN CITADEL SPELLS

		//
		// RUBY SANCTUM SPELLS
		//
		// Soul Consumption
		ApplySpellFix(new[]
					{
						74799
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 1, spellEffectInfo => { spellEffectInfo.RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards12); }); });

		// Twilight Mending
		ApplySpellFix(new[]
					{
						75509
					},
					spellInfo =>
					{
						spellInfo.AttributesEx6 |= SpellAttr6.IgnorePhaseShift;
						spellInfo.AttributesEx2 |= SpellAttr2.IgnoreLineOfSight;
					});

		// Awaken Flames
		ApplySpellFix(new[]
					{
						75888
					},
					spellInfo => { spellInfo.AttributesEx |= SpellAttr1.ExcludeCaster; });
		// ENDOF RUBY SANCTUM SPELLS

		//
		// EYE OF ETERNITY SPELLS
		//
		ApplySpellFix(new[]
					{
						57473, // Arcane Storm bonus explicit visual spell
						57431, // Summon Static Field
						56091, // Flame Spike (Wyrmrest Skytalon)
						56092, // Engulf in Flames (Wyrmrest Skytalon)
						57090, // Revivify (Wyrmrest Skytalon)
						57143  // Life Burst (Wyrmrest Skytalon)
					},
					spellInfo =>
					{
						// All spells work even without these changes. The LOS attribute is due to problem
						// from collision between maps & gos with active destroyed state.
						spellInfo.AttributesEx2 |= SpellAttr2.IgnoreLineOfSight;
					});

		// Arcane Barrage (cast by players and NONMELEEDAMAGELOG with caster Scion of Eternity (original caster)).
		ApplySpellFix(new[]
					{
						63934
					},
					spellInfo =>
					{
						// This would never crit on retail and it has attribute for SPELL_ATTR3_NO_DONE_BONUS because is handled from player,
						// until someone figures how to make scions not critting without hack and without making them main casters this should stay here.
						spellInfo.AttributesEx2 |= SpellAttr2.CantCrit;
					});
		// ENDOF EYE OF ETERNITY SPELLS

		ApplySpellFix(new[]
					{
						40055, // Introspection
						40165, // Introspection
						40166, // Introspection
						40167, // Introspection
					},
					spellInfo => { spellInfo.Attributes |= SpellAttr0.AuraIsDebuff; });

		//
		// STONECORE SPELLS
		//
		ApplySpellFix(new[]
					{
						95284, // Teleport (from entrance to Slabhide)
						95285  // Teleport (from Slabhide to entrance)
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetB = new SpellImplicitTargetInfo(Targets.DestDb); }); });
		// ENDOF STONECORE SPELLS

		//
		// HALLS OF ORIGINATION SPELLS
		//
		ApplySpellFix(new[]
					{
						76606, // Disable Beacon Beams L
						76608  // Disable Beacon Beams R
					},
					spellInfo =>
					{
						// Little hack, Increase the radius so it can hit the Cave In Stalkers in the platform.
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.MaxRadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards45); });
					});

		// ENDOF HALLS OF ORIGINATION SPELLS

		// Threatening Gaze
		ApplySpellFix(new[]
					{
						24314
					},
					spellInfo => { spellInfo.AuraInterruptFlags |= SpellAuraInterruptFlags.Action | SpellAuraInterruptFlags.Moving | SpellAuraInterruptFlags.Anim; });

		// Travel Form (dummy) - cannot be cast indoors.
		ApplySpellFix(new[]
					{
						783
					},
					spellInfo => { spellInfo.Attributes |= SpellAttr0.OnlyOutdoors; });

		// Tree of Life (Passive)
		ApplySpellFix(new[]
					{
						5420
					},
					spellInfo => { spellInfo.Stances = 1ul << ((int)ShapeShiftForm.TreeOfLife - 1); });

		// Gaze of Occu'thar
		ApplySpellFix(new[]
					{
						96942
					},
					spellInfo => { spellInfo.AttributesEx &= ~SpellAttr1.IsChannelled; });

		// Evolution
		ApplySpellFix(new[]
					{
						75610
					},
					spellInfo => { spellInfo.MaxAffectedTargets = 1; });

		// Evolution
		ApplySpellFix(new[]
					{
						75697
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitSrcAreaEntry); }); });

		//
		// ISLE OF CONQUEST SPELLS
		//
		// Teleport
		ApplySpellFix(new[]
					{
						66551
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(13); // 50000yd
					});
		// ENDOF ISLE OF CONQUEST SPELLS

		// Aura of Fear
		ApplySpellFix(new[]
					{
						40453
					},
					spellInfo =>
					{
						// Bad DBC data? Copying 25820 here due to spell description
						// either is a periodic with chance on tick, or a proc

						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												spellEffectInfo.ApplyAuraName = AuraType.ProcTriggerSpell;
												spellEffectInfo.ApplyAuraPeriod = 0;
											});

						spellInfo.ProcChance = 10;
					});

		// Survey Sinkholes
		ApplySpellFix(new[]
					{
						45853
					},
					spellInfo =>
					{
						spellInfo.RangeEntry = CliDB.SpellRangeStorage.LookupByKey(5); // 40 yards
					});

		// Baron Rivendare (Stratholme) - Unholy Aura
		ApplySpellFix(new[]
					{
						17466, 17467
					},
					spellInfo => { spellInfo.AttributesEx2 |= SpellAttr2.NoInitialThreat; });

		// Spore - Spore Visual
		ApplySpellFix(new[]
					{
						42525
					},
					spellInfo =>
					{
						spellInfo.AttributesEx3 |= SpellAttr3.AllowAuraWhileDead;
						spellInfo.AttributesEx2 |= SpellAttr2.AllowDeadTarget;
					});

		// Soul Sickness (Forge of Souls)
		ApplySpellFix(new[]
					{
						69131
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 1, spellEffectInfo => { spellEffectInfo.ApplyAuraName = AuraType.ModDecreaseSpeed; }); });

		//
		// FIRELANDS SPELLS
		//
		// Torment Searcher
		ApplySpellFix(new[]
					{
						99253
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.MaxRadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(EffectRadiusIndex.Yards15); }); });

		// Torment Damage
		ApplySpellFix(new[]
					{
						99256
					},
					spellInfo => { spellInfo.Attributes |= SpellAttr0.AuraIsDebuff; });

		// Blaze of Glory
		ApplySpellFix(new[]
					{
						99252
					},
					spellInfo => { spellInfo.AuraInterruptFlags |= SpellAuraInterruptFlags.LeaveWorld; });
		// ENDOF FIRELANDS SPELLS

		//
		// ANTORUS THE BURNING THRONE SPELLS
		//

		// Decimation
		ApplySpellFix(new[]
					{
						244449
					},
					spellInfo =>
					{
						// For some reason there is a instakill effect that serves absolutely no purpose.
						// Until we figure out what it's actually used for we disable it.
						ApplySpellEffectFix(spellInfo, 2, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.None; });
					});

		// ENDOF ANTORUS THE BURNING THRONE SPELLS

		// Summon Master Li Fei
		ApplySpellFix(new[]
					{
						102445
					},
					spellInfo => { ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.DestDb); }); });

		// Earthquake
		ApplySpellFix(new[]
					{
						61882
					},
					spellInfo => { spellInfo.NegativeEffects.Add(2); });

		// Headless Horseman Climax - Return Head (Hallow End)
		// Headless Horseman Climax - Body Regen (confuse only - removed on death)
		// Headless Horseman Climax - Head Is Dead
		ApplySpellFix(new[]
					{
						42401, 43105, 42428
					},
					spellInfo => { spellInfo.Attributes |= SpellAttr0.NoImmunities; });

		// Horde / Alliance switch (BG mercenary system)
		ApplySpellFix(new[]
					{
						195838, 195843
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo, 0, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.ApplyAura; });
						ApplySpellEffectFix(spellInfo, 1, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.ApplyAura; });
						ApplySpellEffectFix(spellInfo, 2, spellEffectInfo => { spellEffectInfo.Effect = SpellEffectName.ApplyAura; });
					});

		// Fire Cannon
		ApplySpellFix(new[]
					{
						181593
					},
					spellInfo =>
					{
						ApplySpellEffectFix(spellInfo,
											0,
											spellEffectInfo =>
											{
												// This spell never triggers, theory is that it was supposed to be only triggered until target reaches some health percentage
												// but was broken and always caused visuals to break, then target was changed to immediately spawn with desired health
												// leaving old data in db2
												spellEffectInfo.TriggerSpell = 0;
											});
					});

		// Ray of Frost (Fingers of Frost charges)
		ApplySpellFix(new[]
					{
						269748
					},
					spellInfo => { spellInfo.AttributesEx &= ~SpellAttr1.IsChannelled; });

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
			{
				// Fix range for trajectory triggered spell
				foreach (var spellEffectInfo in spellInfo.Effects)
				{
					if (spellEffectInfo.IsEffect() && (spellEffectInfo.TargetA.Target == Targets.DestTraj || spellEffectInfo.TargetB.Target == Targets.DestTraj))
						// Get triggered spell if any
						foreach (var spellInfoTrigger in _GetSpellInfo(spellEffectInfo.TriggerSpell).Values)
						{
							var maxRangeMain = spellInfo.GetMaxRange();
							var maxRangeTrigger = spellInfoTrigger.GetMaxRange();

							// check if triggered spell has enough max range to cover trajectory
							if (maxRangeTrigger < maxRangeMain)
								spellInfoTrigger.RangeEntry = spellInfo.RangeEntry;
						}

					switch (spellEffectInfo.Effect)
					{
						case SpellEffectName.Charge:
						case SpellEffectName.ChargeDest:
						case SpellEffectName.Jump:
						case SpellEffectName.JumpDest:
						case SpellEffectName.LeapBack:
							if (spellInfo.Speed == 0 && spellInfo.SpellFamilyName == 0 && !spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
								spellInfo.Speed = MotionMaster.SPEED_CHARGE;

							break;
					}

					if (spellEffectInfo.TargetA.SelectionCategory == SpellTargetSelectionCategories.Cone || spellEffectInfo.TargetB.SelectionCategory == SpellTargetSelectionCategories.Cone)
						if (MathFunctions.fuzzyEq(spellInfo.ConeAngle, 0.0f))
							spellInfo.ConeAngle = 90.0f;

					// Area auras may not target area (they're self cast)
					if (spellEffectInfo.IsAreaAuraEffect && spellEffectInfo.IsTargetingArea)
					{
						spellEffectInfo.TargetA = new SpellImplicitTargetInfo(Targets.UnitCaster);
						spellEffectInfo.TargetB = new SpellImplicitTargetInfo();
					}
				}

				// disable proc for magnet auras, they're handled differently
				if (spellInfo.HasAura(AuraType.SpellMagnet))
					spellInfo.ProcFlags = new ProcFlagsInit();

				// due to the way spell system works, unit would change orientation in Spell::_cast
				if (spellInfo.HasAura(AuraType.ControlVehicle))
					spellInfo.AttributesEx5 |= SpellAttr5.AiDoesntFaceTarget;

				if (spellInfo.ActiveIconFileDataId == 135754) // flight
					spellInfo.Attributes |= SpellAttr0.Passive;

				if (spellInfo.IsSingleTarget() && spellInfo.MaxAffectedTargets == 0)
					spellInfo.MaxAffectedTargets = 1;
			}

		var properties = CliDB.SummonPropertiesStorage.LookupByKey(121);

		if (properties != null)
			properties.Title = SummonTitle.Totem;

		properties = CliDB.SummonPropertiesStorage.LookupByKey(647); // 52893

		if (properties != null)
			properties.Title = SummonTitle.Totem;

		properties = CliDB.SummonPropertiesStorage.LookupByKey(628);

		if (properties != null) // Hungry Plaguehound
			properties.Control = SummonCategory.Pet;

		Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo corrections in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellInfoSpellSpecificAndAuraState()
	{
		var oldMSTime = Time.MSTime;

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
			{
				// AuraState depends on SpellSpecific
				spellInfo._LoadSpellSpecific();
				spellInfo._LoadAuraState();
			}

		Log.outInfo(LogFilter.ServerLoading, $"Loaded SpellInfo SpellSpecific and AuraState in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void LoadSpellInfoDiminishing()
	{
		var oldMSTime = Time.MSTime;

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
			{
				if (spellInfo == null)
					continue;

				spellInfo._LoadSpellDiminishInfo();
			}

		Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo diminishing infos in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSpellInfoImmunities()
	{
		var oldMSTime = Time.MSTime;

		foreach (var kvp in _spellInfoMap.Values)
			foreach (var spellInfo in kvp.Values)
			{
				if (spellInfo == null)
					continue;

				spellInfo._LoadImmunityInfo();
			}

		Log.outInfo(LogFilter.ServerLoading, "Loaded SpellInfo immunity infos in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadPetFamilySpellsStore()
	{
		Dictionary<uint, SpellLevelsRecord> levelsBySpell = new();

		foreach (var levels in CliDB.SpellLevelsStorage.Values)
			if (levels.DifficultyID == 0)
				levelsBySpell[levels.SpellID] = levels;

		foreach (var skillLine in CliDB.SkillLineAbilityStorage.Values)
		{
			var spellInfo = GetSpellInfo(skillLine.Spell, Difficulty.None);

			if (spellInfo == null)
				continue;

			var levels = levelsBySpell.LookupByKey(skillLine.Spell);

			if (levels != null && levels.SpellLevel != 0)
				continue;

			if (spellInfo.IsPassive)
				foreach (var cFamily in CliDB.CreatureFamilyStorage.Values)
				{
					if (skillLine.SkillLine != cFamily.SkillLine[0] && skillLine.SkillLine != cFamily.SkillLine[1])
						continue;

					if (skillLine.AcquireMethod != AbilityLearnType.OnSkillLearn)
						continue;

					Global.SpellMgr.PetFamilySpellsStorage.Add(cFamily.Id, spellInfo.Id);
				}
		}
	}

	public void LoadSpellTotemModel()
	{
		var oldMSTime = Time.MSTime;

		var result = DB.World.Query("SELECT SpellID, RaceID, DisplayID from spell_totem_model");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell totem model records. DB table `spell_totem_model` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var spellId = result.Read<uint>(0);
			var race = result.Read<byte>(1);
			var displayId = result.Read<uint>(2);

			var spellEntry = GetSpellInfo(spellId, Difficulty.None);

			if (spellEntry == null)
			{
				Log.outError(LogFilter.Sql, $"SpellID: {spellId} in `spell_totem_model` table could not be found in dbc, skipped.");

				continue;
			}

			if (!CliDB.ChrRacesStorage.ContainsKey(race))
			{
				Log.outError(LogFilter.Sql, $"Race {race} defined in `spell_totem_model` does not exists, skipped.");

				continue;
			}

			if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
			{
				Log.outError(LogFilter.Sql, $"SpellID: {spellId} defined in `spell_totem_model` has non-existing model ({displayId}).");

				continue;
			}

			_spellTotemModel[Tuple.Create(spellId, race)] = displayId;
			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} spell totem model records in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	#endregion
}
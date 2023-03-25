﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Combat;

public class ThreatManager
{
	public static uint THREAT_UPDATE_INTERVAL = 1000u;

	public Unit _owner;

	public bool NeedClientUpdate;
	public double[] _singleSchoolModifiers = new double[(int)SpellSchools.Max];        // most spells are single school - we pre-calculate these and store them
	public volatile Dictionary<SpellSchoolMask, double> _multiSchoolModifiers = new(); // these are calculated on demand

	public List<Tuple<ObjectGuid, uint>> _redirectInfo = new();                      // current redirection targets and percentages (updated from registry in ThreatManager::UpdateRedirectInfo)
	public Dictionary<uint, Dictionary<ObjectGuid, uint>> _redirectRegistry = new(); // spellid . (victim . pct); all redirection effects on us (removal individually managed by spell scripts because blizzard is dumb)
	readonly List<ThreatReference> _sortedThreatList = new();
	readonly Dictionary<ObjectGuid, ThreatReference> _myThreatListEntries = new();
	readonly List<ThreatReference> _needsAIUpdate = new();
	readonly Dictionary<ObjectGuid, ThreatReference> _threatenedByMe = new(); // these refs are entries for myself on other units' threat lists
	bool _ownerCanHaveThreatList;
	uint _updateTimer;
	ThreatReference _currentVictimRef;
	ThreatReference _fixateRef;

	public Unit CurrentVictim
	{
		get
		{
			if (_currentVictimRef == null || _currentVictimRef.ShouldBeOffline)
				UpdateVictim();

			return _currentVictimRef?.Victim;
		}
	}

	public Unit LastVictim
	{
		get
		{
			if (_currentVictimRef != null && !_currentVictimRef.ShouldBeOffline)
				return _currentVictimRef.Victim;

			return null;
		}
	}

	// can our owner have a threat list?
	// identical to ThreatManager::CanHaveThreatList(GetOwner())

	public bool CanHaveThreatList => _ownerCanHaveThreatList;

	public int ThreatListSize => _sortedThreatList.Count;

	// fastest of the three threat list getters - gets the threat list in "arbitrary" order

	public List<ThreatReference> SortedThreatList => _sortedThreatList;

	public Dictionary<ObjectGuid, ThreatReference> ThreatenedByMeList => _threatenedByMe;

	// never nullptr

	public Unit Owner => _owner;

	public ThreatManager(Unit owner)
	{
		_owner = owner;
		_updateTimer = THREAT_UPDATE_INTERVAL;

		for (var i = 0; i < (int)SpellSchools.Max; ++i)
			_singleSchoolModifiers[i] = 1.0f;
	}

	public static bool CanHaveThreatListForUnit(Unit who)
	{
		var cWho = who.AsCreature;

		// only creatures can have threat list
		if (!cWho)
			return false;

		// pets, totems and triggers cannot have threat list
		if (cWho.IsPet || cWho.IsTotem || cWho.IsTrigger)
			return false;

		// summons cannot have a threat list if they were summoned by a player
		if (cWho.HasUnitTypeMask(UnitTypeMask.Minion | UnitTypeMask.Guardian))
		{
			var tWho = cWho.ToTempSummon();

			if (tWho != null)
				if (tWho.GetSummonerGUID().IsPlayer)
					return false;
		}

		return true;
	}

	public void Initialize()
	{
		_ownerCanHaveThreatList = CanHaveThreatListForUnit(_owner);
	}

	public void Update(uint tdiff)
	{
		if (!CanHaveThreatList || IsThreatListEmpty(true))
			return;

		if (_updateTimer <= tdiff)
		{
			UpdateVictim();
			_updateTimer = THREAT_UPDATE_INTERVAL;
		}
		else
		{
			_updateTimer -= tdiff;
		}
	}

	public Unit GetAnyTarget()
	{
		foreach (var refe in _sortedThreatList)
			if (!refe.IsOffline)
				return refe.Victim;

		return null;
	}

	public bool IsThreatListEmpty(bool includeOffline = false)
	{
		if (includeOffline)
			return _sortedThreatList.Empty();

		foreach (var refe in _sortedThreatList)
			if (refe.IsAvailable)
				return false;

		return true;
	}

	public bool IsThreatenedBy(ObjectGuid who, bool includeOffline = false)
	{
		var refe = _myThreatListEntries.LookupByKey(who);

		if (refe == null)
			return false;

		return (includeOffline || refe.IsAvailable);
	}

	public bool IsThreatenedBy(Unit who, bool includeOffline = false)
	{
		return IsThreatenedBy(who.GUID, includeOffline);
	}

	public double GetThreat(Unit who, bool includeOffline = false)
	{
		var refe = _myThreatListEntries.LookupByKey(who.GUID);

		if (refe == null)
			return 0.0f;

		return (includeOffline || refe.IsAvailable) ? refe.Threat : 0.0f;
	}

	public List<ThreatReference> GetModifiableThreatList()
	{
		return new List<ThreatReference>(_sortedThreatList);
	}

	public bool IsThreateningAnyone(bool includeOffline = false)
	{
		if (includeOffline)
			return !_threatenedByMe.Empty();

		foreach (var pair in _threatenedByMe)
			if (pair.Value.IsAvailable)
				return true;

		return false;
	}

	public bool IsThreateningTo(ObjectGuid who, bool includeOffline = false)
	{
		var refe = _threatenedByMe.LookupByKey(who);

		if (refe == null)
			return false;

		return (includeOffline || refe.IsAvailable);
	}

	public bool IsThreateningTo(Unit who, bool includeOffline = false)
	{
		return IsThreateningTo(who.GUID, includeOffline);
	}

	public void EvaluateSuppressed(bool canExpire = false)
	{
		foreach (var pair in _threatenedByMe)
		{
			var shouldBeSuppressed = pair.Value.ShouldBeSuppressed;

			if (pair.Value.IsOnline && shouldBeSuppressed)
			{
				pair.Value.Online = OnlineState.Suppressed;
				pair.Value.ListNotifyChanged();
			}
			else if (canExpire && pair.Value.IsSuppressed && !shouldBeSuppressed)
			{
				pair.Value.Online = OnlineState.Online;
				pair.Value.ListNotifyChanged();
			}
		}
	}

	public void AddThreat(Unit target, double amount, SpellInfo spell = null, bool ignoreModifiers = false, bool ignoreRedirects = false)
	{
		// step 1: we can shortcut if the spell has one of the NO_THREAT attrs set - nothing will happen
		if (spell != null)
		{
			if (spell.HasAttribute(SpellAttr1.NoThreat))
				return;

			if (!_owner.IsEngaged && spell.HasAttribute(SpellAttr2.NoInitialThreat))
				return;
		}

		// while riding a vehicle, all threat goes to the vehicle, not the pilot
		var vehicle = target.VehicleBase;

		if (vehicle != null)
		{
			AddThreat(vehicle, amount, spell, ignoreModifiers, ignoreRedirects);

			if (target.HasUnitTypeMask(UnitTypeMask.Accessory)) // accessories are fully treated as components of the parent and cannot have threat
				return;

			amount = 0.0f;
		}

		// If victim is personal spawn, redirect all aggro to summoner
		if (target.IsPrivateObject && (!Owner.IsPrivateObject || !Owner.CheckPrivateObjectOwnerVisibility(target)))
		{
			var privateObjectOwner = Global.ObjAccessor.GetUnit(Owner, target.PrivateObjectOwner);

			if (privateObjectOwner != null)
			{
				AddThreat(privateObjectOwner, amount, spell, ignoreModifiers, ignoreRedirects);
				amount = 0.0f;
			}
		}

		// if we cannot actually have a threat list, we instead just set combat state and avoid creating threat refs altogether
		if (!CanHaveThreatList)
		{
			var combatMgr = _owner.GetCombatManager();

			if (!combatMgr.SetInCombatWith(target))
				return;

			// traverse redirects and put them in combat, too
			foreach (var pair in target.GetThreatManager()._redirectInfo)
				if (!combatMgr.IsInCombatWith(pair.Item1))
				{
					var redirTarget = Global.ObjAccessor.GetUnit(_owner, pair.Item1);

					if (redirTarget != null)
						combatMgr.SetInCombatWith(redirTarget);
				}

			return;
		}

		// apply threat modifiers to the amount
		if (!ignoreModifiers)
			amount = CalculateModifiedThreat(amount, target, spell);

		// if we're increasing threat, send some/all of it to redirection targets instead if applicable
		if (!ignoreRedirects && amount > 0.0f)
		{
			var redirInfo = target.GetThreatManager()._redirectInfo;

			if (!redirInfo.Empty())
			{
				var origAmount = amount;

				// intentional iteration by index - there's a nested AddThreat call further down that might cause AI calls which might modify redirect info through spells
				for (var i = 0; i < redirInfo.Count; ++i)
				{
					var pair = redirInfo[i]; // (victim,pct)
					Unit redirTarget;

					var refe = _myThreatListEntries.LookupByKey(pair.Item1); // try to look it up in our threat list first (faster)

					if (refe != null)
						redirTarget = refe.Victim;
					else
						redirTarget = Global.ObjAccessor.GetUnit(_owner, pair.Item1);

					if (redirTarget)
					{
						var amountRedirected = MathFunctions.CalculatePct(origAmount, pair.Item2);
						AddThreat(redirTarget, amountRedirected, spell, true, true);
						amount -= amountRedirected;
					}
				}
			}
		}

		// ensure we're in combat (threat implies combat!)
		if (!_owner.GetCombatManager().SetInCombatWith(target)) // if this returns false, we're not actually in combat, and thus cannot have threat!
			return;                                             // typical causes: bad scripts trying to add threat to GMs, dead targets etc

		// ok, now we actually apply threat
		// check if we already have an entry - if we do, just increase threat for that entry and we're done
		var targetRefe = _myThreatListEntries.LookupByKey(target.GUID);

		if (targetRefe != null)
		{
			// SUPPRESSED threat states don't go back to ONLINE until threat is caused by them (retail behavior)
			if (targetRefe.OnlineState == OnlineState.Suppressed)
				if (!targetRefe.ShouldBeSuppressed)
				{
					targetRefe.Online = OnlineState.Online;
					targetRefe.ListNotifyChanged();
				}

			if (targetRefe.IsOnline)
				targetRefe.AddThreat(amount);

			return;
		}

		// ok, we're now in combat - create the threat list reference and push it to the respective managers
		ThreatReference newRefe = new(this, target);
		PutThreatListRef(target.GUID, newRefe);
		target.GetThreatManager().PutThreatenedByMeRef(_owner.GUID, newRefe);

		// afterwards, we evaluate whether this is an online reference (it might not be an acceptable target, but we need to add it to our threat list before we check!)
		newRefe.UpdateOffline();

		if (newRefe.IsOnline) // we only add the threat if the ref is currently available
			newRefe.AddThreat(amount);

		if (_currentVictimRef == null)
			UpdateVictim();
		else
			ProcessAIUpdates();
	}

	public void MatchUnitThreatToHighestThreat(Unit target)
	{
		if (_sortedThreatList.Empty())
			return;

		var index = 0;

		var highest = _sortedThreatList[index];

		if (!highest.IsAvailable)
			return;

		if (highest.IsTaunting && (++index) != _sortedThreatList.Count - 1) // might need to skip this - max threat could be the preceding element (there is only one taunt element)
		{
			var a = _sortedThreatList[index];

			if (a.IsAvailable && a.Threat > highest.Threat)
				highest = a;
		}

		AddThreat(target, highest.Threat - GetThreat(target, true), null, true, true);
	}

	public void TauntUpdate()
	{
		var tauntEffects = _owner.GetAuraEffectsByType(AuraType.ModTaunt);
		var state = TauntState.Taunt;
		Dictionary<ObjectGuid, TauntState> tauntStates = new();

		// Only the last taunt effect applied by something still on our threat list is considered
		foreach (var auraEffect in tauntEffects)
			tauntStates[auraEffect.CasterGuid] = state++;

		foreach (var pair in _myThreatListEntries)
			if (tauntStates.TryGetValue(pair.Key, out var tauntState))
				pair.Value.UpdateTauntState(tauntState);
			else
				pair.Value.UpdateTauntState();

		// taunt aura update also re-evaluates all suppressed states (retail behavior)
		EvaluateSuppressed(true);
	}

	public void ResetAllThreat()
	{
		foreach (var pair in _myThreatListEntries)
			pair.Value.ScaleThreat(0.0f);
	}

	public void ClearThreat(Unit target)
	{
		var refe = _myThreatListEntries.LookupByKey(target.GUID);

		if (refe != null)
			ClearThreat(refe);
	}

	public void ClearThreat(ThreatReference refe)
	{
		SendRemoveToClients(refe.Victim);
		refe.UnregisterAndFree();

		if (_currentVictimRef == null)
			UpdateVictim();
	}

	public void ClearAllThreat()
	{
		if (!_myThreatListEntries.Empty())
		{
			SendClearAllThreatToClients();

			do
			{
				_myThreatListEntries.FirstOrDefault().Value.UnregisterAndFree();
			} while (!_myThreatListEntries.Empty());
		}
	}

	public void FixateTarget(Unit target)
	{
		if (target)
		{
			var it = _myThreatListEntries.LookupByKey(target.GUID);

			if (it != null)
			{
				_fixateRef = it;

				return;
			}
		}

		_fixateRef = null;
	}

	public Unit GetFixateTarget()
	{
		if (_fixateRef != null)
			return _fixateRef.Victim;
		else
			return null;
	}

	public void ClearFixate()
	{
		FixateTarget(null);
	}

	// returns true if a is LOWER on the threat list than b
	public static bool CompareReferencesLT(ThreatReference a, ThreatReference b, float aWeight)
	{
		if (a.OnlineState != b.OnlineState) // online state precedence (ONLINE > SUPPRESSED > OFFLINE)
			return a.OnlineState < b.OnlineState;

		if (a.TauntState != b.TauntState) // taunt state precedence (TAUNT > NONE > DETAUNT)
			return a.TauntState < b.TauntState;

		return (a.Threat * aWeight < b.Threat);
	}

	public static double CalculateModifiedThreat(double threat, Unit victim, SpellInfo spell)
	{
		// modifiers by spell
		if (spell != null)
		{
			var threatEntry = Global.SpellMgr.GetSpellThreatEntry(spell.Id);

			if (threatEntry != null)
				if (threatEntry.PctMod != 1.0f) // flat/AP modifiers handled in Spell::HandleThreatSpells
					threat *= threatEntry.PctMod;

			var modOwner = victim.SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(spell, SpellModOp.Hate, ref threat);
		}

		// modifiers by effect school
		var victimMgr = victim.GetThreatManager();
		var mask = spell != null ? spell.GetSchoolMask() : SpellSchoolMask.Normal;

		switch (mask)
		{
			case SpellSchoolMask.Normal:
				threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Normal];

				break;
			case SpellSchoolMask.Holy:
				threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Holy];

				break;
			case SpellSchoolMask.Fire:
				threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Fire];

				break;
			case SpellSchoolMask.Nature:
				threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Nature];

				break;
			case SpellSchoolMask.Frost:
				threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Frost];

				break;
			case SpellSchoolMask.Shadow:
				threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Shadow];

				break;
			case SpellSchoolMask.Arcane:
				threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Arcane];

				break;
			default:
			{
				if (victimMgr._multiSchoolModifiers.TryGetValue(mask, out var value))
				{
					threat *= value;

					break;
				}

				var mod = victim.GetTotalAuraMultiplierByMiscMask(AuraType.ModThreat, (uint)mask);
				victimMgr._multiSchoolModifiers[mask] = mod;
				threat *= mod;

				break;
			}
		}

		return threat;
	}

	public void ForwardThreatForAssistingMe(Unit assistant, double baseAmount, SpellInfo spell = null, bool ignoreModifiers = false)
	{
		if (spell != null && (spell.HasAttribute(SpellAttr1.NoThreat) || spell.HasAttribute(SpellAttr4.NoHelpfulThreat))) // shortcut, none of the calls would do anything
			return;

		if (_threatenedByMe.Empty())
			return;

		List<Creature> canBeThreatened = new();
		List<Creature> cannotBeThreatened = new();

		foreach (var pair in _threatenedByMe)
		{
			var owner = pair.Value.Owner;

			if (!owner.HasUnitState(UnitState.Controlled))
				canBeThreatened.Add(owner);
			else
				cannotBeThreatened.Add(owner);
		}

		if (!canBeThreatened.Empty()) // targets under CC cannot gain assist threat - split evenly among the rest
		{
			var perTarget = baseAmount / canBeThreatened.Count;

			foreach (var threatened in canBeThreatened)
				threatened.GetThreatManager().AddThreat(assistant, perTarget, spell, ignoreModifiers);
		}

		foreach (var threatened in cannotBeThreatened)
			threatened.GetThreatManager().AddThreat(assistant, 0.0f, spell, true);
	}

	public void RemoveMeFromThreatLists()
	{
		while (!_threatenedByMe.Empty())
		{
			var refe = _threatenedByMe.FirstOrDefault().Value;
			refe._mgr.ClearThreat(_owner);
		}
	}

	public void UpdateMyTempModifiers()
	{
		double mod = 0;

		foreach (var eff in _owner.GetAuraEffectsByType(AuraType.ModTotalThreat))
			mod += eff.Amount;

		if (_threatenedByMe.Empty())
			return;

		foreach (var pair in _threatenedByMe)
		{
			pair.Value.TempModifier = (int)mod;
			pair.Value.ListNotifyChanged();
		}
	}

	public void UpdateMySpellSchoolModifiers()
	{
		for (byte i = 0; i < (int)SpellSchools.Max; ++i)
			_singleSchoolModifiers[i] = _owner.GetTotalAuraMultiplierByMiscMask(AuraType.ModThreat, 1u << i);

		_multiSchoolModifiers.Clear();
	}

	public void RegisterRedirectThreat(uint spellId, ObjectGuid victim, uint pct)
	{
		if (!_redirectRegistry.ContainsKey(spellId))
			_redirectRegistry[spellId] = new Dictionary<ObjectGuid, uint>();

		_redirectRegistry[spellId][victim] = pct;
		UpdateRedirectInfo();
	}

	public void UnregisterRedirectThreat(uint spellId)
	{
		if (_redirectRegistry.Remove(spellId))
			UpdateRedirectInfo();
	}

	public void SendRemoveToClients(Unit victim)
	{
		ThreatRemove threatRemove = new();
		threatRemove.UnitGUID = _owner.GUID;
		threatRemove.AboutGUID = victim.GUID;
		_owner.SendMessageToSet(threatRemove, false);
	}

	public void PurgeThreatListRef(ObjectGuid guid)
	{
		var refe = _myThreatListEntries.LookupByKey(guid);

		if (refe == null)
			return;

		_myThreatListEntries.Remove(guid);
		_sortedThreatList.Remove(refe);

		if (_fixateRef == refe)
			_fixateRef = null;

		if (_currentVictimRef == refe)
			_currentVictimRef = null;
	}

	public void PurgeThreatenedByMeRef(ObjectGuid guid)
	{
		_threatenedByMe.Remove(guid);
	}

	public void ListNotifyChanged()
	{
		_sortedThreatList.Sort();
	}

	// Modify target's threat by +percent%
	public void ModifyThreatByPercent(Unit target, double percent)
	{
		if (percent != 0)
			ScaleThreat(target, 0.01f * (100f + percent));
	}

	// Resets the specified unit's threat to zero
	public void ResetThreat(Unit target)
	{
		ScaleThreat(target, 0.0f);
	}

	public void RegisterForAIUpdate(ThreatReference refe)
	{
		_needsAIUpdate.Add(refe);
	}

	void ScaleThreat(Unit target, double factor)
	{
		var refe = _myThreatListEntries.LookupByKey(target.GUID);

		if (refe != null)
			refe.ScaleThreat(Math.Max(factor, 0.0f));
	}

	void UpdateVictim()
	{
		var newVictim = ReselectVictim();
		var newHighest = newVictim != null && (newVictim != _currentVictimRef);

		_currentVictimRef = newVictim;

		if (newHighest || NeedClientUpdate)
		{
			SendThreatListToClients(newHighest);
			NeedClientUpdate = false;
		}

		ProcessAIUpdates();
	}

	ThreatReference ReselectVictim()
	{
		if (_sortedThreatList.Empty())
			return null;

		foreach (var pair in _myThreatListEntries)
			pair.Value.UpdateOffline(); // AI notifies are processed in ::UpdateVictim caller

		// fixated target is always preferred
		if (_fixateRef != null && _fixateRef.IsAvailable)
			return _fixateRef;

		var oldVictimRef = _currentVictimRef;

		if (oldVictimRef != null && oldVictimRef.IsOffline)
			oldVictimRef = null;

		// in 99% of cases - we won't need to actually look at anything beyond the first element
		var highest = _sortedThreatList.First();

		// if the highest reference is offline, the entire list is offline, and we indicate this
		if (!highest.IsAvailable)
			return null;

		// if we have no old victim, or old victim is still highest, then highest is our target and we're done
		if (oldVictimRef == null || highest == oldVictimRef)
			return highest;

		// if highest threat doesn't break 110% of old victim, nothing below it is going to do so either; new victim = old victim and done
		if (!CompareReferencesLT(oldVictimRef, highest, 1.1f))
			return oldVictimRef;

		// if highest threat breaks 130%, it's our new target regardless of range (and we're done)
		if (CompareReferencesLT(oldVictimRef, highest, 1.3f))
			return highest;

		// if it doesn't break 130%, we need to check if it's melee - if yes, it breaks 110% (we checked earlier) and is our new target
		if (_owner.IsWithinMeleeRange(highest.Victim))
			return highest;

		// If we get here, highest threat is ranged, but below 130% of current - there might be a melee that breaks 110% below us somewhere, so now we need to actually look at the next highest element
		// luckily, this is a heap, so getting the next highest element is O(log n), and we're just gonna do that repeatedly until we've seen enough targets (or find a target)
		foreach (var next in _sortedThreatList)
		{
			// if we've found current victim, we're done (nothing above is higher, and nothing below can be higher)
			if (next == oldVictimRef)
				return next;

			// if next isn't above 110% threat, then nothing below it can be either - we're done, old victim stays
			if (!CompareReferencesLT(oldVictimRef, next, 1.1f))
				return oldVictimRef;

			// if next is melee, he's above 110% and our new victim
			if (_owner.IsWithinMeleeRange(next.Victim))
				return next;

			// otherwise the next highest target may still be a melee above 110% and we need to look further
		}

		return null;
	}

	void ProcessAIUpdates()
	{
		var ai = _owner.AsCreature.AI;
		List<ThreatReference> v = new(_needsAIUpdate); // _needClientUpdate is now empty in case this triggers a recursive call

		if (ai == null)
			return;

		foreach (var refe in v)
			ai.JustStartedThreateningMe(refe.Victim);
	}

	void UnregisterRedirectThreat(uint spellId, ObjectGuid victim)
	{
		var victimMap = _redirectRegistry.LookupByKey(spellId);

		if (victimMap == null)
			return;

		if (victimMap.Remove(victim))
			UpdateRedirectInfo();
	}

	void SendClearAllThreatToClients()
	{
		ThreatClear threatClear = new();
		threatClear.UnitGUID = _owner.GUID;
		_owner.SendMessageToSet(threatClear, false);
	}

	void SendThreatListToClients(bool newHighest)
	{
		void fillSharedPacketDataAndSend(dynamic packet)
		{
			packet.UnitGUID = _owner.GUID;

			foreach (var refe in _sortedThreatList)
			{
				if (!refe.IsAvailable)
					continue;

				ThreatInfo threatInfo = new();
				threatInfo.UnitGUID = refe.Victim.GUID;
				threatInfo.Threat = (long)(refe.Threat * 100);
				packet.ThreatList.Add(threatInfo);
			}

			_owner.SendMessageToSet(packet, false);
		}

		if (newHighest)
		{
			HighestThreatUpdate highestThreatUpdate = new();
			highestThreatUpdate.HighestThreatGUID = _currentVictimRef.Victim.GUID;
			fillSharedPacketDataAndSend(highestThreatUpdate);
		}
		else
		{
			ThreatUpdate threatUpdate = new();
			fillSharedPacketDataAndSend(threatUpdate);
		}
	}

	void PutThreatListRef(ObjectGuid guid, ThreatReference refe)
	{
		NeedClientUpdate = true;
		_myThreatListEntries[guid] = refe;
		_sortedThreatList.Add(refe);
		_sortedThreatList.Sort();
	}

	void PutThreatenedByMeRef(ObjectGuid guid, ThreatReference refe)
	{
		_threatenedByMe[guid] = refe;
	}

	void UpdateRedirectInfo()
	{
		_redirectInfo.Clear();
		uint totalPct = 0;

		foreach (var pair in _redirectRegistry) // (spellid, victim . pct)
		{
			foreach (var victimPair in pair.Value) // (victim,pct)
			{
				var thisPct = Math.Min(100 - totalPct, victimPair.Value);

				if (thisPct > 0)
				{
					_redirectInfo.Add(Tuple.Create(victimPair.Key, thisPct));
					totalPct += thisPct;

					if (totalPct == 100)
						return;
				}
			}
		}
	}
}
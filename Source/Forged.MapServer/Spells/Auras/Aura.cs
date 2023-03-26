// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Models;
using Serilog;

namespace Forged.MapServer.Spells.Auras;

public class Aura
{
	const int UPDATE_TARGET_MAP_INTERVAL = 500;

	static readonly List<IAuraScript> Dummy = new();
	static readonly HashSet<int> DummyHashset = new();
	static readonly List<(IAuraScript, IAuraEffectHandler)> DummyAuraEffects = new();
	static readonly Dictionary<Unit, HashSet<int>> DummyAuraFill = new();
	readonly Dictionary<Type, List<IAuraScript>> _auraScriptsByType = new();
	readonly Dictionary<int, Dictionary<AuraScriptHookType, List<(IAuraScript, IAuraEffectHandler)>>> _effectHandlers = new();
	readonly SpellInfo _spellInfo;
	readonly Difficulty _castDifficulty;
	readonly long _applyTime;
	readonly WorldObject _owner;
	readonly List<SpellPowerRecord> _periodicCosts = new(); // Periodic costs

	readonly uint _casterLevel; // Aura level (store caster level for correct show level dep amount)
	readonly Dictionary<ObjectGuid, AuraApplication> _auraApplications = new();
	readonly List<AuraApplication> _removedApplications = new();
	readonly ObjectGuid _castId;
	readonly ObjectGuid _casterGuid;
	readonly SpellCastVisual _spellCastVisual;

	List<AuraScript> _loadedScripts = new();
	ObjectGuid _castItemGuid;
	uint _castItemId;
	int _castItemLevel;

	int _maxDuration;             // Max aura duration
	int _duration;                // Current time
	int _timeCla;                 // Timer for power per sec calcultion
	int _updateTargetMapInterval; // Timer for UpdateTargetMapOfEffect
	byte _procCharges;            // Aura charges (0 for infinite)
	byte _stackAmount;            // Aura stack amount

	//might need to be arrays still
	Dictionary<int, AuraEffect> _effects;

	bool _isRemoved;
	bool _isSingleTarget; // true if it's a single target spell and registered at caster - can change at spell steal for example
	bool _isUsingCharges;

	ChargeDropEvent _chargeDropEvent;

	DateTime _procCooldown;
	DateTime _lastProcAttemptTime;
	DateTime _lastProcSuccessTime;
	public Guid Guid { get; } = Guid.NewGuid();
	public byte? EmpoweredStage { get; set; }

	public SpellInfo SpellInfo => _spellInfo;

	public uint Id => _spellInfo.Id;

	public Difficulty CastDifficulty => _castDifficulty;

	public ObjectGuid CastId => _castId;

	public ObjectGuid CasterGuid => _casterGuid;

	public ObjectGuid CastItemGuid
	{
		get => _castItemGuid;
		set => _castItemGuid = value;
	}

	public uint CastItemId
	{
		get => _castItemId;
		set => _castItemId = value;
	}

	public int CastItemLevel
	{
		get => _castItemLevel;
		set => _castItemLevel = value;
	}

	public Unit Caster
	{
		get
		{
			if (_owner.GUID == _casterGuid)
				return OwnerAsUnit;

			return Global.ObjAccessor.GetUnit(_owner, _casterGuid);
		}
	}

	public SpellCastVisual SpellVisual => _spellCastVisual;

	public WorldObject Owner => _owner;

	public Unit OwnerAsUnit => _owner.AsUnit;

	public DynamicObject DynobjOwner => _owner.AsDynamicObject;

	public long ApplyTime => _applyTime;

	public int MaxDuration => _maxDuration;

	public int Duration => _duration;

	public bool IsExpired => Duration == 0 && _chargeDropEvent == null;

	public bool IsPermanent => _maxDuration == -1;

	public byte Charges => _procCharges;

	public byte StackAmount => _stackAmount;

	public byte CasterLevel => (byte)_casterLevel;

	public bool IsRemoved => _isRemoved;

	public bool IsSingleTarget
	{
		get => _isSingleTarget;
		set => _isSingleTarget = value;
	}

	public Dictionary<ObjectGuid, AuraApplication> ApplicationMap => _auraApplications;

	public bool IsUsingCharges
	{
		get => _isUsingCharges;
		set => _isUsingCharges = value;
	}

	public Dictionary<int, AuraEffect> AuraEffects => _effects;

	public AuraObjectType AuraObjType => (_owner.TypeId == TypeId.DynamicObject) ? AuraObjectType.DynObj : AuraObjectType.Unit;

	public bool IsPassive => _spellInfo.IsPassive;

	public bool IsDeathPersistent => SpellInfo.IsDeathPersistent;

	public Aura(AuraCreateInfo createInfo)
	{
		_spellInfo = createInfo.SpellInfoInternal;
		_castDifficulty = createInfo.CastDifficulty;
		_castId = createInfo.CastId;
		_casterGuid = createInfo.CasterGuid;
		_castItemGuid = createInfo.CastItemGuid;
		_castItemId = createInfo.CastItemId;
		_castItemLevel = createInfo.CastItemLevel;
		_spellCastVisual = new SpellCastVisual(createInfo.Caster ? createInfo.Caster.GetCastSpellXSpellVisualId(createInfo.SpellInfoInternal) : createInfo.SpellInfoInternal.GetSpellXSpellVisualId(), 0);
		_applyTime = GameTime.GetGameTime();
		_owner = createInfo.OwnerInternal;
		_timeCla = 0;
		_updateTargetMapInterval = 0;
		_casterLevel = createInfo.Caster ? createInfo.Caster.Level : _spellInfo.SpellLevel;
		_procCharges = 0;
		_stackAmount = 1;
		_isRemoved = false;
		_isSingleTarget = false;
		_isUsingCharges = false;
		_lastProcAttemptTime = (DateTime.Now - TimeSpan.FromSeconds(10));
		_lastProcSuccessTime = (DateTime.Now - TimeSpan.FromSeconds(120));

		foreach (var power in _spellInfo.PowerCosts)
			if (power != null && (power.ManaPerSecond != 0 || power.PowerPctPerSecond > 0.0f))
				_periodicCosts.Add(power);

		if (!_periodicCosts.Empty())
			_timeCla = 1 * Time.InMilliseconds;

		_maxDuration = CalcMaxDuration(createInfo.Caster);
		_duration = _maxDuration;
		_procCharges = CalcMaxCharges(createInfo.Caster);
		_isUsingCharges = _procCharges != 0;
		// m_casterLevel = cast item level/caster level, caster level should be saved to db, confirmed with sniffs
	}

	public T GetScript<T>() where T : AuraScript
	{
		return (T)GetScriptByType(typeof(T));
	}

	public AuraScript GetScriptByType(Type type)
	{
		foreach (var auraScript in _loadedScripts)
			if (auraScript.GetType() == type)
				return auraScript;

		return null;
	}

	public void _InitEffects(HashSet<int> effMask, Unit caster, Dictionary<int, double> baseAmount)
	{
		// shouldn't be in constructor - functions in AuraEffect.AuraEffect use polymorphism
		_effects = new Dictionary<int, AuraEffect>();

		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (effMask.Contains(spellEffectInfo.EffectIndex))
				_effects[spellEffectInfo.EffectIndex] = new AuraEffect(this, spellEffectInfo, baseAmount != null ? baseAmount[spellEffectInfo.EffectIndex] : null, caster);
	}

	public virtual void Dispose()
	{
		// unload scripts
		foreach (var itr in _loadedScripts.ToList())
			itr._Unload();

		if (_auraApplications.Count > 0)
			foreach (var app in _auraApplications.Values.ToArray())
				OwnerAsUnit?.RemoveAura(app);

		_auraApplications.Clear();
		_DeleteRemovedApplications();
	}

	public virtual void _ApplyForTarget(Unit target, Unit caster, AuraApplication auraApp)
	{
		if (target == null || auraApp == null) return;
		// aura mustn't be already applied on target
		//Cypher.Assert(!IsAppliedOnTarget(target.GetGUID()) && "Aura._ApplyForTarget: aura musn't be already applied on target");

		_auraApplications[target.GUID] = auraApp;

		// set infinity cooldown state for spells
		if (caster != null && caster.IsTypeId(TypeId.Player))
			if (_spellInfo.IsCooldownStartedOnEvent)
			{
				var castItem = !_castItemGuid.IsEmpty ? caster.AsPlayer.GetItemByGuid(_castItemGuid) : null;
				caster.SpellHistory.StartCooldown(_spellInfo, castItem != null ? castItem.Entry : 0, null, true);
			}

		ForEachAuraScript<IAuraOnApply>(a => a.AuraApply());
	}

	public virtual void _UnapplyForTarget(Unit target, Unit caster, AuraApplication auraApp)
	{
		if (target == null || !auraApp.HasRemoveMode || auraApp == null)
			return;

		var app = _auraApplications.LookupByKey(target.GUID);

		// @todo Figure out why this happens
		if (app == null)
		{
			Log.Logger.Error("Aura._UnapplyForTarget, target: {0}, caster: {1}, spell: {2} was not found in owners application map!",
							target.GUID.ToString(),
							caster ? caster.GUID.ToString() : "",
							auraApp.Base.SpellInfo.Id);

			return;
		}

		// aura has to be already applied

		_auraApplications.Remove(target.GUID);

		_removedApplications.Add(auraApp);

		// reset cooldown state for spells
		if (caster != null && SpellInfo.IsCooldownStartedOnEvent)
			// note: item based cooldowns and cooldown spell mods with charges ignored (unknown existed cases)
			caster. // note: item based cooldowns and cooldown spell mods with charges ignored (unknown existed cases)
				SpellHistory.SendCooldownEvent(SpellInfo);
	}

	// removes aura from all targets
	// and marks aura as removed
	public void _Remove(AuraRemoveMode removeMode)
	{
		_isRemoved = true;

		foreach (var pair in _auraApplications.ToList())
		{
			var aurApp = pair.Value;
			var target = aurApp.Target;
			target._UnapplyAura(aurApp, removeMode);
		}

		if (_chargeDropEvent != null)
		{
			_chargeDropEvent.ScheduleAbort();
			_chargeDropEvent = null;
		}

		ForEachAuraScript<IAuraOnRemove>(a => a.AuraRemoved(removeMode));
	}

	public void UpdateTargetMap(Unit caster, bool apply = true)
	{
		if (IsRemoved)
			return;

		_updateTargetMapInterval = UPDATE_TARGET_MAP_INTERVAL;

		// fill up to date target list
		//       target, effMask
		var targets = FillTargetMap(caster);

		List<Unit> targetsToRemove = new();

		// mark all auras as ready to remove
		foreach (var app in _auraApplications)
			// not found in current area - remove the aura
			if (!targets.TryGetValue(app.Value.Target, out var existing))
			{
				targetsToRemove.Add(app.Value.Target);
			}
			else
			{
				// needs readding - remove now, will be applied in next update cycle
				// (dbcs do not have auras which apply on same type of targets but have different radius, so this is not really needed)
				if (app.Value.Target.IsImmunedToSpell(SpellInfo, caster, true) || !CanBeAppliedOn(app.Value.Target))
				{
					targetsToRemove.Add(app.Value.Target);

					continue;
				}

				// check target immunities (for existing targets)
				foreach (var spellEffectInfo in SpellInfo.Effects)
					if (app.Value.Target.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, caster, true))
						existing.Remove(spellEffectInfo.EffectIndex);

				targets[app.Value.Target] = existing;

				// needs to add/remove effects from application, don't remove from map so it gets updated
				if (!app.Value.EffectMask.SetEquals(existing))
					continue;

				// nothing to do - aura already applied
				// remove from auras to register list
				targets.Remove(app.Value.Target);
			}

		// register auras for units
		foreach (var unit in targets.Keys.ToList())
		{
			var addUnit = true;
			// check target immunities
			var aurApp = GetApplicationOfTarget(unit.GUID);

			if (aurApp == null)
			{
				// check target immunities (for new targets)
				foreach (var spellEffectInfo in SpellInfo.Effects)
					if (unit.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, caster))
						targets[unit].Remove(spellEffectInfo.EffectIndex);

				if (targets[unit].Count == 0 || unit.IsImmunedToSpell(SpellInfo, caster) || !CanBeAppliedOn(unit))
					addUnit = false;
			}

			if (addUnit && !unit.IsHighestExclusiveAura(this, true))
				addUnit = false;

			// Dynobj auras don't hit flying targets
			if (AuraObjType == AuraObjectType.DynObj && unit.IsInFlight)
				addUnit = false;

			// Do not apply aura if it cannot stack with existing auras
			if (addUnit)
				// Allow to remove by stack when aura is going to be applied on owner
				if (unit != Owner)
					// check if not stacking aura already on target
					// this one prevents unwanted usefull buff loss because of stacking and prevents overriding auras periodicaly by 2 near area aura owners
					foreach (var iter in unit.AppliedAuras)
					{
						var aura = iter.Base;

						if (!CanStackWith(aura))
						{
							addUnit = false;

							break;
						}
					}

			if (!addUnit)
			{
				targets.Remove(unit);
			}
			else
			{
				// owner has to be in world, or effect has to be applied to self
				if (!_owner.IsSelfOrInSameMap(unit))
					// @todo There is a crash caused by shadowfiend load addon
					Log.Logger.Fatal("Aura {0}: Owner {1} (map {2}) is not in the same map as target {3} (map {4}).",
									SpellInfo.Id,
									_owner.GetName(),
									_owner.IsInWorld ? (int)_owner.Map.Id : -1,
									unit.GetName(),
									unit.IsInWorld ? (int)unit.Map.Id : -1);

				if (aurApp != null)
				{
					aurApp.UpdateApplyEffectMask(targets[unit], true); // aura is already applied, this means we need to update effects of current application
					targets.Remove(unit);
				}
				else
				{
					unit._CreateAuraApplication(this, targets[unit]);
				}
			}
		}

		// remove auras from units no longer needing them
		foreach (var unit in targetsToRemove)
		{
			var aurApp = GetApplicationOfTarget(unit.GUID);

			if (aurApp != null)
				unit._UnapplyAura(aurApp, AuraRemoveMode.Default);
		}

		if (!apply)
			return;

		// apply aura effects for units
		foreach (var pair in targets)
		{
			var aurApp = GetApplicationOfTarget(pair.Key.GUID);

			if (aurApp != null && ((!_owner.IsInWorld && _owner == pair.Key) || _owner.IsInMap(pair.Key)))
				// owner has to be in world, or effect has to be applied to self
				pair.Key._ApplyAura(aurApp, pair.Value);
		}
	}

	// targets have to be registered and not have effect applied yet to use this function
	public void _ApplyEffectForTargets(int effIndex)
	{
		// prepare list of aura targets
		List<Unit> targetList = new();

		foreach (var app in _auraApplications.Values)
			if (app.EffectsToApply.Contains(effIndex) && !app.HasEffect(effIndex))
				targetList.Add(app.Target);

		// apply effect to targets
		foreach (var unit in targetList)
			if (GetApplicationOfTarget(unit.GUID) != null)
				// owner has to be in world, or effect has to be applied to self
				unit._ApplyAuraEffect(this, effIndex);
	}

	public void UpdateOwner(uint diff, WorldObject owner)
	{
		var caster = Caster;
		// Apply spellmods for channeled auras
		// used for example when triggered spell of spell:10 is modded
		Spell modSpell = null;
		Player modOwner = null;

		if (caster != null)
		{
			modOwner = caster.SpellModOwner;

			if (modOwner != null)
			{
				modSpell = modOwner.FindCurrentSpellBySpellId(Id);

				if (modSpell != null)
					modOwner.SetSpellModTakingSpell(modSpell, true);
			}
		}

		Update(diff, caster);

		if (_updateTargetMapInterval <= diff)
			UpdateTargetMap(caster);
		else
			_updateTargetMapInterval -= (int)diff;

		// update aura effects
		foreach (var effect in AuraEffects)
			effect.Value.Update(diff, caster);

		// remove spellmods after effects update
		if (modSpell != null)
			modOwner.SetSpellModTakingSpell(modSpell, false);

		_DeleteRemovedApplications();
	}

	public int CalcMaxDuration(Unit caster)
	{
		return CalcMaxDuration(SpellInfo, caster);
	}

	public static int CalcMaxDuration(SpellInfo spellInfo, WorldObject caster)
	{
		Player modOwner = null;
		int maxDuration;

		if (caster != null)
		{
			modOwner = caster.SpellModOwner;
			maxDuration = caster.CalcSpellDuration(spellInfo);
		}
		else
		{
			maxDuration = spellInfo.Duration;
		}

		if (spellInfo.IsPassive && spellInfo.DurationEntry == null)
			maxDuration = -1;

		// IsPermanent() checks max duration (which we are supposed to calculate here)
		if (maxDuration != -1 && modOwner != null)
			modOwner.ApplySpellMod(spellInfo, SpellModOp.Duration, ref maxDuration);

		return maxDuration;
	}

	public void SetDuration(double duration, bool withMods = false, bool updateMaxDuration = false)
	{
		SetDuration((int)duration, withMods, updateMaxDuration);
	}

	public void SetDuration(int duration, bool withMods = false, bool updateMaxDuration = false)
	{
		if (withMods)
		{
			var caster = Caster;

			if (caster)
			{
				var modOwner = caster.SpellModOwner;

				if (modOwner)
					modOwner.ApplySpellMod(SpellInfo, SpellModOp.Duration, ref duration);
			}
		}

		if (updateMaxDuration && duration > _maxDuration)
			_maxDuration = duration;

		_duration = duration;
		SetNeedClientUpdateForTargets();
	}

	/// <summary>
	///  Adds the given duration to the auras duration.
	/// </summary>
	public void ModDuration(int duration, bool withMods = false, bool updateMaxDuration = false)
	{
		SetDuration(Duration + duration, withMods, updateMaxDuration);
	}

	public void ModDuration(double duration, bool withMods = false, bool updateMaxDuration = false)
	{
		SetDuration((int)duration, withMods, updateMaxDuration);
	}

	public void RefreshDuration(bool withMods = false)
	{
		var caster = Caster;

		if (withMods && caster)
		{
			var duration = _spellInfo.MaxDuration;

			// Calculate duration of periodics affected by haste.
			if (_spellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
				duration = (int)(duration * caster.UnitData.ModCastingSpeed);

			SetMaxDuration(duration);
			SetDuration(duration);
		}
		else
		{
			SetDuration(MaxDuration);
		}

		if (!_periodicCosts.Empty())
			_timeCla = 1 * Time.InMilliseconds;

		// also reset periodic counters
		foreach (var aurEff in AuraEffects)
			aurEff.Value.ResetTicks();
	}

	public void SetCharges(int charges)
	{
		if (_procCharges == charges)
			return;

		_procCharges = (byte)charges;
		_isUsingCharges = _procCharges != 0;
		SetNeedClientUpdateForTargets();
	}

	public bool ModCharges(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		if (IsUsingCharges)
		{
			var charges = _procCharges + num;
			int maxCharges = CalcMaxCharges();

			// limit charges (only on charges increase, charges may be changed manually)
			if ((num > 0) && (charges > maxCharges))
			{
				charges = maxCharges;
			}
			// we're out of charges, remove
			else if (charges <= 0)
			{
				Remove(removeMode);

				return true;
			}

			SetCharges((byte)charges);
		}

		return false;
	}

	public void ModChargesDelayed(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		_chargeDropEvent = null;
		ModCharges(num, removeMode);
	}

	public void DropChargeDelayed(uint delay, AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		// aura is already during delayed charge drop
		if (_chargeDropEvent != null)
			return;

		// only units have events
		var owner = _owner.AsUnit;

		if (!owner)
			return;

		_chargeDropEvent = new ChargeDropEvent(this, removeMode);
		owner.Events.AddEvent(_chargeDropEvent, owner.Events.CalculateTime(TimeSpan.FromMilliseconds(delay)));
	}

	public void SetStackAmount(byte stackAmount)
	{
		_stackAmount = stackAmount;
		var caster = Caster;

		var applications = GetApplicationList();

		foreach (var aurApp in applications)
			if (!aurApp.HasRemoveMode)
				HandleAuraSpecificMods(aurApp, caster, false, true);

		foreach (var aurEff in AuraEffects)
			aurEff.Value.ChangeAmount(aurEff.Value.CalculateAmount(caster), false, true);

		foreach (var aurApp in applications)
			if (!aurApp.HasRemoveMode)
				HandleAuraSpecificMods(aurApp, caster, true, true);

		SetNeedClientUpdateForTargets();
	}

	public bool IsUsingStacks()
	{
		return _spellInfo.StackAmount > 0;
	}

	public uint CalcMaxStackAmount()
	{
		var maxStackAmount = _spellInfo.StackAmount;
		var caster = Caster;

		if (caster != null)
		{
			var modOwner = caster.SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(_spellInfo, SpellModOp.MaxAuraStacks, ref maxStackAmount);
		}

		return maxStackAmount;
	}

	public bool ModStackAmount(double num, AuraRemoveMode removeMode = AuraRemoveMode.Default, bool resetPeriodicTimer = true)
	{
		return ModStackAmount((int)num, removeMode, resetPeriodicTimer);
	}

	public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default, bool resetPeriodicTimer = true)
	{
		var stackAmount = _stackAmount + num;
		var maxStackAmount = CalcMaxStackAmount();

		// limit the stack amount (only on stack increase, stack amount may be changed manually)
		if ((num > 0) && (stackAmount > maxStackAmount))
		{
			// not stackable aura - set stack amount to 1
			if (_spellInfo.StackAmount == 0)
				stackAmount = 1;
			else
				stackAmount = (int)_spellInfo.StackAmount;
		}
		// we're out of stacks, remove
		else if (stackAmount <= 0)
		{
			Remove(removeMode);

			return true;
		}

		var refresh = stackAmount >= StackAmount && (_spellInfo.StackAmount != 0 || (!_spellInfo.HasAttribute(SpellAttr1.AuraUnique) && !_spellInfo.HasAttribute(SpellAttr5.AuraUniquePerCaster)));

		// Update stack amount
		SetStackAmount((byte)stackAmount);

		if (refresh)
		{
			RefreshTimers(resetPeriodicTimer);

			// reset charges
			SetCharges(CalcMaxCharges());
		}

		SetNeedClientUpdateForTargets();

		return false;
	}

	public bool HasMoreThanOneEffectForType(AuraType auraType)
	{
		uint count = 0;

		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (HasEffect(spellEffectInfo.EffectIndex) && spellEffectInfo.ApplyAuraName == auraType)
				++count;

		return count > 1;
	}

	public bool IsArea()
	{
		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (HasEffect(spellEffectInfo.EffectIndex) && spellEffectInfo.IsAreaAuraEffect)
				return true;

		return false;
	}

	public bool IsRemovedOnShapeLost(Unit target)
	{
		return CasterGuid == target.GUID && _spellInfo.Stances != 0 && !_spellInfo.HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm) && !_spellInfo.HasAttribute(SpellAttr0.NotShapeshifted);
	}

	public bool CanBeSaved()
	{
		if (IsPassive)
			return false;

		if (SpellInfo.IsChanneled)
			return false;

		// Check if aura is single target, not only spell info
		if (CasterGuid != Owner.GUID)
		{
			// owner == caster for area auras, check for possible bad data in DB
			foreach (var spellEffectInfo in SpellInfo.Effects)
			{
				if (!spellEffectInfo.IsEffect())
					continue;

				if (spellEffectInfo.IsTargetingArea || spellEffectInfo.IsAreaAuraEffect)
					return false;
			}

			if (IsSingleTarget || SpellInfo.IsSingleTarget())
				return false;
		}

		if (SpellInfo.HasAttribute(SpellCustomAttributes.AuraCannotBeSaved))
			return false;

		// don't save auras removed by proc system
		if (IsUsingCharges && Charges == 0)
			return false;

		// don't save permanent auras triggered by items, they'll be recasted on login if necessary
		if (!CastItemGuid.IsEmpty && IsPermanent)
			return false;

		return true;
	}

	public bool IsSingleTargetWith(Aura aura)
	{
		// Same spell?
		if (SpellInfo.IsRankOf(aura.SpellInfo))
			return true;

		var spec = SpellInfo.GetSpellSpecific();

		// spell with single target specific types
		switch (spec)
		{
			case SpellSpecificType.MagePolymorph:
				if (aura.SpellInfo.GetSpellSpecific() == spec)
					return true;

				break;
			default:
				break;
		}

		return false;
	}

	public void UnregisterSingleTarget()
	{
		var caster = Caster;
		caster.SingleCastAuras.Remove(this);
		IsSingleTarget = false;
	}

	public int CalcDispelChance(Unit auraTarget, bool offensive)
	{
		// we assume that aura dispel chance is 100% on start
		// need formula for level difference based chance
		var resistChance = 0;

		// Apply dispel mod from aura caster
		var caster = Caster;

		if (caster != null)
		{
			var modOwner = caster.SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(SpellInfo, SpellModOp.DispelResistance, ref resistChance);
		}

		resistChance = resistChance < 0 ? 0 : resistChance;
		resistChance = resistChance > 100 ? 100 : resistChance;

		return 100 - resistChance;
	}

	public AuraKey GenerateKey(out uint recalculateMask)
	{
		AuraKey key = new(CasterGuid, CastItemGuid, Id, 0);
		recalculateMask = 0;

		foreach (var aurEff in _effects)
		{
			key.EffectMask |= 1u << aurEff.Key;

			if (aurEff.Value.CanBeRecalculated())
				recalculateMask |= 1u << aurEff.Key;
		}

		return key;
	}

	public void SetLoadedState(int maxduration, int duration, int charges, byte stackamount, uint recalculateMask, Dictionary<int, double> amount)
	{
		_maxDuration = maxduration;
		_duration = duration;
		_procCharges = (byte)charges;
		_isUsingCharges = _procCharges != 0;
		_stackAmount = stackamount;
		var caster = Caster;

		foreach (var effect in AuraEffects)
		{
			effect.Value.SetAmount(amount[effect.Value.EffIndex]);
			effect.Value.SetCanBeRecalculated(Convert.ToBoolean(recalculateMask & (1 << effect.Value.EffIndex)));
			effect.Value.CalculatePeriodic(caster, false, true);
			effect.Value.CalculateSpellMod();
			effect.Value.RecalculateAmount(caster);
		}
	}

	public bool HasEffectType(AuraType type)
	{
		foreach (var eff in AuraEffects)
			if (eff.Value.AuraType == type)
				return true;

		return false;
	}

	public static bool EffectTypeNeedsSendingAmount(AuraType type)
	{
		switch (type)
		{
			case AuraType.OverrideActionbarSpells:
			case AuraType.OverrideActionbarSpellsTriggered:
			case AuraType.ModSpellCategoryCooldown:
			case AuraType.ModMaxCharges:
			case AuraType.ChargeRecoveryMod:
			case AuraType.ChargeRecoveryMultiplier:
				return true;
			default:
				break;
		}

		return false;
	}

	public void RecalculateAmountOfEffects()
	{
		var caster = Caster;

		foreach (var effect in AuraEffects)
			if (!IsRemoved)
				effect.Value.RecalculateAmount(caster);
	}

	public void HandleAllEffects(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
	{
		foreach (var effect in AuraEffects)
			if (!IsRemoved)
				effect.Value.HandleEffect(aurApp, mode, apply);
	}

	public List<AuraApplication> GetApplicationList()
	{
		var applicationList = new List<AuraApplication>();

		foreach (var aurApp in _auraApplications.Values)
			if (aurApp.EffectMask.Count != 0)
				applicationList.Add(aurApp);

		return applicationList;
	}

	public void SetNeedClientUpdateForTargets()
	{
		foreach (var app in _auraApplications.Values)
			app.SetNeedClientUpdate();
	}

	// trigger effects on real aura apply/remove
	public void HandleAuraSpecificMods(AuraApplication aurApp, Unit caster, bool apply, bool onReapply)
	{
		var target = aurApp.Target;
		var removeMode = aurApp.RemoveMode;
		// handle spell_area table
		var saBounds = Global.SpellMgr.GetSpellAreaForAuraMapBounds(Id);

		if (saBounds != null)
		{
			target.GetZoneAndAreaId(out var zone, out var area);

			foreach (var spellArea in saBounds)
				// some auras remove at aura remove
				if (spellArea.Flags.HasAnyFlag(SpellAreaFlag.AutoRemove) && !spellArea.IsFitToRequirements((Player)target, zone, area))
					target.RemoveAura(spellArea.SpellId);
				// some auras applied at aura apply
				else if (spellArea.Flags.HasAnyFlag(SpellAreaFlag.AutoCast))
					if (!target.HasAura(spellArea.SpellId))
						target.CastSpell(target, spellArea.SpellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));
		}

		// handle spell_linked_spell table
		if (!onReapply)
		{
			// apply linked auras
			if (apply)
			{
				var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, Id);

				if (spellTriggered != null)
					foreach (var spell in spellTriggered)
						if (spell < 0)
							target.ApplySpellImmune(Id, SpellImmunity.Id, (uint)-spell, true);
						else if (caster != null)
							caster.AddAura((uint)spell, target);
			}
			else
			{
				// remove linked auras
				var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Remove, Id);

				if (spellTriggered != null)
					foreach (var spell in spellTriggered)
						if (spell < 0)
							target.RemoveAura((uint)-spell);
						else if (removeMode != AuraRemoveMode.Death)
							target.CastSpell(target,
											(uint)spell,
											new CastSpellExtraArgs(TriggerCastFlags.FullMask)
												.SetOriginalCaster(CasterGuid)
												.SetOriginalCastId(CastId));

				spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, Id);

				if (spellTriggered != null)
					foreach (var id in spellTriggered)
						if (id < 0)
							target.ApplySpellImmune(Id, SpellImmunity.Id, (uint)-id, false);
						else
							target.RemoveAura((uint)id, CasterGuid, removeMode);
			}
		}
		else if (apply)
		{
			// modify stack amount of linked auras
			var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, Id);

			if (spellTriggered != null)
				foreach (var id in spellTriggered)
					if (id > 0)
					{
						var triggeredAura = target.GetAura((uint)id, CasterGuid);

						if (triggeredAura != null)
							triggeredAura.ModStackAmount(StackAmount - triggeredAura.StackAmount);
					}
		}

		// mods at aura apply
		if (apply)
			switch (SpellInfo.SpellFamilyName)
			{
				case SpellFamilyNames.Generic:
					switch (Id)
					{
						case 33572: // Gronn Lord's Grasp, becomes stoned
							if (StackAmount >= 5 && !target.HasAura(33652))
								target.CastSpell(target, 33652, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));

							break;
						case 50836: //Petrifying Grip, becomes stoned
							if (StackAmount >= 5 && !target.HasAura(50812))
								target.CastSpell(target, 50812, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));

							break;
						case 60970: // Heroic Fury (remove Intercept cooldown)
							if (target.IsTypeId(TypeId.Player))
								target.SpellHistory.ResetCooldown(20252, true);

							break;
					}

					break;
				case SpellFamilyNames.Druid:
					if (caster == null)
						break;

					// Rejuvenation
					if (SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x10u) && GetEffect(0) != null)
						// Druid T8 Restoration 4P Bonus
						if (caster.HasAura(64760))
						{
							CastSpellExtraArgs args = new(GetEffect(0));
							args.AddSpellMod(SpellValueMod.BasePoint0, GetEffect(0).Amount);
							caster.CastSpell(target, 64801, args);
						}

					break;
			}
		// mods at aura remove
		else
			switch (SpellInfo.SpellFamilyName)
			{
				case SpellFamilyNames.Mage:
					switch (Id)
					{
						case 66: // Invisibility
							if (removeMode != AuraRemoveMode.Expire)
								break;

							target.CastSpell(target, 32612, new CastSpellExtraArgs(GetEffect(1)));

							break;
						default:
							break;
					}

					break;
				case SpellFamilyNames.Priest:
					if (caster == null)
						break;

					// Power word: shield
					if (removeMode == AuraRemoveMode.EnemySpell && SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000001u))
					{
						// Rapture
						var aura = caster.GetAuraOfRankedSpell(47535);

						if (aura != null)
						{
							// check cooldown
							if (caster.IsTypeId(TypeId.Player))
							{
								if (caster.SpellHistory.HasCooldown(aura.SpellInfo))
								{
									// This additional check is needed to add a minimal delay before cooldown in in effect
									// to allow all bubbles broken by a single damage source proc mana return
									if (caster.SpellHistory.GetRemainingCooldown(aura.SpellInfo) <= TimeSpan.FromSeconds(11))
										break;
								}
								else // and add if needed
								{
									caster.SpellHistory.AddCooldown(aura.Id, 0, TimeSpan.FromSeconds(12));
								}
							}

							// effect on caster
							var aurEff = aura.GetEffect(0);

							if (aurEff != null)
							{
								var multiplier = aurEff.Amount;
								CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
								args.SetOriginalCastId(CastId);
								args.AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(caster.GetMaxPower(PowerType.Mana), multiplier));
								caster.CastSpell(caster, 47755, args);
							}
						}
					}

					break;
				case SpellFamilyNames.Rogue:
					// Remove Vanish on stealth remove
					if (Id == 1784)
						target.RemoveAurasWithFamily(SpellFamilyNames.Rogue, new FlagArray128(0x0000800, 0, 0), target.GUID);

					break;
			}

		// mods at aura apply or remove
		switch (SpellInfo.SpellFamilyName)
		{
			case SpellFamilyNames.Hunter:
				switch (Id)
				{
					case 19574: // Bestial Wrath
						// The Beast Within cast on owner if talent present
						var owner = target.OwnerUnit;

						if (owner != null)
							// Search talent
							if (owner.HasAura(34692))
							{
								if (apply)
									owner.CastSpell(owner, 34471, new CastSpellExtraArgs(GetEffect(0)));
								else
									owner.RemoveAura(34471);
							}

						break;
				}

				break;
			case SpellFamilyNames.Paladin:
				switch (Id)
				{
					case 31821:
						// Aura Mastery Triggered Spell Handler
						// If apply Concentration Aura . trigger . apply Aura Mastery Immunity
						// If remove Concentration Aura . trigger . remove Aura Mastery Immunity
						// If remove Aura Mastery . trigger . remove Aura Mastery Immunity
						// Do effects only on aura owner
						if (CasterGuid != target.GUID)
							break;

						if (apply)
						{
							if ((SpellInfo.Id == 31821 && target.HasAura(19746, CasterGuid)) || (SpellInfo.Id == 19746 && target.HasAura(31821)))
								target.CastSpell(target, 64364, new CastSpellExtraArgs(true));
						}
						else
						{
							target.RemoveAurasDueToSpell(64364, CasterGuid);
						}

						break;
					case 31842: // Divine Favor
						// Item - Paladin T10 Holy 2P Bonus
						if (target.HasAura(70755))
						{
							if (apply)
								target.CastSpell(target, 71166, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));
							else
								target.RemoveAura(71166);
						}

						break;
				}

				break;
			case SpellFamilyNames.Warlock:
				// Drain Soul - If the target is at or below 25% health, Drain Soul causes four times the normal damage
				if (SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00004000u))
				{
					if (caster == null)
						break;

					if (apply)
					{
						if (target != caster && !target.HealthAbovePct(25))
							caster.CastSpell(caster, 100001, new CastSpellExtraArgs(true));
					}
					else
					{
						if (target != caster)
							caster.RemoveAura(Id);
						else
							caster.RemoveAura(100001);
					}
				}

				break;
		}
	}

	public bool CanStackWith(Aura existingAura)
	{
		// Can stack with self
		if (this == existingAura)
			return true;

		var sameCaster = CasterGuid == existingAura.CasterGuid;
		var existingSpellInfo = existingAura.SpellInfo;

		// Dynobj auras do not stack when they come from the same spell cast by the same caster
		if (AuraObjType == AuraObjectType.DynObj || existingAura.AuraObjType == AuraObjectType.DynObj)
		{
			if (sameCaster && _spellInfo.Id == existingSpellInfo.Id)
				return false;

			return true;
		}

		// passive auras don't stack with another rank of the spell cast by same caster
		if (IsPassive && sameCaster && (_spellInfo.IsDifferentRankOf(existingSpellInfo) || (_spellInfo.Id == existingSpellInfo.Id && _castItemGuid.IsEmpty)))
			return false;

		foreach (var spellEffectInfo in existingSpellInfo.Effects)
			// prevent remove triggering aura by triggered aura
			if (spellEffectInfo.TriggerSpell == Id)
				return true;

		foreach (var spellEffectInfo in SpellInfo.Effects)
			// prevent remove triggered aura by triggering aura refresh
			if (spellEffectInfo.TriggerSpell == existingAura.Id)
				return true;

		// check spell specific stack rules
		if (_spellInfo.IsAuraExclusiveBySpecificWith(existingSpellInfo) || (sameCaster && _spellInfo.IsAuraExclusiveBySpecificPerCasterWith(existingSpellInfo)))
			return false;

		// check spell group stack rules
		switch (Global.SpellMgr.CheckSpellGroupStackRules(_spellInfo, existingSpellInfo))
		{
			case SpellGroupStackRule.Exclusive:
			case SpellGroupStackRule.ExclusiveHighest: // if it reaches this point, existing aura is lower/equal
				return false;
			case SpellGroupStackRule.ExclusiveFromSameCaster:
				if (sameCaster)
					return false;

				break;
			case SpellGroupStackRule.Default:
			case SpellGroupStackRule.ExclusiveSameEffect:
			default:
				break;
		}

		if (_spellInfo.SpellFamilyName != existingSpellInfo.SpellFamilyName)
			return true;

		if (!sameCaster)
		{
			// Channeled auras can stack if not forbidden by db or aura type
			if (existingAura.SpellInfo.IsChanneled)
				return true;

			if (_spellInfo.HasAttribute(SpellAttr3.DotStackingRule))
				return true;

			// check same periodic auras
			bool hasPeriodicNonAreaEffect(SpellInfo spellInfo)
			{
				foreach (var spellEffectInfo in spellInfo.Effects)
					switch (spellEffectInfo.ApplyAuraName)
					{
						// DOT or HOT from different casters will stack
						case AuraType.PeriodicDamage:
						case AuraType.PeriodicDummy:
						case AuraType.PeriodicHeal:
						case AuraType.PeriodicTriggerSpell:
						case AuraType.PeriodicEnergize:
						case AuraType.PeriodicManaLeech:
						case AuraType.PeriodicLeech:
						case AuraType.PowerBurn:
						case AuraType.ObsModPower:
						case AuraType.ObsModHealth:
						case AuraType.PeriodicTriggerSpellWithValue:
						{
							// periodic auras which target areas are not allowed to stack this way (replenishment for example)
							if (spellEffectInfo.IsTargetingArea)
								return false;

							return true;
						}
						default:
							break;
					}

				return false;
			}

			if (hasPeriodicNonAreaEffect(_spellInfo) && hasPeriodicNonAreaEffect(existingSpellInfo))
				return true;
		}

		if (HasEffectType(AuraType.ControlVehicle) && existingAura.HasEffectType(AuraType.ControlVehicle))
		{
			Vehicle veh = null;

			if (Owner.AsUnit)
				veh = Owner.AsUnit.VehicleKit1;

			if (!veh) // We should probably just let it stack. Vehicle system will prevent undefined behaviour later
				return true;

			if (veh.GetAvailableSeatCount() == 0)
				return false; // No empty seat available

			return true; // Empty seat available (skip rest)
		}

		if (HasEffectType(AuraType.ShowConfirmationPrompt) || HasEffectType(AuraType.ShowConfirmationPromptWithDifficulty))
			if (existingAura.HasEffectType(AuraType.ShowConfirmationPrompt) || existingAura.HasEffectType(AuraType.ShowConfirmationPromptWithDifficulty))
				return false;

		// spell of same spell rank chain
		if (_spellInfo.IsRankOf(existingSpellInfo))
		{
			// don't allow passive area auras to stack
			if (_spellInfo.IsMultiSlotAura && !IsArea())
				return true;

			if (!CastItemGuid.IsEmpty && !existingAura.CastItemGuid.IsEmpty)
				if (CastItemGuid != existingAura.CastItemGuid && _spellInfo.HasAttribute(SpellCustomAttributes.EnchantProc))
					return true;

			// same spell with same caster should not stack
			return false;
		}

		return true;
	}

	public bool IsProcOnCooldown(DateTime now)
	{
		return _procCooldown > now;
	}

	public void AddProcCooldown(SpellProcEntry procEntry, DateTime now)
	{
		// cooldowns should be added to the whole aura (see 51698 area aura)
		var procCooldown = (int)procEntry.Cooldown;
		var caster = Caster;

		if (caster != null)
		{
			var modOwner = caster.SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(SpellInfo, SpellModOp.ProcCooldown, ref procCooldown);
		}

		_procCooldown = now + TimeSpan.FromMilliseconds(procCooldown);
	}

	public void ResetProcCooldown()
	{
		_procCooldown = DateTime.Now;
	}

	public void PrepareProcToTrigger(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
	{
		var prepare = CallScriptPrepareProcHandlers(aurApp, eventInfo);

		if (!prepare)
			return;

		var procEntry = Global.SpellMgr.GetSpellProcEntry(SpellInfo);

		PrepareProcChargeDrop(procEntry, eventInfo);

		// cooldowns should be added to the whole aura (see 51698 area aura)
		AddProcCooldown(procEntry, now);

		SetLastProcSuccessTime(now);
	}

	public void PrepareProcChargeDrop(SpellProcEntry procEntry, ProcEventInfo eventInfo)
	{
		// take one charge, aura expiration will be handled in Aura.TriggerProcOnEvent (if needed)
		if (!procEntry.AttributesMask.HasAnyFlag(ProcAttributes.UseStacksForCharges) && IsUsingCharges && (eventInfo.SpellInfo == null || !eventInfo.SpellInfo.HasAttribute(SpellAttr6.DoNotConsumeResources)))
		{
			--_procCharges;
			SetNeedClientUpdateForTargets();
		}
	}

	public void ConsumeProcCharges(SpellProcEntry procEntry)
	{
		// Remove aura if we've used last charge to proc
		if (procEntry.AttributesMask.HasFlag(ProcAttributes.UseStacksForCharges))
			ModStackAmount(-1);
		else if (IsUsingCharges)
			if (Charges == 0)
				Remove();
	}

	public HashSet<int> GetProcEffectMask(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
	{
		SpellProcEntry procEntry = null;
		ForEachAuraScript<IAuraOverrideProcInfo>(a => procEntry = a.SpellProcEntry);

		if (procEntry == null)
			Global.SpellMgr.GetSpellProcEntry(SpellInfo);

		// only auras with spell proc entry can trigger proc
		if (procEntry == null)
			return DummyHashset;

		// check spell triggering us
		var spell = eventInfo.ProcSpell;

		if (spell)
		{
			// Do not allow auras to proc from effect triggered from itself
			if (spell.IsTriggeredByAura(_spellInfo))
				return DummyHashset;

			// check if aura can proc when spell is triggered (exception for hunter auto shot & wands)
			if (!spell.TriggeredAllowProc && !SpellInfo.HasAttribute(SpellAttr3.CanProcFromProcs) && !procEntry.AttributesMask.HasFlag(ProcAttributes.TriggeredCanProc) && !eventInfo.TypeMask.HasFlag(ProcFlags.AutoAttackMask))
				if (spell.IsTriggered && !spell.SpellInfo.HasAttribute(SpellAttr3.NotAProc))
					return DummyHashset;

			if (spell.CastItem != null && procEntry.AttributesMask.HasFlag(ProcAttributes.CantProcFromItemCast))
				return DummyHashset;

			if (spell.SpellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs) && SpellInfo.HasAttribute(SpellAttr6.AuraIsWeaponProc))
				return DummyHashset;

			if (SpellInfo.HasAttribute(SpellAttr12.OnlyProcFromClassAbilities) && !spell.SpellInfo.HasAttribute(SpellAttr13.AllowClassAbilityProcs))
				return DummyHashset;
		}

		// check don't break stealth attr present
		if (_spellInfo.HasAura(AuraType.ModStealth))
		{
			var eventSpellInfo = eventInfo.SpellInfo;

			if (eventSpellInfo != null)
				if (eventSpellInfo.HasAttribute(SpellCustomAttributes.DontBreakStealth))
					return DummyHashset;
		}

		// check if we have charges to proc with
		if (IsUsingCharges)
		{
			if (Charges == 0)
				return DummyHashset;

			if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReqSpellmod))
			{
				var eventSpell = eventInfo.ProcSpell;

				if (eventSpell != null)
					if (!eventSpell.AppliedMods.Contains(this))
						return DummyHashset;
			}
		}

		// check proc cooldown
		if (IsProcOnCooldown(now))
			return DummyHashset;

		// do checks against db data

		if (!SpellManager.CanSpellTriggerProcOnEvent(procEntry, eventInfo))
			return DummyHashset;

		// do checks using conditions table
		if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.SpellProc, Id, eventInfo.Actor, eventInfo.ActionTarget))
			return DummyHashset;

		// AuraScript Hook
		var check = CallScriptCheckProcHandlers(aurApp, eventInfo);

		if (!check)
			return DummyHashset;

		// At least one effect has to pass checks to proc aura
		var procEffectMask = aurApp.EffectMask.ToHashSet();

		foreach (var aurEff in AuraEffects)
			if (procEffectMask.Contains(aurEff.Key))
				if ((procEntry.DisableEffectsMask & (1u << aurEff.Key)) != 0 || !aurEff.Value.CheckEffectProc(aurApp, eventInfo))
					procEffectMask.Remove(aurEff.Key);

		if (procEffectMask.Count == 0)
			return DummyHashset;

		// @todo
		// do allow additional requirements for procs
		// this is needed because this is the last moment in which you can prevent aura charge drop on proc
		// and possibly a way to prevent default checks (if there're going to be any)

		// Check if current equipment meets aura requirements
		// do that only for passive spells
		// @todo this needs to be unified for all kinds of auras
		var target = aurApp.Target;

		if (IsPassive && target.IsPlayer && SpellInfo.EquippedItemClass != ItemClass.None)
			if (!SpellInfo.HasAttribute(SpellAttr3.NoProcEquipRequirement))
			{
				Item item = null;

				if (SpellInfo.EquippedItemClass == ItemClass.Weapon)
				{
					if (target.AsPlayer.IsInFeralForm)
						return DummyHashset;

					var damageInfo = eventInfo.DamageInfo;

					if (damageInfo != null)
					{
						if (damageInfo.AttackType != WeaponAttackType.OffAttack)
							item = target.AsPlayer.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
						else
							item = target.AsPlayer.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
					}
				}
				else if (SpellInfo.EquippedItemClass == ItemClass.Armor)
				{
					// Check if player is wearing shield
					item = target.AsPlayer.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
				}

				if (!item || item.IsBroken || !item.IsFitToSpellRequirements(SpellInfo))
					return DummyHashset;
			}

		if (_spellInfo.HasAttribute(SpellAttr3.OnlyProcOutdoors))
			if (!target.IsOutdoors)
				return DummyHashset;

		if (_spellInfo.HasAttribute(SpellAttr3.OnlyProcOnCaster))
			if (target.GUID != CasterGuid)
				return DummyHashset;

		if (!_spellInfo.HasAttribute(SpellAttr4.AllowProcWhileSitting))
			if (!target.IsStandState)
				return DummyHashset;

		var success = RandomHelper.randChance(CalcProcChance(procEntry, eventInfo));

		SetLastProcAttemptTime(now);

		if (success)
			return procEffectMask;

		return DummyHashset;
	}

	public void TriggerProcOnEvent(HashSet<int> procEffectMask, AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		var prevented = CallScriptProcHandlers(aurApp, eventInfo);

		if (!prevented)
		{
			foreach (var aurEff in AuraEffects)
			{
				if (!procEffectMask.Contains(aurEff.Key))
					continue;

				// OnEffectProc / AfterEffectProc hooks handled in AuraEffect.HandleProc()
				if (aurApp.HasEffect(aurEff.Key))
					aurEff.Value.HandleProc(aurApp, eventInfo);
			}

			CallScriptAfterProcHandlers(aurApp, eventInfo);
		}

		ConsumeProcCharges(Global.SpellMgr.GetSpellProcEntry(SpellInfo));
	}

	public double CalcPPMProcChance(Unit actor)
	{
		// Formula see http://us.battle.net/wow/en/forum/topic/8197741003#1
		var ppm = _spellInfo.CalcProcPPM(actor, CastItemLevel);
		var averageProcInterval = 60.0f / ppm;

		var currentTime = GameTime.Now();
		var secondsSinceLastAttempt = Math.Min((float)(currentTime - _lastProcAttemptTime).TotalSeconds, 10.0f);
		var secondsSinceLastProc = Math.Min((float)(currentTime - _lastProcSuccessTime).TotalSeconds, 1000.0f);

		var chance = Math.Max(1.0f, 1.0f + ((secondsSinceLastProc / averageProcInterval - 1.5f) * 3.0f)) * ppm * secondsSinceLastAttempt / 60.0f;
		MathFunctions.RoundToInterval(ref chance, 0.0f, 1.0f);

		return chance * 100.0f;
	}

	public void LoadScripts()
	{
		_loadedScripts = Global.ScriptMgr.CreateAuraScripts(_spellInfo.Id, this);

		foreach (var script in _loadedScripts)
		{
			Log.Logger.Debug("Aura.LoadScripts: Script `{0}` for aura `{1}` is loaded now", script._GetScriptName(), _spellInfo.Id);
			script.Register();

			if (script is IAuraScript)
				foreach (var iFace in script.GetType().GetInterfaces())
				{
					if (iFace.Name == nameof(IBaseSpellScript) || iFace.Name == nameof(IAuraScript))
						continue;

					if (!_auraScriptsByType.TryGetValue(iFace, out var spellScripts))
					{
						spellScripts = new List<IAuraScript>();
						_auraScriptsByType[iFace] = spellScripts;
					}

					spellScripts.Add(script);
					RegisterSpellEffectHandler(script);
				}
		}
	}

	public List<IAuraScript> GetAuraScripts<T>() where T : IAuraScript
	{
		if (_auraScriptsByType.TryGetValue(typeof(T), out var scripts))
			return scripts;

		return Dummy;
	}

	public void ForEachAuraScript<T>(Action<T> action) where T : IAuraScript
	{
		foreach (T script in GetAuraScripts<T>())
			try
			{
				action.Invoke(script);
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public List<(IAuraScript, IAuraEffectHandler)> GetEffectScripts(AuraScriptHookType h, int index)
	{
		if (_effectHandlers.TryGetValue(index, out var effDict) &&
			effDict.TryGetValue(h, out var scripts))
			return scripts;

		return DummyAuraEffects;
	}

	public bool UsesScriptType<T>()
	{
		return _auraScriptsByType.ContainsKey(typeof(T));
	}

	public virtual void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		ForEachAuraScript<IAuraOnRemove>(a => a.AuraRemoved(removeMode));
	}

	public void _RegisterForTargets()
	{
		var caster = Caster;
		UpdateTargetMap(caster, false);
	}

	public void ApplyForTargets()
	{
		var caster = Caster;
		UpdateTargetMap(caster, true);
	}

	public void SetMaxDuration(double duration)
	{
		SetMaxDuration((int)duration);
	}

	public void SetMaxDuration(int duration)
	{
		_maxDuration = duration;
	}

	public int CalcMaxDuration()
	{
		return CalcMaxDuration(Caster);
	}

	public byte CalcMaxCharges()
	{
		return CalcMaxCharges(Caster);
	}

	public bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		return ModCharges(-1, removeMode);
	}

	public bool HasEffect(int index)
	{
		return GetEffect(index) != null;
	}

	public AuraEffect GetEffect(int index)
	{
		if (_effects.TryGetValue(index, out var val))
			return val;

		return null;
	}

	public bool TryGetEffect(int index, out AuraEffect val)
	{
		if (_effects.TryGetValue(index, out val))
			return true;

		return false;
	}

	public AuraApplication GetApplicationOfTarget(ObjectGuid guid)
	{
		return _auraApplications.LookupByKey(guid);
	}

	public bool IsAppliedOnTarget(ObjectGuid guid)
	{
		return _auraApplications.ContainsKey(guid);
	}

	public virtual Dictionary<Unit, HashSet<int>> FillTargetMap(Unit caster)
	{
		return DummyAuraFill;
	}

	public void SetLastProcAttemptTime(DateTime lastProcAttemptTime)
	{
		_lastProcAttemptTime = lastProcAttemptTime;
	}

	public void SetLastProcSuccessTime(DateTime lastProcSuccessTime)
	{
		_lastProcSuccessTime = lastProcSuccessTime;
	}

	public UnitAura ToUnitAura()
	{
		if (AuraObjType == AuraObjectType.Unit) return (UnitAura)this;

		return null;
	}

	public DynObjAura ToDynObjAura()
	{
		if (AuraObjType == AuraObjectType.DynObj) return (DynObjAura)this;

		return null;
	}

	//Static Methods
	public static HashSet<int> BuildEffectMaskForOwner(SpellInfo spellProto, HashSet<int> availableEffectMask, WorldObject owner)
	{
		var effMask = new HashSet<int>();

		switch (owner.TypeId)
		{
			case TypeId.Unit:
			case TypeId.Player:
				foreach (var spellEffectInfo in spellProto.Effects)
					if (spellEffectInfo.IsUnitOwnedAuraEffect)
						effMask.Add(spellEffectInfo.EffectIndex);

				break;
			case TypeId.DynamicObject:
				foreach (var spellEffectInfo in spellProto.Effects)
					if (spellEffectInfo.Effect == SpellEffectName.PersistentAreaAura)
						effMask.Add(spellEffectInfo.EffectIndex);

				break;
			default:
				break;
		}

		effMask.IntersectWith(availableEffectMask);

		return effMask;
	}

	public static Aura TryRefreshStackOrCreate(AuraCreateInfo createInfo, bool updateEffectMask = true)
	{
		createInfo.IsRefresh = false;

		createInfo.AuraEffectMask = BuildEffectMaskForOwner(createInfo.SpellInfo, createInfo.AuraEffectMask, createInfo.Owner);
		createInfo.TargetEffectMask = createInfo.AuraEffectMask.ToHashSet();

		var effMask = createInfo.AuraEffectMask;

		if (createInfo.TargetEffectMask.Count != 0)
			effMask = createInfo.TargetEffectMask;

		if (effMask.Count == 0)
			return null;

		var foundAura = createInfo.Owner.AsUnit._TryStackingOrRefreshingExistingAura(createInfo);

		if (foundAura != null)
		{
			// we've here aura, which script triggered removal after modding stack amount
			// check the state here, so we won't create new Aura object
			if (foundAura.IsRemoved)
				return null;

			createInfo.IsRefresh = true;

			// add owner
			var unit = createInfo.Owner.AsUnit;

			// check effmask on owner application (if existing)
			if (updateEffectMask)
			{
				var aurApp = foundAura.GetApplicationOfTarget(unit.GUID);

				if (aurApp != null)
					aurApp.UpdateApplyEffectMask(effMask, false);
			}

			return foundAura;
		}
		else
		{
			return Create(createInfo);
		}
	}

	public static Aura TryCreate(AuraCreateInfo createInfo)
	{
		var effMask = createInfo.AuraEffectMask;

		if (createInfo.TargetEffectMask.Count != 0)
			effMask = createInfo.TargetEffectMask;

		effMask = BuildEffectMaskForOwner(createInfo.SpellInfo, effMask, createInfo.Owner);

		if (effMask.Count == 0)
			return null;

		return Create(createInfo);
	}

	public static Aura Create(AuraCreateInfo createInfo)
	{
		// try to get caster of aura
		if (!createInfo.CasterGuid.IsEmpty)
		{
			if (createInfo.CasterGuid.IsUnit)
			{
				if (createInfo.OwnerInternal.GUID == createInfo.CasterGuid)
					createInfo.Caster = createInfo.OwnerInternal.AsUnit;
				else
					createInfo.Caster = Global.ObjAccessor.GetUnit(createInfo.OwnerInternal, createInfo.CasterGuid);
			}
		}
		else if (createInfo.Caster != null)
		{
			createInfo.CasterGuid = createInfo.Caster.GUID;
		}

		// check if aura can be owned by owner
		if (createInfo.Owner.IsTypeMask(TypeMask.Unit))
			if (!createInfo.Owner.IsInWorld || createInfo.Owner.AsUnit.IsDuringRemoveFromWorld)
				// owner not in world so don't allow to own not self casted single target auras
				if (createInfo.CasterGuid != createInfo.Owner.GUID && createInfo.SpellInfo.IsSingleTarget())
					return null;

		Aura aura;

		switch (createInfo.Owner.TypeId)
		{
			case TypeId.Unit:
			case TypeId.Player:
				aura = new UnitAura(createInfo);

				// aura can be removed in Unit::_AddAura call
				if (aura.IsRemoved)
					return null;

				// add owner
				var effMask = createInfo.AuraEffectMask;

				if (createInfo.TargetEffectMask.Count != 0)
					effMask = createInfo.TargetEffectMask;

				effMask = BuildEffectMaskForOwner(createInfo.SpellInfo, effMask, createInfo.Owner);

				var unit = createInfo.Owner.AsUnit;
				aura.ToUnitAura().AddStaticApplication(unit, effMask);

				break;
			case TypeId.DynamicObject:
				createInfo.AuraEffectMask = BuildEffectMaskForOwner(createInfo.SpellInfo, createInfo.AuraEffectMask, createInfo.Owner);

				aura = new DynObjAura(createInfo);

				break;
			default:
				return null;
		}

		// scripts, etc.
		if (aura.IsRemoved)
			return null;

		return aura;
	}

	WorldObject GetWorldObjectCaster()
	{
		if (CasterGuid.IsUnit)
			return Caster;

		return Global.ObjAccessor.GetWorldObject(Owner, CasterGuid);
	}

	void Update(uint diff, Unit caster)
	{
		ForEachAuraScript<IAuraOnUpdate>(u => u.AuraOnUpdate(diff));

		if (_duration > 0)
		{
			_duration -= (int)diff;

			if (_duration < 0)
				_duration = 0;

			// handle manaPerSecond/manaPerSecondPerLevel
			if (_timeCla != 0)
			{
				if (_timeCla > diff)
					_timeCla -= (int)diff;
				else if (caster != null && (caster == Owner || !SpellInfo.HasAttribute(SpellAttr2.NoTargetPerSecondCosts)))
					if (!_periodicCosts.Empty())
					{
						_timeCla += (int)(1000 - diff);

						foreach (var power in _periodicCosts)
						{
							if (power.RequiredAuraSpellID != 0 && !caster.HasAura(power.RequiredAuraSpellID))
								continue;

							var manaPerSecond = (int)power.ManaPerSecond;

							if (power.PowerType != PowerType.Health)
								manaPerSecond += MathFunctions.CalculatePct(caster.GetMaxPower(power.PowerType), power.PowerPctPerSecond);
							else
								manaPerSecond += (int)MathFunctions.CalculatePct(caster.MaxHealth, power.PowerPctPerSecond);

							if (manaPerSecond != 0)
							{
								if (power.PowerType == PowerType.Health)
								{
									if ((int)caster.Health > manaPerSecond)
										caster.ModifyHealth(-manaPerSecond);
									else
										Remove();
								}
								else if (caster.GetPower(power.PowerType) >= manaPerSecond)
								{
									caster.ModifyPower(power.PowerType, -manaPerSecond);
								}
								else
								{
									Remove();
								}
							}
						}
					}
			}
		}
	}

	void RefreshTimers(bool resetPeriodicTimer)
	{
		_maxDuration = CalcMaxDuration();

		if (_spellInfo.HasAttribute(SpellAttr8.DontResetPeriodicTimer))
		{
			var minPeriod = _maxDuration;

			foreach (var aurEff in AuraEffects)
			{
				var period = aurEff.Value.Period;

				if (period != 0)
					minPeriod = Math.Min(period, minPeriod);
			}

			// If only one tick remaining, roll it over into new duration
			if (Duration <= minPeriod)
			{
				_maxDuration += Duration;
				resetPeriodicTimer = false;
			}
		}

		RefreshDuration();
		var caster = Caster;

		foreach (var aurEff in AuraEffects)
			aurEff.Value.CalculatePeriodic(caster, resetPeriodicTimer, false);
	}

	bool CanBeAppliedOn(Unit target)
	{
		foreach (var label in SpellInfo.Labels)
			if (target.HasAuraTypeWithMiscvalue(AuraType.SuppressItemPassiveEffectBySpellLabel, (int)label))
				return false;

		// unit not in world or during remove from world
		if (!target.IsInWorld || target.IsDuringRemoveFromWorld)
		{
			// area auras mustn't be applied
			if (Owner != target)
				return false;

			// not selfcasted single target auras mustn't be applied
			if (CasterGuid != Owner.GUID && SpellInfo.IsSingleTarget())
				return false;

			return true;
		}
		else
		{
			return CheckAreaTarget(target);
		}
	}

	bool CheckAreaTarget(Unit target)
	{
		return CallScriptCheckAreaTargetHandlers(target);
	}

	double CalcProcChance(SpellProcEntry procEntry, ProcEventInfo eventInfo)
	{
		double chance = procEntry.Chance;
		// calculate chances depending on unit with caster's data
		// so talents modifying chances and judgements will have properly calculated proc chance
		var caster = Caster;

		if (caster != null)
		{
			// calculate ppm chance if present and we're using weapon
			if (eventInfo.DamageInfo != null && procEntry.ProcsPerMinute != 0)
			{
				var WeaponSpeed = caster.GetBaseAttackTime(eventInfo.DamageInfo.AttackType);
				chance = caster.GetPPMProcChance(WeaponSpeed, procEntry.ProcsPerMinute, SpellInfo);
			}

			if (SpellInfo.ProcBasePpm > 0.0f)
				chance = CalcPPMProcChance(caster);

			// apply chance modifer aura, applies also to ppm chance (see improved judgement of light spell)
			var modOwner = caster.SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(SpellInfo, SpellModOp.ProcChance, ref chance);
		}

		// proc chance is reduced by an additional 3.333% per level past 60
		if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReduceProc60) && eventInfo.Actor.Level > 60)
			chance = Math.Max(0.0f, (1.0f - ((eventInfo.Actor.Level - 60) * 1.0f / 30.0f)) * chance);

		return chance;
	}

	void _DeleteRemovedApplications()
	{
		_removedApplications.Clear();
	}

	private void RegisterSpellEffectHandler(AuraScript script)
	{
		if (script is IHasAuraEffects hse)
			foreach (var effect in hse.AuraEffects)
				if (effect is IAuraEffectHandler se)
				{
					uint mask = 0;

					if (se.EffectIndex == SpellConst.EffectAll || se.EffectIndex == SpellConst.EffectFirstFound)
					{
						foreach (var aurEff in AuraEffects)
						{
							if (se.EffectIndex == SpellConst.EffectFirstFound && mask != 0)
								break;

							if (CheckAuraEffectHandler(se, aurEff.Key))
								AddAuraEffect(aurEff.Key, script, se);
						}
					}
					else
					{
						if (CheckAuraEffectHandler(se, se.EffectIndex))
							AddAuraEffect(se.EffectIndex, script, se);
					}
				}
	}

	private bool CheckAuraEffectHandler(IAuraEffectHandler ae, int effIndex)
	{
		if (_spellInfo.Effects.Count <= effIndex)
			return false;

		var spellEffectInfo = _spellInfo.GetEffect(effIndex);

		if (spellEffectInfo.ApplyAuraName == 0 && ae.AuraType == 0)
			return true;

		if (spellEffectInfo.ApplyAuraName == 0)
			return false;

		return ae.AuraType == AuraType.Any || spellEffectInfo.ApplyAuraName == ae.AuraType;
	}

	private void AddAuraEffect(int index, IAuraScript script, IAuraEffectHandler effect)
	{
		if (!_effectHandlers.TryGetValue(index, out var effecTypes))
		{
			effecTypes = new Dictionary<AuraScriptHookType, List<(IAuraScript, IAuraEffectHandler)>>();
			_effectHandlers.Add(index, effecTypes);
		}

		if (!effecTypes.TryGetValue(effect.HookType, out var effects))
		{
			effects = new List<(IAuraScript, IAuraEffectHandler)>();
			effecTypes.Add(effect.HookType, effects);
		}

		effects.Add((script, effect));
	}

	byte CalcMaxCharges(Unit caster)
	{
		var maxProcCharges = _spellInfo.ProcCharges;
		var procEntry = Global.SpellMgr.GetSpellProcEntry(SpellInfo);

		if (procEntry != null)
			maxProcCharges = procEntry.Charges;

		if (caster != null)
		{
			var modOwner = caster.SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(SpellInfo, SpellModOp.ProcCharges, ref maxProcCharges);
		}

		return (byte)maxProcCharges;
	}

	#region CallScripts

	bool CallScriptCheckAreaTargetHandlers(Unit target)
	{
		var result = true;

		foreach (IAuraCheckAreaTarget auraScript in GetAuraScripts<IAuraCheckAreaTarget>())
			try
			{
				auraScript._PrepareScriptCall(AuraScriptHookType.CheckAreaTarget);

				result &= auraScript.CheckAreaTarget(target);

				auraScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return result;
	}

	public void CallScriptDispel(DispelInfo dispelInfo)
	{
		foreach (IAuraOnDispel auraScript in GetAuraScripts<IAuraOnDispel>())
			try
			{
				auraScript._PrepareScriptCall(AuraScriptHookType.Dispel);

				auraScript.OnDispel(dispelInfo);

				auraScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptAfterDispel(DispelInfo dispelInfo)
	{
		foreach (IAfterAuraDispel auraScript in GetAuraScripts<IAfterAuraDispel>())
			try
			{
				auraScript._PrepareScriptCall(AuraScriptHookType.AfterDispel);

				auraScript.HandleDispel(dispelInfo);

				auraScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public bool CallScriptEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
	{
		var preventDefault = false;

		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectApply, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectApply, aurApp);

				((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

				if (!preventDefault)
					preventDefault = auraScript.Item1._IsDefaultActionPrevented();

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return preventDefault;
	}

	public bool CallScriptEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
	{
		var preventDefault = false;

		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectRemove, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectRemove, aurApp);

				((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

				if (!preventDefault)
					preventDefault = auraScript.Item1._IsDefaultActionPrevented();

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return preventDefault;
	}

	public void CallScriptAfterEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterApply, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterApply, aurApp);

				((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptAfterEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterRemove, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterRemove, aurApp);

				((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public bool CallScriptEffectPeriodicHandlers(AuraEffect aurEff, AuraApplication aurApp)
	{
		var preventDefault = false;

		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectPeriodic, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectPeriodic, aurApp);

				((IAuraPeriodic)auraScript.Item2).HandlePeriodic(aurEff);

				if (!preventDefault)
					preventDefault = auraScript.Item1._IsDefaultActionPrevented();

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return preventDefault;
	}

	public void CallScriptEffectUpdatePeriodicHandlers(AuraEffect aurEff)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectUpdatePeriodic, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectUpdatePeriodic);

				((IAuraUpdatePeriodic)auraScript.Item2).UpdatePeriodic(aurEff);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectCalcAmountHandlers(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcAmount, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcAmount);

				((IAuraCalcAmount)auraScript.Item2).HandleCalcAmount(aurEff, ref amount, ref canBeRecalculated);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectCalcPeriodicHandlers(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcPeriodic, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcPeriodic);

				var boxed = new BoxedValue<bool>(isPeriodic);
				var amp = new BoxedValue<int>(amplitude);

				((IAuraCalcPeriodic)auraScript.Item2).CalcPeriodic(aurEff, boxed, amp);

				isPeriodic = boxed.Value;
				amplitude = amp.Value;

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectCalcSpellModHandlers(AuraEffect aurEff, SpellModifier spellMod)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcSpellmod, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcSpellmod);

				((IAuraCalcSpellMod)auraScript.Item2).CalcSpellMod(aurEff, spellMod);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectCalcCritChanceHandlers(AuraEffect aurEff, AuraApplication aurApp, Unit victim, ref double critChance)
	{
		foreach (var loadedScript in GetEffectScripts(AuraScriptHookType.EffectCalcCritChance, aurEff.EffIndex))
			try
			{
				loadedScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcCritChance, aurApp);

				critChance = ((IAuraCalcCritChance)loadedScript.Item2).CalcCritChance(aurEff, victim, critChance);

				loadedScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount, ref bool defaultPrevented)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAbsorb, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAbsorb, aurApp);

				absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

				defaultPrevented = auraScript.Item1._IsDefaultActionPrevented();
				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectAfterAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterAbsorb, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterAbsorb, aurApp);

				absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, HealInfo healInfo, ref double absorbAmount, ref bool defaultPrevented)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAbsorbHeal, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAbsorb, aurApp);

				absorbAmount = ((IAuraEffectAbsorbHeal)auraScript.Item2).HandleAbsorb(aurEff, healInfo, absorbAmount);

				defaultPrevented = auraScript.Item1._IsDefaultActionPrevented();
				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectAfterAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, HealInfo healInfo, ref double absorbAmount)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterAbsorb, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterAbsorbHeal, aurApp);

				absorbAmount = ((IAuraEffectAbsorbHeal)auraScript.Item2).HandleAbsorb(aurEff, healInfo, absorbAmount);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount, ref bool defaultPrevented)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectManaShield, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectManaShield, aurApp);

				absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectAfterManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterManaShield, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterManaShield, aurApp);

				absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEffectSplitHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double splitAmount)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectSplit, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectSplit, aurApp);

				splitAmount = ((IAuraSplitHandler)auraScript.Item2).Split(aurEff, dmgInfo, splitAmount);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public void CallScriptEnterLeaveCombatHandlers(AuraApplication aurApp, bool isNowInCombat)
	{
		foreach (IAuraEnterLeaveCombat loadedScript in GetAuraScripts<IAuraEnterLeaveCombat>())
			try
			{
				loadedScript._PrepareScriptCall(AuraScriptHookType.EnterLeaveCombat, aurApp);

				loadedScript.EnterLeaveCombat(isNowInCombat);

				loadedScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public bool CallScriptCheckProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		var result = true;

		foreach (IAuraCheckProc auraScript in GetAuraScripts<IAuraCheckProc>())
			try
			{
				auraScript._PrepareScriptCall(AuraScriptHookType.CheckProc, aurApp);

				result &= auraScript.CheckProc(eventInfo);

				auraScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return result;
	}

	public bool CallScriptPrepareProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		var prepare = true;

		foreach (IAuraPrepareProc auraScript in GetAuraScripts<IAuraPrepareProc>())
			try
			{
				auraScript._PrepareScriptCall(AuraScriptHookType.PrepareProc, aurApp);

				auraScript.DoPrepareProc(eventInfo);

				if (prepare)
					prepare = !auraScript._IsDefaultActionPrevented();

				auraScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return prepare;
	}

	public bool CallScriptProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		var handled = false;

		foreach (IAuraOnProc auraScript in GetAuraScripts<IAuraOnProc>())
			try
			{
				auraScript._PrepareScriptCall(AuraScriptHookType.Proc, aurApp);

				auraScript.OnProc(eventInfo);

				handled |= auraScript._IsDefaultActionPrevented();
				auraScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return handled;
	}

	public void CallScriptAfterProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		foreach (IAuraAfterProc auraScript in GetAuraScripts<IAuraAfterProc>())
			try
			{
				auraScript._PrepareScriptCall(AuraScriptHookType.AfterProc, aurApp);

				auraScript.AfterProc(eventInfo);

				auraScript._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public bool CallScriptCheckEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		var result = true;

		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.CheckEffectProc, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.CheckEffectProc, aurApp);

				result &= ((IAuraCheckEffectProc)auraScript.Item2).CheckProc(aurEff, eventInfo);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return result;
	}

	public bool CallScriptEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		var preventDefault = false;

		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectProc, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectProc, aurApp);

				((IAuraEffectProcHandler)auraScript.Item2).HandleProc(aurEff, eventInfo);

				if (!preventDefault)
					preventDefault = auraScript.Item1._IsDefaultActionPrevented();

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}

		return preventDefault;
	}

	public void CallScriptAfterEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
	{
		foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterProc, aurEff.EffIndex))
			try
			{
				auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterProc, aurApp);

				((IAuraEffectProcHandler)auraScript.Item2).HandleProc(aurEff, eventInfo);

				auraScript.Item1._FinishScriptCall();
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
	}

	public virtual string GetDebugInfo()
	{
		return $"Id: {Id} Name: '{SpellInfo.SpellName[Global.WorldMgr.DefaultDbcLocale]}' Caster: {CasterGuid}\nOwner: {(Owner != null ? Owner.GetDebugInfo() : "NULL")}";
	}

	#endregion
}
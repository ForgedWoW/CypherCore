// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Maps.Grids;
using Game.Maps.Interfaces;
using Game.Movement;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.ISpell;

namespace Game.Spells;

public partial class Spell : IDisposable
{
	public SpellInfo SpellInfo;
	public Item CastItem;
	public ObjectGuid CastItemGuid;
	public uint CastItemEntry;
	public int CastItemLevel;
	public ObjectGuid CastId;
	public ObjectGuid OriginalCastId;
	public bool FromClient;
	public SpellCastFlagsEx CastFlagsEx;
	public SpellMisc SpellMisc;
	public object CustomArg;
	public SpellCastVisual SpellVisual;
	public SpellCastTargets Targets;
	public sbyte ComboPointGain;
	public SpellCustomErrors CustomErrors;

	public List<Aura> AppliedMods;
	public SpellValue SpellValue;
	public Spell SelfContainer;

	// Current targets, to be used in SpellEffects (MUST BE USED ONLY IN SPELL EFFECTS)
	public Unit UnitTarget;
	public Item ItemTarget;
	public GameObject GameObjTarget;
	public Corpse CorpseTarget;
	public WorldLocation DestTarget;
	public double Damage;
	public SpellMissInfo TargetMissInfo;
	public double Variance;
	public SpellEffectInfo EffectInfo;

	// Damage and healing in effects need just calculate
	public double DamageInEffects;  // Damge   in effects count here
	public double HealingInEffects; // Healing in effects count here

	// *****************************************
	// Spell target subsystem
	// *****************************************
	// Targets store structures and data
	public List<TargetInfo> UniqueTargetInfo = new();
	public List<TargetInfo> UniqueTargetInfoOrgi = new();

	// if need this can be replaced by Aura copy
	// we can't store original aura link to prevent access to deleted auras
	// and in same time need aura data and after aura deleting.
	public SpellInfo TriggeredByAuraSpell;
	static readonly List<ISpellScript> Dummy = new();
	static readonly List<(ISpellScript, ISpellEffect)> DummySpellEffects = new();

	//Spell data
	internal SpellSchoolMask SpellSchoolMask; // Spell school (can be overwrite for some spells (wand shoot for example)
	internal WeaponAttackType AttackType;     // For weapon based attack

	internal bool NeedComboPoints;

	// used in effects handlers
	internal UnitAura SpellAura;
	internal DynObjAura DynObjAura;

	// ******************************************
	// Spell trigger system
	// ******************************************
	internal ProcFlagsInit ProcAttacker; // Attacker trigger flags
	internal ProcFlagsInit ProcVictim;   // Victim   trigger flags
	internal ProcFlagsHit HitMask;
	readonly Dictionary<Type, List<ISpellScript>> _spellScriptsByType = new();
	readonly Dictionary<int, Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>> _effectHandlers = new();


	readonly Dictionary<SpellEffectName, SpellLogEffect> _executeLogEffects = new();
	readonly WorldObject _caster;
	readonly bool _canReflect; // can reflect this spell?
	readonly Dictionary<int, double> _damageMultipliers = new();

	readonly List<GOTargetInfo> _uniqueGoTargetInfo = new();
	readonly List<ItemTargetInfo> _uniqueItemInfo = new();
	readonly List<CorpseTargetInfo> _uniqueCorpseTargetInfo = new();
	readonly Dictionary<int, SpellDestination> _destTargets = new();
	readonly List<HitTriggerSpell> _hitTriggerSpells = new();
	readonly TriggerCastFlags _triggeredCastFlags;

	List<SpellScript> _loadedScripts = new();
	PathGenerator _preGeneratedPath;
	ObjectGuid _originalCasterGuid;
	Unit _originalCaster;

	List<SpellPowerCost> _powerCosts = new();
	int _casttime;          // Calculated spell cast time initialized only in Spell.prepare
	int _channeledDuration; // Calculated channeled spell duration in order to calculate correct pushback.
	bool _autoRepeat;
	byte _runesState;
	byte _delayAtDamageCount;

	// Delayed spells system
	ulong _delayStart;      // time of spell delay start, filled by event handler, zero = just started
	ulong _delayMoment;     // moment of next delay call, used internally
	bool _launchHandled;    // were launch actions handled
	bool _immediateHandled; // were immediate actions handled? (used by delayed spells only)

	// These vars are used in both delayed spell system and modified immediate spell system
	bool _referencedFromCurrentSpell;
	bool _executedCurrently;
	uint _applyMultiplierMask;
	SpellEffectHandleMode _effectHandleMode;

	// -------------------------------------------
	GameObject _focusObject;
	uint _channelTargetEffectMask; // Mask req. alive targets

	SpellState _spellState;
	int _timer;

	// Empower spell meta
	byte _empoweredSpellStage;
	uint _empoweredSpellDelta;

	SpellEvent _spellEvent;

	public SpellState State
	{
		get => _spellState;
		set => _spellState = value;
	}

	public int CastTime => _casttime;

	private bool IsAutoRepeat
	{
		get => _autoRepeat;
		set => _autoRepeat = value;
	}

	public bool IsDeletable => !_referencedFromCurrentSpell && !_executedCurrently;

	public bool IsInterruptable => !_executedCurrently;

	public ulong DelayStart
	{
		get => _delayStart;
		set => _delayStart = value;
	}

	public ulong DelayMoment => _delayMoment;

	public WorldObject Caster => _caster;

	public ObjectGuid OriginalCasterGuid => _originalCasterGuid;

	public Unit OriginalCaster => _originalCaster;

	public List<SpellPowerCost> PowerCost => _powerCosts;

	private bool DontReport => Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.DontReportCastError);

	public bool IsEmpowered => SpellInfo.EmpowerStages.Count > 0 && _caster.IsPlayer;

	public byte? EmpoweredStage { get; set; }

	public Spell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGuid = default, ObjectGuid originalCastId = default, byte? empoweredStage = null)
	{
		SpellInfo = info;
		_caster = (info.HasAttribute(SpellAttr6.OriginateFromController) && caster.CharmerOrOwner != null ? caster.CharmerOrOwner : caster);
		SpellValue = new SpellValue(SpellInfo, caster);
		NeedComboPoints = SpellInfo.NeedsComboPoints;

		// Get data for type of attack
		AttackType = info.GetAttackType();

		SpellSchoolMask = SpellInfo.GetSchoolMask(); // Can be override for some spell (wand shoot for example)

		if (originalCasterGuid.IsEmpty)
			_originalCasterGuid = _caster.GUID;

		var playerCaster = _caster.AsPlayer;

		if (playerCaster != null)
			// wand case
			if (AttackType == WeaponAttackType.RangedAttack)
				if ((playerCaster.ClassMask & (uint)Class.ClassMaskWandUsers) != 0)
				{
					var pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

					if (pItem != null)
						SpellSchoolMask = (SpellSchoolMask)(1 << (int)pItem.GetTemplate().GetDamageType());
				}

		var modOwner = caster.GetSpellModOwner();

		if (modOwner != null)
			modOwner.ApplySpellMod(info, SpellModOp.Doses, ref SpellValue.AuraStackAmount, this);


		if (_originalCasterGuid == _caster.GUID)
		{
			_originalCaster = _caster.AsUnit;
		}
		else
		{
			_originalCaster = Global.ObjAccessor.GetUnit(_caster, _originalCasterGuid);

			if (_originalCaster != null && !_originalCaster.IsInWorld)
				_originalCaster = null;
			else
				_originalCaster = _caster.AsUnit;
		}

		_triggeredCastFlags = triggerFlags;

		if (info.HasAttribute(SpellAttr2.DoNotReportSpellFailure))
			_triggeredCastFlags = _triggeredCastFlags | TriggerCastFlags.DontReportCastError;

		if (SpellInfo.HasAttribute(SpellAttr4.AllowCastWhileCasting))
			_triggeredCastFlags = _triggeredCastFlags | TriggerCastFlags.IgnoreCastInProgress;

		CastItemLevel = -1;

		if (IsIgnoringCooldowns())
			CastFlagsEx |= SpellCastFlagsEx.IgnoreCooldown;

		CastId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, _caster.Location.MapId, SpellInfo.Id, _caster.GetMap().GenerateLowGuid(HighGuid.Cast));
		OriginalCastId = originalCastId;
		SpellVisual.SpellXSpellVisualID = caster.GetCastSpellXSpellVisualId(SpellInfo);

		//Auto Shot & Shoot (wand)
		_autoRepeat = SpellInfo.IsAutoRepeatRangedSpell;

		// Determine if spell can be reflected back to the caster
		// Patch 1.2 notes: Spell Reflection no longer reflects abilities
		_canReflect = caster.IsUnit && SpellInfo.DmgClass == SpellDmgClass.Magic && !SpellInfo.HasAttribute(SpellAttr0.IsAbility) && !SpellInfo.HasAttribute(SpellAttr1.NoReflection) && !SpellInfo.HasAttribute(SpellAttr0.NoImmunities) && !SpellInfo.IsPassive;
		CleanupTargetList();

		foreach (var effect in SpellInfo.Effects)
			_destTargets[effect.EffectIndex] = new SpellDestination(_caster);

		Targets = new SpellCastTargets();
		AppliedMods = new List<Aura>();
		EmpoweredStage = empoweredStage;
	}

	public virtual void Dispose()
	{
		// unload scripts
		for (var i = 0; i < _loadedScripts.Count; ++i)
			_loadedScripts[i]._Unload();

		if (_referencedFromCurrentSpell && SelfContainer && SelfContainer == this)
		{
			// Clean the reference to avoid later crash.
			// If this error is repeating, we may have to add an ASSERT to better track down how we get into this case.
			Log.outError(LogFilter.Spells, "SPELL: deleting spell for spell ID {0}. However, spell still referenced.", SpellInfo.Id);
			SelfContainer = null;
		}

		if (_caster && _caster.TypeId == TypeId.Player)
			Cypher.Assert(_caster.AsPlayer.SpellModTakingSpell != this);
	}

	public void InitExplicitTargets(SpellCastTargets targets)
	{
		Targets = targets;

		// this function tries to correct spell explicit targets for spell
		// client doesn't send explicit targets correctly sometimes - we need to fix such spells serverside
		// this also makes sure that we correctly send explicit targets to client (removes redundant data)
		var neededTargets = SpellInfo.GetExplicitTargetMask();

		var target = Targets.ObjectTarget;

		if (target != null)
		{
			// check if object target is valid with needed target flags
			// for unit case allow corpse target mask because player with not released corpse is a unit target
			if ((target.AsUnit && !neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)) || (target.IsTypeId(TypeId.GameObject) && !neededTargets.HasFlag(SpellCastTargetFlags.GameobjectMask)) || (target.IsTypeId(TypeId.Corpse) && !neededTargets.HasFlag(SpellCastTargetFlags.CorpseMask)))
				Targets.RemoveObjectTarget();
		}
		else
		{
			// try to select correct unit target if not provided by client or by serverside cast
			if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask))
			{
				Unit unit = null;
				// try to use player selection as a target
				var playerCaster = _caster.AsPlayer;

				if (playerCaster != null)
				{
					// selection has to be found and to be valid target for the spell
					var selectedUnit = Global.ObjAccessor.GetUnit(_caster, playerCaster.Target);

					if (selectedUnit != null)
						if (SpellInfo.CheckExplicitTarget(_caster, selectedUnit) == SpellCastResult.SpellCastOk)
							unit = selectedUnit;
				}
				// try to use attacked unit as a target
				else if (_caster.IsTypeId(TypeId.Unit) && neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.Unit))
				{
					unit = _caster.AsUnit.Victim;
				}

				// didn't find anything - let's use self as target
				if (unit == null && neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitAlly))
					unit = _caster.AsUnit;

				Targets.
				UnitTarget = unit;
			}
		}

		// check if spell needs dst target
		if (neededTargets.HasFlag(SpellCastTargetFlags.DestLocation))
		{
			// and target isn't set
			if (!Targets.HasDst)
			{
				// try to use unit target if provided
				var targett = targets.ObjectTarget;

				if (targett != null)
					Targets.SetDst(targett);
				// or use self if not available
				else
					Targets.SetDst(_caster);
			}
		}
		else
		{
			Targets.RemoveDst();
		}

		if (neededTargets.HasFlag(SpellCastTargetFlags.SourceLocation))
		{
			if (!targets.HasSrc)
				Targets.SetSrc(_caster);
		}
		else
		{
			Targets.RemoveSrc();
		}
	}

	public void SelectSpellTargets()
	{
		// select targets for cast phase
		SelectExplicitTargets();

		uint processedAreaEffectsMask = 0;

		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			// not call for empty effect.
			// Also some spells use not used effect targets for store targets for dummy effect in triggered spells
			if (!spellEffectInfo.IsEffect())
				continue;

			// set expected type of implicit targets to be sent to client
			var implicitTargetMask = SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetA.ObjectType) | SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetB.ObjectType);

			if (Convert.ToBoolean(implicitTargetMask & SpellCastTargetFlags.Unit))
				Targets.SetTargetFlag(SpellCastTargetFlags.Unit);

			if (Convert.ToBoolean(implicitTargetMask & (SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem)))
				Targets.SetTargetFlag(SpellCastTargetFlags.Gameobject);

			SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetA, ref processedAreaEffectsMask);
			SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetB, ref processedAreaEffectsMask);

			// Select targets of effect based on effect type
			// those are used when no valid target could be added for spell effect based on spell target type
			// some spell effects use explicit target as a default target added to target map (like SPELL_EFFECT_LEARN_SPELL)
			// some spell effects add target to target map only when target type specified (like SPELL_EFFECT_WEAPON)
			// some spell effects don't add anything to target map (confirmed with sniffs) (like SPELL_EFFECT_DESTROY_ALL_TOTEMS)
			SelectEffectTypeImplicitTargets(spellEffectInfo);

			if (Targets.HasDst)
				AddDestTarget(Targets.Dst, spellEffectInfo.EffectIndex);

			if (spellEffectInfo.TargetA.ObjectType == SpellTargetObjectTypes.Unit || spellEffectInfo.TargetA.ObjectType == SpellTargetObjectTypes.UnitAndDest || spellEffectInfo.TargetB.ObjectType == SpellTargetObjectTypes.Unit || spellEffectInfo.TargetB.ObjectType == SpellTargetObjectTypes.UnitAndDest)
			{
				if (SpellInfo.HasAttribute(SpellAttr1.RequireAllTargets))
				{
					var noTargetFound = !UniqueTargetInfo.Any(target => (target.EffectMask & 1 << spellEffectInfo.EffectIndex) != 0);

					if (noTargetFound)
					{
						SendCastResult(SpellCastResult.BadImplicitTargets);
						Finish(SpellCastResult.BadImplicitTargets);

						return;
					}
				}

				if (SpellInfo.HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
				{
					var anyNonImmuneTargetFound = UniqueTargetInfo.Any(target => (target.EffectMask & 1 << spellEffectInfo.EffectIndex) != 0 && target.MissCondition != SpellMissInfo.Immune && target.MissCondition != SpellMissInfo.Immune2);

					if (!anyNonImmuneTargetFound)
					{
						SendCastResult(SpellCastResult.Immune);
						Finish(SpellCastResult.Immune);

						return;
					}
				}
			}

			if (SpellInfo.IsChanneled)
			{
				// maybe do this for all spells?
				if (_focusObject == null && UniqueTargetInfo.Empty() && _uniqueGoTargetInfo.Empty() && _uniqueItemInfo.Empty() && !Targets.HasDst)
				{
					SendCastResult(SpellCastResult.BadImplicitTargets);
					Finish(SpellCastResult.BadImplicitTargets);

					return;
				}

				var mask = (1u << spellEffectInfo.EffectIndex);

				foreach (var ihit in UniqueTargetInfo)
					if (Convert.ToBoolean(ihit.EffectMask & mask))
					{
						_channelTargetEffectMask |= mask;

						break;
					}
			}
		}

		var dstDelay = CalculateDelayMomentForDst(SpellInfo.LaunchDelay);

		if (dstDelay != 0)
			_delayMoment = dstDelay;
	}

	public void RecalculateDelayMomentForDst()
	{
		_delayMoment = CalculateDelayMomentForDst(0.0f);
		_caster.Events.ModifyEventTime(_spellEvent, TimeSpan.FromMilliseconds(DelayStart + _delayMoment));
	}

	public GridMapTypeMask GetSearcherTypeMask(SpellTargetObjectTypes objType, List<Condition> condList)
	{
		// this function selects which containers need to be searched for spell target
		var retMask = GridMapTypeMask.All;

		// filter searchers based on searched object type
		switch (objType)
		{
			case SpellTargetObjectTypes.Unit:
			case SpellTargetObjectTypes.UnitAndDest:
				retMask &= GridMapTypeMask.Player | GridMapTypeMask.Creature;

				break;
			case SpellTargetObjectTypes.Corpse:
			case SpellTargetObjectTypes.CorpseEnemy:
			case SpellTargetObjectTypes.CorpseAlly:
				retMask &= GridMapTypeMask.Player | GridMapTypeMask.Corpse | GridMapTypeMask.Creature;

				break;
			case SpellTargetObjectTypes.Gobj:
			case SpellTargetObjectTypes.GobjItem:
				retMask &= GridMapTypeMask.GameObject;

				break;
			default:
				break;
		}

		if (SpellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
			retMask &= GridMapTypeMask.Corpse | GridMapTypeMask.Player;

		if (SpellInfo.HasAttribute(SpellAttr3.OnlyOnGhosts))
			retMask &= GridMapTypeMask.Player;

		if (SpellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
			retMask &= ~GridMapTypeMask.Player;

		if (condList != null)
			retMask &= Global.ConditionMgr.GetSearcherTypeMaskForConditionList(condList);

		return retMask;
	}

	public void CleanupTargetList()
	{
		UniqueTargetInfo.Clear();
		_uniqueGoTargetInfo.Clear();
		_uniqueItemInfo.Clear();
		_delayMoment = 0;
	}

	public long GetUnitTargetCountForEffect(int effect)
	{
		return UniqueTargetInfo.Count(targetInfo => targetInfo.MissCondition == SpellMissInfo.None && (targetInfo.EffectMask & (1 << effect)) != 0);
	}

	public long GetGameObjectTargetCountForEffect(int effect)
	{
		return _uniqueGoTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << effect)) != 0);
	}

	public long GetItemTargetCountForEffect(int effect)
	{
		return _uniqueItemInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << effect)) != 0);
	}

	public long GetCorpseTargetCountForEffect(int effect)
	{
		return _uniqueCorpseTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1u << effect)) != 0);
	}

	public SpellMissInfo PreprocessSpellHit(Unit unit, TargetInfo hitInfo)
	{
		if (unit == null)
			return SpellMissInfo.Evade;

		// Target may have begun evading between launch and hit phases - re-check now
		var creatureTarget = unit.AsCreature;

		if (creatureTarget != null && creatureTarget.IsEvadingAttacks)
			return SpellMissInfo.Evade;

		// For delayed spells immunity may be applied between missile launch and hit - check immunity for that case
		if (SpellInfo.HasHitDelay && unit.IsImmunedToSpell(SpellInfo, _caster))
			return SpellMissInfo.Immune;

		CallScriptBeforeHitHandlers(hitInfo.MissCondition);

		var player = unit.AsPlayer;

		if (player != null)
		{
			player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, SpellInfo.Id);
			player.UpdateCriteria(CriteriaType.BeSpellTarget, SpellInfo.Id, 0, 0, _caster);
			player.UpdateCriteria(CriteriaType.GainAura, SpellInfo.Id);
		}

		var casterPlayer = _caster.AsPlayer;

		if (casterPlayer)
		{
			casterPlayer.StartCriteriaTimer(CriteriaStartEvent.CastSpell, SpellInfo.Id);
			casterPlayer.UpdateCriteria(CriteriaType.LandTargetedSpellOnTarget, SpellInfo.Id, 0, 0, unit);
		}

		if (_caster != unit)
		{
			// Recheck  UNIT_FLAG_NON_ATTACKABLE for delayed spells
			if (SpellInfo.HasHitDelay && unit.HasUnitFlag(UnitFlags.NonAttackable) && unit.CharmerOrOwnerGUID != _caster.GUID)
				return SpellMissInfo.Evade;

			if (_caster.IsValidAttackTarget(unit, SpellInfo))
			{
				unit.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.HostileActionReceived);
			}
			else if (_caster.IsFriendlyTo(unit))
			{
				// for delayed spells ignore negative spells (after duel end) for friendly targets
				if (SpellInfo.HasHitDelay && unit.IsPlayer && !IsPositive() && !_caster.IsValidAssistTarget(unit, SpellInfo))
					return SpellMissInfo.Evade;

				// assisting case, healing and resurrection
				if (unit.HasUnitState(UnitState.AttackPlayer))
				{
					var playerOwner = _caster.GetCharmerOrOwnerPlayerOrPlayerItself();

					if (playerOwner != null)
					{
						playerOwner.SetContestedPvP();
						playerOwner.UpdatePvP(true);
					}
				}

				if (_originalCaster && unit.IsInCombat && SpellInfo.HasInitialAggro)
				{
					if (_originalCaster.HasUnitFlag(UnitFlags.PlayerControlled))          // only do explicit combat forwarding for PvP enabled units
						_originalCaster.GetCombatManager().InheritCombatStatesFrom(unit); // for creature v creature combat, the threat forward does it for us

					unit.GetThreatManager().ForwardThreatForAssistingMe(_originalCaster, 0.0f, null, true);
				}
			}
		}

		// original caster for auras
		var origCaster = _caster;

		if (_originalCaster)
			origCaster = _originalCaster;

		// check immunity due to diminishing returns
		if (Aura.BuildEffectMaskForOwner(SpellInfo, SpellConst.MaxEffectMask, unit) != 0)
		{
			foreach (var spellEffectInfo in SpellInfo.Effects)
				hitInfo.AuraBasePoints[spellEffectInfo.EffectIndex] = (SpellValue.CustomBasePointsMask & (1 << spellEffectInfo.EffectIndex)) != 0 ? SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex] : spellEffectInfo.CalcBaseValue(_originalCaster, unit, CastItemEntry, CastItemLevel);

			// Get Data Needed for Diminishing Returns, some effects may have multiple auras, so this must be done on spell hit, not aura add
			hitInfo.DrGroup = SpellInfo.DiminishingReturnsGroupForSpell;

			var diminishLevel = DiminishingLevels.Level1;

			if (hitInfo.DrGroup != 0)
			{
				diminishLevel = unit.GetDiminishing(hitInfo.DrGroup);
				var type = SpellInfo.DiminishingReturnsGroupType;

				// Increase Diminishing on unit, current informations for actually casts will use values above
				if (type == DiminishingReturnsType.All || (type == DiminishingReturnsType.Player && unit.IsAffectedByDiminishingReturns))
					unit.IncrDiminishing(SpellInfo);
			}

			// Now Reduce spell duration using data received at spell hit
			// check whatever effects we're going to apply, diminishing returns only apply to negative aura effects
			hitInfo.Positive = true;

			if (origCaster == unit || !origCaster.IsFriendlyTo(unit))
				foreach (var spellEffectInfo in SpellInfo.Effects)
					// mod duration only for effects applying aura!
					if ((hitInfo.EffectMask & (1 << spellEffectInfo.EffectIndex)) != 0 &&
						spellEffectInfo.IsUnitOwnedAuraEffect() &&
						!SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
					{
						hitInfo.Positive = false;

						break;
					}

			hitInfo.AuraDuration = Aura.CalcMaxDuration(SpellInfo, origCaster);

			// unit is immune to aura if it was diminished to 0 duration
			if (!hitInfo.Positive && !unit.ApplyDiminishingToDuration(SpellInfo, ref hitInfo.AuraDuration, origCaster, diminishLevel))
				if (SpellInfo.Effects.All(effInfo => !effInfo.IsEffect() || effInfo.IsEffect(SpellEffectName.ApplyAura)))
					return SpellMissInfo.Immune;
		}

		return SpellMissInfo.None;
	}

	public void DoSpellEffectHit(Unit unit, SpellEffectInfo spellEffectInfo, TargetInfo hitInfo)
	{
		var aura_effmask = Aura.BuildEffectMaskForOwner(SpellInfo, 1u << spellEffectInfo.EffectIndex, unit);

		if (aura_effmask != 0)
		{
			var caster = _caster;

			if (_originalCaster)
				caster = _originalCaster;

			if (caster != null)
			{
				// delayed spells with multiple targets need to create a new aura object, otherwise we'll access a deleted aura
				if (hitInfo.HitAura == null)
				{
					var resetPeriodicTimer = (SpellInfo.StackAmount < 2) && !_triggeredCastFlags.HasFlag(TriggerCastFlags.DontResetPeriodicTimer);
					var allAuraEffectMask = Aura.BuildEffectMaskForOwner(SpellInfo, SpellConst.MaxEffectMask, unit);

					AuraCreateInfo createInfo = new(CastId, SpellInfo, GetCastDifficulty(), allAuraEffectMask, unit);
					createInfo.SetCasterGuid(caster.GUID);
					createInfo.SetBaseAmount(hitInfo.AuraBasePoints);
					createInfo.SetCastItem(CastItemGuid, CastItemEntry, CastItemLevel);
					createInfo.SetPeriodicReset(resetPeriodicTimer);
					createInfo.SetOwnerEffectMask(aura_effmask);

					var aura = Aura.TryRefreshStackOrCreate(createInfo, false);

					if (aura != null)
					{
						hitInfo.HitAura = aura.ToUnitAura();

						// Set aura stack amount to desired value
						if (SpellValue.AuraStackAmount > 1)
						{
							if (!createInfo.IsRefresh)
								hitInfo.HitAura.SetStackAmount((byte)SpellValue.AuraStackAmount);
							else
								hitInfo.HitAura.ModStackAmount(SpellValue.AuraStackAmount);
						}

						hitInfo.HitAura.SetDiminishGroup(hitInfo.DrGroup);

						if (!SpellValue.Duration.HasValue)
						{
							hitInfo.AuraDuration = caster.ModSpellDuration(SpellInfo, unit, hitInfo.AuraDuration, hitInfo.Positive, hitInfo.HitAura.GetEffectMask());

							if (hitInfo.AuraDuration > 0)
							{
								hitInfo.AuraDuration *= (int)SpellValue.DurationMul;

								// Haste modifies duration of channeled spells
								if (SpellInfo.IsChanneled)
								{
									caster.ModSpellDurationTime(SpellInfo, ref hitInfo.AuraDuration, this);
								}
								else if (SpellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
								{
									var origDuration = hitInfo.AuraDuration;
									hitInfo.AuraDuration = 0;

									foreach (var auraEff in hitInfo.HitAura.AuraEffects)
									{
										var period = auraEff.Value.Period;

										if (period != 0) // period is hastened by UNIT_MOD_CAST_SPEED
											hitInfo.AuraDuration = Math.Max(Math.Max(origDuration / period, 1) * period, hitInfo.AuraDuration);
									}

									// if there is no periodic effect
									if (hitInfo.AuraDuration == 0)
										hitInfo.AuraDuration = (int)(origDuration * _originalCaster.UnitData.ModCastingSpeed);
								}
							}
						}
						else
						{
							hitInfo.AuraDuration = SpellValue.Duration.Value;
						}

						if (hitInfo.AuraDuration != hitInfo.HitAura.MaxDuration)
						{
							hitInfo.HitAura.SetMaxDuration(hitInfo.AuraDuration);
							hitInfo.HitAura.SetDuration(hitInfo.AuraDuration);
						}

						if (createInfo.IsRefresh)
							hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
					}
				}
				else
				{
					hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
				}
			}
		}

		SpellAura = hitInfo.HitAura;
		HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
		SpellAura = null;
	}

	public void DoTriggersOnSpellHit(Unit unit)
	{
		// handle SPELL_AURA_ADD_TARGET_TRIGGER auras
		// this is executed after spell proc spells on target hit
		// spells are triggered for each hit spell target
		// info confirmed with retail sniffs of permafrost and shadow weaving
		if (!_hitTriggerSpells.Empty())
		{
			var _duration = 0;

			foreach (var hit in _hitTriggerSpells)
				if (CanExecuteTriggersOnHit(unit, hit.TriggeredByAura) && RandomHelper.randChance(hit.Chance))
				{
					_caster.CastSpell(unit,
									hit.TriggeredSpell.Id,
									new CastSpellExtraArgs(TriggerCastFlags.FullMask)
										.SetTriggeringSpell(this)
										.SetCastDifficulty(hit.TriggeredSpell.Difficulty));

					Log.outDebug(LogFilter.Spells, "Spell {0} triggered spell {1} by SPELL_AURA_ADD_TARGET_TRIGGER aura", SpellInfo.Id, hit.TriggeredSpell.Id);

					// SPELL_AURA_ADD_TARGET_TRIGGER auras shouldn't trigger auras without duration
					// set duration of current aura to the triggered spell
					if (hit.TriggeredSpell.Duration == -1)
					{
						var triggeredAur = unit.GetAura(hit.TriggeredSpell.Id, _caster.GUID);

						if (triggeredAur != null)
						{
							// get duration from aura-only once
							if (_duration == 0)
							{
								var aur = unit.GetAura(SpellInfo.Id, _caster.GUID);
								_duration = aur != null ? aur.Duration : -1;
							}

							triggeredAur.SetDuration(_duration);
						}
					}
				}
		}

		// trigger linked auras remove/apply
		// @todo remove/cleanup this, as this table is not documented and people are doing stupid things with it
		var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Hit, SpellInfo.Id);

		if (spellTriggered != null)
			foreach (var id in spellTriggered)
				if (id < 0)
					unit.RemoveAura((uint)-id);
				else
					unit.CastSpell(unit, (uint)id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(_caster.GUID).SetTriggeringSpell(this));
	}

	public SpellCastResult Prepare(SpellCastTargets targets, AuraEffect triggeredByAura = null)
	{
		if (CastItem != null)
		{
			CastItemGuid = CastItem.GUID;
			CastItemEntry = CastItem.Entry;

			var owner = CastItem.GetOwner();

			if (owner)
			{
				CastItemLevel = (int)CastItem.GetItemLevel(owner);
			}
			else if (CastItem.OwnerGUID == _caster.GUID)
			{
				CastItemLevel = (int)CastItem.GetItemLevel(_caster.AsPlayer);
			}
			else
			{
				SendCastResult(SpellCastResult.EquippedItem);
				Finish(SpellCastResult.EquippedItem);

				return SpellCastResult.EquippedItem;
			}
		}

		InitExplicitTargets(targets);

		_spellState = SpellState.Preparing;

		if (triggeredByAura != null)
		{
			TriggeredByAuraSpell = triggeredByAura.SpellInfo;
			CastItemLevel = triggeredByAura.Base.CastItemLevel;
		}

		// create and add update event for this spell
		_spellEvent = new SpellEvent(this);
		_caster.Events.AddEvent(_spellEvent, _caster.Events.CalculateTime(TimeSpan.FromMilliseconds(1)));

		// check disables
		if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, _caster))
		{
			SendCastResult(SpellCastResult.SpellUnavailable);
			Finish(SpellCastResult.SpellUnavailable);

			return SpellCastResult.SpellUnavailable;
		}

		// Prevent casting at cast another spell (ServerSide check)
		if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress) && _caster.AsUnit != null && _caster.AsUnit.IsNonMeleeSpellCast(false, true, true, SpellInfo.Id == 75) && !CastId.IsEmpty)
		{
			SendCastResult(SpellCastResult.SpellInProgress);
			Finish(SpellCastResult.SpellInProgress);

			return SpellCastResult.SpellInProgress;
		}

		LoadScripts();

		// Fill cost data (not use power for item casts
		if (CastItem == null)
			_powerCosts = SpellInfo.CalcPowerCost(_caster, SpellSchoolMask, this);

		// Set combo point requirement
		if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreComboPoints) || CastItem != null)
			NeedComboPoints = false;

		int param1 = 0, param2 = 0;
		var result = CheckCast(true, ref param1, ref param2);

		// target is checked in too many locations and with different results to handle each of them
		// handle just the general SPELL_FAILED_BAD_TARGETS result which is the default result for most DBC target checks
		if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreTargetCheck) && result == SpellCastResult.BadTargets)
			result = SpellCastResult.SpellCastOk;

		if (result != SpellCastResult.SpellCastOk)
		{
			// Periodic auras should be interrupted when aura triggers a spell which can't be cast
			// for example bladestorm aura should be removed on disarm as of patch 3.3.5
			// channeled periodic spells should be affected by this (arcane missiles, penance, etc)
			// a possible alternative sollution for those would be validating aura target on unit state change
			if (triggeredByAura != null && triggeredByAura.IsPeriodic() && !triggeredByAura.Base.IsPassive)
			{
				SendChannelUpdate(0);
				triggeredByAura.Base.SetDuration(0);
			}

			if (param1 != 0 || param2 != 0)
				SendCastResult(result, param1, param2);
			else
				SendCastResult(result);

			// queue autorepeat spells for future repeating
			if (GetCurrentContainer() == CurrentSpellTypes.AutoRepeat && _caster.IsUnit)
				_caster.				AsUnit.SetCurrentCastSpell(this);

			Finish(result);

			return result;
		}

		// Prepare data for triggers
		PrepareDataForTriggerSystem();

		_casttime = CallScriptCalcCastTimeHandlers(SpellInfo.CalcCastTime(this));

		if (_caster.IsUnit && _caster.AsUnit.IsMoving)
		{
			result = CheckMovement();

			if (result != SpellCastResult.SpellCastOk)
			{
				SendCastResult(result);
				Finish(result);

				return result;
			}
		}

		// Creatures focus their target when possible
		if (_casttime != 0 && _caster.IsCreature && !SpellInfo.IsNextMeleeSwingSpell && !IsAutoRepeat && !_caster.AsUnit.HasUnitFlag(UnitFlags.Possessed))
		{
			// Channeled spells and some triggered spells do not focus a cast target. They face their target later on via channel object guid and via spell attribute or not at all
			var focusTarget = !SpellInfo.IsChanneled && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing);

			if (focusTarget && Targets.ObjectTarget && _caster != Targets.ObjectTarget)
				_caster.				AsCreature.SetSpellFocus(this, Targets.ObjectTarget);
			else
				_caster.				AsCreature.SetSpellFocus(this, null);
		}

		CallScriptOnPrecastHandler();

		// set timer base at cast time
		ReSetTimer();

		Log.outDebug(LogFilter.Spells, "Spell.prepare: spell id {0} source {1} caster {2} customCastFlags {3} mask {4}", SpellInfo.Id, _caster.Entry, _originalCaster != null ? (int)_originalCaster.Entry : -1, _triggeredCastFlags, Targets.TargetMask);

		if (SpellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
			SendSpellCooldown();

		//Containers for channeled spells have to be set
		// @todoApply this to all casted spells if needed
		// Why check duration? 29350: channelled triggers channelled
		if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.CastDirectly) && (!SpellInfo.IsChanneled || SpellInfo.MaxDuration == 0))
		{
			Cast(true);
		}
		else
		{
			// commented out !m_spellInfo->StartRecoveryTime, it forces instant spells with global cooldown to be processed in spell::update
			// as a result a spell that passed CheckCast and should be processed instantly may suffer from this delayed process
			// the easiest bug to observe is LoS check in AddUnitTarget, even if spell passed the CheckCast LoS check the situation can change in spell::update
			// because target could be relocated in the meantime, making the spell fly to the air (no targets can be registered, so no effects processed, nothing in combat log)
			var willCastDirectly = _casttime == 0 && /*!m_spellInfo->StartRecoveryTime && */ GetCurrentContainer() == CurrentSpellTypes.Generic;

			var unitCaster = _caster.AsUnit;

			if (unitCaster != null)
			{
				// stealth must be removed at cast starting (at show channel bar)
				// skip triggered spell (item equip spell casting and other not explicit character casts/item uses)
				if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) && !SpellInfo.HasAttribute(SpellAttr2.NotAnAction))
					unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Action, SpellInfo);

				// Do not register as current spell when requested to ignore cast in progress
				// We don't want to interrupt that other spell with cast time
				if (!willCastDirectly || !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress))
					unitCaster.SetCurrentCastSpell(this);
			}

			SendSpellStart();

			if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreGCD))
				TriggerGlobalCooldown();

			// Call CreatureAI hook OnSpellStart
			var caster = _caster.AsCreature;

			if (caster != null)
				if (caster.IsAIEnabled)
					caster.GetAI().OnSpellStart(SpellInfo);

			if (willCastDirectly)
				Cast(true);
		}

		return SpellCastResult.SpellCastOk;
	}

	public void Cancel()
	{
		if (_spellState == SpellState.Finished)
			return;

		var oldState = _spellState;
		_spellState = SpellState.Finished;

		_autoRepeat = false;

		switch (oldState)
		{
			case SpellState.Preparing:
				CancelGlobalCooldown();
				goto case SpellState.Delayed;
			case SpellState.Delayed:
				SendInterrupted(0);
				SendCastResult(SpellCastResult.Interrupted);

				break;

			case SpellState.Casting:
				foreach (var ihit in UniqueTargetInfo)
					if (ihit.MissCondition == SpellMissInfo.None)
					{
						var unit = _caster.GUID == ihit.TargetGuid ? _caster.AsUnit : Global.ObjAccessor.GetUnit(_caster, ihit.TargetGuid);

						if (unit != null)
							unit.RemoveOwnedAura(SpellInfo.Id, _originalCasterGuid, 0, AuraRemoveMode.Cancel);
					}

				EndEmpoweredSpell();
				SendChannelUpdate(0);
				SendInterrupted(0);
				SendCastResult(SpellCastResult.Interrupted);

				AppliedMods.Clear();

				break;

			default:
				break;
		}

		SetReferencedFromCurrent(false);

		if (SelfContainer != null && SelfContainer == this)
			SelfContainer = null;

		// originalcaster handles gameobjects/dynobjects for gob caster
		if (_originalCaster != null)
		{
			_originalCaster.RemoveDynObject(SpellInfo.Id);

			if (SpellInfo.IsChanneled) // if not channeled then the object for the current cast wasn't summoned yet
				_originalCaster.RemoveGameObject(SpellInfo.Id, true);
		}

		//set state back so finish will be processed
		_spellState = oldState;

		Finish(SpellCastResult.Interrupted);
	}

	public void Cast(bool skipCheck = false)
	{
		var modOwner = _caster.GetSpellModOwner();
		Spell lastSpellMod = null;

		if (modOwner)
		{
			lastSpellMod = modOwner.SpellModTakingSpell;

			if (lastSpellMod)
				modOwner.SetSpellModTakingSpell(lastSpellMod, false);
		}

		_cast(skipCheck);

		if (lastSpellMod)
			modOwner.SetSpellModTakingSpell(lastSpellMod, true);
	}

	public ulong HandleDelayed(ulong offset)
	{
		if (!UpdatePointers())
		{
			// finish the spell if UpdatePointers() returned false, something wrong happened there
			Finish(SpellCastResult.Fizzle);

			return 0;
		}

		var single_missile = Targets.HasDst;
		ulong next_time = 0;

		if (!_launchHandled)
		{
			var launchMoment = (ulong)Math.Floor(SpellInfo.LaunchDelay * 1000.0f);

			if (launchMoment > offset)
				return launchMoment;

			HandleLaunchPhase();
			_launchHandled = true;

			if (_delayMoment > offset)
			{
				if (single_missile)
					return _delayMoment;

				next_time = _delayMoment;

				if ((UniqueTargetInfo.Count > 2 || (UniqueTargetInfo.Count == 1 && UniqueTargetInfo[0].TargetGuid == _caster.GUID)) || !_uniqueGoTargetInfo.Empty())
					offset = 0; // if LaunchDelay was present then the only target that has timeDelay = 0 is m_caster - and that is the only target we want to process now
			}
		}

		if (single_missile && offset == 0)
			return _delayMoment;

		var modOwner = _caster.GetSpellModOwner();

		if (modOwner != null)
			modOwner.SetSpellModTakingSpell(this, true);

		PrepareTargetProcessing();

		if (!_immediateHandled && offset != 0)
		{
			_handle_immediate_phase();
			_immediateHandled = true;
		}

		// now recheck units targeting correctness (need before any effects apply to prevent adding immunity at first effect not allow apply second spell effect and similar cases)
		{
			List<TargetInfo> delayedTargets = new();

			UniqueTargetInfo.RemoveAll(target =>
			{
				if (single_missile || target.TimeDelay <= offset)
				{
					target.TimeDelay = offset;
					delayedTargets.Add(target);

					return true;
				}
				else if (next_time == 0 || target.TimeDelay < next_time)
				{
					next_time = target.TimeDelay;
				}

				return false;
			});

			DoProcessTargetContainer(delayedTargets);

			if (next_time == 0)
				CallScriptOnHitHandlers();
		}

		// now recheck gameobject targeting correctness
		{
			List<GOTargetInfo> delayedGOTargets = new();

			_uniqueGoTargetInfo.RemoveAll(goTarget =>
			{
				if (single_missile || goTarget.TimeDelay <= offset)
				{
					goTarget.TimeDelay = offset;
					delayedGOTargets.Add(goTarget);

					return true;
				}
				else if (next_time == 0 || goTarget.TimeDelay < next_time)
				{
					next_time = goTarget.TimeDelay;
				}

				return false;
			});

			DoProcessTargetContainer(delayedGOTargets);
		}

		FinishTargetProcessing();

		if (modOwner)
			modOwner.SetSpellModTakingSpell(this, false);

		// All targets passed - need finish phase
		if (next_time == 0)
		{
			// spell is finished, perform some last features of the spell here
			_handle_finish_phase();

			Finish(); // successfully finish spell cast

			// return zero, spell is finished now
			return 0;
		}
		else
		{
			// spell is unfinished, return next execution time
			return next_time;
		}
	}

	public void Update(uint difftime)
	{
		if (!UpdatePointers())
		{
			// cancel the spell if UpdatePointers() returned false, something wrong happened there
			Cancel();

			return;
		}

		if (!Targets.UnitTargetGUID.IsEmpty && Targets.UnitTarget == null)
		{
			Log.outDebug(LogFilter.Spells, "Spell {0} is cancelled due to removal of target.", SpellInfo.Id);
			Cancel();

			return;
		}

		// check if the player caster has moved before the spell finished
		// with the exception of spells affected with SPELL_AURA_CAST_WHILE_WALKING effect
		if (_timer != 0 && _caster.IsUnit && _caster.AsUnit.IsMoving && CheckMovement() != SpellCastResult.SpellCastOk)
			// if charmed by creature, trust the AI not to cheat and allow the cast to proceed
			// @todo this is a hack, "creature" movesplines don't differentiate turning/moving right now
			// however, checking what type of movement the spline is for every single spline would be really expensive
			if (!_caster.AsUnit.CharmerGUID.IsCreature)
				Cancel();

		switch (_spellState)
		{
			case SpellState.Preparing:
			{
				if (_timer > 0)
				{
					if (difftime >= _timer)
						_timer = 0;
					else
						_timer -= (int)difftime;
				}

				if (_timer == 0 && !SpellInfo.IsNextMeleeSwingSpell)
					// don't CheckCast for instant spells - done in spell.prepare, skip duplicate checks, needed for range checks for example
					Cast(_casttime == 0);

				break;
			}
			case SpellState.Casting:
			{
				if (_timer != 0)
				{
					// check if there are alive targets left
					if (!UpdateChanneledTargetList())
					{
						Log.outDebug(LogFilter.Spells, "Channeled spell {0} is removed due to lack of targets", SpellInfo.Id);
						_timer = 0;

						// Also remove applied auras
						foreach (var target in UniqueTargetInfo)
						{
							var unit = _caster.GUID == target.TargetGuid ? _caster.AsUnit : Global.ObjAccessor.GetUnit(_caster, target.TargetGuid);

							if (unit)
								unit.RemoveOwnedAura(SpellInfo.Id, _originalCasterGuid, 0, AuraRemoveMode.Cancel);
						}
					}

					if (_timer > 0)
					{
						UpdateEmpoweredSpell(difftime);

						if (difftime >= _timer)
							_timer = 0;
						else
							_timer -= (int)difftime;
					}
				}

				if (_timer == 0)
				{
					EndEmpoweredSpell();
					SendChannelUpdate(0);
					Finish();

					// We call the hook here instead of in Spell::finish because we only want to call it for completed channeling. Everything else is handled by interrupts
					var creatureCaster = _caster.AsCreature;

					if (creatureCaster != null)
						if (creatureCaster.IsAIEnabled)
							creatureCaster.GetAI().OnChannelFinished(SpellInfo);
				}

				break;
			}
			default:
				break;
		}
	}

	public void EndEmpoweredSpell()
	{
		if (GetPlayerIfIsEmpowered(out var p) &&
			SpellInfo.EmpowerStages.TryGetValue(_empoweredSpellStage, out var stageinfo)) // ensure stage is valid
		{
			var duration = SpellInfo.Duration;
			var timeCasted = SpellInfo.Duration - _timer;

			if (MathFunctions.GetPctOf(timeCasted, duration) < p.EmpoweredSpellMinHoldPct) // ensure we held for long enough
				return;

			ForEachSpellScript<ISpellOnEpowerSpellEnd>(s => s.EmpowerSpellEnd(stageinfo, _empoweredSpellDelta));
			var stageUpdate = new SpellEmpowerStageUpdate();
			stageUpdate.Caster = p.GUID;
			stageUpdate.CastID = CastId;
			stageUpdate.TimeRemaining = _timer;
			var unusedDurations = new List<uint>();

			var nextStage = _empoweredSpellStage;
			nextStage++;

			while (SpellInfo.EmpowerStages.TryGetValue(nextStage, out var nextStageinfo))
			{
				unusedDurations.Add(nextStageinfo.DurationMs);
				nextStage++;
			}

			stageUpdate.RemainingStageDurations = unusedDurations;
			p.SendPacket(stageUpdate);
		}
	}

	public void Finish(SpellCastResult result = SpellCastResult.SpellCastOk)
	{
		if (_spellState == SpellState.Finished)
			return;

		_spellState = SpellState.Finished;

		if (!_caster)
			return;

		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return;

		// successful cast of the initial autorepeat spell is moved to idle state so that it is not deleted as long as autorepeat is active
		if (IsAutoRepeat && unitCaster.GetCurrentSpell(CurrentSpellTypes.AutoRepeat) == this)
			_spellState = SpellState.Idle;

		if (SpellInfo.IsChanneled)
			unitCaster.UpdateInterruptMask();

		if (unitCaster.HasUnitState(UnitState.Casting) && !unitCaster.IsNonMeleeSpellCast(false, false, true))
			unitCaster.ClearUnitState(UnitState.Casting);

		// Unsummon summon as possessed creatures on spell cancel
		if (SpellInfo.IsChanneled && unitCaster.IsTypeId(TypeId.Player))
		{
			var charm = unitCaster.Charmed;

			if (charm != null)
				if (charm.IsTypeId(TypeId.Unit) && charm.AsCreature.HasUnitTypeMask(UnitTypeMask.Puppet) && charm.UnitData.CreatedBySpell == SpellInfo.Id)
					((Puppet)charm).UnSummon();
		}

		var creatureCaster = unitCaster.AsCreature;

		if (creatureCaster != null)
			creatureCaster.ReleaseSpellFocus(this);

		if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
			Unit.ProcSkillsAndAuras(unitCaster, null, new ProcFlagsInit(ProcFlags.CastEnded), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, this, null, null);

		if (result != SpellCastResult.SpellCastOk)
		{
			// on failure (or manual cancel) send TraitConfigCommitFailed to revert talent UI saved config selection
			if (_caster.IsPlayer && SpellInfo.HasEffect(SpellEffectName.ChangeActiveCombatTraitConfig))
				if (CustomArg is TraitConfig)
					_caster.					AsPlayer.SendPacket(new TraitConfigCommitFailed((CustomArg as TraitConfig).ID));

			return;
		}

		if (unitCaster.IsTypeId(TypeId.Unit) && unitCaster.AsCreature.IsSummon)
		{
			// Unsummon statue
			uint spell = unitCaster.UnitData.CreatedBySpell;
			var spellInfo = Global.SpellMgr.GetSpellInfo(spell, GetCastDifficulty());

			if (spellInfo != null && spellInfo.IconFileDataId == 134230)
			{
				Log.outDebug(LogFilter.Spells, "Statue {0} is unsummoned in spell {1} finish", unitCaster.GUID.ToString(), SpellInfo.Id);

				// Avoid infinite loops with setDeathState(JUST_DIED) being called over and over
				// It might make sense to do this check in Unit::setDeathState() and all overloaded functions
				if (unitCaster.DeathState != DeathState.JustDied)
					unitCaster.SetDeathState(DeathState.JustDied);

				return;
			}
		}

		if (IsAutoActionResetSpell())
			if (!SpellInfo.HasAttribute(SpellAttr2.DoNotResetCombatTimers))
			{
				unitCaster.ResetAttackTimer(WeaponAttackType.BaseAttack);

				if (unitCaster.HaveOffhandWeapon())
					unitCaster.ResetAttackTimer(WeaponAttackType.OffAttack);

				unitCaster.ResetAttackTimer(WeaponAttackType.RangedAttack);
			}

		// potions disabled by client, send event "not in combat" if need
		if (unitCaster.IsTypeId(TypeId.Player))
			if (TriggeredByAuraSpell == null)
				unitCaster.				AsPlayer.UpdatePotionCooldown(this);

		// Stop Attack for some spells
		if (SpellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat))
			unitCaster.AttackStop();
	}

	public void SendCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
	{
		if (result == SpellCastResult.SpellCastOk)
			return;

		if (!_caster.IsTypeId(TypeId.Player))
			return;

		if (_caster.AsPlayer.IsLoading) // don't send cast results at loading time
			return;

		if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
			result = SpellCastResult.DontReport;

		CastFailed castFailed = new();
		castFailed.Visual = SpellVisual;
		FillSpellCastFailedArgs(castFailed, CastId, SpellInfo, result, CustomErrors, param1, param2, _caster.AsPlayer);
		_caster.		AsPlayer.SendPacket(castFailed);
	}

	public void SendPetCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
	{
		if (result == SpellCastResult.SpellCastOk)
			return;

		var owner = _caster.CharmerOrOwner;

		if (!owner || !owner.IsTypeId(TypeId.Player))
			return;

		if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
			result = SpellCastResult.DontReport;

		PetCastFailed petCastFailed = new();
		FillSpellCastFailedArgs(petCastFailed, CastId, SpellInfo, result, SpellCustomErrors.None, param1, param2, owner.AsPlayer);
		owner.		AsPlayer.SendPacket(petCastFailed);
	}

	public static void SendCastResult(Player caster, SpellInfo spellInfo, SpellCastVisual spellVisual, ObjectGuid castCount, SpellCastResult result, SpellCustomErrors customError = SpellCustomErrors.None, int? param1 = null, int? param2 = null)
	{
		if (result == SpellCastResult.SpellCastOk)
			return;

		CastFailed packet = new();
		packet.Visual = spellVisual;
		FillSpellCastFailedArgs(packet, castCount, spellInfo, result, customError, param1, param2, caster);
		caster.SendPacket(packet);
	}

	public SpellLogEffect GetExecuteLogEffect(SpellEffectName effect)
	{
		var spellLogEffect = _executeLogEffects.LookupByKey(effect);

		if (spellLogEffect != null)
			return spellLogEffect;

		SpellLogEffect executeLogEffect = new();
		executeLogEffect.Effect = (int)effect;
		_executeLogEffects.Add(effect, executeLogEffect);

		return executeLogEffect;
	}

	public void SendChannelUpdate(uint time)
	{
		// GameObjects don't channel
		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return;

		if (time == 0)
		{
			unitCaster.ClearChannelObjects();
			unitCaster.			ChannelSpellId = 0;
			unitCaster.SetChannelVisual(new SpellCastVisualField());
		}

		SpellChannelUpdate spellChannelUpdate = new();
		spellChannelUpdate.CasterGUID = unitCaster.GUID;
		spellChannelUpdate.TimeRemaining = (int)time;
		unitCaster.SendMessageToSet(spellChannelUpdate, true);
	}

	public void HandleEffects(Unit pUnitTarget, Item pItemTarget, GameObject pGoTarget, Corpse pCorpseTarget, SpellEffectInfo spellEffectInfo, SpellEffectHandleMode mode)
	{
		_effectHandleMode = mode;
		UnitTarget = pUnitTarget;
		ItemTarget = pItemTarget;
		GameObjTarget = pGoTarget;
		CorpseTarget = pCorpseTarget;
		DestTarget = _destTargets[spellEffectInfo.EffectIndex].Position;
		EffectInfo = spellEffectInfo;

		Damage = CalculateDamage(spellEffectInfo, UnitTarget, out Variance);

		var preventDefault = CallScriptEffectHandlers(spellEffectInfo.EffectIndex, mode);

		if (!preventDefault)
			Global.SpellMgr.GetSpellEffectHandler(spellEffectInfo.Effect).Invoke(this);
	}

	public static Spell ExtractSpellFromEvent(BasicEvent basicEvent)
	{
		var spellEvent = (SpellEvent)basicEvent;

		if (spellEvent != null)
			return spellEvent.GetSpell();

		return null;
	}

	public SpellCastResult CheckCast(bool strict)
	{
		int param1 = 0, param2 = 0;

		return CheckCast(strict, ref param1, ref param2);
	}

	public SpellCastResult CheckCast(bool strict, ref int param1, ref int param2)
	{
		SpellCastResult castResult;

		// check death state
		if (_caster.AsUnit && !_caster.AsUnit.IsAlive && !SpellInfo.IsPassive && !(SpellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead) || (IsTriggered() && TriggeredByAuraSpell == null)))
			return SpellCastResult.CasterDead;

		// Prevent cheating in case the player has an immunity effect and tries to interact with a non-allowed gameobject. The error message is handled by the client so we don't report anything here
		if (_caster.IsPlayer && Targets.GOTarget != null)
			if (Targets.GOTarget.GetGoInfo().GetNoDamageImmune() != 0 && _caster.AsUnit.HasUnitFlag(UnitFlags.Immune))
				return SpellCastResult.DontReport;

		// check cooldowns to prevent cheating
		if (!SpellInfo.IsPassive)
		{
			var playerCaster = _caster.AsPlayer;

			if (playerCaster != null)
			{
				//can cast triggered (by aura only?) spells while have this flag
				if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAurastate))
				{
					// These two auras check SpellFamilyName defined by db2 class data instead of current spell SpellFamilyName
					if (playerCaster.HasAuraType(AuraType.DisableCastingExceptAbilities) && !SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && !SpellInfo.HasEffect(SpellEffectName.Attack) && !SpellInfo.HasAttribute(SpellAttr12.IgnoreCastingDisabled) && !playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableCastingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.Class).SpellClassSet, SpellInfo.SpellFamilyFlags))
						return SpellCastResult.CantDoThatRightNow;

					if (playerCaster.HasAuraType(AuraType.DisableAttackingExceptAbilities))
						if (!playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableAttackingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.Class).SpellClassSet, SpellInfo.SpellFamilyFlags))
							if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || SpellInfo.IsNextMeleeSwingSpell || SpellInfo.HasAttribute(SpellAttr1.InitiatesCombatEnablesAutoAttack) || SpellInfo.HasAttribute(SpellAttr2.InitiateCombatPostCastEnablesAutoAttack) || SpellInfo.HasEffect(SpellEffectName.Attack) || SpellInfo.HasEffect(SpellEffectName.NormalizedWeaponDmg) || SpellInfo.HasEffect(SpellEffectName.WeaponDamageNoSchool) || SpellInfo.HasEffect(SpellEffectName.WeaponPercentDamage) || SpellInfo.HasEffect(SpellEffectName.WeaponDamage))
								return SpellCastResult.CantDoThatRightNow;
				}

				// check if we are using a potion in combat for the 2nd+ time. Cooldown is added only after caster gets out of combat
				if (!IsIgnoringCooldowns() && playerCaster.GetLastPotionId() != 0 && CastItem && (CastItem.IsPotion() || SpellInfo.IsCooldownStartedOnEvent))
					return SpellCastResult.NotReady;
			}

			if (!IsIgnoringCooldowns() && _caster.AsUnit != null)
			{
				if (!_caster.AsUnit.GetSpellHistory().IsReady(SpellInfo, CastItemEntry))
				{
					if (TriggeredByAuraSpell != null)
						return SpellCastResult.DontReport;
					else
						return SpellCastResult.NotReady;
				}

				if ((IsAutoRepeat || SpellInfo.CategoryId == 76) && !_caster.AsUnit.IsAttackReady(WeaponAttackType.RangedAttack))
					return SpellCastResult.DontReport;
			}
		}

		if (SpellInfo.HasAttribute(SpellAttr7.IsCheatSpell) && _caster.IsUnit && !_caster.AsUnit.HasUnitFlag2(UnitFlags2.AllowCheatSpells))
		{
			CustomErrors = SpellCustomErrors.GmOnly;

			return SpellCastResult.CustomError;
		}

		// Check global cooldown
		if (strict && !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreGCD) && HasGlobalCooldown())
			return !SpellInfo.HasAttribute(SpellAttr0.CooldownOnEvent) ? SpellCastResult.NotReady : SpellCastResult.DontReport;

		// only triggered spells can be processed an ended Battleground
		if (!IsTriggered() && _caster.IsTypeId(TypeId.Player))
		{
			var bg = _caster.AsPlayer.GetBattleground();

			if (bg)
				if (bg.GetStatus() == BattlegroundStatus.WaitLeave)
					return SpellCastResult.DontReport;
		}

		if (_caster.IsTypeId(TypeId.Player) && Global.VMapMgr.IsLineOfSightCalcEnabled())
		{
			if (SpellInfo.HasAttribute(SpellAttr0.OnlyOutdoors) && !_caster.IsOutdoors())
				return SpellCastResult.OnlyOutdoors;

			if (SpellInfo.HasAttribute(SpellAttr0.OnlyIndoors) && _caster.IsOutdoors())
				return SpellCastResult.OnlyIndoors;
		}

		var unitCaster = _caster.AsUnit;

		if (unitCaster != null)
		{
			if (SpellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed) && unitCaster.IsCharmed)
				return SpellCastResult.Charmed;

			// only check at first call, Stealth auras are already removed at second call
			// for now, ignore triggered spells
			if (strict && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreShapeshift))
			{
				var checkForm = true;
				// Ignore form req aura
				var ignore = unitCaster.GetAuraEffectsByType(AuraType.ModIgnoreShapeshift);

				foreach (var aurEff in ignore)
				{
					if (!aurEff.IsAffectingSpell(SpellInfo))
						continue;

					checkForm = false;

					break;
				}

				if (checkForm)
				{
					// Cannot be used in this stance/form
					var shapeError = SpellInfo.CheckShapeshift(unitCaster.ShapeshiftForm);

					if (shapeError != SpellCastResult.SpellCastOk)
						return shapeError;

					if (SpellInfo.HasAttribute(SpellAttr0.OnlyStealthed) && !unitCaster.HasStealthAura())
						return SpellCastResult.OnlyStealthed;
				}
			}

			var reqCombat = true;
			var stateAuras = unitCaster.GetAuraEffectsByType(AuraType.AbilityIgnoreAurastate);

			foreach (var aura in stateAuras)
				if (aura.IsAffectingSpell(SpellInfo))
				{
					NeedComboPoints = false;

					if (aura.MiscValue == 1)
					{
						reqCombat = false;

						break;
					}
				}

			// caster state requirements
			// not for triggered spells (needed by execute)
			if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterAurastate))
			{
				if (SpellInfo.CasterAuraState != 0 && !unitCaster.HasAuraState(SpellInfo.CasterAuraState, SpellInfo, unitCaster))
					return SpellCastResult.CasterAurastate;

				if (SpellInfo.ExcludeCasterAuraState != 0 && unitCaster.HasAuraState(SpellInfo.ExcludeCasterAuraState, SpellInfo, unitCaster))
					return SpellCastResult.CasterAurastate;

				// Note: spell 62473 requres casterAuraSpell = triggering spell
				if (SpellInfo.CasterAuraSpell != 0 && !unitCaster.HasAura(SpellInfo.CasterAuraSpell))
					return SpellCastResult.CasterAurastate;

				if (SpellInfo.ExcludeCasterAuraSpell != 0 && unitCaster.HasAura(SpellInfo.ExcludeCasterAuraSpell))
					return SpellCastResult.CasterAurastate;

				if (SpellInfo.CasterAuraType != 0 && !unitCaster.HasAuraType(SpellInfo.CasterAuraType))
					return SpellCastResult.CasterAurastate;

				if (SpellInfo.ExcludeCasterAuraType != 0 && unitCaster.HasAuraType(SpellInfo.ExcludeCasterAuraType))
					return SpellCastResult.CasterAurastate;

				if (reqCombat && unitCaster.IsInCombat && !SpellInfo.CanBeUsedInCombat)
					return SpellCastResult.AffectingCombat;
			}

			// Check vehicle flags
			if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
			{
				var vehicleCheck = SpellInfo.CheckVehicle(unitCaster);

				if (vehicleCheck != SpellCastResult.SpellCastOk)
					return vehicleCheck;
			}
		}

		// check spell cast conditions from database
		{
			ConditionSourceInfo condInfo = new(_caster, Targets.ObjectTarget);

			if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Spell, SpellInfo.Id, condInfo))
			{
				// mLastFailedCondition can be NULL if there was an error processing the condition in Condition.Meets (i.e. wrong data for ConditionTarget or others)
				if (condInfo.mLastFailedCondition != null && condInfo.mLastFailedCondition.ErrorType != 0)
				{
					if (condInfo.mLastFailedCondition.ErrorType == (uint)SpellCastResult.CustomError)
						CustomErrors = (SpellCustomErrors)condInfo.mLastFailedCondition.ErrorTextId;

					return (SpellCastResult)condInfo.mLastFailedCondition.ErrorType;
				}

				if (condInfo.mLastFailedCondition == null || condInfo.mLastFailedCondition.ConditionTarget == 0)
					return SpellCastResult.CasterAurastate;

				return SpellCastResult.BadTargets;
			}
		}

		// Don't check explicit target for passive spells (workaround) (check should be skipped only for learn case)
		// those spells may have incorrect target entries or not filled at all (for example 15332)
		// such spells when learned are not targeting anyone using targeting system, they should apply directly to caster instead
		// also, such casts shouldn't be sent to client
		if (!(SpellInfo.IsPassive && (Targets.UnitTarget == null || Targets.UnitTarget == _caster)))
		{
			// Check explicit target for m_originalCaster - todo: get rid of such workarounds
			var caster = _caster;

			// in case of gameobjects like traps, we need the gameobject itself to check target validity
			// otherwise, if originalCaster is far away and cannot detect the target, the trap would not hit the target
			if (_originalCaster != null && !caster.IsGameObject)
				caster = _originalCaster;

			castResult = SpellInfo.CheckExplicitTarget(caster, Targets.ObjectTarget, Targets.ItemTarget);

			if (castResult != SpellCastResult.SpellCastOk)
				return castResult;
		}

		var unitTarget = Targets.UnitTarget;

		if (unitTarget != null)
		{
			castResult = SpellInfo.CheckTarget(_caster, unitTarget, _caster.IsGameObject); // skip stealth checks for GO casts

			if (castResult != SpellCastResult.SpellCastOk)
				return castResult;

			// If it's not a melee spell, check if vision is obscured by SPELL_AURA_INTERFERE_TARGETTING
			if (SpellInfo.DmgClass != SpellDmgClass.Melee)
			{
				var unitCaster1 = _caster.AsUnit;

				if (unitCaster1 != null)
				{
					foreach (var auraEffect in unitCaster1.GetAuraEffectsByType(AuraType.InterfereTargetting))
						if (!unitCaster1.IsFriendlyTo(auraEffect.Caster) && !unitTarget.HasAura(auraEffect.Id, auraEffect.CasterGuid))
							return SpellCastResult.VisionObscured;

					foreach (var auraEffect in unitTarget.GetAuraEffectsByType(AuraType.InterfereTargetting))
						if (!unitCaster1.IsFriendlyTo(auraEffect.Caster) && (!unitTarget.HasAura(auraEffect.Id, auraEffect.CasterGuid) || !unitCaster1.HasAura(auraEffect.Id, auraEffect.CasterGuid)))
							return SpellCastResult.VisionObscured;
				}
			}

			if (unitTarget != _caster)
			{
				// Must be behind the target
				if (SpellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget) && unitTarget.Location.HasInArc(MathFunctions.PI, _caster.Location))
					return SpellCastResult.NotBehind;

				// Target must be facing you
				if (SpellInfo.HasAttribute(SpellCustomAttributes.ReqTargetFacingCaster) && !unitTarget.Location.HasInArc(MathFunctions.PI, _caster.Location))
					return SpellCastResult.NotInfront;

				// Ignore LOS for gameobjects casts
				if (!_caster.IsGameObject)
				{
					var losTarget = _caster;

					if (IsTriggered() && TriggeredByAuraSpell != null)
					{
						var dynObj = _caster.AsUnit.GetDynObject(TriggeredByAuraSpell.Id);

						if (dynObj)
							losTarget = dynObj;
					}

					if (!SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) && !Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS) && !unitTarget.IsWithinLOSInMap(losTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
						return SpellCastResult.LineOfSight;
				}
			}
		}

		// Check for line of sight for spells with dest
		if (Targets.HasDst)
			if (!SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) && !Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS) && !_caster.IsWithinLOS(Targets.DstPos, LineOfSightChecks.All, ModelIgnoreFlags.M2))
				return SpellCastResult.LineOfSight;

		// check pet presence
		if (unitCaster != null)
		{
			if (SpellInfo.HasAttribute(SpellAttr2.NoActivePets))
				if (!unitCaster.PetGUID.IsEmpty)
					return SpellCastResult.AlreadyHavePet;

			foreach (var spellEffectInfo in SpellInfo.Effects)
				if (spellEffectInfo.TargetA.Target == Framework.Constants.Targets.UnitPet)
				{
					if (unitCaster.GetGuardianPet() == null)
					{
						if (TriggeredByAuraSpell != null) // not report pet not existence for triggered spells
							return SpellCastResult.DontReport;
						else
							return SpellCastResult.NoPet;
					}

					break;
				}
		}

		// Spell casted only on Battleground
		if (SpellInfo.HasAttribute(SpellAttr3.OnlyBattlegrounds))
			if (!_caster.GetMap().IsBattleground())
				return SpellCastResult.OnlyBattlegrounds;

		// do not allow spells to be cast in arenas or rated Battlegrounds
		var player = _caster.AsPlayer;

		if (player != null)
			if (player.InArena /* || player.InRatedBattleground() NYI*/)
			{
				castResult = CheckArenaAndRatedBattlegroundCastRules();

				if (castResult != SpellCastResult.SpellCastOk)
					return castResult;
			}

		// zone check
		if (!_caster.IsPlayer || !_caster.AsPlayer.IsGameMaster)
		{
			_caster.GetZoneAndAreaId(out var zone, out var area);

			var locRes = SpellInfo.CheckLocation(_caster.Location.MapId, zone, area, _caster.AsPlayer);

			if (locRes != SpellCastResult.SpellCastOk)
				return locRes;
		}

		// not let players cast spells at mount (and let do it to creatures)
		if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
			if (_caster.IsPlayer && _caster.AsPlayer.IsMounted && !SpellInfo.IsPassive && !SpellInfo.HasAttribute(SpellAttr0.AllowWhileMounted))
			{
				if (_caster.AsPlayer.IsInFlight)
					return SpellCastResult.NotOnTaxi;
				else
					return SpellCastResult.NotMounted;
			}

		// check spell focus object
		if (SpellInfo.RequiresSpellFocus != 0)
			if (!_caster.IsUnit || !_caster.AsUnit.HasAuraTypeWithMiscvalue(AuraType.ProvideSpellFocus, (int)SpellInfo.RequiresSpellFocus))
			{
				_focusObject = SearchSpellFocus();

				if (!_focusObject)
					return SpellCastResult.RequiresSpellFocus;
			}

		// always (except passive spells) check items (focus object can be required for any type casts)
		if (!SpellInfo.IsPassive)
		{
			castResult = CheckItems(ref param1, ref param2);

			if (castResult != SpellCastResult.SpellCastOk)
				return castResult;
		}

		// Triggered spells also have range check
		// @todo determine if there is some flag to enable/disable the check
		castResult = CheckRange(strict);

		if (castResult != SpellCastResult.SpellCastOk)
			return castResult;

		if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
		{
			castResult = CheckPower();

			if (castResult != SpellCastResult.SpellCastOk)
				return castResult;
		}

		if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterAuras))
		{
			castResult = CheckCasterAuras(ref param1);

			if (castResult != SpellCastResult.SpellCastOk)
				return castResult;
		}

		// script hook
		castResult = CallScriptCheckCastHandlers();

		if (castResult != SpellCastResult.SpellCastOk)
			return castResult;

		uint approximateAuraEffectMask = 0;
		uint nonAuraEffectMask = 0;

		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			// for effects of spells that have only one target
			switch (spellEffectInfo.Effect)
			{
				case SpellEffectName.Dummy:
				{
					if (SpellInfo.Id == 19938) // Awaken Peon
					{
						var unit = Targets.UnitTarget;

						if (unit == null || !unit.HasAura(17743))
							return SpellCastResult.BadTargets;
					}
					else if (SpellInfo.Id == 31789) // Righteous Defense
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.DontReport;

						var target = Targets.UnitTarget;

						if (target == null || !target.IsFriendlyTo(_caster) || target.Attackers.Empty())
							return SpellCastResult.BadTargets;
					}

					break;
				}
				case SpellEffectName.LearnSpell:
				{
					if (spellEffectInfo.TargetA.Target != Framework.Constants.Targets.UnitPet)
						break;

					var pet = _caster.AsPlayer.GetPet();

					if (pet == null)
						return SpellCastResult.NoPet;

					var learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

					if (learn_spellproto == null)
						return SpellCastResult.NotKnown;

					if (SpellInfo.SpellLevel > pet.Level)
						return SpellCastResult.Lowlevel;

					break;
				}
				case SpellEffectName.UnlockGuildVaultTab:
				{
					if (!_caster.IsTypeId(TypeId.Player))
						return SpellCastResult.BadTargets;

					var guild = _caster.AsPlayer.Guild;

					if (guild != null)
						if (guild.GetLeaderGUID() != _caster.AsPlayer.GUID)
							return SpellCastResult.CantDoThatRightNow;

					break;
				}
				case SpellEffectName.LearnPetSpell:
				{
					// check target only for unit target case
					var target = Targets.UnitTarget;

					if (target != null)
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.BadTargets;

						var pet = target.AsPet;

						if (pet == null || pet.GetOwner() != _caster)
							return SpellCastResult.BadTargets;

						var learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

						if (learn_spellproto == null)
							return SpellCastResult.NotKnown;

						if (SpellInfo.SpellLevel > pet.Level)
							return SpellCastResult.Lowlevel;
					}

					break;
				}
				case SpellEffectName.ApplyGlyph:
				{
					if (!_caster.IsTypeId(TypeId.Player))
						return SpellCastResult.GlyphNoSpec;

					var caster = _caster.AsPlayer;

					if (!caster.HasSpell(SpellMisc.SpellId))
						return SpellCastResult.NotKnown;

					var glyphId = (uint)spellEffectInfo.MiscValue;

					if (glyphId != 0)
					{
						var glyphProperties = CliDB.GlyphPropertiesStorage.LookupByKey(glyphId);

						if (glyphProperties == null)
							return SpellCastResult.InvalidGlyph;

						var glyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);

						if (glyphBindableSpells.Empty())
							return SpellCastResult.InvalidGlyph;

						if (!glyphBindableSpells.Contains(SpellMisc.SpellId))
							return SpellCastResult.InvalidGlyph;

						var glyphRequiredSpecs = Global.DB2Mgr.GetGlyphRequiredSpecs(glyphId);

						if (!glyphRequiredSpecs.Empty())
						{
							if (caster.GetPrimarySpecialization() == 0)
								return SpellCastResult.GlyphNoSpec;

							if (!glyphRequiredSpecs.Contains(caster.GetPrimarySpecialization()))
								return SpellCastResult.GlyphInvalidSpec;
						}

						uint replacedGlyph = 0;

						foreach (var activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
						{
							var activeGlyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(activeGlyphId);

							if (!activeGlyphBindableSpells.Empty())
								if (activeGlyphBindableSpells.Contains(SpellMisc.SpellId))
								{
									replacedGlyph = activeGlyphId;

									break;
								}
						}

						foreach (var activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
						{
							if (activeGlyphId == replacedGlyph)
								continue;

							if (activeGlyphId == glyphId)
								return SpellCastResult.UniqueGlyph;

							if (CliDB.GlyphPropertiesStorage.LookupByKey(activeGlyphId).GlyphExclusiveCategoryID == glyphProperties.GlyphExclusiveCategoryID)
								return SpellCastResult.GlyphExclusiveCategory;
						}
					}

					break;
				}
				case SpellEffectName.FeedPet:
				{
					if (!_caster.IsTypeId(TypeId.Player))
						return SpellCastResult.BadTargets;

					var foodItem = Targets.ItemTarget;

					if (!foodItem)
						return SpellCastResult.BadTargets;

					var pet = _caster.AsPlayer.GetPet();

					if (!pet)
						return SpellCastResult.NoPet;

					if (!pet.HaveInDiet(foodItem.GetTemplate()))
						return SpellCastResult.WrongPetFood;

					if (foodItem.GetTemplate().GetBaseItemLevel() + 30 <= pet.Level)
						return SpellCastResult.FoodLowlevel;

					if (_caster.AsPlayer.IsInCombat || pet.IsInCombat)
						return SpellCastResult.AffectingCombat;

					break;
				}
				case SpellEffectName.Charge:
				{
					if (unitCaster == null)
						return SpellCastResult.BadTargets;

					if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAuras) && unitCaster.HasUnitState(UnitState.Root))
						return SpellCastResult.Rooted;

					if (SpellInfo.NeedsExplicitUnitTarget)
					{
						var target = Targets.UnitTarget;

						if (target == null)
							return SpellCastResult.DontReport;

						// first we must check to see if the target is in LoS. A path can usually be built but LoS matters for charge spells
						if (!target.IsWithinLOSInMap(unitCaster)) //Do full LoS/Path check. Don't exclude m2
							return SpellCastResult.LineOfSight;

						var objSize = target.CombatReach;
						var range = SpellInfo.GetMaxRange(true, unitCaster, this) * 1.5f + objSize; // can't be overly strict

						_preGeneratedPath = new PathGenerator(unitCaster);
						_preGeneratedPath.SetPathLengthLimit(range);

						// first try with raycast, if it fails fall back to normal path
						var result = _preGeneratedPath.CalculatePath(target.Location, false);

						if (_preGeneratedPath.GetPathType().HasAnyFlag(PathType.Short))
							return SpellCastResult.NoPath;
						else if (!result || _preGeneratedPath.GetPathType().HasAnyFlag(PathType.NoPath | PathType.Incomplete))
							return SpellCastResult.NoPath;
						else if (_preGeneratedPath.IsInvalidDestinationZ(target)) // Check position z, if not in a straight line
							return SpellCastResult.NoPath;

						_preGeneratedPath.ShortenPathUntilDist(target.Location, objSize); //move back
					}

					break;
				}
				case SpellEffectName.Skinning:
				{
					if (!_caster.IsTypeId(TypeId.Player) || Targets.UnitTarget == null || !Targets.UnitTarget.IsTypeId(TypeId.Unit))
						return SpellCastResult.BadTargets;

					if (!Targets.UnitTarget.HasUnitFlag(UnitFlags.Skinnable))
						return SpellCastResult.TargetUnskinnable;

					var creature = Targets.UnitTarget.AsCreature;
					var loot = creature.GetLootForPlayer(_caster.AsPlayer);

					if (loot != null && (!loot.IsLooted() || loot.loot_type == LootType.Skinning))
						return SpellCastResult.TargetNotLooted;

					var skill = creature.CreatureTemplate.GetRequiredLootSkill();

					var skillValue = _caster.AsPlayer.GetSkillValue(skill);
					var TargetLevel = Targets.UnitTarget.GetLevelForTarget(_caster);
					var ReqValue = (int)(skillValue < 100 ? (TargetLevel - 10) * 10 : TargetLevel * 5);

					if (ReqValue > skillValue)
						return SpellCastResult.LowCastlevel;

					break;
				}
				case SpellEffectName.OpenLock:
				{
					if (spellEffectInfo.TargetA.Target != Framework.Constants.Targets.GameobjectTarget &&
						spellEffectInfo.TargetA.Target != Framework.Constants.Targets.GameobjectItemTarget)
						break;

					if (!_caster.IsTypeId(TypeId.Player) // only players can open locks, gather etc.
						// we need a go target in case of TARGET_GAMEOBJECT_TARGET
						||
						(spellEffectInfo.TargetA.Target == Framework.Constants.Targets.GameobjectTarget && Targets.GOTarget == null))
						return SpellCastResult.BadTargets;

					Item pTempItem = null;

					if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.TradeItem))
					{
						var pTrade = _caster.AsPlayer.GetTradeData();

						if (pTrade != null)
							pTempItem = pTrade.GetTraderData().GetItem(TradeSlots.NonTraded);
					}
					else if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.Item))
					{
						pTempItem = _caster.AsPlayer.GetItemByGuid(Targets.ItemTargetGuid);
					}

					// we need a go target, or an openable item target in case of TARGET_GAMEOBJECT_ITEM_TARGET
					if (spellEffectInfo.TargetA.Target == Framework.Constants.Targets.GameobjectItemTarget &&
						Targets.						GOTarget == null &&
						(pTempItem == null || pTempItem.GetTemplate().GetLockID() == 0 || !pTempItem.IsLocked()))
						return SpellCastResult.BadTargets;

					if (SpellInfo.Id != 1842 ||
						(Targets.GOTarget != null &&
						Targets.						GOTarget.GetGoInfo().type != GameObjectTypes.Trap))
						if (_caster.AsPlayer.InBattleground() && // In Battlegroundplayers can use only flags and banners
							!_caster.AsPlayer.CanUseBattlegroundObject(Targets.GOTarget))
							return SpellCastResult.TryAgain;

					// get the lock entry
					uint lockId = 0;
					var go = Targets.GOTarget;
					var itm = Targets.ItemTarget;

					if (go != null)
					{
						lockId = go.GetGoInfo().GetLockId();

						if (lockId == 0)
							return SpellCastResult.BadTargets;

						if (go.GetGoInfo().GetNotInCombat() != 0 && _caster.AsUnit.IsInCombat)
							return SpellCastResult.AffectingCombat;
					}
					else if (itm != null)
					{
						lockId = itm.GetTemplate().GetLockID();
					}

					var skillId = SkillType.None;
					var reqSkillValue = 0;
					var skillValue = 0;

					// check lock compatibility
					var res = CanOpenLock(spellEffectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);

					if (res != SpellCastResult.SpellCastOk)
						return res;

					break;
				}
				case SpellEffectName.ResurrectPet:
				{
					var playerCaster = _caster.AsPlayer;

					if (playerCaster == null || playerCaster.GetPetStable() == null)
						return SpellCastResult.BadTargets;

					var pet = playerCaster.GetPet();

					if (pet != null && pet.IsAlive)
						return SpellCastResult.AlreadyHaveSummon;

					var petStable = playerCaster.GetPetStable();
					var deadPetInfo = petStable.ActivePets.FirstOrDefault(petInfo => petInfo?.Health == 0);

					if (deadPetInfo == null)
						return SpellCastResult.BadTargets;

					break;
				}
				// This is generic summon effect
				case SpellEffectName.Summon:
				{
					if (unitCaster == null)
						break;

					var SummonProperties = CliDB.SummonPropertiesStorage.LookupByKey(spellEffectInfo.MiscValueB);

					if (SummonProperties == null)
						break;

					switch (SummonProperties.Control)
					{
						case SummonCategory.Pet:
							if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !unitCaster.PetGUID.IsEmpty)
								return SpellCastResult.AlreadyHaveSummon;

							goto case SummonCategory.Puppet;
						case SummonCategory.Puppet:
							if (!unitCaster.CharmedGUID.IsEmpty)
								return SpellCastResult.AlreadyHaveCharm;

							break;
					}

					break;
				}
				case SpellEffectName.CreateTamedPet:
				{
					if (Targets.UnitTarget != null)
					{
						if (!Targets.UnitTarget.IsTypeId(TypeId.Player))
							return SpellCastResult.BadTargets;

						if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !Targets.UnitTarget.PetGUID.IsEmpty)
							return SpellCastResult.AlreadyHaveSummon;
					}

					break;
				}
				case SpellEffectName.SummonPet:
				{
					if (unitCaster == null)
						return SpellCastResult.BadTargets;

					if (!unitCaster.PetGUID.IsEmpty) //let warlock do a replacement summon
					{
						if (unitCaster.IsTypeId(TypeId.Player))
						{
							if (strict) //starting cast, trigger pet stun (cast by pet so it doesn't attack player)
							{
								var pet = unitCaster.AsPlayer.GetPet();

								if (pet != null)
									pet.CastSpell(pet,
												32752,
												new CastSpellExtraArgs(TriggerCastFlags.FullMask)
													.SetOriginalCaster(pet.GUID)
													.SetTriggeringSpell(this));
							}
						}
						else if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
						{
							return SpellCastResult.AlreadyHaveSummon;
						}
					}

					if (!unitCaster.CharmedGUID.IsEmpty)
						return SpellCastResult.AlreadyHaveCharm;

					var playerCaster = unitCaster.AsPlayer;

					if (playerCaster != null && playerCaster.GetPetStable() != null)
					{
						PetSaveMode? petSlot = null;

						if (spellEffectInfo.MiscValue == 0)
						{
							petSlot = (PetSaveMode)spellEffectInfo.CalcValue();

							// No pet can be summoned if any pet is dead
							foreach (var activePet in playerCaster.GetPetStable().ActivePets)
								if (activePet?.Health == 0)
								{
									playerCaster.SendTameFailure(PetTameResult.Dead);

									return SpellCastResult.DontReport;
								}
						}

						var info = Pet.GetLoadPetInfo(playerCaster.GetPetStable(), (uint)spellEffectInfo.MiscValue, 0, petSlot);

						if (info.Item1 != null)
						{
							if (info.Item1.Type == PetType.Hunter)
							{
								var creatureInfo = Global.ObjectMgr.GetCreatureTemplate(info.Item1.CreatureId);

								if (creatureInfo == null || !creatureInfo.IsTameable(playerCaster.CanTameExoticPets))
								{
									// if problem in exotic pet
									if (creatureInfo != null && creatureInfo.IsTameable(true))
										playerCaster.SendTameFailure(PetTameResult.CantControlExotic);
									else
										playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

									return SpellCastResult.DontReport;
								}
							}
						}
						else if (spellEffectInfo.MiscValue == 0) // when miscvalue is present it is allowed to create new pets
						{
							playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

							return SpellCastResult.DontReport;
						}
					}

					break;
				}
				case SpellEffectName.DismissPet:
				{
					var playerCaster = _caster.AsPlayer;

					if (playerCaster == null)
						return SpellCastResult.BadTargets;

					var pet = playerCaster.GetPet();

					if (pet == null)
						return SpellCastResult.NoPet;

					if (!pet.IsAlive)
						return SpellCastResult.TargetsDead;

					break;
				}
				case SpellEffectName.SummonPlayer:
				{
					if (!_caster.IsTypeId(TypeId.Player))
						return SpellCastResult.BadTargets;

					if (_caster.AsPlayer.Target.IsEmpty)
						return SpellCastResult.BadTargets;

					var target = Global.ObjAccessor.FindPlayer(_caster.AsPlayer.Target);

					if (target == null || _caster.AsPlayer == target || (!target.IsInSameRaidWith(_caster.AsPlayer) && SpellInfo.Id != 48955)) // refer-a-friend spell
						return SpellCastResult.BadTargets;

					if (target.HasSummonPending)
						return SpellCastResult.SummonPending;

					// check if our map is dungeon
					var map = _caster.GetMap().ToInstanceMap();

					if (map != null)
					{
						var mapId = map.GetId();
						var difficulty = map.GetDifficultyID();
						var mapLock = map.GetInstanceLock();

						if (mapLock != null)
							if (Global.InstanceLockMgr.CanJoinInstanceLock(target.GUID, new MapDb2Entries(mapId, difficulty), mapLock) != TransferAbortReason.None)
								return SpellCastResult.TargetLockedToRaidInstance;

						if (!target.Satisfy(Global.ObjectMgr.GetAccessRequirement(mapId, difficulty), mapId))
							return SpellCastResult.BadTargets;
					}

					break;
				}
				// RETURN HERE
				case SpellEffectName.SummonRafFriend:
				{
					if (!_caster.IsTypeId(TypeId.Player))
						return SpellCastResult.BadTargets;

					var playerCaster = _caster.AsPlayer;

					//
					if (playerCaster.Target.IsEmpty)
						return SpellCastResult.BadTargets;

					var target = Global.ObjAccessor.FindPlayer(playerCaster.Target);

					if (target == null ||
						!(target.Session.RecruiterId == playerCaster.Session.AccountId || target.Session.AccountId == playerCaster.Session.RecruiterId))
						return SpellCastResult.BadTargets;

					break;
				}
				case SpellEffectName.Leap:
				case SpellEffectName.TeleportUnitsFaceCaster:
				{
					//Do not allow to cast it before BG starts.
					if (_caster.IsTypeId(TypeId.Player))
					{
						var bg = _caster.AsPlayer.GetBattleground();

						if (bg)
							if (bg.GetStatus() != BattlegroundStatus.InProgress)
								return SpellCastResult.TryAgain;
					}

					break;
				}
				case SpellEffectName.StealBeneficialBuff:
				{
					if (Targets.UnitTarget == null || Targets.UnitTarget == _caster)
						return SpellCastResult.BadTargets;

					break;
				}
				case SpellEffectName.LeapBack:
				{
					if (unitCaster == null)
						return SpellCastResult.BadTargets;

					if (unitCaster.HasUnitState(UnitState.Root))
					{
						if (unitCaster.IsTypeId(TypeId.Player))
							return SpellCastResult.Rooted;
						else
							return SpellCastResult.DontReport;
					}

					break;
				}
				case SpellEffectName.Jump:
				case SpellEffectName.JumpDest:
				{
					if (unitCaster == null)
						return SpellCastResult.BadTargets;

					if (unitCaster.HasUnitState(UnitState.Root))
						return SpellCastResult.Rooted;

					break;
				}
				case SpellEffectName.TalentSpecSelect:
				{
					var spec = CliDB.ChrSpecializationStorage.LookupByKey(SpellMisc.SpecializationId);
					var playerCaster = _caster.AsPlayer;

					if (!playerCaster)
						return SpellCastResult.TargetNotPlayer;

					if (spec == null || (spec.ClassID != (uint)player.Class && !spec.IsPetSpecialization()))
						return SpellCastResult.NoSpec;

					if (spec.IsPetSpecialization())
					{
						var pet = player.GetPet();

						if (!pet || pet.GetPetType() != PetType.Hunter || pet.GetCharmInfo() == null)
							return SpellCastResult.NoPet;
					}

					// can't change during already started arena/Battleground
					var bg = player.GetBattleground();

					if (bg)
						if (bg.GetStatus() == BattlegroundStatus.InProgress)
							return SpellCastResult.NotInBattleground;

					break;
				}
				case SpellEffectName.RemoveTalent:
				{
					var playerCaster = _caster.AsPlayer;

					if (playerCaster == null)
						return SpellCastResult.BadTargets;

					var talent = CliDB.TalentStorage.LookupByKey(SpellMisc.TalentId);

					if (talent == null)
						return SpellCastResult.DontReport;

					if (playerCaster.GetSpellHistory().HasCooldown(talent.SpellID))
					{
						param1 = (int)talent.SpellID;

						return SpellCastResult.CantUntalent;
					}

					break;
				}
				case SpellEffectName.GiveArtifactPower:
				case SpellEffectName.GiveArtifactPowerNoBonus:
				{
					var playerCaster = _caster.AsPlayer;

					if (playerCaster == null)
						return SpellCastResult.BadTargets;

					var artifactAura = playerCaster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

					if (artifactAura == null)
						return SpellCastResult.NoArtifactEquipped;

					var artifact = playerCaster.AsPlayer.GetItemByGuid(artifactAura.CastItemGuid);

					if (artifact == null)
						return SpellCastResult.NoArtifactEquipped;

					if (spellEffectInfo.Effect == SpellEffectName.GiveArtifactPower)
					{
						var artifactEntry = CliDB.ArtifactStorage.LookupByKey(artifact.GetTemplate().GetArtifactID());

						if (artifactEntry == null || artifactEntry.ArtifactCategoryID != spellEffectInfo.MiscValue)
							return SpellCastResult.WrongArtifactEquipped;
					}

					break;
				}
				case SpellEffectName.ChangeBattlepetQuality:
				case SpellEffectName.GrantBattlepetLevel:
				case SpellEffectName.GrantBattlepetExperience:
				{
					var playerCaster = _caster.AsPlayer;

					if (playerCaster == null || !Targets.UnitTarget || !Targets.UnitTarget.IsCreature)
						return SpellCastResult.BadTargets;

					var battlePetMgr = playerCaster.Session.BattlePetMgr;

					if (!battlePetMgr.HasJournalLock())
						return SpellCastResult.CantDoThatRightNow;

					var creature = Targets.UnitTarget.AsCreature;

					if (creature != null)
					{
						if (playerCaster.SummonedBattlePetGUID.IsEmpty || creature.BattlePetCompanionGUID.IsEmpty)
							return SpellCastResult.NoPet;

						if (playerCaster.SummonedBattlePetGUID != creature.BattlePetCompanionGUID)
							return SpellCastResult.BadTargets;

						var battlePet = battlePetMgr.GetPet(creature.BattlePetCompanionGUID);

						if (battlePet != null)
						{
							var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(battlePet.PacketInfo.Species);

							if (battlePetSpecies != null)
							{
								var battlePetType = (uint)spellEffectInfo.MiscValue;

								if (battlePetType != 0)
									if ((battlePetType & (1 << battlePetSpecies.PetTypeEnum)) == 0)
										return SpellCastResult.WrongBattlePetType;

								if (spellEffectInfo.Effect == SpellEffectName.ChangeBattlepetQuality)
								{
									var qualityRecord = CliDB.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < spellEffectInfo.BasePoints);

									var quality = BattlePetBreedQuality.Poor;

									if (qualityRecord != null)
										quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

									if (battlePet.PacketInfo.Quality >= (byte)quality)
										return SpellCastResult.CantUpgradeBattlePet;
								}

								if (spellEffectInfo.Effect == SpellEffectName.GrantBattlepetLevel || spellEffectInfo.Effect == SpellEffectName.GrantBattlepetExperience)
									if (battlePet.PacketInfo.Level >= SharedConst.MaxBattlePetLevel)
										return SpellCastResult.GrantPetLevelFail;

								if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.CantBattle))
									return SpellCastResult.BadTargets;
							}
						}
					}

					break;
				}
				default:
					break;
			}

			if (spellEffectInfo.IsAura())
				approximateAuraEffectMask |= 1u << spellEffectInfo.EffectIndex;
			else if (spellEffectInfo.IsEffect())
				nonAuraEffectMask |= 1u << spellEffectInfo.EffectIndex;
		}

		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			switch (spellEffectInfo.ApplyAuraName)
			{
				case AuraType.ModPossessPet:
				{
					if (!_caster.IsTypeId(TypeId.Player))
						return SpellCastResult.NoPet;

					var pet = _caster.AsPlayer.GetPet();

					if (pet == null)
						return SpellCastResult.NoPet;

					if (!pet.CharmerGUID.IsEmpty)
						return SpellCastResult.AlreadyHaveCharm;

					break;
				}
				case AuraType.ModPossess:
				case AuraType.ModCharm:
				case AuraType.AoeCharm:
				{
					var unitCaster1 = (_originalCaster ? _originalCaster : _caster.AsUnit);

					if (unitCaster1 == null)
						return SpellCastResult.BadTargets;

					if (!unitCaster1.CharmerGUID.IsEmpty)
						return SpellCastResult.AlreadyHaveCharm;

					if (spellEffectInfo.ApplyAuraName == AuraType.ModCharm || spellEffectInfo.ApplyAuraName == AuraType.ModPossess)
					{
						if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !unitCaster1.PetGUID.IsEmpty)
							return SpellCastResult.AlreadyHaveSummon;

						if (!unitCaster1.CharmedGUID.IsEmpty)
							return SpellCastResult.AlreadyHaveCharm;
					}

					var target = Targets.UnitTarget;

					if (target != null)
					{
						if (target.IsTypeId(TypeId.Unit) && target.AsCreature.IsVehicle)
							return SpellCastResult.BadImplicitTargets;

						if (target.IsMounted)
							return SpellCastResult.CantBeCharmed;

						if (!target.CharmerGUID.IsEmpty)
							return SpellCastResult.Charmed;

						if (target.GetOwner() != null && target.GetOwner().IsTypeId(TypeId.Player))
							return SpellCastResult.TargetIsPlayerControlled;

						var damage = CalculateDamage(spellEffectInfo, target);

						if (damage != 0 && target.GetLevelForTarget(_caster) > damage)
							return SpellCastResult.Highlevel;
					}

					break;
				}
				case AuraType.Mounted:
				{
					if (unitCaster == null)
						return SpellCastResult.BadTargets;

					if (unitCaster.IsInWater && SpellInfo.HasAura(AuraType.ModIncreaseMountedFlightSpeed))
						return SpellCastResult.OnlyAbovewater;

					if (unitCaster.IsInDisallowedMountForm)
					{
						SendMountResult(MountResult.Shapeshifted); // mount result gets sent before the cast result

						return SpellCastResult.DontReport;
					}

					break;
				}
				case AuraType.RangedAttackPowerAttackerBonus:
				{
					if (Targets.UnitTarget == null)
						return SpellCastResult.BadImplicitTargets;

					// can be casted at non-friendly unit or own pet/charm
					if (_caster.IsFriendlyTo(Targets.UnitTarget))
						return SpellCastResult.TargetFriendly;

					break;
				}
				case AuraType.Fly:
				case AuraType.ModIncreaseFlightSpeed:
				{
					// not allow cast fly spells if not have req. skills  (all spells is self target)
					// allow always ghost flight spells
					if (_originalCaster != null && _originalCaster.IsTypeId(TypeId.Player) && _originalCaster.IsAlive)
					{
						var Bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(_originalCaster.GetMap(), _originalCaster.GetZoneId());
						var area = CliDB.AreaTableStorage.LookupByKey(_originalCaster.GetAreaId());

						if (area != null)
							if (area.HasFlag(AreaFlags.NoFlyZone) || (Bf != null && !Bf.CanFlyIn()))
								return SpellCastResult.NotHere;
					}

					break;
				}
				case AuraType.PeriodicManaLeech:
				{
					if (spellEffectInfo.IsTargetingArea())
						break;

					if (Targets.UnitTarget == null)
						return SpellCastResult.BadImplicitTargets;

					if (!_caster.IsTypeId(TypeId.Player) || CastItem != null)
						break;

					if (Targets.UnitTarget.GetPowerType() != PowerType.Mana)
						return SpellCastResult.BadTargets;

					break;
				}
				default:
					break;
			}

			// check if target already has the same type, but more powerful aura
			if (!SpellInfo.HasAttribute(SpellAttr4.AuraNeverBounces) && (nonAuraEffectMask == 0 || SpellInfo.HasAttribute(SpellAttr4.AuraBounceFailsSpell)) && (approximateAuraEffectMask & (1 << spellEffectInfo.EffectIndex)) != 0 && !SpellInfo.IsTargetingArea)
			{
				var target = Targets.UnitTarget;

				if (target != null)
					if (!target.IsHighestExclusiveAuraEffect(SpellInfo, spellEffectInfo.ApplyAuraName, spellEffectInfo.CalcValue(_caster, SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex], null, CastItemEntry, CastItemLevel), approximateAuraEffectMask, false))
						return SpellCastResult.AuraBounced;
			}
		}

		// check trade slot case (last, for allow catch any another cast problems)
		if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.TradeItem))
		{
			if (CastItem != null)
				return SpellCastResult.ItemEnchantTradeWindow;

			if (SpellInfo.HasAttribute(SpellAttr2.EnchantOwnItemOnly))
				return SpellCastResult.ItemEnchantTradeWindow;

			if (!_caster.IsTypeId(TypeId.Player))
				return SpellCastResult.NotTrading;

			var my_trade = _caster.AsPlayer.GetTradeData();

			if (my_trade == null)
				return SpellCastResult.NotTrading;

			var slot = (TradeSlots)Targets.ItemTargetGuid.LowValue;

			if (slot != TradeSlots.NonTraded)
				return SpellCastResult.BadTargets;

			if (!IsTriggered())
				if (my_trade.GetSpell() != 0)
					return SpellCastResult.ItemAlreadyEnchanted;
		}

		// check if caster has at least 1 combo point for spells that require combo points
		if (NeedComboPoints)
		{
			var plrCaster = _caster.AsPlayer;

			if (plrCaster != null)
				if (plrCaster.GetComboPoints() == 0)
					return SpellCastResult.NoComboPoints;
		}

		// all ok
		return SpellCastResult.SpellCastOk;
	}

	public SpellCastResult CheckPetCast(Unit target)
	{
		var unitCaster = _caster.AsUnit;

		if (unitCaster != null && unitCaster.HasUnitState(UnitState.Casting) && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress)) //prevent spellcast interruption by another spellcast
			return SpellCastResult.SpellInProgress;

		// dead owner (pets still alive when owners ressed?)
		var owner = _caster.CharmerOrOwner;

		if (owner != null)
			if (!owner.IsAlive)
				return SpellCastResult.CasterDead;

		if (target == null && Targets.UnitTarget != null)
			target = Targets.UnitTarget;

		if (SpellInfo.NeedsExplicitUnitTarget)
		{
			if (target == null)
				return SpellCastResult.BadImplicitTargets;

			Targets.
			UnitTarget = target;
		}

		// cooldown
		var creatureCaster = _caster.AsCreature;

		if (creatureCaster)
			if (creatureCaster.GetSpellHistory().HasCooldown(SpellInfo.Id))
				return SpellCastResult.NotReady;

		// Check if spell is affected by GCD
		if (SpellInfo.StartRecoveryCategory > 0)
			if (unitCaster.GetCharmInfo() != null && unitCaster.GetSpellHistory().HasGlobalCooldown(SpellInfo))
				return SpellCastResult.NotReady;

		return CheckCast(true);
	}

	public bool CanAutoCast(Unit target)
	{
		if (!target)
			return (CheckPetCast(target) == SpellCastResult.SpellCastOk);

		var targetguid = target.GUID;

		// check if target already has the same or a more powerful aura
		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			if (!spellEffectInfo.IsAura())
				continue;

			var auraType = spellEffectInfo.ApplyAuraName;
			var auras = target.GetAuraEffectsByType(auraType);

			foreach (var eff in auras)
			{
				if (SpellInfo.Id == eff.SpellInfo.Id)
					return false;

				switch (Global.SpellMgr.CheckSpellGroupStackRules(SpellInfo, eff.SpellInfo))
				{
					case SpellGroupStackRule.Exclusive:
						return false;
					case SpellGroupStackRule.ExclusiveFromSameCaster:
						if (Caster == eff.Caster)
							return false;

						break;
					case SpellGroupStackRule.ExclusiveSameEffect: // this one has further checks, but i don't think they're necessary for autocast logic
					case SpellGroupStackRule.ExclusiveHighest:
						if (Math.Abs(spellEffectInfo.BasePoints) <= Math.Abs(eff.Amount))
							return false;

						break;
					case SpellGroupStackRule.Default:
					default:
						break;
				}
			}
		}

		var result = CheckPetCast(target);

		if (result == SpellCastResult.SpellCastOk || result == SpellCastResult.UnitNotInfront)
		{
			// do not check targets for ground-targeted spells (we target them on top of the intended target anyway)
			if (SpellInfo.ExplicitTargetMask.HasFlag(SpellCastTargetFlags.DestLocation))
				return true;

			SelectSpellTargets();

			//check if among target units, our WANTED target is as well (.only self cast spells return false)
			foreach (var ihit in UniqueTargetInfo)
				if (ihit.TargetGuid == targetguid)
					return true;
		}

		// either the cast failed or the intended target wouldn't be hit
		return false;
	}

	public void Delayed() // only called in DealDamage()
	{
		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return;

		if (IsDelayableNoMore()) // Spells may only be delayed twice
			return;

		//check pushback reduce
		var delaytime = 500;      // spellcasting delay is normally 500ms
		double delayReduce = 100; // must be initialized to 100 for percent modifiers

		var player = unitCaster.GetSpellModOwner();

		if (player != null)
			player.ApplySpellMod(SpellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

		delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

		if (delayReduce >= 100)
			return;

		MathFunctions.AddPct(ref delaytime, -delayReduce);

		if (_timer + delaytime > _casttime)
		{
			delaytime = _casttime - _timer;
			_timer = _casttime;
		}
		else
		{
			_timer += delaytime;
		}

		SpellDelayed spellDelayed = new();
		spellDelayed.Caster = unitCaster.GUID;
		spellDelayed.ActualDelay = delaytime;

		unitCaster.SendMessageToSet(spellDelayed, true);
	}

	public void DelayedChannel()
	{
		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return;

		if (_spellState != SpellState.Casting)
			return;

		if (IsDelayableNoMore()) // Spells may only be delayed twice
			return;

		//check pushback reduce
		// should be affected by modifiers, not take the dbc duration.
		var duration = ((_channeledDuration > 0) ? _channeledDuration : SpellInfo.Duration);

		var delaytime = MathFunctions.CalculatePct(duration, 25); // channeling delay is normally 25% of its time per hit
		double delayReduce = 100;                                 // must be initialized to 100 for percent modifiers

		var player = unitCaster.GetSpellModOwner();

		if (player != null)
			player.ApplySpellMod(SpellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

		delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

		if (delayReduce >= 100)
			return;

		MathFunctions.AddPct(ref delaytime, -delayReduce);

		if (_timer <= delaytime)
		{
			delaytime = _timer;
			_timer = 0;
		}
		else
		{
			_timer -= delaytime;
		}

		foreach (var ihit in UniqueTargetInfo)
			if (ihit.MissCondition == SpellMissInfo.None)
			{
				var unit = (unitCaster.GUID == ihit.TargetGuid) ? unitCaster : Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGuid);

				if (unit != null)
					unit.DelayOwnedAuras(SpellInfo.Id, _originalCasterGuid, delaytime);
			}

		// partially interrupt persistent area auras
		var dynObj = unitCaster.GetDynObject(SpellInfo.Id);

		if (dynObj != null)
			dynObj.Delay(delaytime);

		SendChannelUpdate((uint)_timer);
	}

	public bool HasPowerTypeCost(PowerType power)
	{
		return GetPowerTypeCostAmount(power).HasValue;
	}

	public int? GetPowerTypeCostAmount(PowerType power)
	{
		var powerCost = _powerCosts.Find(cost => cost.Power == power);

		if (powerCost == null)
			return null;

		return powerCost.Amount;
	}

	public CurrentSpellTypes GetCurrentContainer()
	{
		if (SpellInfo.IsNextMeleeSwingSpell)
			return CurrentSpellTypes.Melee;
		else if (IsAutoRepeat)
			return CurrentSpellTypes.AutoRepeat;
		else if (SpellInfo.IsChanneled)
			return CurrentSpellTypes.Channeled;

		return CurrentSpellTypes.Generic;
	}

	public Difficulty GetCastDifficulty()
	{
		return _caster.GetMap().GetDifficultyID();
	}

	public bool IsPositive()
	{
		return SpellInfo.IsPositive && (TriggeredByAuraSpell == null || TriggeredByAuraSpell.IsPositive);
	}

	public Unit GetUnitCasterForEffectHandlers()
	{
		return _originalCaster != null ? _originalCaster : _caster.AsUnit;
	}

	public void SetSpellValue(SpellValueMod mod, float value)
	{
		if (mod < SpellValueMod.End)
		{
			SpellValue.EffectBasePoints[(int)mod] = value;
			SpellValue.CustomBasePointsMask |= 1u << (int)mod;

			return;
		}

		switch (mod)
		{
			case SpellValueMod.RadiusMod:
				SpellValue.RadiusMod = value / 10000;

				break;
			case SpellValueMod.MaxTargets:
				SpellValue.MaxAffectedTargets = (uint)value;

				break;
			case SpellValueMod.AuraStack:
				SpellValue.AuraStackAmount = (int)value;

				break;
			case SpellValueMod.CritChance:
				SpellValue.CriticalChance = value / 100.0f; // @todo ugly /100 remove when basepoints are double

				break;
			case SpellValueMod.DurationPct:
				SpellValue.DurationMul = value / 100.0f;

				break;
			case SpellValueMod.Duration:
				SpellValue.Duration = (int)value;

				break;
			case SpellValueMod.SummonDuration:
				SpellValue.SummonDuration = value;

				break;
		}
	}

	public bool CheckTargetHookEffect(ITargetHookHandler th, int effIndexToCheck)
	{
		if (SpellInfo.Effects.Count <= effIndexToCheck)
			return false;

		return CheckTargetHookEffect(th, SpellInfo.GetEffect(effIndexToCheck));
	}

	public bool CheckTargetHookEffect(ITargetHookHandler th, SpellEffectInfo spellEffectInfo)
	{
		if (th.TargetType == 0)
			return false;

		if (spellEffectInfo.TargetA.Target != th.TargetType && spellEffectInfo.TargetB.Target != th.TargetType)
			return false;

		SpellImplicitTargetInfo targetInfo = new(th.TargetType);

		switch (targetInfo.SelectionCategory)
		{
			case SpellTargetSelectionCategories.Channel: // SINGLE
				return !th.Area;
			case SpellTargetSelectionCategories.Nearby: // BOTH
				return true;
			case SpellTargetSelectionCategories.Cone: // AREA
			case SpellTargetSelectionCategories.Line: // AREA
				return th.Area;
			case SpellTargetSelectionCategories.Area: // AREA
				if (targetInfo.ObjectType == SpellTargetObjectTypes.UnitAndDest)
					return th.Area || th.Dest;

				return th.Area;
			case SpellTargetSelectionCategories.Default:
				switch (targetInfo.ObjectType)
				{
					case SpellTargetObjectTypes.Src: // EMPTY
						return false;
					case SpellTargetObjectTypes.Dest: // Dest
						return th.Dest;
					default:
						switch (targetInfo.ReferenceType)
						{
							case SpellTargetReferenceTypes.Caster: // SINGLE
								return !th.Area;
							case SpellTargetReferenceTypes.Target: // BOTH
								return true;
							default:
								break;
						}

						break;
				}

				break;
			default:
				break;
		}

		return false;
	}

	public void CallScriptBeforeHitHandlers(SpellMissInfo missInfo)
	{
		foreach (var script in GetSpellScripts<ISpellBeforeHit>())
		{
			script._InitHit();
			script._PrepareScriptCall(SpellScriptHookType.BeforeHit);
			((ISpellBeforeHit)script).BeforeHit(missInfo);
			script._FinishScriptCall();
		}
	}

	public void CallScriptOnHitHandlers()
	{
		foreach (var script in GetSpellScripts<ISpellOnHit>())
		{
			script._PrepareScriptCall(SpellScriptHookType.Hit);
			((ISpellOnHit)script).OnHit();
			script._FinishScriptCall();
		}
	}

	public void CallScriptAfterHitHandlers()
	{
		foreach (var script in GetSpellScripts<ISpellAfterHit>())
		{
			script._PrepareScriptCall(SpellScriptHookType.AfterHit);
			((ISpellAfterHit)script).AfterHit();
			script._FinishScriptCall();
		}
	}

	public void CallScriptCalcCritChanceHandlers(Unit victim, ref double critChance)
	{
		foreach (var loadedScript in GetSpellScripts<ISpellCalcCritChance>())
		{
			loadedScript._PrepareScriptCall(SpellScriptHookType.CalcCritChance);

			((ISpellCalcCritChance)loadedScript).CalcCritChance(victim, ref critChance);

			loadedScript._FinishScriptCall();
		}
	}

	public void CallScriptOnResistAbsorbCalculateHandlers(DamageInfo damageInfo, ref double resistAmount, ref double absorbAmount)
	{
		foreach (var script in GetSpellScripts<ISpellCheckCast>())
		{
			script._PrepareScriptCall(SpellScriptHookType.OnResistAbsorbCalculation);

			((ISpellCalculateResistAbsorb)script).CalculateResistAbsorb(damageInfo, ref resistAmount, ref absorbAmount);

			script._FinishScriptCall();
		}
	}

	public bool CanExecuteTriggersOnHit(Unit unit, SpellInfo triggeredByAura = null)
	{
		var onlyOnTarget = triggeredByAura != null && triggeredByAura.HasAttribute(SpellAttr4.ClassTriggerOnlyOnTarget);

		if (!onlyOnTarget)
			return true;

		// If triggeredByAura has SPELL_ATTR4_CLASS_TRIGGER_ONLY_ON_TARGET then it can only proc on either noncaster units...
		if (unit != _caster)
			return true;

		// ... or caster if it is the only target
		if (UniqueTargetInfo.Count == 1)
			return true;

		return false;
	}

	public List<ISpellScript> GetSpellScripts<T>() where T : ISpellScript
	{
		if (_spellScriptsByType.TryGetValue(typeof(T), out var scripts))
			return scripts;

		return Dummy;
	}

	public void ForEachSpellScript<T>(Action<T> action) where T : ISpellScript
	{
		foreach (T script in GetSpellScripts<T>())
			action.Invoke(script);
	}

	public List<(ISpellScript, ISpellEffect)> GetEffectScripts(SpellScriptHookType h, int index)
	{
		if (_effectHandlers.TryGetValue(index, out var effDict) &&
			effDict.TryGetValue(h, out var scripts))
			return scripts;

		return DummySpellEffects;
	}


	public SpellCastResult CheckMovement()
	{
		if (IsTriggered())
			return SpellCastResult.SpellCastOk;

		var unitCaster = _caster.AsUnit;

		if (unitCaster != null)
			if (!unitCaster.CanCastSpellWhileMoving(SpellInfo))
			{
				if (State == SpellState.Preparing)
				{
					if (_casttime > 0 && SpellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Movement))
						return SpellCastResult.Moving;
				}
				else if (State == SpellState.Casting && !SpellInfo.IsMoveAllowedChannel)
				{
					return SpellCastResult.Moving;
				}
			}

		return SpellCastResult.SpellCastOk;
	}

	public bool IsTriggered()
	{
		return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.FullMask);
	}

	public bool IsTriggeredByAura(SpellInfo auraSpellInfo)
	{
		return (auraSpellInfo == TriggeredByAuraSpell);
	}

	public bool IsIgnoringCooldowns()
	{
		return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreSpellAndCategoryCD);
	}

	public bool IsFocusDisabled()
	{
		return _triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing) || (SpellInfo.IsChanneled && !SpellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel));
	}

	public bool IsProcDisabled()
	{
		return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DisallowProcEvents);
	}

	public bool IsChannelActive()
	{
		return _caster.IsUnit && _caster.AsUnit.ChannelSpellId != 0;
	}

	public void SetReferencedFromCurrent(bool yes)
	{
		_referencedFromCurrentSpell = yes;
	}

	public bool GetPlayerIfIsEmpowered(out Player p)
	{
		p = null;

		return SpellInfo.EmpowerStages.Count > 0 && _caster.TryGetAsPlayer(out p);
	}

	public SpellInfo GetTriggeredByAuraSpell()
	{
		return TriggeredByAuraSpell;
	}

	public static implicit operator bool(Spell spell)
	{
		return spell != null;
	}

	public uint StandardVariance(double damage)
	{
		return (uint)(damage * (Random.Shared.Next(975, 1025) * 0.001));
	}

	public uint StandardVariance(int damage)
	{
		return (uint)(damage * (Random.Shared.Next(975, 1025) * 0.001));
	}

	public uint StandardVariance(uint damage)
	{
		return (uint)(damage * (Random.Shared.Next(975, 1025) * 0.001));
	}

	void SelectExplicitTargets()
	{
		// here go all explicit target changes made to explicit targets after spell prepare phase is finished
		var target = Targets.UnitTarget;

		if (target != null)
			// check for explicit target redirection, for Grounding Totem for example
			if (SpellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitEnemy) || (SpellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.Unit) && !_caster.IsFriendlyTo(target)))
			{
				Unit redirect = null;

				switch (SpellInfo.DmgClass)
				{
					case SpellDmgClass.Magic:
						redirect = _caster.GetMagicHitRedirectTarget(target, SpellInfo);

						break;
					case SpellDmgClass.Melee:
					case SpellDmgClass.Ranged:
						// should gameobjects cast damagetype melee/ranged spells this needs to be changed
						redirect = _caster.AsUnit.GetMeleeHitRedirectTarget(target, SpellInfo);

						break;
					default:
						break;
				}

				if (redirect != null && (redirect != target))
					Targets.					UnitTarget = redirect;
			}
	}

	ulong CalculateDelayMomentForDst(float launchDelay)
	{
		if (Targets.HasDst)
		{
			if (Targets.HasTraj)
			{
				var speed = Targets.SpeedXY;

				if (speed > 0.0f)
					return (ulong)(Math.Floor((Targets.Dist2d / speed + launchDelay) * 1000.0f));
			}
			else if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
			{
				return (ulong)(Math.Floor((SpellInfo.Speed + launchDelay) * 1000.0f));
			}
			else if (SpellInfo.Speed > 0.0f)
			{
				// We should not subtract caster size from dist calculation (fixes execution time desync with animation on client, eg. Malleable Goo cast by PP)
				var dist = _caster.Location.GetExactDist(Targets.DstPos);

				return (ulong)(Math.Floor((dist / SpellInfo.Speed + launchDelay) * 1000.0f));
			}

			return (ulong)Math.Floor(launchDelay * 1000.0f);
		}

		return 0;
	}

	void SelectEffectImplicitTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, ref uint processedEffectMask)
	{
		if (targetType.Target == 0)
			return;

		var effectMask = (1u << spellEffectInfo.EffectIndex);

		// set the same target list for all effects
		// some spells appear to need this, however this requires more research
		switch (targetType.SelectionCategory)
		{
			case SpellTargetSelectionCategories.Nearby:
			case SpellTargetSelectionCategories.Cone:
			case SpellTargetSelectionCategories.Area:
			case SpellTargetSelectionCategories.Line:
			{
				// targets for effect already selected
				if (Convert.ToBoolean(effectMask & processedEffectMask))
					return;

				var effects = SpellInfo.Effects;

				// choose which targets we can select at once
				for (var j = spellEffectInfo.EffectIndex + 1; j < effects.Count; ++j)
					if (effects[j].IsEffect() &&
						spellEffectInfo.TargetA.Target == effects[j].TargetA.Target &&
						spellEffectInfo.TargetB.Target == effects[j].TargetB.Target &&
						spellEffectInfo.ImplicitTargetConditions == effects[j].ImplicitTargetConditions &&
						spellEffectInfo.CalcRadius(_caster) == effects[j].CalcRadius(_caster) &&
						CheckScriptEffectImplicitTargets(spellEffectInfo.EffectIndex, j))
						effectMask |= 1u << j;

				processedEffectMask |= effectMask;

				break;
			}
			default:
				break;
		}

		switch (targetType.SelectionCategory)
		{
			case SpellTargetSelectionCategories.Channel:
				SelectImplicitChannelTargets(spellEffectInfo, targetType);

				break;
			case SpellTargetSelectionCategories.Nearby:
				SelectImplicitNearbyTargets(spellEffectInfo, targetType, effectMask);

				break;
			case SpellTargetSelectionCategories.Cone:
				SelectImplicitConeTargets(spellEffectInfo, targetType, effectMask);

				break;
			case SpellTargetSelectionCategories.Area:
				SelectImplicitAreaTargets(spellEffectInfo, targetType, effectMask);

				break;
			case SpellTargetSelectionCategories.Traj:
				// just in case there is no dest, explanation in SelectImplicitDestDestTargets
				CheckDst();

				SelectImplicitTrajTargets(spellEffectInfo, targetType);

				break;
			case SpellTargetSelectionCategories.Line:
				SelectImplicitLineTargets(spellEffectInfo, targetType, effectMask);

				break;
			case SpellTargetSelectionCategories.Default:
				switch (targetType.ObjectType)
				{
					case SpellTargetObjectTypes.Src:
						switch (targetType.ReferenceType)
						{
							case SpellTargetReferenceTypes.Caster:
								Targets.SetSrc(_caster);

								break;
							default:
								Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT_SRC");

								break;
						}

						break;
					case SpellTargetObjectTypes.Dest:
						switch (targetType.ReferenceType)
						{
							case SpellTargetReferenceTypes.Caster:
								SelectImplicitCasterDestTargets(spellEffectInfo, targetType);

								break;
							case SpellTargetReferenceTypes.Target:
								SelectImplicitTargetDestTargets(spellEffectInfo, targetType);

								break;
							case SpellTargetReferenceTypes.Dest:
								SelectImplicitDestDestTargets(spellEffectInfo, targetType);

								break;
							default:
								Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT_DEST");

								break;
						}

						break;
					default:
						switch (targetType.ReferenceType)
						{
							case SpellTargetReferenceTypes.Caster:
								SelectImplicitCasterObjectTargets(spellEffectInfo, targetType);

								break;
							case SpellTargetReferenceTypes.Target:
								SelectImplicitTargetObjectTargets(spellEffectInfo, targetType);

								break;
							default:
								Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT");

								break;
						}

						break;
				}

				break;
			case SpellTargetSelectionCategories.Nyi:
				Log.outDebug(LogFilter.Spells, "SPELL: target type {0}, found in spellID {1}, effect {2} is not implemented yet!", SpellInfo.Id, spellEffectInfo.EffectIndex, targetType.Target);

				break;
			default:
				Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target category");

				break;
		}
	}

	void SelectImplicitChannelTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
	{
		if (targetType.ReferenceType != SpellTargetReferenceTypes.Caster)
		{
			Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target reference type");

			return;
		}

		var channeledSpell = _originalCaster.GetCurrentSpell(CurrentSpellTypes.Channeled);

		if (channeledSpell == null)
		{
			Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitChannelTargets: cannot find channel spell for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);

			return;
		}

		switch (targetType.Target)
		{
			case Framework.Constants.Targets.UnitChannelTarget:
			{
				foreach (var channelTarget in _originalCaster.UnitData.ChannelObjects)
				{
					WorldObject target = Global.ObjAccessor.GetUnit(_caster, channelTarget);
					CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
					// unit target may be no longer avalible - teleported out of map for example
					var unitTarget = target ? target.AsUnit : null;

					if (unitTarget)
						AddUnitTarget(unitTarget, 1u << spellEffectInfo.EffectIndex);
					else
						Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell target for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
				}

				break;
			}
			case Framework.Constants.Targets.DestChannelTarget:
			{
				if (channeledSpell.Targets.HasDst)
				{
					Targets.SetDst(channeledSpell.Targets);
				}
				else
				{
					List<ObjectGuid> channelObjects = _originalCaster.UnitData.ChannelObjects;
					var target = !channelObjects.Empty() ? Global.ObjAccessor.GetWorldObject(_caster, channelObjects[0]) : null;

					if (target != null)
					{
						CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

						if (target)
						{
							SpellDestination dest = new(target);

							if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
								dest.Position.Orientation = spellEffectInfo.PositionFacing;

							CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
							Targets.							Dst = dest;
						}
					}
					else
					{
						Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell destination for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
					}
				}

				break;
			}
			case Framework.Constants.Targets.DestChannelCaster:
			{
				SpellDestination dest = new(channeledSpell.Caster);

				if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
					dest.Position.Orientation = spellEffectInfo.PositionFacing;

				CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
				Targets.				Dst = dest;

				break;
			}
			default:
				Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target type");

				break;
		}
	}

	void SelectImplicitNearbyTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
	{
		if (targetType.ReferenceType != SpellTargetReferenceTypes.Caster)
		{
			Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target reference type");

			return;
		}

		var range = 0.0f;

		switch (targetType.CheckType)
		{
			case SpellTargetCheckTypes.Enemy:
				range = SpellInfo.GetMaxRange(false, _caster, this);

				break;
			case SpellTargetCheckTypes.Ally:
			case SpellTargetCheckTypes.Party:
			case SpellTargetCheckTypes.Raid:
			case SpellTargetCheckTypes.RaidClass:
				range = SpellInfo.GetMaxRange(true, _caster, this);

				break;
			case SpellTargetCheckTypes.Entry:
			case SpellTargetCheckTypes.Default:
				range = SpellInfo.GetMaxRange(IsPositive(), _caster, this);

				break;
			default:
				Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented selection check type");

				break;
		}

		var condList = spellEffectInfo.ImplicitTargetConditions;

		// handle emergency case - try to use other provided targets if no conditions provided
		if (targetType.CheckType == SpellTargetCheckTypes.Entry && (condList == null || condList.Empty()))
		{
			Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: no conditions entry for target with TARGET_CHECK_ENTRY of spell ID {0}, effect {1} - selecting default targets", SpellInfo.Id, spellEffectInfo.EffectIndex);

			switch (targetType.ObjectType)
			{
				case SpellTargetObjectTypes.Gobj:
					if (SpellInfo.RequiresSpellFocus != 0)
					{
						if (_focusObject != null)
						{
							AddGOTarget(_focusObject, effMask);
						}
						else
						{
							SendCastResult(SpellCastResult.BadImplicitTargets);
							Finish(SpellCastResult.BadImplicitTargets);
						}

						return;
					}

					break;
				case SpellTargetObjectTypes.Dest:
					if (SpellInfo.RequiresSpellFocus != 0)
					{
						if (_focusObject != null)
						{
							SpellDestination dest = new(_focusObject);

							if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
								dest.Position.Orientation = spellEffectInfo.PositionFacing;

							CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
							Targets.							Dst = dest;
						}
						else
						{
							SendCastResult(SpellCastResult.BadImplicitTargets);
							Finish(SpellCastResult.BadImplicitTargets);
						}

						return;
					}

					break;
				default:
					break;
			}
		}

		var target = SearchNearbyTarget(range, targetType.ObjectType, targetType.CheckType, condList);

		if (target == null)
		{
			Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: cannot find nearby target for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
			SendCastResult(SpellCastResult.BadImplicitTargets);
			Finish(SpellCastResult.BadImplicitTargets);

			return;
		}

		CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

		if (!target)
		{
			Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set NULL target, effect {spellEffectInfo.EffectIndex}");
			SendCastResult(SpellCastResult.BadImplicitTargets);
			Finish(SpellCastResult.BadImplicitTargets);

			return;
		}

		switch (targetType.ObjectType)
		{
			case SpellTargetObjectTypes.Unit:
				var unitTarget = target.AsUnit;

				if (unitTarget != null)
				{
					AddUnitTarget(unitTarget, effMask, true, false);
				}
				else
				{
					Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong type, expected unit, got {target.GUID.High}, effect {effMask}");
					SendCastResult(SpellCastResult.BadImplicitTargets);
					Finish(SpellCastResult.BadImplicitTargets);

					return;
				}

				break;
			case SpellTargetObjectTypes.Gobj:
				var gobjTarget = target.AsGameObject;

				if (gobjTarget != null)
				{
					AddGOTarget(gobjTarget, effMask);
				}
				else
				{
					Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong type, expected gameobject, got {target.GUID.High}, effect {effMask}");
					SendCastResult(SpellCastResult.BadImplicitTargets);
					Finish(SpellCastResult.BadImplicitTargets);

					return;
				}

				break;
			case SpellTargetObjectTypes.Corpse:
				var corpseTarget = target.AsCorpse;

				if (corpseTarget != null)
				{
					AddCorpseTarget(corpseTarget, effMask);
				}
				else
				{
					Log.outDebug(LogFilter.Spells, $"Spell::SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong type, expected corpse, got {target.GUID.TypeId}, effect {effMask}");
					SendCastResult(SpellCastResult.BadImplicitTargets);
					Finish(SpellCastResult.BadImplicitTargets);

					return;
				}

				break;
			case SpellTargetObjectTypes.Dest:
				SpellDestination dest = new(target);

				if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
					dest.Position.Orientation = spellEffectInfo.PositionFacing;

				CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
				Targets.				Dst = dest;

				break;
			default:
				Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target object type");

				break;
		}

		SelectImplicitChainTargets(spellEffectInfo, targetType, target, effMask);
	}

	void SelectImplicitConeTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
	{
		Position coneSrc = new(_caster.Location);
		var coneAngle = SpellInfo.ConeAngle;

		switch (targetType.ReferenceType)
		{
			case SpellTargetReferenceTypes.Caster:
				break;
			case SpellTargetReferenceTypes.Dest:
				if (_caster.Location.GetExactDist2d(Targets.DstPos) > 0.1f)
					coneSrc.Orientation = _caster.Location.GetAbsoluteAngle(Targets.DstPos);

				break;
			default:
				break;
		}

		switch (targetType.Target)
		{
			case Framework.Constants.Targets.UnitCone180DegEnemy:
				if (coneAngle == 0.0f)
					coneAngle = 180.0f;

				break;
			default:
				break;
		}

		List<WorldObject> targets = new();
		var objectType = targetType.ObjectType;
		var selectionType = targetType.CheckType;

		var condList = spellEffectInfo.ImplicitTargetConditions;
		var radius = spellEffectInfo.CalcRadius(_caster) * SpellValue.RadiusMod;

		var containerTypeMask = GetSearcherTypeMask(objectType, condList);

		if (containerTypeMask != 0)
		{
			var extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
			var spellCone = new WorldObjectSpellConeTargetCheck(coneSrc, MathFunctions.DegToRad(coneAngle), SpellInfo.Width != 0 ? SpellInfo.Width : _caster.CombatReach, radius, _caster, SpellInfo, selectionType, condList, objectType);
			var searcher = new WorldObjectListSearcher(_caster, targets, spellCone, containerTypeMask);
			SearchTargets(searcher, containerTypeMask, _caster, _caster.Location, radius + extraSearchRadius);

			CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

			if (!targets.Empty())
			{
				// Other special target selection goes here
				var maxTargets = SpellValue.MaxAffectedTargets;

				if (maxTargets != 0)
					targets.RandomResize(maxTargets);

				foreach (var obj in targets)
					if (obj.IsUnit)
						AddUnitTarget(obj.AsUnit, effMask, false);
					else if (obj.IsGameObject)
						AddGOTarget(obj.AsGameObject, effMask);
					else if (obj.IsCorpse)
						AddCorpseTarget(obj.AsCorpse, effMask);
			}
		}
	}

	void SelectImplicitAreaTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
	{
		WorldObject referer;

		switch (targetType.ReferenceType)
		{
			case SpellTargetReferenceTypes.Src:
			case SpellTargetReferenceTypes.Dest:
			case SpellTargetReferenceTypes.Caster:
				referer = _caster;

				break;
			case SpellTargetReferenceTypes.Target:
				referer = Targets.UnitTarget;

				break;
			case SpellTargetReferenceTypes.Last:
			{
				referer = _caster;

				// find last added target for this effect
				foreach (var target in UniqueTargetInfo)
					if (Convert.ToBoolean(target.EffectMask & (1 << spellEffectInfo.EffectIndex)))
					{
						referer = Global.ObjAccessor.GetUnit(_caster, target.TargetGuid);

						break;
					}

				break;
			}
			default:
				Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented target reference type");

				return;
		}

		if (referer == null)
			return;

		Position center;

		switch (targetType.ReferenceType)
		{
			case SpellTargetReferenceTypes.Src:
				center = Targets.SrcPos;

				break;
			case SpellTargetReferenceTypes.Dest:
				center = Targets.DstPos;

				break;
			case SpellTargetReferenceTypes.Caster:
			case SpellTargetReferenceTypes.Target:
			case SpellTargetReferenceTypes.Last:
				center = referer.Location;

				break;
			default:
				Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented target reference type");

				return;
		}

		var radius = spellEffectInfo.CalcRadius(_caster) * SpellValue.RadiusMod;
		List<WorldObject> targets = new();

		switch (targetType.Target)
		{
			case Framework.Constants.Targets.UnitCasterAndPassengers:
				targets.Add(_caster);
				var unit = _caster.AsUnit;

				if (unit != null)
				{
					var vehicleKit = unit.GetVehicleKit();

					if (vehicleKit != null)
						for (sbyte seat = 0; seat < SharedConst.MaxVehicleSeats; ++seat)
						{
							var passenger = vehicleKit.GetPassenger(seat);

							if (passenger != null)
								targets.Add(passenger);
						}
				}

				break;
			case Framework.Constants.Targets.UnitTargetAllyOrRaid:
				var targetedUnit = Targets.UnitTarget;

				if (targetedUnit != null)
				{
					if (!_caster.IsUnit || !_caster.AsUnit.IsInRaidWith(targetedUnit))
						targets.Add(Targets.UnitTarget);
					else
						SearchAreaTargets(targets, radius, targetedUnit.Location, referer, targetType.ObjectType, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions);
				}

				break;
			case Framework.Constants.Targets.UnitCasterAndSummons:
				targets.Add(_caster);
				SearchAreaTargets(targets, radius, center, referer, targetType.ObjectType, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions);

				break;
			default:
				SearchAreaTargets(targets, radius, center, referer, targetType.ObjectType, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions);

				break;
		}

		if (targetType.ObjectType == SpellTargetObjectTypes.UnitAndDest)
		{
			SpellDestination dest = new(referer);

			if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
				dest.Position.Orientation = spellEffectInfo.PositionFacing;

			CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);

			Targets.ModDst(dest);
		}

		CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

		if (targetType.Target == Framework.Constants.Targets.UnitSrcAreaFurthestEnemy)
			targets.Sort(new ObjectDistanceOrderPred(referer, false));

		if (!targets.Empty())
		{
			// Other special target selection goes here
			var maxTargets = SpellValue.MaxAffectedTargets;

			if (maxTargets != 0)
			{
				if (targetType.Target != Framework.Constants.Targets.UnitSrcAreaFurthestEnemy)
					targets.RandomResize(maxTargets);
				else if (targets.Count > maxTargets)
					targets.Resize(maxTargets);
			}

			foreach (var obj in targets)
				if (obj.IsUnit)
					AddUnitTarget(obj.AsUnit, effMask, false, true, center);
				else if (obj.IsGameObject)
					AddGOTarget(obj.AsGameObject, effMask);
				else if (obj.IsCorpse)
					AddCorpseTarget(obj.AsCorpse, effMask);
		}
	}

	void SelectImplicitCasterDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
	{
		SpellDestination dest = new(_caster);

		switch (targetType.Target)
		{
			case Framework.Constants.Targets.DestCaster:
				break;
			case Framework.Constants.Targets.DestHome:
				var playerCaster = _caster.AsPlayer;

				if (playerCaster != null)
					dest = new SpellDestination(playerCaster.Homebind);

				break;
			case Framework.Constants.Targets.DestDb:
				var st = Global.SpellMgr.GetSpellTargetPosition(SpellInfo.Id, spellEffectInfo.EffectIndex);

				if (st != null)
				{
					// @todo fix this check
					if (SpellInfo.HasEffect(SpellEffectName.TeleportUnits) || SpellInfo.HasEffect(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen) || SpellInfo.HasEffect(SpellEffectName.Bind))
						dest = new SpellDestination(st.X, st.Y, st.Z, st.Orientation, st.TargetMapId);
					else if (st.TargetMapId == _caster.Location.MapId)
						dest = new SpellDestination(st.X, st.Y, st.Z, st.Orientation);
				}
				else
				{
					Log.outDebug(LogFilter.Spells, "SPELL: unknown target coordinates for spell ID {0}", SpellInfo.Id);
					var target = Targets.ObjectTarget;

					if (target)
						dest = new SpellDestination(target);
				}

				break;
			case Framework.Constants.Targets.DestCasterFishing:
			{
				var minDist = SpellInfo.GetMinRange(true);
				var maxDist = SpellInfo.GetMaxRange(true);
				var dis = (float)RandomHelper.NextDouble() * (maxDist - minDist) + minDist;
				var angle = (float)RandomHelper.NextDouble() * (MathFunctions.PI * 35.0f / 180.0f) - (float)(Math.PI * 17.5f / 180.0f);
				var pos = new Position();
				_caster.GetClosePoint(pos, SharedConst.DefaultPlayerBoundingRadius, dis, angle);

				var ground = _caster.GetMapHeight(pos);
				var liquidLevel = MapConst.VMAPInvalidHeightValue;

				if (_caster.GetMap().GetLiquidStatus(_caster.PhaseShift, pos, LiquidHeaderTypeFlags.AllLiquids, out var liquidData, _caster.CollisionHeight) != 0)
					liquidLevel = liquidData.level;

				if (liquidLevel <= ground) // When there is no liquid Map.GetWaterOrGroundLevel returns ground level
				{
					SendCastResult(SpellCastResult.NotHere);
					SendChannelUpdate(0);
					Finish(SpellCastResult.NotHere);

					return;
				}

				if (ground + 0.75 > liquidLevel)
				{
					SendCastResult(SpellCastResult.TooShallow);
					SendChannelUpdate(0);
					Finish(SpellCastResult.TooShallow);

					return;
				}

				dest = new SpellDestination(pos.X, pos.Y, liquidLevel, _caster.Location.Orientation);

				break;
			}
			case Framework.Constants.Targets.DestCasterFrontLeap:
			case Framework.Constants.Targets.DestCasterMovementDirection:
			{
				var unitCaster = _caster.AsUnit;

				if (unitCaster == null)
					break;

				var dist = spellEffectInfo.CalcRadius(unitCaster);
				var angle = targetType.CalcDirectionAngle();

				if (targetType.Target == Framework.Constants.Targets.DestCasterMovementDirection)
					switch (_caster.MovementInfo.MovementFlags & (MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight))
					{
						case MovementFlag.None:
						case MovementFlag.Forward:
						case MovementFlag.Forward | MovementFlag.Backward:
						case MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
						case MovementFlag.Forward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
						case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
							angle = 0.0f;

							break;
						case MovementFlag.Backward:
						case MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
							angle = MathF.PI;

							break;
						case MovementFlag.StrafeLeft:
						case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft:
							angle = (MathF.PI / 2);

							break;
						case MovementFlag.Forward | MovementFlag.StrafeLeft:
							angle = (MathF.PI / 4);

							break;
						case MovementFlag.Backward | MovementFlag.StrafeLeft:
							angle = (3 * MathF.PI / 4);

							break;
						case MovementFlag.StrafeRight:
						case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeRight:
							angle = (-MathF.PI / 2);

							break;
						case MovementFlag.Forward | MovementFlag.StrafeRight:
							angle = (-MathF.PI / 4);

							break;
						case MovementFlag.Backward | MovementFlag.StrafeRight:
							angle = (-3 * MathF.PI / 4);

							break;
						default:
							angle = 0.0f;

							break;
					}

				Position pos = new(dest.Position);

				unitCaster.MovePositionToFirstCollision(pos, dist, angle);
				dest.Relocate(pos);

				break;
			}
			case Framework.Constants.Targets.DestCasterGround:
			case Framework.Constants.Targets.DestCasterGround2:
				dest.Position.Z = _caster.GetMapWaterOrGroundLevel(dest.Position.X, dest.Position.Y, dest.Position.Z);

				break;
			case Framework.Constants.Targets.DestSummoner:
			{
				var unitCaster = _caster.AsUnit;

				if (unitCaster != null)
				{
					var casterSummon = unitCaster.ToTempSummon();

					if (casterSummon != null)
					{
						var summoner = casterSummon.GetSummoner();

						if (summoner != null)
							dest = new SpellDestination(summoner);
					}
				}

				break;
			}
			default:
			{
				var dist = spellEffectInfo.CalcRadius(_caster);
				var angl = targetType.CalcDirectionAngle();
				var objSize = _caster.CombatReach;

				switch (targetType.Target)
				{
					case Framework.Constants.Targets.DestCasterSummon:
						dist = SharedConst.PetFollowDist;

						break;
					case Framework.Constants.Targets.DestCasterRandom:
						if (dist > objSize)
							dist = objSize + (dist - objSize) * (float)RandomHelper.NextDouble();

						break;
					case Framework.Constants.Targets.DestCasterFrontLeft:
					case Framework.Constants.Targets.DestCasterBackLeft:
					case Framework.Constants.Targets.DestCasterFrontRight:
					case Framework.Constants.Targets.DestCasterBackRight:
					{
						var DefaultTotemDistance = 3.0f;

						if (!spellEffectInfo.HasRadius() && !spellEffectInfo.HasMaxRadius())
							dist = DefaultTotemDistance;

						break;
					}
					default:
						break;
				}

				if (dist < objSize)
					dist = objSize;

				Position pos = new(dest.Position);
				_caster.MovePositionToFirstCollision(pos, dist, angl);

				dest.Relocate(pos);

				break;
			}
		}

		if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
			dest.Position.Orientation = spellEffectInfo.PositionFacing;

		CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
		Targets.		Dst = dest;
	}

	void SelectImplicitTargetDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
	{
		var target = Targets.ObjectTarget;

		SpellDestination dest = new(target);

		switch (targetType.Target)
		{
			case Framework.Constants.Targets.DestTargetEnemy:
			case Framework.Constants.Targets.DestAny:
			case Framework.Constants.Targets.DestTargetAlly:
				break;
			default:
			{
				var angle = targetType.CalcDirectionAngle();
				var dist = spellEffectInfo.CalcRadius(null);

				if (targetType.Target == Framework.Constants.Targets.DestRandom)
					dist *= (float)RandomHelper.NextDouble();

				Position pos = new(dest.Position);
				target.MovePositionToFirstCollision(pos, dist, angle);

				dest.Relocate(pos);
			}

				break;
		}

		if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
			dest.Position.Orientation = spellEffectInfo.PositionFacing;

		CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
		Targets.		Dst = dest;
	}

	void SelectImplicitDestDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
	{
		// set destination to caster if no dest provided
		// can only happen if previous destination target could not be set for some reason
		// (not found nearby target, or channel target for example
		// maybe we should abort the spell in such case?
		CheckDst();

		var dest = Targets.Dst;

		switch (targetType.Target)
		{
			case Framework.Constants.Targets.DestDynobjEnemy:
			case Framework.Constants.Targets.DestDynobjAlly:
			case Framework.Constants.Targets.DestDynobjNone:
			case Framework.Constants.Targets.DestDest:
				break;
			case Framework.Constants.Targets.DestDestGround:
				dest.Position.Z = _caster.GetMapHeight(dest.Position.X, dest.Position.Y, dest.Position.Z);

				break;
			default:
			{
				var angle = targetType.CalcDirectionAngle();
				var dist = spellEffectInfo.CalcRadius(_caster);

				if (targetType.Target == Framework.Constants.Targets.DestRandom)
					dist *= (float)RandomHelper.NextDouble();

				Position pos = new(Targets.DstPos);
				_caster.MovePositionToFirstCollision(pos, dist, angle);

				dest.Relocate(pos);
			}

				break;
		}

		if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
			dest.Position.Orientation = spellEffectInfo.PositionFacing;

		CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
		Targets.ModDst(dest);
	}

	void SelectImplicitCasterObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
	{
		WorldObject target = null;
		var checkIfValid = true;

		switch (targetType.Target)
		{
			case Framework.Constants.Targets.UnitCaster:
				target = _caster;
				checkIfValid = false;

				break;
			case Framework.Constants.Targets.UnitMaster:
				target = _caster.CharmerOrOwner;

				break;
			case Framework.Constants.Targets.UnitPet:
			{
				var unitCaster = _caster.AsUnit;

				if (unitCaster != null)
					target = unitCaster.GetGuardianPet();

				break;
			}
			case Framework.Constants.Targets.UnitSummoner:
			{
				var unitCaster = _caster.AsUnit;

				if (unitCaster != null)
					if (unitCaster.IsSummon)
						target = unitCaster.ToTempSummon().GetSummonerUnit();

				break;
			}
			case Framework.Constants.Targets.UnitVehicle:
			{
				var unitCaster = _caster.AsUnit;

				if (unitCaster != null)
					target = unitCaster.GetVehicleBase();

				break;
			}
			case Framework.Constants.Targets.UnitPassenger0:
			case Framework.Constants.Targets.UnitPassenger1:
			case Framework.Constants.Targets.UnitPassenger2:
			case Framework.Constants.Targets.UnitPassenger3:
			case Framework.Constants.Targets.UnitPassenger4:
			case Framework.Constants.Targets.UnitPassenger5:
			case Framework.Constants.Targets.UnitPassenger6:
			case Framework.Constants.Targets.UnitPassenger7:
				var vehicleBase = _caster.AsCreature;

				if (vehicleBase != null && vehicleBase.IsVehicle)
					target = vehicleBase.GetVehicleKit().GetPassenger((sbyte)(targetType.Target - Framework.Constants.Targets.UnitPassenger0));

				break;
			case Framework.Constants.Targets.UnitTargetTapList:
				var creatureCaster = _caster.AsCreature;

				if (creatureCaster != null && !creatureCaster.TapList.Empty())
					target = Global.ObjAccessor.GetWorldObject(creatureCaster, creatureCaster.TapList.SelectRandom());

				break;
			case Framework.Constants.Targets.UnitOwnCritter:
			{
				var unitCaster = _caster.AsUnit;

				if (unitCaster != null)
					target = ObjectAccessor.GetCreatureOrPetOrVehicle(_caster, unitCaster.CritterGUID);

				break;
			}
			default:
				break;
		}

		CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

		if (target)
		{
			if (target.IsUnit)
				AddUnitTarget(target.AsUnit, 1u << spellEffectInfo.EffectIndex, checkIfValid);
			else if (target.IsGameObject)
				AddGOTarget(target.AsGameObject, 1u << spellEffectInfo.EffectIndex);
			else if (target.IsCorpse)
				AddCorpseTarget(target.AsCorpse, 1u << spellEffectInfo.EffectIndex);
		}
	}

	void SelectImplicitTargetObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
	{
		var target = Targets.ObjectTarget;

		CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

		var item = Targets.ItemTarget;

		if (target != null)
		{
			if (target.IsUnit)
				AddUnitTarget(target.AsUnit, 1u << spellEffectInfo.EffectIndex, true, false);
			else if (target.IsGameObject)
				AddGOTarget(target.AsGameObject, 1u << spellEffectInfo.EffectIndex);
			else if (target.IsCorpse)
				AddCorpseTarget(target.AsCorpse, 1u << spellEffectInfo.EffectIndex);

			SelectImplicitChainTargets(spellEffectInfo, targetType, target, 1u << spellEffectInfo.EffectIndex);
		}
		// Script hook can remove object target and we would wrongly land here
		else if (item != null)
		{
			AddItemTarget(item, 1u << spellEffectInfo.EffectIndex);
		}
	}

	void SelectImplicitChainTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, WorldObject target, uint effMask)
	{
		var maxTargets = spellEffectInfo.ChainTargets;
		var modOwner = _caster.GetSpellModOwner();

		if (modOwner)
			modOwner.ApplySpellMod(SpellInfo, SpellModOp.ChainTargets, ref maxTargets, this);

		if (maxTargets > 1)
		{
			// mark damage multipliers as used
			for (var k = spellEffectInfo.EffectIndex; k < SpellInfo.Effects.Count; ++k)
				if (Convert.ToBoolean(effMask & (1 << k)))
					_damageMultipliers[spellEffectInfo.EffectIndex] = 1.0f;

			_applyMultiplierMask |= effMask;

			List<WorldObject> targets = new();
			SearchChainTargets(targets, (uint)maxTargets - 1, target, targetType.ObjectType, targetType.CheckType, spellEffectInfo, targetType.Target == Framework.Constants.Targets.UnitChainhealAlly);

			// Chain primary target is added earlier
			CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

			Position losPosition = SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? _caster.Location : target.Location;

			foreach (var obj in targets)
			{
				var unitTarget = obj.AsUnit;

				if (unitTarget)
					AddUnitTarget(unitTarget, effMask, false, true, losPosition);

				if (!SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) && !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
					losPosition = obj.Location;
			}
		}
	}

	float Tangent(float x)
	{
		x = (float)Math.Tan(x);

		if (x < 100000.0f && x > -100000.0f) return x;
		if (x >= 100000.0f) return 100000.0f;
		if (x <= 100000.0f) return -100000.0f;

		return 0.0f;
	}

	void SelectImplicitTrajTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
	{
		if (!Targets.HasTraj)
			return;

		var dist2d = Targets.Dist2d;

		if (dist2d == 0)
			return;

		var srcPos = Targets.SrcPos;
		srcPos.Orientation = _caster.Location.Orientation;
		var srcToDestDelta = Targets.DstPos.Z - srcPos.Z;

		List<WorldObject> targets = new();
		var spellTraj = new WorldObjectSpellTrajTargetCheck(dist2d, srcPos, _caster, SpellInfo, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions, SpellTargetObjectTypes.None);
		var searcher = new WorldObjectListSearcher(_caster, targets, spellTraj);
		SearchTargets(searcher, GridMapTypeMask.All, _caster, srcPos, dist2d);

		if (targets.Empty())
			return;

		targets.Sort(new ObjectDistanceOrderPred(_caster));

		var b = Tangent(Targets.Pitch);
		var a = (srcToDestDelta - dist2d * b) / (dist2d * dist2d);

		if (a > -0.0001f)
			a = 0f;

		// We should check if triggered spell has greater range (which is true in many cases, and initial spell has too short max range)
		// limit max range to 300 yards, sometimes triggered spells can have 50000yds
		var bestDist = SpellInfo.GetMaxRange(false);
		var triggerSpellInfo = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, GetCastDifficulty());

		if (triggerSpellInfo != null)
			bestDist = Math.Min(Math.Max(bestDist, triggerSpellInfo.GetMaxRange(false)), Math.Min(dist2d, 300.0f));

		// GameObjects don't cast traj
		var unitCaster = _caster.AsUnit;

		foreach (var obj in targets)
		{
			if (SpellInfo.CheckTarget(unitCaster, obj, true) != SpellCastResult.SpellCastOk)
				continue;

			var unitTarget = obj.AsUnit;

			if (unitTarget)
			{
				if (unitCaster == obj || unitCaster.IsOnVehicle(unitTarget) || unitTarget.GetVehicle())
					continue;

				var creatureTarget = unitTarget.AsCreature;

				if (creatureTarget)
					if (!creatureTarget.CreatureTemplate.TypeFlags.HasAnyFlag(CreatureTypeFlags.CollideWithMissiles))
						continue;
			}

			var size = Math.Max(obj.CombatReach, 1.0f);
			var objDist2d = srcPos.GetExactDist2d(obj.Location);
			var dz = obj.Location.Z - srcPos.Z;

			var horizontalDistToTraj = (float)Math.Abs(objDist2d * Math.Sin(srcPos.GetRelativeAngle(obj.Location)));
			var sizeFactor = (float)Math.Cos((horizontalDistToTraj / size) * (Math.PI / 2.0f));
			var distToHitPoint = (float)Math.Max(objDist2d * Math.Cos(srcPos.GetRelativeAngle(obj.Location)) - size * sizeFactor, 0.0f);
			var height = distToHitPoint * (a * distToHitPoint + b);

			if (Math.Abs(dz - height) > size + b / 2.0f + SpellConst.TrajectoryMissileSize)
				continue;

			if (distToHitPoint < bestDist)
			{
				bestDist = distToHitPoint;

				break;
			}
		}

		if (dist2d > bestDist)
		{
			var x = (float)(Targets.SrcPos.X + Math.Cos(unitCaster.Location.Orientation) * bestDist);
			var y = (float)(Targets.SrcPos.Y + Math.Sin(unitCaster.Location.Orientation) * bestDist);
			var z = Targets.SrcPos.Z + bestDist * (a * bestDist + b);

			SpellDestination dest = new(x, y, z, unitCaster.Location.Orientation);

			if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
				dest.Position.Orientation = spellEffectInfo.PositionFacing;

			CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
			Targets.ModDst(dest);
		}
	}

	void SelectImplicitLineTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
	{
		List<WorldObject> targets = new();
		var objectType = targetType.ObjectType;
		var selectionType = targetType.CheckType;

		Position dst;

		switch (targetType.ReferenceType)
		{
			case SpellTargetReferenceTypes.Src:
				dst = Targets.SrcPos;

				break;
			case SpellTargetReferenceTypes.Dest:
				dst = Targets.DstPos;

				break;
			case SpellTargetReferenceTypes.Caster:
				dst = _caster.Location;

				break;
			case SpellTargetReferenceTypes.Target:
				dst = Targets.UnitTarget.Location;

				break;
			default:
				Cypher.Assert(false, "Spell.SelectImplicitLineTargets: received not implemented target reference type");

				return;
		}

		var condList = spellEffectInfo.ImplicitTargetConditions;
		var radius = spellEffectInfo.CalcRadius(_caster) * SpellValue.RadiusMod;

		var containerTypeMask = GetSearcherTypeMask(objectType, condList);

		if (containerTypeMask != 0)
		{
			WorldObjectSpellLineTargetCheck check = new(_caster.Location, dst, SpellInfo.Width != 0 ? SpellInfo.Width : _caster.CombatReach, radius, _caster, SpellInfo, selectionType, condList, objectType);
			WorldObjectListSearcher searcher = new(_caster, targets, check, containerTypeMask);
			SearchTargets(searcher, containerTypeMask, _caster, _caster.Location, radius);

			CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

			if (!targets.Empty())
			{
				// Other special target selection goes here
				var maxTargets = SpellValue.MaxAffectedTargets;

				if (maxTargets != 0)
					if (maxTargets < targets.Count)
					{
						targets.Sort(new ObjectDistanceOrderPred(_caster));
						targets.Resize(maxTargets);
					}

				foreach (var obj in targets)
					if (obj.IsUnit)
						AddUnitTarget(obj.AsUnit, effMask, false);
					else if (obj.IsGameObject)
						AddGOTarget(obj.AsGameObject, effMask);
					else if (obj.IsCorpse)
						AddCorpseTarget(obj.AsCorpse, effMask);
			}
		}
	}

	void SelectEffectTypeImplicitTargets(SpellEffectInfo spellEffectInfo)
	{
		// special case for SPELL_EFFECT_SUMMON_RAF_FRIEND and SPELL_EFFECT_SUMMON_PLAYER, queue them on map for later execution
		switch (spellEffectInfo.Effect)
		{
			case SpellEffectName.SummonRafFriend:
			case SpellEffectName.SummonPlayer:
				if (_caster.IsTypeId(TypeId.Player) && !_caster.AsPlayer.Target.IsEmpty)
				{
					WorldObject rafTarget = Global.ObjAccessor.FindPlayer(_caster.AsPlayer.Target);

					CallScriptObjectTargetSelectHandlers(ref rafTarget, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

					// scripts may modify the target - recheck
					if (rafTarget != null && rafTarget.IsPlayer)
					{
						// target is not stored in target map for those spells
						// since we're completely skipping AddUnitTarget logic, we need to check immunity manually
						// eg. aura 21546 makes target immune to summons
						var player = rafTarget.AsPlayer;

						if (player.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, null))
							return;

						var spell = this;
						var targetGuid = rafTarget.GUID;

						rafTarget.GetMap()
								.AddFarSpellCallback(map =>
								{
									var player = Global.ObjAccessor.GetPlayer(map, targetGuid);

									if (player == null)
										return;

									// check immunity again in case it changed during update
									if (player.IsImmunedToSpellEffect(spell.SpellInfo, spellEffectInfo, null))
										return;

									spell.HandleEffects(player, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
								});
					}
				}

				return;
			default:
				break;
		}

		// select spell implicit targets based on effect type
		if (spellEffectInfo.GetImplicitTargetType() == 0)
			return;

		var targetMask = spellEffectInfo.GetMissingTargetMask();

		if (targetMask == 0)
			return;

		WorldObject target = null;

		switch (spellEffectInfo.GetImplicitTargetType())
		{
			// add explicit object target or self to the target map
			case SpellEffectImplicitTargetTypes.Explicit:
				// player which not released his spirit is Unit, but target flag for it is TARGET_FLAG_CORPSE_MASK
				if (Convert.ToBoolean(targetMask & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)))
				{
					var unitTarget = Targets.UnitTarget;

					if (unitTarget != null)
					{
						target = unitTarget;
					}
					else if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.CorpseMask))
					{
						var corpseTarget = Targets.CorpseTarget;

						if (corpseTarget != null)
							target = corpseTarget;
					}
					else //if (targetMask & TARGET_FLAG_UNIT_MASK)
					{
						target = _caster;
					}
				}

				if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.ItemMask))
				{
					var itemTarget = Targets.ItemTarget;

					if (itemTarget != null)
						AddItemTarget(itemTarget, (uint)(1 << spellEffectInfo.EffectIndex));

					return;
				}

				if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.GameobjectMask))
					target = Targets.GOTarget;

				break;
			// add self to the target map
			case SpellEffectImplicitTargetTypes.Caster:
				if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.UnitMask))
					target = _caster;

				break;
			default:
				break;
		}

		CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

		if (target != null)
		{
			if (target.IsUnit)
				AddUnitTarget(target.AsUnit, 1u << spellEffectInfo.EffectIndex, false);
			else if (target.IsGameObject)
				AddGOTarget(target.AsGameObject, 1u << spellEffectInfo.EffectIndex);
			else if (target.IsCorpse)
				AddCorpseTarget(target.AsCorpse, 1u << spellEffectInfo.EffectIndex);
		}
	}

	void SearchTargets(IGridNotifier notifier, GridMapTypeMask containerMask, WorldObject referer, Position pos, float radius)
	{
		if (containerMask == 0)
			return;

		var searchInWorld = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.Player | GridMapTypeMask.Corpse | GridMapTypeMask.GameObject);

		if (searchInWorld)
		{
			var x = pos.X;
			var y = pos.Y;

			var p = GridDefines.ComputeCellCoord(x, y);
			Cell cell = new(p);
			cell.SetNoCreate();

			var map = referer.GetMap();

			if (searchInWorld)
				Cell.VisitGrid(x, y, map, notifier, radius);
		}
	}

	WorldObject SearchNearbyTarget(float range, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
	{
		var containerTypeMask = GetSearcherTypeMask(objectType, condList);

		if (containerTypeMask == 0)
			return null;

		var check = new WorldObjectSpellNearbyTargetCheck(range, _caster, SpellInfo, selectionType, condList, objectType);
		var searcher = new WorldObjectLastSearcher(_caster, check, containerTypeMask);
		SearchTargets(searcher, containerTypeMask, _caster, _caster.Location, range);

		return searcher.GetTarget();
	}

	void SearchAreaTargets(List<WorldObject> targets, float range, Position position, WorldObject referer, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
	{
		var containerTypeMask = GetSearcherTypeMask(objectType, condList);

		if (containerTypeMask == 0)
			return;

		var extraSearchRadius = range > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
		var check = new WorldObjectSpellAreaTargetCheck(range, position, _caster, referer, SpellInfo, selectionType, condList, objectType);
		var searcher = new WorldObjectListSearcher(_caster, targets, check, containerTypeMask);
		SearchTargets(searcher, containerTypeMask, _caster, position, range + extraSearchRadius);
	}

	void SearchChainTargets(List<WorldObject> targets, uint chainTargets, WorldObject target, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectType, SpellEffectInfo spellEffectInfo, bool isChainHeal)
	{
		// max dist for jump target selection
		var jumpRadius = 0.0f;

		switch (SpellInfo.DmgClass)
		{
			case SpellDmgClass.Ranged:
				// 7.5y for multi shot
				jumpRadius = 7.5f;

				break;
			case SpellDmgClass.Melee:
				// 5y for swipe, cleave and similar
				jumpRadius = 5.0f;

				break;
			case SpellDmgClass.None:
			case SpellDmgClass.Magic:
				// 12.5y for chain heal spell since 3.2 patch
				if (isChainHeal)
					jumpRadius = 12.5f;
				// 10y as default for magic chain spells
				else
					jumpRadius = 10.0f;

				break;
		}

		var modOwner = _caster.GetSpellModOwner();

		if (modOwner)
			modOwner.ApplySpellMod(SpellInfo, SpellModOp.ChainJumpDistance, ref jumpRadius, this);

		float searchRadius;

		if (SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster))
			searchRadius = GetMinMaxRange(false).maxRange;
		else if (spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
			searchRadius = jumpRadius;
		else
			searchRadius = jumpRadius * chainTargets;

		var chainSource = SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? _caster : target;
		List<WorldObject> tempTargets = new();
		SearchAreaTargets(tempTargets, searchRadius, chainSource.Location, _caster, objectType, selectType, spellEffectInfo.ImplicitTargetConditions);
		tempTargets.Remove(target);

		// remove targets which are always invalid for chain spells
		// for some spells allow only chain targets in front of caster (swipe for example)
		if (SpellInfo.HasAttribute(SpellAttr5.MeleeChainTargeting))
			tempTargets.RemoveAll(obj => !_caster.Location.HasInArc(MathF.PI, obj.Location));

		while (chainTargets != 0)
		{
			// try to get unit for next chain jump
			WorldObject found = null;

			// get unit with highest hp deficit in dist
			if (isChainHeal)
			{
				uint maxHPDeficit = 0;

				foreach (var obj in tempTargets)
				{
					var unitTarget = obj.AsUnit;

					if (unitTarget != null)
					{
						var deficit = (uint)(unitTarget.GetMaxHealth() - unitTarget.GetHealth());

						if ((deficit > maxHPDeficit || found == null) && chainSource.IsWithinDist(unitTarget, jumpRadius) && chainSource.IsWithinLOSInMap(unitTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
						{
							found = obj;
							maxHPDeficit = deficit;
						}
					}
				}
			}
			// get closest object
			else
			{
				foreach (var obj in tempTargets)
					if (found == null)
					{
						if (chainSource.IsWithinDist(obj, jumpRadius) && chainSource.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
							found = obj;
					}
					else if (chainSource.GetDistanceOrder(obj, found) && chainSource.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
					{
						found = obj;
					}
			}

			// not found any valid target - chain ends
			if (found == null)
				break;

			if (!SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) && !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
				chainSource = found;

			targets.Add(found);
			tempTargets.Remove(found);
			--chainTargets;
		}
	}

	GameObject SearchSpellFocus()
	{
		var check = new GameObjectFocusCheck(_caster, SpellInfo.RequiresSpellFocus);
		var searcher = new GameObjectSearcher(_caster, check, GridType.All);
		SearchTargets(searcher, GridMapTypeMask.GameObject, _caster, _caster.Location, _caster.GetVisibilityRange());

		return searcher.GetTarget();
	}

	void PrepareDataForTriggerSystem()
	{
		//==========================================================================================
		// Now fill data for trigger system, need know:
		// Create base triggers flags for Attacker and Victim (m_procAttacker, m_procVictim and m_hitMask)
		//==========================================================================================

		ProcVictim = ProcAttacker = new ProcFlagsInit();

		// Get data for type of attack and fill base info for trigger
		switch (SpellInfo.DmgClass)
		{
			case SpellDmgClass.Melee:
				ProcAttacker = new ProcFlagsInit(ProcFlags.DealMeleeAbility);

				if (AttackType == WeaponAttackType.OffAttack)
					ProcAttacker.Or(ProcFlags.OffHandWeaponSwing);
				else
					ProcAttacker.Or(ProcFlags.MainHandWeaponSwing);

				ProcVictim = new ProcFlagsInit(ProcFlags.TakeMeleeAbility);

				break;
			case SpellDmgClass.Ranged:
				// Auto attack
				if (SpellInfo.HasAttribute(SpellAttr2.AutoRepeat))
				{
					ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
					ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
				}
				else // Ranged spell attack
				{
					ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAbility);
					ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAbility);
				}

				break;
			default:
				if (SpellInfo.EquippedItemClass == ItemClass.Weapon &&
					Convert.ToBoolean(SpellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassWeapon.Wand)) &&
					SpellInfo.HasAttribute(SpellAttr2.AutoRepeat)) // Wands auto attack
				{
					ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
					ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
				}

				break;
			// For other spells trigger procflags are set in Spell::TargetInfo::DoDamageAndTriggers
			// Because spell positivity is dependant on target
		}
	}

	void AddUnitTarget(Unit target, uint effectMask, bool checkIfValid = true, bool Implicit = true, Position losPosition = null)
	{
		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (!spellEffectInfo.IsEffect() || !CheckEffectTarget(target, spellEffectInfo, losPosition))
				effectMask &= ~(1u << spellEffectInfo.EffectIndex);

		// no effects left
		if (effectMask == 0)
			return;

		if (checkIfValid)
			if (SpellInfo.CheckTarget(_caster, target, Implicit) != SpellCastResult.SpellCastOk) // skip stealth checks for AOE
				return;

		// Check for effect immune skip if immuned
		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (target.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, _caster))
				effectMask &= ~(1u << spellEffectInfo.EffectIndex);

		var targetGUID = target.GUID;

		// Lookup target in already in list
		var index = UniqueTargetInfo.FindIndex(target => target.TargetGuid == targetGUID);

		if (index != -1) // Found in list
		{
			// Immune effects removed from mask
			UniqueTargetInfo[index].EffectMask |= effectMask;

			return;
		}

		// This is new target calculate data for him

		// Get spell hit result on target
		TargetInfo targetInfo = new();
		targetInfo.TargetGuid = targetGUID; // Store target GUID
		targetInfo.EffectMask = effectMask; // Store all effects not immune
		targetInfo.IsAlive = target.IsAlive;

		// Calculate hit result
		var caster = _originalCaster ? _originalCaster : _caster;
		targetInfo.MissCondition = caster.SpellHitResult(target, SpellInfo, _canReflect && !(IsPositive() && _caster.IsFriendlyTo(target)));

		// Spell have speed - need calculate incoming time
		// Incoming time is zero for self casts. At least I think so.
		if (_caster != target)
		{
			var hitDelay = SpellInfo.LaunchDelay;
			var missileSource = _caster;

			if (SpellInfo.HasAttribute(SpellAttr4.BouncyChainMissiles))
			{
				var previousTargetInfo = UniqueTargetInfo.FindLast(target => (target.EffectMask & effectMask) != 0);

				if (previousTargetInfo != null)
				{
					hitDelay = 0.0f; // this is not the first target in chain, LaunchDelay was already included

					var previousTarget = Global.ObjAccessor.GetWorldObject(_caster, previousTargetInfo.TargetGuid);

					if (previousTarget != null)
						missileSource = previousTarget;

					targetInfo.TimeDelay += previousTargetInfo.TimeDelay;
				}
			}

			if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
			{
				hitDelay += SpellInfo.Speed;
			}
			else if (SpellInfo.Speed > 0.0f)
			{
				// calculate spell incoming interval
				/// @todo this is a hack
				var dist = Math.Max(missileSource.GetDistance(target.Location.X, target.Location.Y, target.Location.Z), 5.0f);
				hitDelay += dist / SpellInfo.Speed;
			}

			targetInfo.TimeDelay += (ulong)Math.Floor(hitDelay * 1000.0f);
		}
		else
		{
			targetInfo.TimeDelay = 0L;
		}

		// If target reflect spell back to caster
		if (targetInfo.MissCondition == SpellMissInfo.Reflect)
		{
			// Calculate reflected spell result on caster (shouldn't be able to reflect gameobject spells)
			var unitCaster = _caster.AsUnit;
			targetInfo.ReflectResult = unitCaster.SpellHitResult(unitCaster, SpellInfo, false); // can't reflect twice

			// Proc spell reflect aura when missile hits the original target
			target.Events.AddEvent(new ProcReflectDelayed(target, _originalCasterGuid), target.Events.CalculateTime(TimeSpan.FromMilliseconds(targetInfo.TimeDelay)));

			// Increase time interval for reflected spells by 1.5
			targetInfo.TimeDelay += targetInfo.TimeDelay >> 1;
		}
		else
		{
			targetInfo.ReflectResult = SpellMissInfo.None;
		}

		// Calculate minimum incoming time
		if (targetInfo.TimeDelay != 0 && (_delayMoment == 0 || _delayMoment > targetInfo.TimeDelay))
			_delayMoment = targetInfo.TimeDelay;

		// Add target to list
		UniqueTargetInfo.Add(targetInfo);
		UniqueTargetInfoOrgi.Add(targetInfo);
	}

	void AddGOTarget(GameObject go, uint effectMask)
	{
		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (!spellEffectInfo.IsEffect() || !CheckEffectTarget(go, spellEffectInfo))
				effectMask &= ~(1u << spellEffectInfo.EffectIndex);

		// no effects left
		if (effectMask == 0)
			return;

		var targetGUID = go.GUID;

		// Lookup target in already in list
		var index = _uniqueGoTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);

		if (index != -1) // Found in list
		{
			// Add only effect mask
			_uniqueGoTargetInfo[index].EffectMask |= effectMask;

			return;
		}

		// This is new target calculate data for him
		GOTargetInfo target = new();
		target.TargetGUID = targetGUID;
		target.EffectMask = effectMask;

		// Spell have speed - need calculate incoming time
		if (_caster != go)
		{
			var hitDelay = SpellInfo.LaunchDelay;

			if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
			{
				hitDelay += SpellInfo.Speed;
			}
			else if (SpellInfo.Speed > 0.0f)
			{
				// calculate spell incoming interval
				var dist = Math.Max(_caster.GetDistance(go.Location.X, go.Location.Y, go.Location.Z), 5.0f);
				hitDelay += dist / SpellInfo.Speed;
			}

			target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
		}
		else
		{
			target.TimeDelay = 0UL;
		}

		// Calculate minimum incoming time
		if (target.TimeDelay != 0 && (_delayMoment == 0 || _delayMoment > target.TimeDelay))
			_delayMoment = target.TimeDelay;

		// Add target to list
		_uniqueGoTargetInfo.Add(target);
	}

	void AddItemTarget(Item item, uint effectMask)
	{
		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (!spellEffectInfo.IsEffect() || !CheckEffectTarget(item, spellEffectInfo))
				effectMask &= ~(1u << spellEffectInfo.EffectIndex);

		// no effects left
		if (effectMask == 0)
			return;

		// Lookup target in already in list
		var index = _uniqueItemInfo.FindIndex(target => target.TargetItem == item);

		if (index != -1) // Found in list
		{
			// Add only effect mask
			_uniqueItemInfo[index].EffectMask |= effectMask;

			return;
		}

		// This is new target add data

		ItemTargetInfo target = new();
		target.TargetItem = item;
		target.EffectMask = effectMask;

		_uniqueItemInfo.Add(target);
	}

	void AddCorpseTarget(Corpse corpse, uint effectMask)
	{
		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (!spellEffectInfo.IsEffect())
				effectMask &= ~(1u << spellEffectInfo.EffectIndex);

		// no effects left
		if (effectMask == 0)
			return;

		var targetGUID = corpse.GUID;

		// Lookup target in already in list
		var corpseTargetInfo = _uniqueCorpseTargetInfo.Find(target => { return target.TargetGuid == targetGUID; });

		if (corpseTargetInfo != null) // Found in list
		{
			// Add only effect mask
			corpseTargetInfo.EffectMask |= effectMask;

			return;
		}

		// This is new target calculate data for him
		CorpseTargetInfo target = new();
		target.TargetGuid = targetGUID;
		target.EffectMask = effectMask;

		// Spell have speed - need calculate incoming time
		if (_caster != corpse)
		{
			var hitDelay = SpellInfo.LaunchDelay;

			if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
			{
				hitDelay += SpellInfo.Speed;
			}
			else if (SpellInfo.Speed > 0.0f)
			{
				// calculate spell incoming interval
				var dist = Math.Max(_caster.GetDistance(corpse.Location.X, corpse.Location.Y, corpse.Location.Z), 5.0f);
				hitDelay += dist / SpellInfo.Speed;
			}

			target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
		}
		else
		{
			target.TimeDelay = 0;
		}

		// Calculate minimum incoming time
		if (target.TimeDelay != 0 && (_delayMoment == 0 || _delayMoment > target.TimeDelay))
			_delayMoment = target.TimeDelay;

		// Add target to list
		_uniqueCorpseTargetInfo.Add(target);
	}

	void AddDestTarget(SpellDestination dest, int effIndex)
	{
		_destTargets[effIndex] = dest;
	}

	bool UpdateChanneledTargetList()
	{
		// Not need check return true
		if (_channelTargetEffectMask == 0)
			return true;

		var channelTargetEffectMask = _channelTargetEffectMask;
		uint channelAuraMask = 0;

		foreach (var spellEffectInfo in SpellInfo.Effects)
			if (spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
				channelAuraMask |= 1u << spellEffectInfo.EffectIndex;

		channelAuraMask &= channelTargetEffectMask;

		float range = 0;

		if (channelAuraMask != 0)
		{
			range = SpellInfo.GetMaxRange(IsPositive());
			var modOwner = _caster.GetSpellModOwner();

			if (modOwner != null)
				modOwner.ApplySpellMod(SpellInfo, SpellModOp.Range, ref range, this);

			// add little tolerance level
			range += Math.Min(3.0f, range * 0.1f); // 10% but no more than 3.0f
		}

		foreach (var targetInfo in UniqueTargetInfo)
			if (targetInfo.MissCondition == SpellMissInfo.None && Convert.ToBoolean(channelTargetEffectMask & targetInfo.EffectMask))
			{
				var unit = _caster.GUID == targetInfo.TargetGuid ? _caster.AsUnit : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGuid);

				if (unit == null)
				{
					var unitCaster = _caster.AsUnit;

					if (unitCaster != null)
						unitCaster.RemoveChannelObject(targetInfo.TargetGuid);

					continue;
				}

				if (IsValidDeadOrAliveTarget(unit))
				{
					if (Convert.ToBoolean(channelAuraMask & targetInfo.EffectMask))
					{
						var aurApp = unit.GetAuraApplication(SpellInfo.Id, _originalCasterGuid);

						if (aurApp != null)
						{
							if (_caster != unit && !_caster.IsWithinDistInMap(unit, range))
							{
								targetInfo.EffectMask &= ~aurApp.EffectMask;
								unit.RemoveAura(aurApp);
								var unitCaster = _caster.AsUnit;

								if (unitCaster != null)
									unitCaster.RemoveChannelObject(targetInfo.TargetGuid);

								continue;
							}
						}
						else // aura is dispelled
						{
							var unitCaster = _caster.AsUnit;

							if (unitCaster != null)
								unitCaster.RemoveChannelObject(targetInfo.TargetGuid);

							continue;
						}
					}

					channelTargetEffectMask &= ~targetInfo.EffectMask; // remove from need alive mask effect that have alive target
				}
			}

		// is all effects from m_needAliveTargetMask have alive targets
		return channelTargetEffectMask == 0;
	}

	void _cast(bool skipCheck = false)
	{
		if (!UpdatePointers())
		{
			// cancel the spell if UpdatePointers() returned false, something wrong happened there
			Cancel();

			return;
		}

		// cancel at lost explicit target during cast
		if (!Targets.ObjectTargetGUID.IsEmpty && Targets.ObjectTarget == null)
		{
			Cancel();

			return;
		}

		var playerCaster = _caster.AsPlayer;

		if (playerCaster != null)
		{
			// now that we've done the basic check, now run the scripts
			// should be done before the spell is actually executed
			Global.ScriptMgr.ForEach<IPlayerOnSpellCast>(playerCaster.Class, p => p.OnSpellCast(playerCaster, this, skipCheck));

			// As of 3.0.2 pets begin attacking their owner's target immediately
			// Let any pets know we've attacked something. Check DmgClass for harmful spells only
			// This prevents spells such as Hunter's Mark from triggering pet attack
			if (SpellInfo.DmgClass != SpellDmgClass.None)
			{
				var target = Targets.UnitTarget;

				if (target != null)
					foreach (var controlled in playerCaster.Controlled)
					{
						var cControlled = controlled.AsCreature;

						if (cControlled != null)
						{
							var controlledAI = cControlled.GetAI();

							if (controlledAI != null)
								controlledAI.OwnerAttacked(target);
						}
					}
			}
		}

		SetExecutedCurrently(true);

		// Should this be done for original caster?
		var modOwner = _caster.GetSpellModOwner();

		if (modOwner != null)
			// Set spell which will drop charges for triggered cast spells
			// if not successfully casted, will be remove in finish(false)
			modOwner.SetSpellModTakingSpell(this, true);

		CallScriptBeforeCastHandlers();

		// skip check if done already (for instant cast spells for example)
		if (!skipCheck)
		{
			void cleanupSpell(SpellCastResult result, int? param1 = null, int? param2 = null)
			{
				SendCastResult(result, param1, param2);
				SendInterrupted(0);

				if (modOwner)
					modOwner.SetSpellModTakingSpell(this, false);

				Finish(result);
				SetExecutedCurrently(false);
			}

			int param1 = 0, param2 = 0;
			var castResult = CheckCast(false, ref param1, ref param2);

			if (castResult != SpellCastResult.SpellCastOk)
			{
				cleanupSpell(castResult, param1, param2);

				return;
			}

			// additional check after cast bar completes (must not be in CheckCast)
			// if trade not complete then remember it in trade data
			if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.TradeItem))
				if (modOwner)
				{
					var my_trade = modOwner.GetTradeData();

					if (my_trade != null)
						if (!my_trade.IsInAcceptProcess())
						{
							// Spell will be casted at completing the trade. Silently ignore at this place
							my_trade.SetSpell(SpellInfo.Id, CastItem);
							cleanupSpell(SpellCastResult.DontReport);

							return;
						}
				}

			// check diminishing returns (again, only after finish cast bar, tested on retail)
			var target = Targets.UnitTarget;

			if (target != null)
			{
				uint aura_effmask = 0;

				foreach (var spellEffectInfo in SpellInfo.Effects)
					if (spellEffectInfo.IsUnitOwnedAuraEffect())
						aura_effmask |= 1u << spellEffectInfo.EffectIndex;

				if (aura_effmask != 0)
					if (SpellInfo.DiminishingReturnsGroupForSpell != 0)
					{
						var type = SpellInfo.DiminishingReturnsGroupType;

						if (type == DiminishingReturnsType.All || (type == DiminishingReturnsType.Player && target.IsAffectedByDiminishingReturns))
						{
							var caster1 = _originalCaster ? _originalCaster : _caster.AsUnit;

							if (caster1 != null)
								if (target.HasStrongerAuraWithDR(SpellInfo, caster1))
								{
									cleanupSpell(SpellCastResult.AuraBounced);

									return;
								}
						}
					}
			}
		}

		// The spell focusing is making sure that we have a valid cast target guid when we need it so only check for a guid value here.
		var creatureCaster = _caster.AsCreature;

		if (creatureCaster != null)
			if (!creatureCaster.Target.IsEmpty && !creatureCaster.HasUnitFlag(UnitFlags.Possessed))
			{
				WorldObject target = Global.ObjAccessor.GetUnit(creatureCaster, creatureCaster.Target);

				if (target != null)
					creatureCaster.SetInFront(target);
			}

		SelectSpellTargets();

		// Spell may be finished after target map check
		if (_spellState == SpellState.Finished)
		{
			SendInterrupted(0);

			if (_caster.IsTypeId(TypeId.Player))
				_caster.				AsPlayer.SetSpellModTakingSpell(this, false);

			Finish(SpellCastResult.Interrupted);
			SetExecutedCurrently(false);

			return;
		}

		var unitCaster = _caster.AsUnit;

		if (unitCaster != null)
			if (SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
			{
				var pet = ObjectAccessor.GetCreature(_caster, unitCaster.PetGUID);

				if (pet != null)
					pet.DespawnOrUnsummon();
			}

		PrepareTriggersExecutedOnHit();

		CallScriptOnCastHandlers();

		// traded items have trade slot instead of guid in m_itemTargetGUID
		// set to real guid to be sent later to the client
		Targets.UpdateTradeSlotItem();

		var player = _caster.AsPlayer;

		if (player != null)
		{
			if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem) && CastItem != null)
			{
				player.StartCriteriaTimer(CriteriaStartEvent.UseItem, CastItem.Entry);
				player.UpdateCriteria(CriteriaType.UseItem, CastItem.Entry);
			}

			player.UpdateCriteria(CriteriaType.CastSpell, SpellInfo.Id);
		}

		var targetItem = Targets.ItemTarget;

		if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
		{
			// Powers have to be taken before SendSpellGo
			TakePower();
			TakeReagents(); // we must remove reagents before HandleEffects to allow place crafted item in same slot
		}
		else if (targetItem != null)
		{
			// Not own traded item (in trader trade slot) req. reagents including triggered spell case
			if (targetItem.OwnerGUID != _caster.GUID)
				TakeReagents();
		}

		// CAST SPELL
		if (!SpellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
			SendSpellCooldown();

		if (SpellInfo.LaunchDelay == 0)
		{
			HandleLaunchPhase();
			_launchHandled = true;
		}

		// we must send smsg_spell_go packet before m_castItem delete in TakeCastItem()...
		SendSpellGo();

		if (!SpellInfo.IsChanneled)
			if (creatureCaster != null)
				creatureCaster.ReleaseSpellFocus(this);

		// Okay, everything is prepared. Now we need to distinguish between immediate and evented delayed spells
		if ((SpellInfo.HasHitDelay && !SpellInfo.IsChanneled) || SpellInfo.HasAttribute(SpellAttr4.NoHarmfulThreat))
		{
			// Remove used for cast item if need (it can be already NULL after TakeReagents call
			// in case delayed spell remove item at cast delay start
			TakeCastItem();

			// Okay, maps created, now prepare flags
			_immediateHandled = false;
			_spellState = SpellState.Delayed;
			DelayStart = 0;

			unitCaster = _caster.AsUnit;

			if (unitCaster != null)
				if (unitCaster.HasUnitState(UnitState.Casting) && !unitCaster.IsNonMeleeSpellCast(false, false, true))
					unitCaster.ClearUnitState(UnitState.Casting);
		}
		else
		{
			// Immediate spell, no big deal
			HandleImmediate();
		}

		CallScriptAfterCastHandlers();

		var spell_triggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Cast, SpellInfo.Id);

		if (spell_triggered != null)
			foreach (var spellId in spell_triggered)
				if (spellId < 0)
				{
					unitCaster = _caster.AsUnit;

					if (unitCaster != null)
						unitCaster.RemoveAura((uint)-spellId);
				}
				else
				{
					_caster.CastSpell(Targets.UnitTarget ?? _caster, (uint)spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(this));
				}

		if (modOwner != null)
		{
			modOwner.SetSpellModTakingSpell(this, false);

			//Clear spell cooldowns after every spell is cast if .cheat cooldown is enabled.
			if (_originalCaster != null && modOwner.GetCommandStatus(PlayerCommandStates.Cooldown))
			{
				_originalCaster.GetSpellHistory().ResetCooldown(SpellInfo.Id, true);
				_originalCaster.GetSpellHistory().RestoreCharge(SpellInfo.ChargeCategoryId);
			}
		}

		SetExecutedCurrently(false);

		if (!_originalCaster)
			return;

		// Handle procs on cast
		var procAttacker = ProcAttacker;

		if (!procAttacker)
		{
			if (SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
			{
				if (IsPositive())
					procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
				else
					procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
			}
			else if (SpellInfo.HasAttribute(SpellAttr0.IsAbility))
			{
				if (IsPositive())
					procAttacker.Or(ProcFlags.DealHelpfulAbility);
				else
					procAttacker.Or(ProcFlags.DealHarmfulSpell);
			}
			else
			{
				if (IsPositive())
					procAttacker.Or(ProcFlags.DealHelpfulSpell);
				else
					procAttacker.Or(ProcFlags.DealHarmfulSpell);
			}
		}

		procAttacker.Or(ProcFlags2.CastSuccessful);

		var hitMask = HitMask;

		if (!hitMask.HasAnyFlag(ProcFlagsHit.Critical))
			hitMask |= ProcFlagsHit.Normal;

		if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) && !SpellInfo.HasAttribute(SpellAttr2.NotAnAction))
			_originalCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ActionDelayed, SpellInfo);

		if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
			Unit.ProcSkillsAndAuras(_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Cast, hitMask, this, null, null);

		// Call CreatureAI hook OnSpellCast
		var caster = _originalCaster.AsCreature;

		if (caster)
			if (caster.IsAIEnabled)
				caster.GetAI().OnSpellCast(SpellInfo);
	}

	void DoProcessTargetContainer<T>(List<T> targetContainer) where T : TargetInfoBase
	{
		foreach (TargetInfoBase target in targetContainer)
			target.PreprocessTarget(this);

		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			foreach (TargetInfoBase target in targetContainer)
				if ((target.EffectMask & (1 << spellEffectInfo.EffectIndex)) != 0)
					target.DoTargetSpellHit(this, spellEffectInfo);
		}

		foreach (TargetInfoBase target in targetContainer)
			target.DoDamageAndTriggers(this);
	}

	void HandleImmediate()
	{
		// start channeling if applicable
		if (SpellInfo.IsChanneled)
		{
			var duration = SpellInfo.Duration;

			if (duration > 0 || SpellValue.Duration.HasValue)
			{
				if (!SpellValue.Duration.HasValue)
				{
					// First mod_duration then haste - see Missile Barrage
					// Apply duration mod
					var modOwner = _caster.GetSpellModOwner();

					if (modOwner != null)
						modOwner.ApplySpellMod(SpellInfo, SpellModOp.Duration, ref duration);

					duration = (int)(duration * SpellValue.DurationMul);

					// Apply haste mods
					_caster.ModSpellDurationTime(SpellInfo, ref duration, this);
				}
				else
				{
					duration = SpellValue.Duration.Value;
				}

				_channeledDuration = duration;
				SendChannelStart((uint)duration);
			}
			else if (duration == -1)
			{
				SendChannelStart(unchecked((uint)duration));
			}

			if (duration != 0)
			{
				_spellState = SpellState.Casting;
				// GameObjects shouldn't cast channeled spells
				_caster.				// GameObjects shouldn't cast channeled spells
				AsUnit?.AddInterruptMask(SpellInfo.ChannelInterruptFlags, SpellInfo.ChannelInterruptFlags2);
			}
		}

		PrepareTargetProcessing();

		// process immediate effects (items, ground, etc.) also initialize some variables
		_handle_immediate_phase();

		// consider spell hit for some spells without target, so they may proc on finish phase correctly
		if (UniqueTargetInfo.Empty())
			HitMask = ProcFlagsHit.Normal;
		else
			DoProcessTargetContainer(UniqueTargetInfo);

		DoProcessTargetContainer(_uniqueGoTargetInfo);

		DoProcessTargetContainer(_uniqueCorpseTargetInfo);
		CallScriptOnHitHandlers();

		FinishTargetProcessing();

		// spell is finished, perform some last features of the spell here
		_handle_finish_phase();

		// Remove used for cast item if need (it can be already NULL after TakeReagents call
		TakeCastItem();

		if (_spellState != SpellState.Casting)
			Finish(); // successfully finish spell cast (not last in case autorepeat or channel spell)
	}

	void _handle_immediate_phase()
	{
		// handle some immediate features of the spell here
		HandleThreatSpells();

		// handle effects with SPELL_EFFECT_HANDLE_HIT mode
		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			// don't do anything for empty effect
			if (!spellEffectInfo.IsEffect())
				continue;

			// call effect handlers to handle destination hit
			HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Hit);
		}

		// process items
		DoProcessTargetContainer(_uniqueItemInfo);
	}

	void _handle_finish_phase()
	{
		var unitCaster = _caster.AsUnit;

		if (unitCaster != null)
		{
			// Take for real after all targets are processed
			if (NeedComboPoints)
				unitCaster.ClearComboPoints();

			// Real add combo points from effects
			if (ComboPointGain != 0)
				unitCaster.AddComboPoints(ComboPointGain);

			if (SpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
				unitCaster.SetLastExtraAttackSpell(SpellInfo.Id);
		}

		// Handle procs on finish
		if (!_originalCaster)
			return;

		var procAttacker = ProcAttacker;

		if (!procAttacker)
		{
			if (SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
			{
				if (IsPositive())
					procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
				else
					procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
			}
			else if (SpellInfo.HasAttribute(SpellAttr0.IsAbility))
			{
				if (IsPositive())
					procAttacker.Or(ProcFlags.DealHelpfulAbility);
				else
					procAttacker.Or(ProcFlags.DealHarmfulAbility);
			}
			else
			{
				if (IsPositive())
					procAttacker.Or(ProcFlags.DealHelpfulSpell);
				else
					procAttacker.Or(ProcFlags.DealHarmfulSpell);
			}
		}

		if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
			Unit.ProcSkillsAndAuras(_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Finish, HitMask, this, null, null);
	}

	void SendSpellCooldown()
	{
		if (!_caster.IsUnit)
			return;

		if (CastItem)
			_caster.			AsUnit.GetSpellHistory().HandleCooldowns(SpellInfo, CastItem, this);
		else
			_caster.			AsUnit.GetSpellHistory().HandleCooldowns(SpellInfo, CastItemEntry, this);

		if (IsAutoRepeat)
			_caster.			AsUnit.ResetAttackTimer(WeaponAttackType.RangedAttack);
	}

	private void UpdateEmpoweredSpell(uint difftime)
	{
		if (GetPlayerIfIsEmpowered(out var p))
		{
			if (_empoweredSpellStage == 0 && _empoweredSpellDelta == 0 && SpellInfo.EmpowerStages.TryGetValue(_empoweredSpellStage, out var stageinfo)) // send stage 0
			{
				ForEachSpellScript<ISpellOnEpowerSpellStageChange>(s => s.EmpowerSpellStageChange(null, stageinfo));
				var stageZero = new SpellEmpowerSetStage();
				stageZero.Stage = 0;
				stageZero.Caster = p.GUID;
				stageZero.CastID = CastId;
				p.SendPacket(stageZero);
			}

			_empoweredSpellDelta += difftime;

			if (SpellInfo.EmpowerStages.TryGetValue(_empoweredSpellStage, out stageinfo) && _empoweredSpellDelta >= stageinfo.DurationMs)
			{
				var nextStageId = _empoweredSpellStage;
				nextStageId++;

				if (SpellInfo.EmpowerStages.TryGetValue(nextStageId, out var nextStage))
				{
					_empoweredSpellStage = nextStageId;
					_empoweredSpellDelta = 0;
					var stageUpdate = new SpellEmpowerSetStage();
					stageUpdate.Stage = 0;
					stageUpdate.Caster = p.GUID;
					stageUpdate.CastID = CastId;
					p.SendPacket(stageUpdate);
					ForEachSpellScript<ISpellOnEpowerSpellStageChange>(s => s.EmpowerSpellStageChange(stageinfo, nextStage));
				}
			}
		}
	}

	static void FillSpellCastFailedArgs<T>(T packet, ObjectGuid castId, SpellInfo spellInfo, SpellCastResult result, SpellCustomErrors customError, int? param1, int? param2, Player caster) where T : CastFailedBase
	{
		packet.CastID = castId;
		packet.SpellID = (int)spellInfo.Id;
		packet.Reason = result;

		switch (result)
		{
			case SpellCastResult.NotReady:
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					packet.FailedArg1 = 0; // unknown (value 1 update cooldowns on client flag)

				break;
			case SpellCastResult.RequiresSpellFocus:
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					packet.FailedArg1 = (int)spellInfo.RequiresSpellFocus; // SpellFocusObject.dbc id

				break;
			case SpellCastResult.RequiresArea: // AreaTable.dbc id
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					// hardcode areas limitation case
					switch (spellInfo.Id)
					{
						case 41617: // Cenarion Mana Salve
						case 41619: // Cenarion Healing Salve
							packet.FailedArg1 = 3905;

							break;
						case 41618: // Bottled Nethergon Energy
						case 41620: // Bottled Nethergon Vapor
							packet.FailedArg1 = 3842;

							break;
						case 45373: // Bloodberry Elixir
							packet.FailedArg1 = 4075;

							break;
						default: // default case (don't must be)
							packet.FailedArg1 = 0;

							break;
					}

				break;
			case SpellCastResult.Totems:
				if (param1.HasValue)
				{
					packet.FailedArg1 = (int)param1;

					if (param2.HasValue)
						packet.FailedArg2 = (int)param2;
				}
				else
				{
					if (spellInfo.Totem[0] != 0)
						packet.FailedArg1 = (int)spellInfo.Totem[0];

					if (spellInfo.Totem[1] != 0)
						packet.FailedArg2 = (int)spellInfo.Totem[1];
				}

				break;
			case SpellCastResult.TotemCategory:
				if (param1.HasValue)
				{
					packet.FailedArg1 = (int)param1;

					if (param2.HasValue)
						packet.FailedArg2 = (int)param2;
				}
				else
				{
					if (spellInfo.TotemCategory[0] != 0)
						packet.FailedArg1 = (int)spellInfo.TotemCategory[0];

					if (spellInfo.TotemCategory[1] != 0)
						packet.FailedArg2 = (int)spellInfo.TotemCategory[1];
				}

				break;
			case SpellCastResult.EquippedItemClass:
			case SpellCastResult.EquippedItemClassMainhand:
			case SpellCastResult.EquippedItemClassOffhand:
				if (param1.HasValue && param2.HasValue)
				{
					packet.FailedArg1 = (int)param1;
					packet.FailedArg2 = (int)param2;
				}
				else
				{
					packet.FailedArg1 = (int)spellInfo.EquippedItemClass;
					packet.FailedArg2 = spellInfo.EquippedItemSubClassMask;
				}

				break;
			case SpellCastResult.TooManyOfItem:
			{
				if (param1.HasValue)
				{
					packet.FailedArg1 = (int)param1;
				}
				else
				{
					uint item = 0;

					foreach (var spellEffectInfo in spellInfo.Effects)
						if (spellEffectInfo.ItemType != 0)
							item = spellEffectInfo.ItemType;

					var proto = Global.ObjectMgr.GetItemTemplate(item);

					if (proto != null && proto.GetItemLimitCategory() != 0)
						packet.FailedArg1 = (int)proto.GetItemLimitCategory();
				}

				break;
			}
			case SpellCastResult.PreventedByMechanic:
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					packet.FailedArg1 = (int)spellInfo.GetAllEffectsMechanicMask(); // SpellMechanic.dbc id

				break;
			case SpellCastResult.NeedExoticAmmo:
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					packet.FailedArg1 = spellInfo.EquippedItemSubClassMask; // seems correct...

				break;
			case SpellCastResult.NeedMoreItems:
				if (param1.HasValue && param2.HasValue)
				{
					packet.FailedArg1 = (int)param1;
					packet.FailedArg2 = (int)param2;
				}
				else
				{
					packet.FailedArg1 = 0; // Item id
					packet.FailedArg2 = 0; // Item count?
				}

				break;
			case SpellCastResult.MinSkill:
				if (param1.HasValue && param2.HasValue)
				{
					packet.FailedArg1 = (int)param1;
					packet.FailedArg2 = (int)param2;
				}
				else
				{
					packet.FailedArg1 = 0; // SkillLine.dbc id
					packet.FailedArg2 = 0; // required skill value
				}

				break;
			case SpellCastResult.FishingTooLow:
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					packet.FailedArg1 = 0; // required fishing skill

				break;
			case SpellCastResult.CustomError:
				packet.FailedArg1 = (int)customError;

				break;
			case SpellCastResult.Silenced:
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					packet.FailedArg1 = 0; // Unknown

				break;
			case SpellCastResult.Reagents:
			{
				if (param1.HasValue)
					packet.FailedArg1 = (int)param1;
				else
					for (uint i = 0; i < SpellConst.MaxReagents; i++)
					{
						if (spellInfo.Reagent[i] <= 0)
							continue;

						var itemid = (uint)spellInfo.Reagent[i];
						var itemcount = spellInfo.ReagentCount[i];

						if (!caster.HasItemCount(itemid, itemcount))
						{
							packet.FailedArg1 = (int)itemid; // first missing item

							break;
						}
					}

				if (param2.HasValue)
					packet.FailedArg2 = (int)param2;
				else if (!param1.HasValue)
					foreach (var reagentsCurrency in spellInfo.ReagentsCurrency)
						if (!caster.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
						{
							packet.FailedArg1 = -1;
							packet.FailedArg2 = reagentsCurrency.CurrencyTypesID;

							break;
						}

				break;
			}
			case SpellCastResult.CantUntalent:
			{
				Cypher.Assert(param1.HasValue);
				packet.FailedArg1 = (int)param1;

				break;
			}
			// TODO: SPELL_FAILED_NOT_STANDING
			default:
				break;
		}
	}

	void SendMountResult(MountResult result)
	{
		if (result == MountResult.Ok)
			return;

		if (!_caster.IsPlayer)
			return;

		var caster = _caster.AsPlayer;

		if (caster.IsLoading) // don't send mount results at loading time
			return;

		MountResultPacket packet = new();
		packet.Result = (uint)result;
		caster.SendPacket(packet);
	}

	void SendSpellStart()
	{
		if (!IsNeedSendToClient())
			return;

		var castFlags = SpellCastFlags.HasTrajectory;
		uint schoolImmunityMask = 0;
		ulong mechanicImmunityMask = 0;
		var unitCaster = _caster.AsUnit;

		if (unitCaster != null)
		{
			schoolImmunityMask = _timer != 0 ? unitCaster.GetSchoolImmunityMask() : 0;
			mechanicImmunityMask = _timer != 0 ? SpellInfo.GetMechanicImmunityMask(unitCaster) : 0;
		}

		if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
			castFlags |= SpellCastFlags.Immunity;

		if (((IsTriggered() && !SpellInfo.IsAutoRepeatRangedSpell) || TriggeredByAuraSpell != null) && !FromClient)
			castFlags |= SpellCastFlags.Pending;

		if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || SpellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) || SpellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
			castFlags |= SpellCastFlags.Projectile;

		if ((_caster.IsTypeId(TypeId.Player) || (_caster.IsTypeId(TypeId.Unit) && _caster.AsCreature.IsPet)) && _powerCosts.Any(cost => cost.Power != PowerType.Health))
			castFlags |= SpellCastFlags.PowerLeftSelf;

		if (HasPowerTypeCost(PowerType.Runes))
			castFlags |= SpellCastFlags.NoGCD; // not needed, but Blizzard sends it

		SpellStart packet = new();
		var castData = packet.Cast;

		if (CastItem)
			castData.CasterGUID = CastItem.GUID;
		else
			castData.CasterGUID = _caster.GUID;

		castData.CasterUnit = _caster.GUID;
		castData.CastID = CastId;
		castData.OriginalCastID = OriginalCastId;
		castData.SpellID = (int)SpellInfo.Id;
		castData.Visual = SpellVisual;
		castData.CastFlags = castFlags;
		castData.CastFlagsEx = CastFlagsEx;
		castData.CastTime = (uint)_casttime;

		Targets.Write(castData.Target);

		if (castFlags.HasAnyFlag(SpellCastFlags.PowerLeftSelf))
			foreach (var cost in _powerCosts)
			{
				SpellPowerData powerData;
				powerData.Type = cost.Power;
				powerData.Cost = _caster.AsUnit.GetPower(cost.Power);
				castData.RemainingPower.Add(powerData);
			}

		if (castFlags.HasAnyFlag(SpellCastFlags.RuneList)) // rune cooldowns list
		{
			castData.RemainingRunes = new RuneData();

			var runeData = castData.RemainingRunes;
			//TODO: There is a crash caused by a spell with CAST_FLAG_RUNE_LIST casted by a creature
			//The creature is the mover of a player, so HandleCastSpellOpcode uses it as the caster

			var player = _caster.AsPlayer;

			if (player)
			{
				runeData.Start = _runesState;            // runes state before
				runeData.Count = player.GetRunesState(); // runes state after

				for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
				{
					// float casts ensure the division is performed on floats as we need float result
					float baseCd = player.GetRuneBaseCooldown();
					runeData.Cooldowns.Add((byte)((baseCd - player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
				}
			}
			else
			{
				runeData.Start = 0;
				runeData.Count = 0;

				for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
					runeData.Cooldowns.Add(0);
			}
		}

		UpdateSpellCastDataAmmo(castData.Ammo);

		if (castFlags.HasAnyFlag(SpellCastFlags.Immunity))
		{
			castData.Immunities.School = schoolImmunityMask;
			castData.Immunities.Value = (uint)mechanicImmunityMask;
		}

		/** @todo implement heal prediction packet data
		if (castFlags & CAST_FLAG_HEAL_PREDICTION)
		{
			castData.Predict.BeconGUID = ??
			castData.Predict.Points = 0;
			castData.Predict.Type = 0;
		}**/

		_caster.SendMessageToSet(packet, true);
	}

	void SendSpellGo()
	{
		// not send invisible spell casting
		if (!IsNeedSendToClient())
			return;

		Log.outDebug(LogFilter.Spells, "Sending SMSG_SPELL_GO id={0}", SpellInfo.Id);

		var castFlags = SpellCastFlags.Unk9;

		// triggered spells with spell visual != 0
		if (((IsTriggered() && !SpellInfo.IsAutoRepeatRangedSpell) || TriggeredByAuraSpell != null) && !FromClient)
			castFlags |= SpellCastFlags.Pending;

		if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || SpellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) || SpellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
			castFlags |= SpellCastFlags.Projectile; // arrows/bullets visual

		if ((_caster.IsTypeId(TypeId.Player) || (_caster.IsTypeId(TypeId.Unit) && _caster.AsCreature.IsPet)) && _powerCosts.Any(cost => cost.Power != PowerType.Health))
			castFlags |= SpellCastFlags.PowerLeftSelf;

		if (_caster.IsTypeId(TypeId.Player) &&
			_caster.			AsPlayer.			Class == Class.Deathknight &&
			HasPowerTypeCost(PowerType.Runes) &&
			!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerAndReagentCost))
		{
			castFlags |= SpellCastFlags.NoGCD;    // same as in SMSG_SPELL_START
			castFlags |= SpellCastFlags.RuneList; // rune cooldowns list
		}

		if (Targets.HasTraj)
			castFlags |= SpellCastFlags.AdjustMissile;

		if (SpellInfo.StartRecoveryTime == 0)
			castFlags |= SpellCastFlags.NoGCD;

		SpellGo packet = new();
		var castData = packet.Cast;

		if (CastItem != null)
			castData.CasterGUID = CastItem.GUID;
		else
			castData.CasterGUID = _caster.GUID;

		castData.CasterUnit = _caster.GUID;
		castData.CastID = CastId;
		castData.OriginalCastID = OriginalCastId;
		castData.SpellID = (int)SpellInfo.Id;
		castData.Visual = SpellVisual;
		castData.CastFlags = castFlags;
		castData.CastFlagsEx = CastFlagsEx;
		castData.CastTime = Time.MSTime;

		castData.HitTargets = new List<ObjectGuid>();
		UpdateSpellCastDataTargets(castData);

		Targets.Write(castData.Target);

		if (Convert.ToBoolean(castFlags & SpellCastFlags.PowerLeftSelf))
		{
			castData.RemainingPower = new List<SpellPowerData>();

			foreach (var cost in _powerCosts)
			{
				SpellPowerData powerData;
				powerData.Type = cost.Power;
				powerData.Cost = _caster.AsUnit.GetPower(cost.Power);
				castData.RemainingPower.Add(powerData);
			}
		}

		if (Convert.ToBoolean(castFlags & SpellCastFlags.RuneList)) // rune cooldowns list
		{
			castData.RemainingRunes = new RuneData();
			var runeData = castData.RemainingRunes;

			var player = _caster.AsPlayer;
			runeData.Start = _runesState;            // runes state before
			runeData.Count = player.GetRunesState(); // runes state after

			for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
			{
				// float casts ensure the division is performed on floats as we need float result
				var baseCd = (float)player.GetRuneBaseCooldown();
				runeData.Cooldowns.Add((byte)((baseCd - (float)player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
			}
		}

		if (castFlags.HasFlag(SpellCastFlags.AdjustMissile))
		{
			castData.MissileTrajectory.TravelTime = (uint)_delayMoment;
			castData.MissileTrajectory.Pitch = Targets.Pitch;
		}

		packet.LogData.Initialize(this);

		_caster.SendCombatLogMessage(packet);

		if (GetPlayerIfIsEmpowered(out var p))
		{
			ForEachSpellScript<ISpellOnEpowerSpellStart>(s => s.EmpowerSpellStart());
			SpellEmpowerStart spellEmpowerSart = new();
			spellEmpowerSart.CastID = packet.Cast.CastID;
			spellEmpowerSart.Caster = packet.Cast.CasterGUID;
			spellEmpowerSart.Targets = UniqueTargetInfo.Select(t => t.TargetGuid).ToList();
			spellEmpowerSart.SpellID = SpellInfo.Id;
			spellEmpowerSart.Visual = packet.Cast.Visual;
			spellEmpowerSart.Duration = (uint)SpellInfo.Duration; //(uint)m_spellInfo.EmpowerStages.Sum(kvp => kvp.Value.DurationMs); these do add up to be the same.
			spellEmpowerSart.FirstStageDuration = spellEmpowerSart.StageDurations.FirstOrDefault().Value;
			spellEmpowerSart.FinalStageDuration = spellEmpowerSart.StageDurations.LastOrDefault().Value;
			spellEmpowerSart.StageDurations = SpellInfo.EmpowerStages.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DurationMs);

			var schoolImmunityMask = p.GetSchoolImmunityMask();
			var mechanicImmunityMask = p.GetMechanicImmunityMask();

			if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
			{
				SpellChannelStartInterruptImmunities interruptImmunities = new();
				interruptImmunities.SchoolImmunities = (int)schoolImmunityMask;
				interruptImmunities.Immunities = (int)mechanicImmunityMask;

				spellEmpowerSart.Immunities = interruptImmunities;
			}

			p.SendPacket(spellEmpowerSart);
		}
	}

	// Writes miss and hit targets for a SMSG_SPELL_GO packet
	void UpdateSpellCastDataTargets(SpellCastData data)
	{
		// This function also fill data for channeled spells:
		// m_needAliveTargetMask req for stop channelig if one target die
		foreach (var targetInfo in UniqueTargetInfo)
		{
			if (targetInfo.EffectMask == 0) // No effect apply - all immuned add state
				// possibly SPELL_MISS_IMMUNE2 for this??
				targetInfo.MissCondition = SpellMissInfo.Immune2;

			if (targetInfo.MissCondition == SpellMissInfo.None || (targetInfo.MissCondition == SpellMissInfo.Block && !SpellInfo.HasAttribute(SpellAttr3.CompletelyBlocked))) // Add only hits and partial blocked
			{
				data.HitTargets.Add(targetInfo.TargetGuid);
				data.HitStatus.Add(new SpellHitStatus(SpellMissInfo.None));

				_channelTargetEffectMask |= targetInfo.EffectMask;
			}
			else // misses
			{
				data.MissTargets.Add(targetInfo.TargetGuid);

				data.MissStatus.Add(new SpellMissStatus(targetInfo.MissCondition, targetInfo.ReflectResult));
			}
		}

		foreach (var targetInfo in _uniqueGoTargetInfo)
			data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

		foreach (var targetInfo in _uniqueCorpseTargetInfo)
			data.HitTargets.Add(targetInfo.TargetGuid); // Always hits

		// Reset m_needAliveTargetMask for non channeled spell
		if (!SpellInfo.IsChanneled)
			_channelTargetEffectMask = 0;
	}

	void UpdateSpellCastDataAmmo(SpellAmmo ammo)
	{
		InventoryType ammoInventoryType = 0;
		uint ammoDisplayID = 0;

		var playerCaster = _caster.AsPlayer;

		if (playerCaster != null)
		{
			var pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

			if (pItem)
			{
				ammoInventoryType = pItem.GetTemplate().GetInventoryType();

				if (ammoInventoryType == InventoryType.Thrown)
				{
					ammoDisplayID = pItem.GetDisplayId(playerCaster);
				}
				else if (playerCaster.HasAura(46699)) // Requires No Ammo
				{
					ammoDisplayID = 5996; // normal arrow
					ammoInventoryType = InventoryType.Ammo;
				}
			}
		}
		else
		{
			var unitCaster = _caster.AsUnit;

			if (unitCaster != null)
			{
				uint nonRangedAmmoDisplayID = 0;
				InventoryType nonRangedAmmoInventoryType = 0;

				for (byte i = (int)WeaponAttackType.BaseAttack; i < (int)WeaponAttackType.Max; ++i)
				{
					var itemId = unitCaster.GetVirtualItemId(i);

					if (itemId != 0)
					{
						var itemEntry = CliDB.ItemStorage.LookupByKey(itemId);

						if (itemEntry != null)
							if (itemEntry.ClassID == ItemClass.Weapon)
							{
								switch ((ItemSubClassWeapon)itemEntry.SubclassID)
								{
									case ItemSubClassWeapon.Thrown:
										ammoDisplayID = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
										ammoInventoryType = (InventoryType)itemEntry.inventoryType;

										break;
									case ItemSubClassWeapon.Bow:
									case ItemSubClassWeapon.Crossbow:
										ammoDisplayID = 5996; // is this need fixing?
										ammoInventoryType = InventoryType.Ammo;

										break;
									case ItemSubClassWeapon.Gun:
										ammoDisplayID = 5998; // is this need fixing?
										ammoInventoryType = InventoryType.Ammo;

										break;
									default:
										nonRangedAmmoDisplayID = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
										nonRangedAmmoInventoryType = itemEntry.inventoryType;

										break;
								}

								if (ammoDisplayID != 0)
									break;
							}
					}
				}

				if (ammoDisplayID == 0 && ammoInventoryType == 0)
				{
					ammoDisplayID = nonRangedAmmoDisplayID;
					ammoInventoryType = nonRangedAmmoInventoryType;
				}
			}
		}

		ammo.DisplayID = (int)ammoDisplayID;
		ammo.InventoryType = (sbyte)ammoInventoryType;
	}

	void SendSpellExecuteLog()
	{
		if (_executeLogEffects.Empty())
			return;

		SpellExecuteLog spellExecuteLog = new();

		spellExecuteLog.Caster = _caster.GUID;
		spellExecuteLog.SpellID = SpellInfo.Id;
		spellExecuteLog.Effects = _executeLogEffects.Values.ToList();
		spellExecuteLog.LogData.Initialize(this);

		_caster.SendCombatLogMessage(spellExecuteLog);
	}

	void ExecuteLogEffectTakeTargetPower(SpellEffectName effect, Unit target, PowerType powerType, uint points, double amplitude)
	{
		SpellLogEffectPowerDrainParams spellLogEffectPowerDrainParams;

		spellLogEffectPowerDrainParams.Victim = target.GUID;
		spellLogEffectPowerDrainParams.Points = points;
		spellLogEffectPowerDrainParams.PowerType = (uint)powerType;
		spellLogEffectPowerDrainParams.Amplitude = (float)amplitude;

		GetExecuteLogEffect(effect).PowerDrainTargets.Add(spellLogEffectPowerDrainParams);
	}

	void ExecuteLogEffectExtraAttacks(SpellEffectName effect, Unit victim, uint numAttacks)
	{
		SpellLogEffectExtraAttacksParams spellLogEffectExtraAttacksParams;
		spellLogEffectExtraAttacksParams.Victim = victim.GUID;
		spellLogEffectExtraAttacksParams.NumAttacks = numAttacks;

		GetExecuteLogEffect(effect).ExtraAttacksTargets.Add(spellLogEffectExtraAttacksParams);
	}

	void SendSpellInterruptLog(Unit victim, uint spellId)
	{
		SpellInterruptLog data = new();
		data.Caster = _caster.GUID;
		data.Victim = victim.GUID;
		data.InterruptedSpellID = SpellInfo.Id;
		data.SpellID = spellId;

		_caster.SendMessageToSet(data, true);
	}

	void ExecuteLogEffectDurabilityDamage(SpellEffectName effect, Unit victim, int itemId, int amount)
	{
		SpellLogEffectDurabilityDamageParams spellLogEffectDurabilityDamageParams;
		spellLogEffectDurabilityDamageParams.Victim = victim.GUID;
		spellLogEffectDurabilityDamageParams.ItemID = itemId;
		spellLogEffectDurabilityDamageParams.Amount = amount;

		GetExecuteLogEffect(effect).DurabilityDamageTargets.Add(spellLogEffectDurabilityDamageParams);
	}

	void ExecuteLogEffectOpenLock(SpellEffectName effect, WorldObject obj)
	{
		SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
		spellLogEffectGenericVictimParams.Victim = obj.GUID;

		GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
	}

	void ExecuteLogEffectCreateItem(SpellEffectName effect, uint entry)
	{
		SpellLogEffectTradeSkillItemParams spellLogEffectTradeSkillItemParams;
		spellLogEffectTradeSkillItemParams.ItemID = (int)entry;

		GetExecuteLogEffect(effect).TradeSkillTargets.Add(spellLogEffectTradeSkillItemParams);
	}

	void ExecuteLogEffectDestroyItem(SpellEffectName effect, uint entry)
	{
		SpellLogEffectFeedPetParams spellLogEffectFeedPetParams;
		spellLogEffectFeedPetParams.ItemID = (int)entry;

		GetExecuteLogEffect(effect).FeedPetTargets.Add(spellLogEffectFeedPetParams);
	}

	void ExecuteLogEffectSummonObject(SpellEffectName effect, WorldObject obj)
	{
		SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
		spellLogEffectGenericVictimParams.Victim = obj.GUID;

		GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
	}

	void ExecuteLogEffectUnsummonObject(SpellEffectName effect, WorldObject obj)
	{
		SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
		spellLogEffectGenericVictimParams.Victim = obj.GUID;

		GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
	}

	void ExecuteLogEffectResurrect(SpellEffectName effect, Unit target)
	{
		SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
		spellLogEffectGenericVictimParams.Victim = target.GUID;

		GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
	}

	void SendInterrupted(byte result)
	{
		SpellFailure failurePacket = new();
		failurePacket.CasterUnit = _caster.GUID;
		failurePacket.CastID = CastId;
		failurePacket.SpellID = SpellInfo.Id;
		failurePacket.Visual = SpellVisual;
		failurePacket.Reason = result;
		_caster.SendMessageToSet(failurePacket, true);

		SpellFailedOther failedPacket = new();
		failedPacket.CasterUnit = _caster.GUID;
		failedPacket.CastID = CastId;
		failedPacket.SpellID = SpellInfo.Id;
		failedPacket.Visual = SpellVisual;
		failedPacket.Reason = result;
		_caster.SendMessageToSet(failedPacket, true);
	}

	void SendChannelStart(uint duration)
	{
		// GameObjects don't channel
		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return;

		SpellChannelStart spellChannelStart = new();
		spellChannelStart.CasterGUID = unitCaster.GUID;
		spellChannelStart.SpellID = (int)SpellInfo.Id;
		spellChannelStart.Visual = SpellVisual;
		spellChannelStart.ChannelDuration = duration;

		var schoolImmunityMask = unitCaster.GetSchoolImmunityMask();
		var mechanicImmunityMask = unitCaster.GetMechanicImmunityMask();

		if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
		{
			SpellChannelStartInterruptImmunities interruptImmunities = new();
			interruptImmunities.SchoolImmunities = (int)schoolImmunityMask;
			interruptImmunities.Immunities = (int)mechanicImmunityMask;

			spellChannelStart.InterruptImmunities = interruptImmunities;
		}

		unitCaster.SendMessageToSet(spellChannelStart, true);

		_timer = (int)duration;

		if (!Targets.HasDst)
		{
			uint channelAuraMask = 0;
			var explicitTargetEffectMask = 0xFFFFFFFF;

			// if there is an explicit target, only add channel objects from effects that also hit ut
			if (!Targets.UnitTargetGUID.IsEmpty)
			{
				var explicitTarget = UniqueTargetInfo.Find(target => target.TargetGuid == Targets.UnitTargetGUID);

				if (explicitTarget != null)
					explicitTargetEffectMask = explicitTarget.EffectMask;
			}

			foreach (var spellEffectInfo in SpellInfo.Effects)
				if (spellEffectInfo.Effect == SpellEffectName.ApplyAura && (explicitTargetEffectMask & (1u << spellEffectInfo.EffectIndex)) != 0)
					channelAuraMask |= 1u << spellEffectInfo.EffectIndex;

			foreach (var target in UniqueTargetInfo)
			{
				if ((target.EffectMask & channelAuraMask) == 0)
					continue;

				var requiredAttribute = target.TargetGuid != unitCaster.GUID ? SpellAttr1.IsChannelled : SpellAttr1.IsSelfChannelled;

				if (!SpellInfo.HasAttribute(requiredAttribute))
					continue;

				unitCaster.AddChannelObject(target.TargetGuid);
			}

			foreach (var target in _uniqueGoTargetInfo)
				if ((target.EffectMask & channelAuraMask) != 0)
					unitCaster.AddChannelObject(target.TargetGUID);
		}
		else if (SpellInfo.HasAttribute(SpellAttr1.IsSelfChannelled))
		{
			unitCaster.AddChannelObject(unitCaster.GUID);
		}

		var creatureCaster = unitCaster.AsCreature;

		if (creatureCaster != null)
			if (unitCaster.UnitData.ChannelObjects.Size() == 1 && unitCaster.UnitData.ChannelObjects[0].IsUnit)
				if (!creatureCaster.HasSpellFocus(this))
					creatureCaster.SetSpellFocus(this, Global.ObjAccessor.GetWorldObject(creatureCaster, unitCaster.UnitData.ChannelObjects[0]));

		unitCaster.
		ChannelSpellId = SpellInfo.Id;
		unitCaster.SetChannelVisual(SpellVisual);
	}

	void SendResurrectRequest(Player target)
	{
		// get resurrector name for creature resurrections, otherwise packet will be not accepted
		// for player resurrections the name is looked up by guid
		var sentName = "";

		if (!_caster.IsPlayer)
			sentName = _caster.GetName(target.Session.SessionDbLocaleIndex);

		ResurrectRequest resurrectRequest = new();
		resurrectRequest.ResurrectOffererGUID = _caster.GUID;
		resurrectRequest.ResurrectOffererVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
		resurrectRequest.Name = sentName;
		resurrectRequest.Sickness = _caster.IsUnit && !_caster.IsTypeId(TypeId.Player); // "you'll be afflicted with resurrection sickness"
		resurrectRequest.UseTimer = !SpellInfo.HasAttribute(SpellAttr3.NoResTimer);

		var pet = target.GetPet();

		if (pet)
		{
			var charmInfo = pet.GetCharmInfo();

			if (charmInfo != null)
				resurrectRequest.PetNumber = charmInfo.GetPetNumber();
		}

		resurrectRequest.SpellID = SpellInfo.Id;

		target.SendPacket(resurrectRequest);
	}

	void TakeCastItem()
	{
		if (CastItem == null || !_caster.IsTypeId(TypeId.Player))
			return;

		// not remove cast item at triggered spell (equipping, weapon damage, etc)
		if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem))
			return;

		var proto = CastItem.GetTemplate();

		if (proto == null)
		{
			// This code is to avoid a crash
			// I'm not sure, if this is really an error, but I guess every item needs a prototype
			Log.outError(LogFilter.Spells, "Cast item has no item prototype {0}", CastItem.GUID.ToString());

			return;
		}

		var expendable = false;
		var withoutCharges = false;

		foreach (var itemEffect in CastItem.GetEffects())
		{
			if (itemEffect.LegacySlotIndex >= CastItem.ItemData.SpellCharges.GetSize())
				continue;

			// item has limited charges
			if (itemEffect.Charges != 0)
			{
				if (itemEffect.Charges < 0)
					expendable = true;

				var charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

				// item has charges left
				if (charges != 0)
				{
					if (charges > 0)
						--charges;
					else
						++charges;

					if (proto.GetMaxStackSize() == 1)
						CastItem.SetSpellCharges(itemEffect.LegacySlotIndex, charges);

					CastItem.SetState(ItemUpdateState.Changed, _caster.AsPlayer);
				}

				// all charges used
				withoutCharges = (charges == 0);
			}
		}

		if (expendable && withoutCharges)
		{
			uint count = 1;
			_caster.			AsPlayer.DestroyItemCount(CastItem, ref count, true);

			// prevent crash at access to deleted m_targets.GetItemTarget
			if (CastItem == Targets.ItemTarget)
				Targets.ItemTarget = null;

			CastItem = null;
			CastItemGuid.Clear();
			CastItemEntry = 0;
		}
	}

	void TakePower()
	{
		// GameObjects don't use power
		var unitCaster = _caster.AsUnit;

		if (!unitCaster)
			return;

		if (CastItem != null || TriggeredByAuraSpell != null)
			return;

		//Don't take power if the spell is cast while .cheat power is enabled.
		if (unitCaster.IsTypeId(TypeId.Player))
			if (unitCaster.AsPlayer.GetCommandStatus(PlayerCommandStates.Power))
				return;

		foreach (var cost in _powerCosts)
		{
			var hit = true;

			if (unitCaster.IsTypeId(TypeId.Player))
				if (SpellInfo.HasAttribute(SpellAttr1.DiscountPowerOnMiss))
				{
					var targetGUID = Targets.UnitTargetGUID;

					if (!targetGUID.IsEmpty)
					{
						var ihit = UniqueTargetInfo.FirstOrDefault(targetInfo => targetInfo.TargetGuid == targetGUID && targetInfo.MissCondition != SpellMissInfo.None);

						if (ihit != null)
						{
							hit = false;
							//lower spell cost on fail (by talent aura)
							var modOwner = unitCaster.GetSpellModOwner();

							if (modOwner != null)
								modOwner.ApplySpellMod(SpellInfo, SpellModOp.PowerCostOnMiss, ref cost.Amount);
						}
					}
				}

			if (cost.Power == PowerType.Runes)
			{
				TakeRunePower(hit);

				continue;
			}

			if (cost.Amount == 0)
				continue;

			// health as power used
			if (cost.Power == PowerType.Health)
			{
				unitCaster.ModifyHealth(-cost.Amount);

				continue;
			}

			if (cost.Power >= PowerType.Max)
			{
				Log.outError(LogFilter.Spells, "Spell.TakePower: Unknown power type '{0}'", cost.Power);

				continue;
			}

			unitCaster.ModifyPower(cost.Power, -cost.Amount);
			ForEachSpellScript<ISpellOnTakePower>(a => a.TakePower(cost));
		}
	}

	SpellCastResult CheckRuneCost()
	{
		var runeCost = _powerCosts.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

		if (runeCost == 0)
			return SpellCastResult.SpellCastOk;

		var player = _caster.AsPlayer;

		if (!player)
			return SpellCastResult.SpellCastOk;

		if (player.Class != Class.Deathknight)
			return SpellCastResult.SpellCastOk;

		var readyRunes = 0;

		for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
			if (player.GetRuneCooldown(i) == 0)
				++readyRunes;

		if (readyRunes < runeCost)
			return SpellCastResult.NoPower; // not sure if result code is correct

		return SpellCastResult.SpellCastOk;
	}

	void TakeRunePower(bool didHit)
	{
		if (!_caster.IsTypeId(TypeId.Player) || _caster.AsPlayer.Class != Class.Deathknight)
			return;

		var player = _caster.AsPlayer;
		_runesState = player.GetRunesState(); // store previous state

		var runeCost = _powerCosts.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

		for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
			if (player.GetRuneCooldown(i) == 0 && runeCost > 0)
			{
				player.SetRuneCooldown(i, didHit ? player.GetRuneBaseCooldown() : RuneCooldowns.Miss);
				--runeCost;
			}
	}

	void TakeReagents()
	{
		if (!_caster.IsTypeId(TypeId.Player))
			return;

		// do not take reagents for these item casts
		if (CastItem != null && CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
			return;

		var p_caster = _caster.AsPlayer;

		if (p_caster.CanNoReagentCast(SpellInfo))
			return;

		for (var x = 0; x < SpellConst.MaxReagents; ++x)
		{
			if (SpellInfo.Reagent[x] <= 0)
				continue;

			var itemid = (uint)SpellInfo.Reagent[x];
			var itemcount = SpellInfo.ReagentCount[x];

			// if CastItem is also spell reagent
			if (CastItem != null && CastItem.Entry == itemid)
			{
				foreach (var itemEffect in CastItem.GetEffects())
				{
					if (itemEffect.LegacySlotIndex >= CastItem.ItemData.SpellCharges.GetSize())
						continue;

					// CastItem will be used up and does not count as reagent
					var charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

					if (itemEffect.Charges < 0 && Math.Abs(charges) < 2)
					{
						++itemcount;

						break;
					}
				}

				CastItem = null;
				CastItemGuid.Clear();
				CastItemEntry = 0;
			}

			// if GetItemTarget is also spell reagent
			if (Targets.ItemTargetEntry == itemid)
				Targets.ItemTarget = null;

			p_caster.DestroyItemCount(itemid, itemcount, true);
		}

		foreach (var reagentsCurrency in SpellInfo.ReagentsCurrency)
			p_caster.RemoveCurrency(reagentsCurrency.CurrencyTypesID, -reagentsCurrency.CurrencyCount, CurrencyDestroyReason.Spell);
	}

	void HandleThreatSpells()
	{
		// wild GameObject spells don't cause threat
		var unitCaster = (_originalCaster ? _originalCaster : _caster.AsUnit);

		if (unitCaster == null)
			return;

		if (UniqueTargetInfo.Empty())
			return;

		if (!SpellInfo.HasInitialAggro)
			return;

		double threat = 0.0f;
		var threatEntry = Global.SpellMgr.GetSpellThreatEntry(SpellInfo.Id);

		if (threatEntry != null)
		{
			if (threatEntry.ApPctMod != 0.0f)
				threat += threatEntry.ApPctMod * unitCaster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

			threat += threatEntry.FlatMod;
		}
		else if (!SpellInfo.HasAttribute(SpellCustomAttributes.NoInitialThreat))
		{
			threat += SpellInfo.SpellLevel;
		}

		// past this point only multiplicative effects occur
		if (threat == 0.0f)
			return;

		// since 2.0.1 threat from positive effects also is distributed among all targets, so the overall caused threat is at most the defined bonus
		threat /= UniqueTargetInfo.Count;

		foreach (var ihit in UniqueTargetInfo)
		{
			var threatToAdd = threat;

			if (ihit.MissCondition != SpellMissInfo.None)
				threatToAdd = 0.0f;

			var target = Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGuid);

			if (target == null)
				continue;

			// positive spells distribute threat among all units that are in combat with target, like healing
			if (IsPositive())
			{
				target.GetThreatManager().ForwardThreatForAssistingMe(unitCaster, threatToAdd, SpellInfo);
			}
			// for negative spells threat gets distributed among affected targets
			else
			{
				if (!target.CanHaveThreatList)
					continue;

				target.GetThreatManager().AddThreat(unitCaster, threatToAdd, SpellInfo, true);
			}
		}

		Log.outDebug(LogFilter.Spells, "Spell {0}, added an additional {1} threat for {2} {3} target(s)", SpellInfo.Id, threat, IsPositive() ? "assisting" : "harming", UniqueTargetInfo.Count);
	}

	SpellCastResult CheckCasterAuras(ref int param1)
	{
		var unitCaster = (_originalCaster ? _originalCaster : _caster.AsUnit);

		if (unitCaster == null)
			return SpellCastResult.SpellCastOk;

		// these attributes only show the spell as usable on the client when it has related aura applied
		// still they need to be checked against certain mechanics

		// SPELL_ATTR5_USABLE_WHILE_STUNNED by default only MECHANIC_STUN (ie no sleep, knockout, freeze, etc.)
		var usableWhileStunned = SpellInfo.HasAttribute(SpellAttr5.AllowWhileStunned);

		// SPELL_ATTR5_USABLE_WHILE_FEARED by default only fear (ie no horror)
		var usableWhileFeared = SpellInfo.HasAttribute(SpellAttr5.AllowWhileFleeing);

		// SPELL_ATTR5_USABLE_WHILE_CONFUSED by default only disorient (ie no polymorph)
		var usableWhileConfused = SpellInfo.HasAttribute(SpellAttr5.AllowWhileConfused);

		// Check whether the cast should be prevented by any state you might have.
		var result = SpellCastResult.SpellCastOk;
		// Get unit state
		var unitflag = (UnitFlags)(uint)unitCaster.UnitData.Flags;

		// this check should only be done when player does cast directly
		// (ie not when it's called from a script) Breaks for example PlayerAI when charmed
		/*if (!unitCaster.GetCharmerGUID().IsEmpty())
		{
			Unit charmer = unitCaster.GetCharmer();
			if (charmer)
				if (charmer.GetUnitBeingMoved() != unitCaster && !CheckSpellCancelsCharm(ref param1))
					result = SpellCastResult.Charmed;
		}*/

		// spell has attribute usable while having a cc state, check if caster has allowed mechanic auras, another mechanic types must prevent cast spell
		SpellCastResult mechanicCheck(AuraType auraType, ref int _param1)
		{
			var foundNotMechanic = false;
			var auras = unitCaster.GetAuraEffectsByType(auraType);

			foreach (var aurEff in auras)
			{
				var mechanicMask = aurEff.SpellInfo.GetAllEffectsMechanicMask();

				if (mechanicMask != 0 && !Convert.ToBoolean(mechanicMask & SpellInfo.AllowedMechanicMask))
				{
					foundNotMechanic = true;

					// fill up aura mechanic info to send client proper error message
					_param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;

					if (_param1 == 0)
						_param1 = (int)aurEff.SpellInfo.Mechanic;

					break;
				}
			}

			if (foundNotMechanic)
				switch (auraType)
				{
					case AuraType.ModStun:
					case AuraType.ModStunDisableGravity:
						return SpellCastResult.Stunned;
					case AuraType.ModFear:
						return SpellCastResult.Fleeing;
					case AuraType.ModConfuse:
						return SpellCastResult.Confused;
					default:
						//ABORT();
						return SpellCastResult.NotKnown;
				}

			return SpellCastResult.SpellCastOk;
		}

		if (unitflag.HasAnyFlag(UnitFlags.Stunned))
		{
			if (usableWhileStunned)
			{
				var mechanicResult = mechanicCheck(AuraType.ModStun, ref param1);

				if (mechanicResult != SpellCastResult.SpellCastOk)
					result = mechanicResult;
			}
			else if (!CheckSpellCancelsStun(ref param1))
			{
				result = SpellCastResult.Stunned;
			}
			else if ((SpellInfo.Mechanic & Mechanics.ImmuneShield) != 0 && _caster.IsUnit && _caster.AsUnit.HasAuraWithMechanic(1 << (int)Mechanics.Banish))
			{
				result = SpellCastResult.Stunned;
			}
		}
		else if (unitCaster.IsSilenced(SpellSchoolMask) && SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) && !CheckSpellCancelsSilence(ref param1))
		{
			result = SpellCastResult.Silenced;
		}
		else if (unitflag.HasAnyFlag(UnitFlags.Pacified) && SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) && !CheckSpellCancelsPacify(ref param1))
		{
			result = SpellCastResult.Pacified;
		}
		else if (unitflag.HasAnyFlag(UnitFlags.Fleeing))
		{
			if (usableWhileFeared)
			{
				var mechanicResult = mechanicCheck(AuraType.ModFear, ref param1);

				if (mechanicResult != SpellCastResult.SpellCastOk)
				{
					result = mechanicResult;
				}
				else
				{
					mechanicResult = mechanicCheck(AuraType.ModStunDisableGravity, ref param1);

					if (mechanicResult != SpellCastResult.SpellCastOk)
						result = mechanicResult;
				}
			}
			else if (!CheckSpellCancelsFear(ref param1))
			{
				result = SpellCastResult.Fleeing;
			}
		}
		else if (unitflag.HasAnyFlag(UnitFlags.Confused))
		{
			if (usableWhileConfused)
			{
				var mechanicResult = mechanicCheck(AuraType.ModConfuse, ref param1);

				if (mechanicResult != SpellCastResult.SpellCastOk)
					result = mechanicResult;
			}
			else if (!CheckSpellCancelsConfuse(ref param1))
			{
				result = SpellCastResult.Confused;
			}
		}
		else if (unitCaster.HasUnitFlag2(UnitFlags2.NoActions) && SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions) && !CheckSpellCancelsNoActions(ref param1))
		{
			result = SpellCastResult.NoActions;
		}

		// Attr must make flag drop spell totally immune from all effects
		if (result != SpellCastResult.SpellCastOk)
			return (param1 != 0) ? SpellCastResult.PreventedByMechanic : result;

		return SpellCastResult.SpellCastOk;
	}

	bool CheckSpellCancelsAuraEffect(AuraType auraType, ref int param1)
	{
		var unitCaster = (_originalCaster ? _originalCaster : _caster.AsUnit);

		if (unitCaster == null)
			return false;

		// Checking auras is needed now, because you are prevented by some state but the spell grants immunity.
		var auraEffects = unitCaster.GetAuraEffectsByType(auraType);

		if (auraEffects.Empty())
			return true;

		foreach (var aurEff in auraEffects)
		{
			if (SpellInfo.SpellCancelsAuraEffect(aurEff))
				continue;

			param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;

			if (param1 == 0)
				param1 = (int)aurEff.SpellInfo.Mechanic;

			return false;
		}

		return true;
	}

	bool CheckSpellCancelsCharm(ref int param1)
	{
		return CheckSpellCancelsAuraEffect(AuraType.ModCharm, ref param1) ||
				CheckSpellCancelsAuraEffect(AuraType.AoeCharm, ref param1) ||
				CheckSpellCancelsAuraEffect(AuraType.ModPossess, ref param1);
	}

	bool CheckSpellCancelsStun(ref int param1)
	{
		return CheckSpellCancelsAuraEffect(AuraType.ModStun, ref param1) &&
				CheckSpellCancelsAuraEffect(AuraType.ModStunDisableGravity, ref param1);
	}

	bool CheckSpellCancelsSilence(ref int param1)
	{
		return CheckSpellCancelsAuraEffect(AuraType.ModSilence, ref param1) ||
				CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
	}

	bool CheckSpellCancelsPacify(ref int param1)
	{
		return CheckSpellCancelsAuraEffect(AuraType.ModPacify, ref param1) ||
				CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
	}

	bool CheckSpellCancelsFear(ref int param1)
	{
		return CheckSpellCancelsAuraEffect(AuraType.ModFear, ref param1);
	}

	bool CheckSpellCancelsConfuse(ref int param1)
	{
		return CheckSpellCancelsAuraEffect(AuraType.ModConfuse, ref param1);
	}

	bool CheckSpellCancelsNoActions(ref int param1)
	{
		return CheckSpellCancelsAuraEffect(AuraType.ModNoActions, ref param1);
	}

	SpellCastResult CheckArenaAndRatedBattlegroundCastRules()
	{
		var isRatedBattleground = false; // NYI
		var isArena = !isRatedBattleground;

		// check USABLE attributes
		// USABLE takes precedence over NOT_USABLE
		if (isRatedBattleground && SpellInfo.HasAttribute(SpellAttr9.UsableInRatedBattlegrounds))
			return SpellCastResult.SpellCastOk;

		if (isArena && SpellInfo.HasAttribute(SpellAttr4.IgnoreDefaultArenaRestrictions))
			return SpellCastResult.SpellCastOk;

		// check NOT_USABLE attributes
		if (SpellInfo.HasAttribute(SpellAttr4.NotInArenaOrRatedBattleground))
			return isArena ? SpellCastResult.NotInArena : SpellCastResult.NotInBattleground;

		if (isArena && SpellInfo.HasAttribute(SpellAttr9.NotUsableInArena))
			return SpellCastResult.NotInArena;

		// check cooldowns
		var spellCooldown = SpellInfo.RecoveryTime1;

		if (isArena && spellCooldown > 10 * Time.Minute * Time.InMilliseconds) // not sure if still needed
			return SpellCastResult.NotInArena;

		if (isRatedBattleground && spellCooldown > 15 * Time.Minute * Time.InMilliseconds)
			return SpellCastResult.NotInBattleground;

		return SpellCastResult.SpellCastOk;
	}

	SpellCastResult CheckRange(bool strict)
	{
		// Don't check for instant cast spells
		if (!strict && _casttime == 0)
			return SpellCastResult.SpellCastOk;

		var (minRange, maxRange) = GetMinMaxRange(strict);

		// dont check max_range to strictly after cast
		if (SpellInfo.RangeEntry != null && SpellInfo.RangeEntry.Flags != SpellRangeFlag.Melee && !strict)
			maxRange += Math.Min(3.0f, maxRange * 0.1f); // 10% but no more than 3.0f

		// get square values for sqr distance checks
		minRange *= minRange;
		maxRange *= maxRange;

		var target = Targets.UnitTarget;

		if (target && target != _caster)
		{
			if (_caster.Location.GetExactDistSq(target.Location) > maxRange)
				return SpellCastResult.OutOfRange;

			if (minRange > 0.0f && _caster.Location.GetExactDistSq(target.Location) < minRange)
				return SpellCastResult.OutOfRange;

			if (_caster.IsTypeId(TypeId.Player) &&
				((SpellInfo.FacingCasterFlags.HasAnyFlag(1u) && !_caster.Location.HasInArc((float)Math.PI, target.Location)) && !_caster.AsPlayer.IsWithinBoundaryRadius(target)))
				return SpellCastResult.UnitNotInfront;
		}

		var goTarget = Targets.GOTarget;

		if (goTarget != null)
			if (!goTarget.IsAtInteractDistance(_caster.AsPlayer, SpellInfo))
				return SpellCastResult.OutOfRange;

		if (Targets.HasDst && !Targets.HasTraj)
		{
			if (_caster.Location.GetExactDistSq(Targets.DstPos) > maxRange)
				return SpellCastResult.OutOfRange;

			if (minRange > 0.0f && _caster.Location.GetExactDistSq(Targets.DstPos) < minRange)
				return SpellCastResult.OutOfRange;
		}

		return SpellCastResult.SpellCastOk;
	}

	(float minRange, float maxRange) GetMinMaxRange(bool strict)
	{
		var rangeMod = 0.0f;
		var minRange = 0.0f;
		var maxRange = 0.0f;

		if (strict && SpellInfo.IsNextMeleeSwingSpell)
			return (0.0f, 100.0f);

		var unitCaster = _caster.AsUnit;

		if (SpellInfo.RangeEntry != null)
		{
			var target = Targets.UnitTarget;

			if (SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
			{
				// when the target is not a unit, take the caster's combat reach as the target's combat reach.
				if (unitCaster)
					rangeMod = unitCaster.GetMeleeRange(target ? target : unitCaster);
			}
			else
			{
				var meleeRange = 0.0f;

				if (SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
					// when the target is not a unit, take the caster's combat reach as the target's combat reach.
					if (unitCaster != null)
						meleeRange = unitCaster.GetMeleeRange(target ? target : unitCaster);

				minRange = _caster.GetSpellMinRangeForTarget(target, SpellInfo) + meleeRange;
				maxRange = _caster.GetSpellMaxRangeForTarget(target, SpellInfo);

				if (target || Targets.CorpseTarget)
				{
					rangeMod = _caster.CombatReach + (target ? target.CombatReach : _caster.CombatReach);

					if (minRange > 0.0f && !SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
						minRange += rangeMod;
				}
			}

			if (target != null &&
				unitCaster != null &&
				unitCaster.				IsMoving &&
				target.				IsMoving &&
				!unitCaster.IsWalking &&
				!target.IsWalking &&
				(SpellInfo.RangeEntry.Flags.HasFlag(SpellRangeFlag.Melee) || target.IsPlayer))
				rangeMod += 8.0f / 3.0f;
		}

		if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && _caster.IsTypeId(TypeId.Player))
		{
			var ranged = _caster.AsPlayer.GetWeaponForAttack(WeaponAttackType.RangedAttack, true);

			if (ranged)
				maxRange *= ranged.GetTemplate().GetRangedModRange() * 0.01f;
		}

		var modOwner = _caster.GetSpellModOwner();

		if (modOwner)
			modOwner.ApplySpellMod(SpellInfo, SpellModOp.Range, ref maxRange, this);

		maxRange += rangeMod;

		return (minRange, maxRange);
	}

	SpellCastResult CheckPower()
	{
		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return SpellCastResult.SpellCastOk;

		// item cast not used power
		if (CastItem != null)
			return SpellCastResult.SpellCastOk;

		foreach (var cost in _powerCosts)
		{
			// health as power used - need check health amount
			if (cost.Power == PowerType.Health)
			{
				if (unitCaster.GetHealth() <= cost.Amount)
					return SpellCastResult.CasterAurastate;

				continue;
			}

			// Check valid power type
			if (cost.Power >= PowerType.Max)
			{
				Log.outError(LogFilter.Spells, "Spell.CheckPower: Unknown power type '{0}'", cost.Power);

				return SpellCastResult.Unknown;
			}

			//check rune cost only if a spell has PowerType == POWER_RUNES
			if (cost.Power == PowerType.Runes)
			{
				var failReason = CheckRuneCost();

				if (failReason != SpellCastResult.SpellCastOk)
					return failReason;
			}

			// Check power amount
			if (unitCaster.GetPower(cost.Power) < cost.Amount)
				return SpellCastResult.NoPower;
		}

		return SpellCastResult.SpellCastOk;
	}

	SpellCastResult CheckItems(ref int param1, ref int param2)
	{
		var player = _caster.AsPlayer;

		if (!player)
			return SpellCastResult.SpellCastOk;

		if (CastItem == null)
		{
			if (!CastItemGuid.IsEmpty)
				return SpellCastResult.ItemNotReady;
		}
		else
		{
			var itemid = CastItem.Entry;

			if (!player.HasItemCount(itemid))
				return SpellCastResult.ItemNotReady;

			var proto = CastItem.GetTemplate();

			if (proto == null)
				return SpellCastResult.ItemNotReady;

			foreach (var itemEffect in CastItem.GetEffects())
				if (itemEffect.LegacySlotIndex < CastItem.ItemData.SpellCharges.GetSize() && itemEffect.Charges != 0)
					if (CastItem.GetSpellCharges(itemEffect.LegacySlotIndex) == 0)
						return SpellCastResult.NoChargesRemain;

			// consumable cast item checks
			if (proto.GetClass() == ItemClass.Consumable && Targets.UnitTarget != null)
			{
				// such items should only fail if there is no suitable effect at all - see Rejuvenation Potions for example
				var failReason = SpellCastResult.SpellCastOk;

				foreach (var spellEffectInfo in SpellInfo.Effects)
				{
					// skip check, pet not required like checks, and for TARGET_UNIT_PET m_targets.GetUnitTarget() is not the real target but the caster
					if (spellEffectInfo.TargetA.Target == Framework.Constants.Targets.UnitPet)
						continue;

					if (spellEffectInfo.Effect == SpellEffectName.Heal)
					{
						if (Targets.UnitTarget.IsFullHealth())
						{
							failReason = SpellCastResult.AlreadyAtFullHealth;

							continue;
						}
						else
						{
							failReason = SpellCastResult.SpellCastOk;

							break;
						}
					}

					// Mana Potion, Rage Potion, Thistle Tea(Rogue), ...
					if (spellEffectInfo.Effect == SpellEffectName.Energize)
					{
						if (spellEffectInfo.MiscValue < 0 || spellEffectInfo.MiscValue >= (int)PowerType.Max)
						{
							failReason = SpellCastResult.AlreadyAtFullPower;

							continue;
						}

						var power = (PowerType)spellEffectInfo.MiscValue;

						if (Targets.UnitTarget.GetPower(power) == Targets.UnitTarget.GetMaxPower(power))
						{
							failReason = SpellCastResult.AlreadyAtFullPower;

							continue;
						}
						else
						{
							failReason = SpellCastResult.SpellCastOk;

							break;
						}
					}
				}

				if (failReason != SpellCastResult.SpellCastOk)
					return failReason;
			}
		}

		// check target item
		if (!Targets.ItemTargetGuid.IsEmpty)
		{
			var item = Targets.ItemTarget;

			if (item == null)
				return SpellCastResult.ItemGone;

			if (!item.IsFitToSpellRequirements(SpellInfo))
				return SpellCastResult.EquippedItemClass;
		}
		// if not item target then required item must be equipped
		else
		{
			if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement))
				if (_caster.IsTypeId(TypeId.Player) && !_caster.AsPlayer.HasItemFitToSpellRequirements(SpellInfo))
					return SpellCastResult.EquippedItemClass;
		}

		// do not take reagents for these item casts
		if (!(CastItem != null && CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost)))
		{
			var checkReagents = !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost) && !player.CanNoReagentCast(SpellInfo);

			// Not own traded item (in trader trade slot) requires reagents even if triggered spell
			if (!checkReagents)
			{
				var targetItem = Targets.ItemTarget;

				if (targetItem != null)
					if (targetItem.OwnerGUID != player.GUID)
						checkReagents = true;
			}

			// check reagents (ignore triggered spells with reagents processed by original spell) and special reagent ignore case.
			if (checkReagents)
			{
				for (byte i = 0; i < SpellConst.MaxReagents; i++)
				{
					if (SpellInfo.Reagent[i] <= 0)
						continue;

					var itemid = (uint)SpellInfo.Reagent[i];
					var itemcount = SpellInfo.ReagentCount[i];

					// if CastItem is also spell reagent
					if (CastItem != null && CastItem.Entry == itemid)
					{
						var proto = CastItem.GetTemplate();

						if (proto == null)
							return SpellCastResult.ItemNotReady;

						foreach (var itemEffect in CastItem.GetEffects())
						{
							if (itemEffect.LegacySlotIndex >= CastItem.ItemData.SpellCharges.GetSize())
								continue;

							// CastItem will be used up and does not count as reagent
							var charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

							if (itemEffect.Charges < 0 && Math.Abs(charges) < 2)
							{
								++itemcount;

								break;
							}
						}
					}

					if (!player.HasItemCount(itemid, itemcount))
					{
						param1 = (int)itemid;

						return SpellCastResult.Reagents;
					}
				}

				foreach (var reagentsCurrency in SpellInfo.ReagentsCurrency)
					if (!player.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
					{
						param1 = -1;
						param2 = reagentsCurrency.CurrencyTypesID;

						return SpellCastResult.Reagents;
					}
			}

			// check totem-item requirements (items presence in inventory)
			uint totems = 2;

			for (var i = 0; i < 2; ++i)
				if (SpellInfo.Totem[i] != 0)
				{
					if (player.HasItemCount(SpellInfo.Totem[i]))
					{
						totems -= 1;

						continue;
					}
				}
				else
				{
					totems -= 1;
				}

			if (totems != 0)
				return SpellCastResult.Totems;

			// Check items for TotemCategory (items presence in inventory)
			uint totemCategory = 2;

			for (byte i = 0; i < 2; ++i)
				if (SpellInfo.TotemCategory[i] != 0)
				{
					if (player.HasItemTotemCategory(SpellInfo.TotemCategory[i]))
					{
						totemCategory -= 1;

						continue;
					}
				}
				else
				{
					totemCategory -= 1;
				}

			if (totemCategory != 0)
				return SpellCastResult.TotemCategory;
		}

		// special checks for spell effects
		foreach (var spellEffectInfo in SpellInfo.Effects)
			switch (spellEffectInfo.Effect)
			{
				case SpellEffectName.CreateItem:
				case SpellEffectName.CreateLoot:
				{
					// m_targets.GetUnitTarget() means explicit cast, otherwise we dont check for possible equip error
					var target = Targets.UnitTarget ?? player;

					if (target.IsPlayer && !IsTriggered())
					{
						// SPELL_EFFECT_CREATE_ITEM_2 differs from SPELL_EFFECT_CREATE_ITEM in that it picks the random item to create from a pool of potential items,
						// so we need to make sure there is at least one free space in the player's inventory
						if (spellEffectInfo.Effect == SpellEffectName.CreateLoot)
							if (target.AsPlayer.GetFreeInventorySpace() == 0)
							{
								player.SendEquipError(InventoryResult.InvFull, null, null, spellEffectInfo.ItemType);

								return SpellCastResult.DontReport;
							}

						if (spellEffectInfo.ItemType != 0)
						{
							var itemTemplate = Global.ObjectMgr.GetItemTemplate(spellEffectInfo.ItemType);

							if (itemTemplate == null)
								return SpellCastResult.ItemNotFound;

							var createCount = (uint)Math.Clamp(spellEffectInfo.CalcValue(), 1u, itemTemplate.GetMaxStackSize());

							List<ItemPosCount> dest = new();
							var msg = target.AsPlayer.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, createCount);

							if (msg != InventoryResult.Ok)
							{
								/// @todo Needs review
								if (itemTemplate.GetItemLimitCategory() == 0)
								{
									player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

									return SpellCastResult.DontReport;
								}
								else
								{
									// Conjure Food/Water/Refreshment spells
									if (SpellInfo.SpellFamilyName != SpellFamilyNames.Mage || (!SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x40000000u)))
									{
										return SpellCastResult.TooManyOfItem;
									}
									else if (!target.AsPlayer.HasItemCount(spellEffectInfo.ItemType))
									{
										player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

										return SpellCastResult.DontReport;
									}
									else if (SpellInfo.Effects.Count > 1)
									{
										player.CastSpell(player,
														(uint)SpellInfo.GetEffect(1).CalcValue(),
														new CastSpellExtraArgs()
															.SetTriggeringSpell(this)); // move this to anywhere
									}

									return SpellCastResult.DontReport;
								}
							}
						}
					}

					break;
				}
				case SpellEffectName.EnchantItem:
					if (spellEffectInfo.ItemType != 0 && Targets.ItemTarget != null && Targets.ItemTarget.IsVellum())
					{
						// cannot enchant vellum for other player
						if (Targets.ItemTarget.GetOwner() != player)
							return SpellCastResult.NotTradeable;

						// do not allow to enchant vellum from scroll made by vellum-prevent exploit
						if (CastItem != null && CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
							return SpellCastResult.TotemCategory;

						List<ItemPosCount> dest = new();
						var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, 1);

						if (msg != InventoryResult.Ok)
						{
							player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

							return SpellCastResult.DontReport;
						}
					}

					goto case SpellEffectName.EnchantItemPrismatic;
				case SpellEffectName.EnchantItemPrismatic:
				{
					var targetItem = Targets.ItemTarget;

					if (targetItem == null)
						return SpellCastResult.ItemNotFound;

					// required level has to be checked also! Exploit fix
					if (targetItem.GetItemLevel(targetItem.GetOwner()) < SpellInfo.BaseLevel || (targetItem.GetRequiredLevel() != 0 && targetItem.GetRequiredLevel() < SpellInfo.BaseLevel))
						return SpellCastResult.Lowlevel;

					var isItemUsable = false;

					foreach (var itemEffect in targetItem.GetEffects())
						if (itemEffect.SpellID != 0 && itemEffect.TriggerType == ItemSpelltriggerType.OnUse)
						{
							isItemUsable = true;

							break;
						}

					var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(spellEffectInfo.MiscValue);

					// do not allow adding usable enchantments to items that have use effect already
					if (enchantEntry != null)
						for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
							switch (enchantEntry.Effect[s])
							{
								case ItemEnchantmentType.UseSpell:
									if (isItemUsable)
										return SpellCastResult.OnUseEnchant;

									break;
								case ItemEnchantmentType.PrismaticSocket:
								{
									uint numSockets = 0;

									for (uint socket = 0; socket < ItemConst.MaxGemSockets; ++socket)
										if (targetItem.GetSocketColor(socket) != 0)
											++numSockets;

									if (numSockets == ItemConst.MaxGemSockets || targetItem.GetEnchantmentId(EnchantmentSlot.Prismatic) != 0)
										return SpellCastResult.MaxSockets;

									break;
								}
							}

					// Not allow enchant in trade slot for some enchant type
					if (targetItem.GetOwner() != player)
					{
						if (enchantEntry == null)
							return SpellCastResult.Error;

						if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
							return SpellCastResult.NotTradeable;
					}

					break;
				}
				case SpellEffectName.EnchantItemTemporary:
				{
					var item = Targets.ItemTarget;

					if (item == null)
						return SpellCastResult.ItemNotFound;

					// Not allow enchant in trade slot for some enchant type
					if (item.GetOwner() != player)
					{
						var enchant_id = spellEffectInfo.MiscValue;
						var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

						if (enchantEntry == null)
							return SpellCastResult.Error;

						if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
							return SpellCastResult.NotTradeable;
					}

					// Apply item level restriction if the enchanting spell has max level restrition set
					if (CastItem != null && SpellInfo.MaxLevel > 0)
					{
						if (item.GetTemplate().GetBaseItemLevel() < CastItem.GetTemplate().GetBaseRequiredLevel())
							return SpellCastResult.Lowlevel;

						if (item.GetTemplate().GetBaseItemLevel() > SpellInfo.MaxLevel)
							return SpellCastResult.Highlevel;
					}

					break;
				}
				case SpellEffectName.EnchantHeldItem:
					// check item existence in effect code (not output errors at offhand hold item effect to main hand for example
					break;
				case SpellEffectName.Disenchant:
				{
					var item = Targets.ItemTarget;

					if (!item)
						return SpellCastResult.CantBeSalvaged;

					// prevent disenchanting in trade slot
					if (item.OwnerGUID != player.GUID)
						return SpellCastResult.CantBeSalvaged;

					var itemProto = item.GetTemplate();

					if (itemProto == null)
						return SpellCastResult.CantBeSalvaged;

					var itemDisenchantLoot = item.GetDisenchantLoot(_caster.AsPlayer);

					if (itemDisenchantLoot == null)
						return SpellCastResult.CantBeSalvaged;

					if (itemDisenchantLoot.SkillRequired > player.GetSkillValue(SkillType.Enchanting))
						return SpellCastResult.CantBeSalvagedSkill;

					break;
				}
				case SpellEffectName.Prospecting:
				{
					var item = Targets.ItemTarget;

					if (!item)
						return SpellCastResult.CantBeProspected;

					//ensure item is a prospectable ore
					if (!item.GetTemplate().HasFlag(ItemFlags.IsProspectable))
						return SpellCastResult.CantBeProspected;

					//prevent prospecting in trade slot
					if (item.OwnerGUID != player.GUID)
						return SpellCastResult.CantBeProspected;

					//Check for enough skill in jewelcrafting
					var item_prospectingskilllevel = item.GetTemplate().GetRequiredSkillRank();

					if (item_prospectingskilllevel > player.GetSkillValue(SkillType.Jewelcrafting))
						return SpellCastResult.LowCastlevel;

					//make sure the player has the required ores in inventory
					if (item.GetCount() < 5)
					{
						param1 = (int)item.Entry;
						param2 = 5;

						return SpellCastResult.NeedMoreItems;
					}

					if (!LootStorage.Prospecting.HaveLootFor(Targets.ItemTargetEntry))
						return SpellCastResult.CantBeProspected;

					break;
				}
				case SpellEffectName.Milling:
				{
					var item = Targets.ItemTarget;

					if (!item)
						return SpellCastResult.CantBeMilled;

					//ensure item is a millable herb
					if (!item.GetTemplate().HasFlag(ItemFlags.IsMillable))
						return SpellCastResult.CantBeMilled;

					//prevent milling in trade slot
					if (item.OwnerGUID != player.GUID)
						return SpellCastResult.CantBeMilled;

					//Check for enough skill in inscription
					var item_millingskilllevel = item.GetTemplate().GetRequiredSkillRank();

					if (item_millingskilllevel > player.GetSkillValue(SkillType.Inscription))
						return SpellCastResult.LowCastlevel;

					//make sure the player has the required herbs in inventory
					if (item.GetCount() < 5)
					{
						param1 = (int)item.Entry;
						param2 = 5;

						return SpellCastResult.NeedMoreItems;
					}

					if (!LootStorage.Milling.HaveLootFor(Targets.ItemTargetEntry))
						return SpellCastResult.CantBeMilled;

					break;
				}
				case SpellEffectName.WeaponDamage:
				case SpellEffectName.WeaponDamageNoSchool:
				{
					if (AttackType != WeaponAttackType.RangedAttack)
						break;

					var item = player.GetWeaponForAttack(AttackType);

					if (item == null || item.IsBroken())
						return SpellCastResult.EquippedItem;

					switch ((ItemSubClassWeapon)item.GetTemplate().GetSubClass())
					{
						case ItemSubClassWeapon.Thrown:
						{
							var ammo = item.Entry;

							if (!player.HasItemCount(ammo))
								return SpellCastResult.NoAmmo;

							break;
						}
						case ItemSubClassWeapon.Gun:
						case ItemSubClassWeapon.Bow:
						case ItemSubClassWeapon.Crossbow:
						case ItemSubClassWeapon.Wand:
							break;
						default:
							break;
					}

					break;
				}
				case SpellEffectName.RechargeItem:
				{
					var itemId = spellEffectInfo.ItemType;

					var proto = Global.ObjectMgr.GetItemTemplate(itemId);

					if (proto == null)
						return SpellCastResult.ItemAtMaxCharges;

					var item = player.GetItemByEntry(itemId);

					if (item != null)
						foreach (var itemEffect in item.GetEffects())
							if (itemEffect.LegacySlotIndex <= item.ItemData.SpellCharges.GetSize() && itemEffect.Charges != 0 && item.GetSpellCharges(itemEffect.LegacySlotIndex) == itemEffect.Charges)
								return SpellCastResult.ItemAtMaxCharges;

					break;
				}
				case SpellEffectName.RespecAzeriteEmpoweredItem:
				{
					var item = Targets.ItemTarget;

					if (item == null)
						return SpellCastResult.AzeriteEmpoweredOnly;

					if (item.OwnerGUID != _caster.GUID)
						return SpellCastResult.DontReport;

					var azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();

					if (azeriteEmpoweredItem == null)
						return SpellCastResult.AzeriteEmpoweredOnly;

					var hasSelections = false;

					for (var tier = 0; tier < SharedConst.MaxAzeriteEmpoweredTier; ++tier)
						if (azeriteEmpoweredItem.GetSelectedAzeritePower(tier) != 0)
						{
							hasSelections = true;

							break;
						}

					if (!hasSelections)
						return SpellCastResult.AzeriteEmpoweredNoChoicesToUndo;

					if (!_caster.AsPlayer.HasEnoughMoney(azeriteEmpoweredItem.GetRespecCost()))
						return SpellCastResult.DontReport;

					break;
				}
				default:
					break;
			}

		// check weapon presence in slots for main/offhand weapons
		if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement) && SpellInfo.EquippedItemClass >= 0)
		{
			var weaponCheck = new Func<WeaponAttackType, SpellCastResult>(attackType =>
			{
				var item = player.AsPlayer.GetWeaponForAttack(attackType);

				// skip spell if no weapon in slot or broken
				if (!item || item.IsBroken())
					return SpellCastResult.EquippedItemClass;

				// skip spell if weapon not fit to triggered spell
				if (!item.IsFitToSpellRequirements(SpellInfo))
					return SpellCastResult.EquippedItemClass;

				return SpellCastResult.SpellCastOk;
			});

			// main hand weapon required
			if (SpellInfo.HasAttribute(SpellAttr3.RequiresMainHandWeapon))
			{
				var mainHandResult = weaponCheck(WeaponAttackType.BaseAttack);

				if (mainHandResult != SpellCastResult.SpellCastOk)
					return mainHandResult;
			}

			// offhand hand weapon required
			if (SpellInfo.HasAttribute(SpellAttr3.RequiresOffHandWeapon))
			{
				var offHandResult = weaponCheck(WeaponAttackType.OffAttack);

				if (offHandResult != SpellCastResult.SpellCastOk)
					return offHandResult;
			}
		}

		return SpellCastResult.SpellCastOk;
	}

	bool UpdatePointers()
	{
		if (_originalCasterGuid == _caster.GUID)
		{
			_originalCaster = _caster.AsUnit;
		}
		else
		{
			_originalCaster = Global.ObjAccessor.GetUnit(_caster, _originalCasterGuid);

			if (_originalCaster != null && !_originalCaster.IsInWorld)
				_originalCaster = null;
			else
				_originalCaster = _caster.AsUnit;
		}

		if (!CastItemGuid.IsEmpty && _caster.IsTypeId(TypeId.Player))
		{
			CastItem = _caster.AsPlayer.GetItemByGuid(CastItemGuid);
			CastItemLevel = -1;

			// cast item not found, somehow the item is no longer where we expected
			if (!CastItem)
				return false;

			// check if the item is really the same, in case it has been wrapped for example
			if (CastItemEntry != CastItem.Entry)
				return false;

			CastItemLevel = (int)CastItem.GetItemLevel(_caster.AsPlayer);
		}

		Targets.Update(_caster);

		// further actions done only for dest targets
		if (!Targets.HasDst)
			return true;

		// cache last transport
		WorldObject transport = null;

		// update effect destinations (in case of moved transport dest target)
		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			var dest = _destTargets[spellEffectInfo.EffectIndex];

			if (dest.TransportGuid.IsEmpty)
				continue;

			if (transport == null || transport.GUID != dest.TransportGuid)
				transport = Global.ObjAccessor.GetWorldObject(_caster, dest.TransportGuid);

			if (transport != null)
			{
				dest.Position.Relocate(transport.Location);
				dest.Position.RelocateOffset(dest.TransportOffset);
			}
		}

		return true;
	}

	bool CheckEffectTarget(Unit target, SpellEffectInfo spellEffectInfo, Position losPosition)
	{
		if (spellEffectInfo == null || !spellEffectInfo.IsEffect())
			return false;

		switch (spellEffectInfo.ApplyAuraName)
		{
			case AuraType.ModPossess:
			case AuraType.ModCharm:
			case AuraType.ModPossessPet:
			case AuraType.AoeCharm:
				if (target.GetVehicleKit() && target.GetVehicleKit().IsControllableVehicle())
					return false;

				if (target.IsMounted)
					return false;

				if (!target.CharmerGUID.IsEmpty)
					return false;

				var damage = CalculateDamage(spellEffectInfo, target);

				if (damage != 0)
					if (target.GetLevelForTarget(_caster) > damage)
						return false;

				break;
			default:
				break;
		}

		// check for ignore LOS on the effect itself
		if (SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS))
			return true;

		// check if gameobject ignores LOS
		var gobCaster = _caster.AsGameObject;

		if (gobCaster != null)
			if (gobCaster.GetGoInfo().GetRequireLOS() == 0)
				return true;

		// if spell is triggered, need to check for LOS disable on the aura triggering it and inherit that behaviour
		if (!SpellInfo.HasAttribute(SpellAttr5.AlwaysLineOfSight) && IsTriggered() && TriggeredByAuraSpell != null && (TriggeredByAuraSpell.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, TriggeredByAuraSpell.Id, null, (byte)DisableFlags.SpellLOS)))
			return true;

		// @todo shit below shouldn't be here, but it's temporary
		//Check targets for LOS visibility
		switch (spellEffectInfo.Effect)
		{
			case SpellEffectName.SkinPlayerCorpse:
			{
				if (Targets.CorpseTargetGUID.IsEmpty)
				{
					if (target.IsWithinLOSInMap(_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2) && target.HasUnitFlag(UnitFlags.Skinnable))
						return true;

					return false;
				}

				var corpse = ObjectAccessor.GetCorpse(_caster, Targets.CorpseTargetGUID);

				if (!corpse)
					return false;

				if (target.GUID != corpse.OwnerGUID)
					return false;

				if (!corpse.HasCorpseDynamicFlag(CorpseDynFlags.Lootable))
					return false;

				if (!corpse.IsWithinLOSInMap(_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
					return false;

				break;
			}
			default:
			{
				if (losPosition == null || SpellInfo.HasAttribute(SpellAttr5.AlwaysAoeLineOfSight))
				{
					// Get GO cast coordinates if original caster . GO
					WorldObject caster = null;

					if (_originalCasterGuid.IsGameObject)
						caster = _caster.GetMap().GetGameObject(_originalCasterGuid);

					if (!caster)
						caster = _caster;

					if (target != _caster && !target.IsWithinLOSInMap(caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
						return false;
				}

				if (losPosition != null)
					if (!target.IsWithinLOS(losPosition.X, losPosition.Y, losPosition.Z, LineOfSightChecks.All, ModelIgnoreFlags.M2))
						return false;

				break;
			}
		}

		return true;
	}

	bool CheckEffectTarget(GameObject target, SpellEffectInfo spellEffectInfo)
	{
		if (spellEffectInfo == null || !spellEffectInfo.IsEffect())
			return false;

		switch (spellEffectInfo.Effect)
		{
			case SpellEffectName.GameObjectDamage:
			case SpellEffectName.GameobjectRepair:
			case SpellEffectName.GameobjectSetDestructionState:
				if (target.GetGoType() != GameObjectTypes.DestructibleBuilding)
					return false;

				break;
			default:
				break;
		}

		return true;
	}

	bool CheckEffectTarget(Item target, SpellEffectInfo spellEffectInfo)
	{
		if (spellEffectInfo == null || !spellEffectInfo.IsEffect())
			return false;

		return true;
	}

	bool IsAutoActionResetSpell()
	{
		if (IsTriggered())
			return false;

		if (_casttime == 0 && SpellInfo.HasAttribute(SpellAttr6.DoesntResetSwingTimerIfInstant))
			return false;

		return true;
	}

	bool IsNeedSendToClient()
	{
		return SpellVisual.SpellXSpellVisualID != 0 ||
				SpellVisual.ScriptVisualID != 0 ||
				SpellInfo.IsChanneled ||
				SpellInfo.HasAttribute(SpellAttr8.AuraSendAmount) ||
				SpellInfo.HasHitDelay ||
				(TriggeredByAuraSpell == null && !IsTriggered());
	}

	bool IsValidDeadOrAliveTarget(Unit target)
	{
		if (target.IsAlive)
			return !SpellInfo.IsRequiringDeadTarget;

		if (SpellInfo.IsAllowingDeadTarget)
			return true;

		return false;
	}

	void HandleLaunchPhase()
	{
		// handle effects with SPELL_EFFECT_HANDLE_LAUNCH mode
		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			// don't do anything for empty effect
			if (!spellEffectInfo.IsEffect())
				continue;

			HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Launch);
		}

		PrepareTargetProcessing();

		foreach (var target in UniqueTargetInfo)
			PreprocessSpellLaunch(target);

		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			double multiplier = 1.0f;

			if ((_applyMultiplierMask & (1 << spellEffectInfo.EffectIndex)) != 0)
				multiplier = spellEffectInfo.CalcDamageMultiplier(_originalCaster, this);

			foreach (var target in UniqueTargetInfo)
			{
				var mask = target.EffectMask;

				if ((mask & (1 << spellEffectInfo.EffectIndex)) == 0)
					continue;

				DoEffectOnLaunchTarget(target, multiplier, spellEffectInfo);
			}
		}

		FinishTargetProcessing();
	}

	void PreprocessSpellLaunch(TargetInfo targetInfo)
	{
		var targetUnit = _caster.GUID == targetInfo.TargetGuid ? _caster.AsUnit : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGuid);

		if (targetUnit == null)
			return;

		// This will only cause combat - the target will engage once the projectile hits (in Spell::TargetInfo::PreprocessTarget)
		if (_originalCaster && targetInfo.MissCondition != SpellMissInfo.Evade && !_originalCaster.IsFriendlyTo(targetUnit) && (!SpellInfo.IsPositive || SpellInfo.HasEffect(SpellEffectName.Dispel)) && (SpellInfo.HasInitialAggro || targetUnit.IsEngaged))
			_originalCaster.SetInCombatWith(targetUnit, true);

		Unit unit = null;

		// In case spell hit target, do all effect on that target
		if (targetInfo.MissCondition == SpellMissInfo.None)
			unit = targetUnit;
		// In case spell reflect from target, do all effect on caster (if hit)
		else if (targetInfo.MissCondition == SpellMissInfo.Reflect && targetInfo.ReflectResult == SpellMissInfo.None)
			unit = _caster.AsUnit;

		if (unit == null)
			return;

		double critChance = SpellValue.CriticalChance;

		if (_originalCaster)
		{
			if (critChance == 0)
				critChance = _originalCaster.SpellCritChanceDone(this, null, SpellSchoolMask, AttackType);

			critChance = unit.SpellCritChanceTaken(_originalCaster, this, null, SpellSchoolMask, critChance, AttackType);
		}

		targetInfo.IsCrit = RandomHelper.randChance(critChance);
	}

	void DoEffectOnLaunchTarget(TargetInfo targetInfo, double multiplier, SpellEffectInfo spellEffectInfo)
	{
		Unit unit = null;

		// In case spell hit target, do all effect on that target
		if (targetInfo.MissCondition == SpellMissInfo.None || (targetInfo.MissCondition == SpellMissInfo.Block && !SpellInfo.HasAttribute(SpellAttr3.CompletelyBlocked)))
			unit = _caster.GUID == targetInfo.TargetGuid ? _caster.AsUnit : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGuid);
		// In case spell reflect from target, do all effect on caster (if hit)
		else if (targetInfo.MissCondition == SpellMissInfo.Reflect && targetInfo.ReflectResult == SpellMissInfo.None)
			unit = _caster.AsUnit;

		if (!unit)
			return;

		DamageInEffects = 0;
		HealingInEffects = 0;

		HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.LaunchTarget);

		if (_originalCaster != null && DamageInEffects > 0)
			if (spellEffectInfo.IsTargetingArea() || spellEffectInfo.IsAreaAuraEffect() || spellEffectInfo.IsEffect(SpellEffectName.PersistentAreaAura) || SpellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect))
			{
				DamageInEffects = unit.CalculateAOEAvoidance(DamageInEffects, (uint)SpellInfo.SchoolMask, _originalCaster.GUID);

				if (_originalCaster.IsPlayer)
				{
					// cap damage of player AOE
					var targetAmount = GetUnitTargetCountForEffect(spellEffectInfo.EffectIndex);

					if (targetAmount > 20)
						DamageInEffects = (int)(DamageInEffects * 20 / targetAmount);
				}
			}

		if ((_applyMultiplierMask & (1 << spellEffectInfo.EffectIndex)) != 0)
		{
			DamageInEffects = (int)(DamageInEffects * _damageMultipliers[spellEffectInfo.EffectIndex]);
			HealingInEffects = (int)(HealingInEffects * _damageMultipliers[spellEffectInfo.EffectIndex]);

			_damageMultipliers[spellEffectInfo.EffectIndex] *= multiplier;
		}

		targetInfo.Damage += DamageInEffects;
		targetInfo.Healing += HealingInEffects;
	}

	SpellCastResult CanOpenLock(SpellEffectInfo effect, uint lockId, ref SkillType skillId, ref int reqSkillValue, ref int skillValue)
	{
		if (lockId == 0) // possible case for GO and maybe for items.
			return SpellCastResult.SpellCastOk;

		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return SpellCastResult.BadTargets;

		// Get LockInfo
		var lockInfo = CliDB.LockStorage.LookupByKey(lockId);

		if (lockInfo == null)
			return SpellCastResult.BadTargets;

		var reqKey = false; // some locks not have reqs

		for (var j = 0; j < SharedConst.MaxLockCase; ++j)
			switch ((LockKeyType)lockInfo.LockType[j])
			{
				// check key item (many fit cases can be)
				case LockKeyType.Item:
					if (lockInfo.Index[j] != 0 && CastItem && CastItem.Entry == lockInfo.Index[j])
						return SpellCastResult.SpellCastOk;

					reqKey = true;

					break;
				// check key skill (only single first fit case can be)
				case LockKeyType.Skill:
				{
					reqKey = true;

					// wrong locktype, skip
					if (effect.MiscValue != lockInfo.Index[j])
						continue;

					skillId = SharedConst.SkillByLockType((LockType)lockInfo.Index[j]);

					if (skillId != SkillType.None || lockInfo.Index[j] == (uint)LockType.Lockpicking)
					{
						reqSkillValue = lockInfo.Skill[j];

						// castitem check: rogue using skeleton keys. the skill values should not be added in this case.
						skillValue = 0;

						if (!CastItem && unitCaster.IsTypeId(TypeId.Player))
							skillValue = unitCaster.AsPlayer.GetSkillValue(skillId);
						else if (lockInfo.Index[j] == (uint)LockType.Lockpicking)
							skillValue = (int)unitCaster.Level * 5;

						// skill bonus provided by casting spell (mostly item spells)
						// add the effect base points modifier from the spell cast (cheat lock / skeleton key etc.)
						if (effect.TargetA.Target == Framework.Constants.Targets.GameobjectItemTarget || effect.TargetB.Target == Framework.Constants.Targets.GameobjectItemTarget)
							skillValue += (int)effect.CalcValue();

						if (skillValue < reqSkillValue)
							return SpellCastResult.LowCastlevel;
					}

					return SpellCastResult.SpellCastOk;
				}
				case LockKeyType.Spell:
					if (SpellInfo.Id == lockInfo.Index[j])
						return SpellCastResult.SpellCastOk;

					reqKey = true;

					break;
			}

		if (reqKey)
			return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}

	void PrepareTargetProcessing() { }

	void FinishTargetProcessing()
	{
		SendSpellExecuteLog();
	}

	void LoadScripts()
	{
		_loadedScripts = Global.ScriptMgr.CreateSpellScripts(SpellInfo.Id, this);

		foreach (var script in _loadedScripts)
		{
			Log.outDebug(LogFilter.Spells, "Spell.LoadScripts: Script `{0}` for spell `{1}` is loaded now", script._GetScriptName(), SpellInfo.Id);
			script.Register();

			if (script is ISpellScript)
				foreach (var iFace in script.GetType().GetInterfaces())
				{
					if (iFace.Name == nameof(ISpellScript) || iFace.Name == nameof(IBaseSpellScript))
						continue;

					if (!_spellScriptsByType.TryGetValue(iFace, out var spellScripts))
					{
						spellScripts = new List<ISpellScript>();
						_spellScriptsByType[iFace] = spellScripts;
					}

					spellScripts.Add((ISpellScript)script);
					RegisterSpellEffectHandler(script);
				}
		}
	}

	private void RegisterSpellEffectHandler(SpellScript script)
	{
		if (script is IHasSpellEffects hse)
			foreach (var effect in hse.SpellEffects)
				if (effect is ISpellEffectHandler se)
				{
					uint mask = 0;

					if (se.EffectIndex == SpellConst.EffectAll || se.EffectIndex == SpellConst.EffectFirstFound)
					{
						foreach (var effInfo in SpellInfo.Effects)
						{
							if (se.EffectIndex == SpellConst.EffectFirstFound && mask != 0)
								break;

							if (CheckSpellEffectHandler(se, effInfo))
								AddSpellEffect(effInfo.EffectIndex, script, se);
						}
					}
					else
					{
						if (CheckSpellEffectHandler(se, se.EffectIndex))
							AddSpellEffect(se.EffectIndex, script, se);
					}
				}
				else if (effect is ITargetHookHandler th)
				{
					uint mask = 0;

					if (th.EffectIndex == SpellConst.EffectAll || th.EffectIndex == SpellConst.EffectFirstFound)
					{
						foreach (var effInfo in SpellInfo.Effects)
						{
							if (th.EffectIndex == SpellConst.EffectFirstFound && mask != 0)
								break;

							if (CheckTargetHookEffect(th, effInfo))
								AddSpellEffect(effInfo.EffectIndex, script, th);
						}
					}
					else
					{
						if (CheckTargetHookEffect(th, th.EffectIndex))
							AddSpellEffect(th.EffectIndex, script, th);
					}
				}
	}

	private bool CheckSpellEffectHandler(ISpellEffectHandler se, int effIndex)
	{
		if (SpellInfo.Effects.Count <= effIndex)
			return false;

		var spellEffectInfo = SpellInfo.GetEffect(effIndex);

		return CheckSpellEffectHandler(se, spellEffectInfo);
	}

	private bool CheckSpellEffectHandler(ISpellEffectHandler se, SpellEffectInfo spellEffectInfo)
	{
		if (spellEffectInfo.Effect == 0 && se.EffectName == 0)
			return true;

		if (spellEffectInfo.Effect == 0)
			return false;

		return se.EffectName == SpellEffectName.Any || spellEffectInfo.Effect == se.EffectName;
	}


	void CallScriptOnPrecastHandler()
	{
		foreach (var script in GetSpellScripts<ISpellOnPrecast>())
		{
			script._PrepareScriptCall(SpellScriptHookType.OnPrecast);
			((ISpellOnPrecast)script).OnPrecast();
			script._FinishScriptCall();
		}
	}

	void CallScriptBeforeCastHandlers()
	{
		foreach (var script in GetSpellScripts<ISpellBeforeCast>())
		{
			script._PrepareScriptCall(SpellScriptHookType.BeforeCast);

			((ISpellBeforeCast)script).BeforeCast();

			script._FinishScriptCall();
		}
	}

	void CallScriptOnCastHandlers()
	{
		foreach (var script in GetSpellScripts<ISpellOnCast>())
		{
			script._PrepareScriptCall(SpellScriptHookType.OnCast);

			((ISpellOnCast)script).OnCast();

			script._FinishScriptCall();
		}
	}

	void CallScriptAfterCastHandlers()
	{
		foreach (var script in GetSpellScripts<ISpellAfterCast>())
		{
			script._PrepareScriptCall(SpellScriptHookType.AfterCast);

			((ISpellAfterCast)script).AfterCast();

			script._FinishScriptCall();
		}
	}

	SpellCastResult CallScriptCheckCastHandlers()
	{
		var retVal = SpellCastResult.SpellCastOk;

		foreach (var script in GetSpellScripts<ISpellCheckCast>())
		{
			script._PrepareScriptCall(SpellScriptHookType.CheckCast);

			var tempResult = ((ISpellCheckCast)script).CheckCast();

			if (tempResult != SpellCastResult.SpellCastOk)
				retVal = tempResult;

			script._FinishScriptCall();
		}

		return retVal;
	}

	int CallScriptCalcCastTimeHandlers(int castTime)
	{
		foreach (var script in GetSpellScripts<ISpellCalculateCastTime>())
		{
			script._PrepareScriptCall(SpellScriptHookType.CalcCastTime);
			castTime = ((ISpellCalculateCastTime)script).CalcCastTime(castTime);
			script._FinishScriptCall();
		}

		return castTime;
	}

	bool CallScriptEffectHandlers(int effIndex, SpellEffectHandleMode mode)
	{
		// execute script effect handler hooks and check if effects was prevented
		var preventDefault = false;

		switch (mode)
		{
			case SpellEffectHandleMode.Launch:

				foreach (var script in GetEffectScripts(SpellScriptHookType.Launch, effIndex))
					preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Launch);

				break;
			case SpellEffectHandleMode.LaunchTarget:

				foreach (var script in GetEffectScripts(SpellScriptHookType.LaunchTarget, effIndex))
					preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.LaunchTarget);

				break;
			case SpellEffectHandleMode.Hit:

				foreach (var script in GetEffectScripts(SpellScriptHookType.Hit, effIndex))
					preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Hit);

				break;
			case SpellEffectHandleMode.HitTarget:

				foreach (var script in GetEffectScripts(SpellScriptHookType.EffectHitTarget, effIndex))
					preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.EffectHitTarget);

				break;
			default:
				Cypher.Assert(false);

				return false;
		}

		return preventDefault;
	}

	private static bool ProcessScript(int effIndex, bool preventDefault, ISpellScript script, ISpellEffect effect, SpellScriptHookType hookType)
	{
		script._InitHit();

		script._PrepareScriptCall(hookType);

		if (!script._IsEffectPrevented(effIndex))
			if (effect is ISpellEffectHandler seh)
				seh.CallEffect(effIndex);

		if (!preventDefault)
			preventDefault = script._IsDefaultEffectPrevented(effIndex);

		script._FinishScriptCall();

		return preventDefault;
	}

	void CallScriptSuccessfulDispel(int effIndex)
	{
		foreach (var script in GetEffectScripts(SpellScriptHookType.EffectSuccessfulDispel, effIndex))
		{
			script.Item1._PrepareScriptCall(SpellScriptHookType.EffectSuccessfulDispel);

			if (script.Item2 is ISpellEffectHandler seh)
				seh.CallEffect(effIndex);

			script.Item1._FinishScriptCall();
		}
	}

	void CallScriptObjectAreaTargetSelectHandlers(List<WorldObject> targets, int effIndex, SpellImplicitTargetInfo targetType)
	{
		foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex))
		{
			script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectAreaTargetSelect);

			if (script.Item2 is ISpellObjectAreaTargetSelect oas)
				if (targetType.Target == oas.TargetType)
					oas.FilterTargets(targets);

			script.Item1._FinishScriptCall();
		}
	}

	void CallScriptObjectTargetSelectHandlers(ref WorldObject target, int effIndex, SpellImplicitTargetInfo targetType)
	{
		foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex))
		{
			script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectTargetSelect);

			if (script.Item2 is ISpellObjectTargetSelectHandler ots)
				if (targetType.Target == ots.TargetType)
					ots.TargetSelect(target);

			script.Item1._FinishScriptCall();
		}
	}

	void CallScriptDestinationTargetSelectHandlers(ref SpellDestination target, int effIndex, SpellImplicitTargetInfo targetType)
	{
		foreach (var script in GetEffectScripts(SpellScriptHookType.DestinationTargetSelect, effIndex))
		{
			script.Item1._PrepareScriptCall(SpellScriptHookType.DestinationTargetSelect);

			if (script.Item2 is ISpellDestinationTargetSelectHandler dts)
				if (targetType.Target == dts.TargetType)
					dts.SetDest(target);

			script.Item1._FinishScriptCall();
		}
	}

	bool CheckScriptEffectImplicitTargets(int effIndex, int effIndexToCheck)
	{
		// Skip if there are not any script
		if (_loadedScripts.Empty())
			return true;

		var otsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex).Count > 0;
		var otsEffIndexCheck = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndexToCheck).Count > 0;

		var oatsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex).Count > 0;
		var oatsEffIndexCheck = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndexToCheck).Count > 0;

		if ((otsTargetEffIndex && !otsEffIndexCheck) ||
			(!otsTargetEffIndex && otsEffIndexCheck))
			return false;

		if ((oatsTargetEffIndex && !oatsEffIndexCheck) ||
			(!oatsTargetEffIndex && oatsEffIndexCheck))
			return false;

		return true;
	}

	void PrepareTriggersExecutedOnHit()
	{
		var unitCaster = _caster.AsUnit;

		if (unitCaster == null)
			return;

		// handle SPELL_AURA_ADD_TARGET_TRIGGER auras:
		// save auras which were present on spell caster on cast, to prevent triggered auras from affecting caster
		// and to correctly calculate proc chance when combopoints are present
		var targetTriggers = unitCaster.GetAuraEffectsByType(AuraType.AddTargetTrigger);

		foreach (var aurEff in targetTriggers)
		{
			if (!aurEff.IsAffectingSpell(SpellInfo))
				continue;

			var spellInfo = Global.SpellMgr.GetSpellInfo(aurEff.GetSpellEffectInfo().TriggerSpell, GetCastDifficulty());

			if (spellInfo != null)
			{
				// calculate the chance using spell base amount, because aura amount is not updated on combo-points change
				// this possibly needs fixing
				var auraBaseAmount = aurEff.BaseAmount;
				// proc chance is stored in effect amount
				var chance = unitCaster.CalculateSpellDamage(null, aurEff.GetSpellEffectInfo(), auraBaseAmount);
				chance *= aurEff.Base.StackAmount;

				// build trigger and add to the list
				_hitTriggerSpells.Add(new HitTriggerSpell(spellInfo, aurEff.SpellInfo, chance));
			}
		}
	}

	bool CanHaveGlobalCooldown(WorldObject caster)
	{
		// Only players or controlled units have global cooldown
		if (!caster.IsPlayer && (!caster.IsCreature || caster.AsCreature.GetCharmInfo() == null))
			return false;

		return true;
	}

	bool HasGlobalCooldown()
	{
		if (!CanHaveGlobalCooldown(_caster))
			return false;

		return _caster.AsUnit.GetSpellHistory().HasGlobalCooldown(SpellInfo);
	}

	void TriggerGlobalCooldown()
	{
		if (!CanHaveGlobalCooldown(_caster))
			return;

		var gcd = TimeSpan.FromMilliseconds(SpellInfo.StartRecoveryTime);

		if (gcd == TimeSpan.Zero || SpellInfo.StartRecoveryCategory == 0)
			return;

		if (_caster.IsTypeId(TypeId.Player))
			if (_caster.AsPlayer.GetCommandStatus(PlayerCommandStates.Cooldown))
				return;

		var MinGCD = TimeSpan.FromMilliseconds(750);
		var MaxGCD = TimeSpan.FromMilliseconds(1500);

		// Global cooldown can't leave range 1..1.5 secs
		// There are some spells (mostly not casted directly by player) that have < 1 sec and > 1.5 sec global cooldowns
		// but as tests show are not affected by any spell mods.
		if (gcd >= MinGCD && gcd <= MaxGCD)
		{
			// gcd modifier auras are applied only to own spells and only players have such mods
			var modOwner = _caster.GetSpellModOwner();

			if (modOwner)
			{
				var intGcd = (int)gcd.TotalMilliseconds;
				modOwner.ApplySpellMod(SpellInfo, SpellModOp.StartCooldown, ref intGcd, this);
				gcd = TimeSpan.FromMilliseconds(intGcd);
			}

			var isMeleeOrRangedSpell = SpellInfo.DmgClass == SpellDmgClass.Melee ||
										SpellInfo.DmgClass == SpellDmgClass.Ranged ||
										SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
										SpellInfo.HasAttribute(SpellAttr0.IsAbility);

			// Apply haste rating
			if (gcd > MinGCD && (SpellInfo.StartRecoveryCategory == 133 && !isMeleeOrRangedSpell))
			{
				gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * _caster.AsUnit.UnitData.ModSpellHaste);
				var intGcd = (int)gcd.TotalMilliseconds;
				MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
				gcd = TimeSpan.FromMilliseconds(intGcd);
			}

			if (gcd > MinGCD && _caster.AsUnit.HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHasteRegen, SpellInfo))
			{
				gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * _caster.AsUnit.UnitData.ModHasteRegen);
				var intGcd = (int)gcd.TotalMilliseconds;
				MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
				gcd = TimeSpan.FromMilliseconds(intGcd);
			}
		}

		_caster.
		AsUnit.GetSpellHistory().AddGlobalCooldown(SpellInfo, gcd);
	}

	void CancelGlobalCooldown()
	{
		if (!CanHaveGlobalCooldown(_caster))
			return;

		if (SpellInfo.StartRecoveryTime == 0)
			return;

		// Cancel global cooldown when interrupting current cast
		if (_caster.AsUnit.GetCurrentSpell(CurrentSpellTypes.Generic) != this)
			return;

		_caster.
		AsUnit.GetSpellHistory().CancelGlobalCooldown(SpellInfo);
	}

	string GetDebugInfo()
	{
		return $"Id: {SpellInfo.Id} Name: '{SpellInfo.SpellName[Global.WorldMgr.GetDefaultDbcLocale()]}' OriginalCaster: {_originalCasterGuid} State: {State}";
	}


	private void AddSpellEffect(int index, ISpellScript script, ISpellEffect effect)
	{
		if (!_effectHandlers.TryGetValue(index, out var effecTypes))
		{
			effecTypes = new Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>();
			_effectHandlers.Add(index, effecTypes);
		}

		if (!effecTypes.TryGetValue(effect.HookType, out var effects))
		{
			effects = new List<(ISpellScript, ISpellEffect)>();
			effecTypes.Add(effect.HookType, effects);
		}

		effects.Add((script, effect));
	}

	double CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target)
	{
		return CalculateDamage(spellEffectInfo, target, out _);
	}

	double CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target, out double variance)
	{
		var needRecalculateBasePoints = (SpellValue.CustomBasePointsMask & (1 << spellEffectInfo.EffectIndex)) == 0;

		return _caster.CalculateSpellDamage(out variance, target, spellEffectInfo, needRecalculateBasePoints ? null : SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex], CastItemEntry, CastItemLevel);
	}

	void CheckSrc()
	{
		if (!Targets.HasSrc) Targets.SetSrc(_caster);
	}

	void CheckDst()
	{
		if (!Targets.HasDst) Targets.SetDst(_caster);
	}

	void ReSetTimer()
	{
		_timer = _casttime > 0 ? _casttime : 0;
	}

	void SetExecutedCurrently(bool yes)
	{
		_executedCurrently = yes;
	}

	bool IsDelayableNoMore()
	{
		if (_delayAtDamageCount >= 2)
			return true;

		++_delayAtDamageCount;

		return false;
	}
}

// Spell modifier (used for modify other spells)
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Common.Networking.Packets.Spell;

namespace Game.Spells;

public class AuraApplication
{
	readonly Unit _target;
	readonly Aura _base;
	readonly byte _slot; // Aura slot on unit
	readonly HashSet<int> _effectMask = new();
	AuraFlags _flags;                     // Aura info flag
	HashSet<int> _effectsToApply = new(); // Used only at spell hit to determine which effect should be applied
	bool _needClientUpdate;

	public Guid Guid { get; } = Guid.NewGuid();

	public Unit Target => _target;

	public Aura Base => _base;

	public byte Slot => _slot;

	public AuraFlags Flags => _flags;

	public HashSet<int> EffectMask => _effectMask;

	public bool IsPositive => _flags.HasAnyFlag(AuraFlags.Positive);

	public HashSet<int> EffectsToApply => _effectsToApply;

	public AuraRemoveMode RemoveMode { get; set; } // Store info for know remove aura reason

	public bool HasRemoveMode => RemoveMode != 0;

	public bool IsNeedClientUpdate => _needClientUpdate;

	private bool IsSelfcasted => _flags.HasAnyFlag(AuraFlags.NoCaster);

	public AuraApplication(Unit target, Unit caster, Aura aura, HashSet<int> effMask)
	{
		_target = target;
		_base = aura;
		RemoveMode = AuraRemoveMode.None;
		_slot = SpellConst.MaxAuras;
		_flags = AuraFlags.None;
		_effectsToApply = effMask;
		_needClientUpdate = false;

		// Try find slot for aura
		byte slot = 0;

		// Lookup for auras already applied from spell
		foreach (var visibleAura in Target.VisibleAuras)
		{
			if (slot < visibleAura.Slot)
				break;

			++slot;
		}

		// Register Visible Aura
		if (slot < SpellConst.MaxAuras)
		{
			_slot = slot;
			Target.SetVisibleAura(this);
			_needClientUpdate = true;
			Log.Logger.Debug("Aura: {0} Effect: {1} put to unit visible auras slot: {2}", Base.Id, EffectMask, slot);
		}
		else
		{
			Log.Logger.Error("Aura: {0} Effect: {1} could not find empty unit visible slot", Base.Id, EffectMask);
		}


		_InitFlags(caster, effMask);
	}

	public void _Remove()
	{
		// update for out of range group members
		if (Slot < SpellConst.MaxAuras)
		{
			Target.RemoveVisibleAura(this);
			ClientUpdate(true);
		}
	}

	public void _HandleEffect(int effIndex, bool apply)
	{
		var aurEff = Base.GetEffect(effIndex);

		if (aurEff == null)
		{
			Log.Logger.Error("Aura {0} has no effect at effectIndex {1} but _HandleEffect was called", Base.SpellInfo.Id, effIndex);

			return;
		}

		if (HasEffect(effIndex) != (!apply))
		{
			Log.Logger.Error("Aura {0} has effect at effectIndex {1}(has effect: {2}) but _HandleEffect with {3} was called", Base.SpellInfo.Id, effIndex, HasEffect(effIndex), apply);

			return;
		}

		Log.Logger.Debug("AuraApplication._HandleEffect: {0}, apply: {1}: amount: {2}", aurEff.AuraType, apply, aurEff.Amount);

		if (apply)
		{
			_effectMask.Add(effIndex);
			aurEff.HandleEffect(this, AuraEffectHandleModes.Real, true);
		}
		else
		{
			_effectMask.Remove(effIndex);
			aurEff.HandleEffect(this, AuraEffectHandleModes.Real, false);
		}

		SetNeedClientUpdate();
	}

	public void UpdateApplyEffectMask(HashSet<int> newEffMask, bool canHandleNewEffects)
	{
		if (_effectsToApply.SetEquals(newEffMask))
			return;

		var toAdd = newEffMask.ToHashSet();
		var toRemove = _effectsToApply.ToHashSet();

		toAdd.SymmetricExceptWith(_effectsToApply);
		toRemove.SymmetricExceptWith(newEffMask);

		toAdd.ExceptWith(_effectsToApply);
		toRemove.ExceptWith(newEffMask);

		// quick check, removes application completely
		if (toAdd.SetEquals(toRemove) && toAdd.Count == 0)
		{
			_target._UnapplyAura(this, AuraRemoveMode.Default);

			return;
		}

		// update real effects only if they were applied already
		_effectsToApply = newEffMask;

		foreach (var eff in Base.AuraEffects)
		{
			if (HasEffect(eff.Key) && toRemove.Contains(eff.Key))
				_HandleEffect(eff.Key, false);

			if (canHandleNewEffects)
				if (toAdd.Contains(eff.Key))
					_HandleEffect(eff.Key, true);
		}
	}

	public void SetNeedClientUpdate()
	{
		if (_needClientUpdate || RemoveMode != AuraRemoveMode.None)
			return;

		_needClientUpdate = true;
		_target.SetVisibleAuraUpdate(this);
	}

	public void BuildUpdatePacket(ref AuraInfo auraInfo, bool remove)
	{
		auraInfo.Slot = Slot;

		if (remove)
			return;

		auraInfo.AuraData = new AuraDataInfo();

		var aura = Base;

		var auraData = auraInfo.AuraData;
		auraData.CastID = aura.CastId;
		auraData.SpellID = (int)aura.Id;
		auraData.Visual = aura.SpellVisual;
		auraData.Flags = Flags;

		if (aura.AuraObjType != AuraObjectType.DynObj && aura.MaxDuration > 0 && !aura.SpellInfo.HasAttribute(SpellAttr5.DoNotDisplayDuration))
			auraData.Flags |= AuraFlags.Duration;

		auraData.ActiveFlags = EffectMask;

		if (!aura.SpellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel))
			auraData.CastLevel = aura.CasterLevel;
		else
			auraData.CastLevel = (ushort)aura.CastItemLevel;

		// send stack amount for aura which could be stacked (never 0 - causes incorrect display) or charges
		// stack amount has priority over charges (checked on retail with spell 50262)
		auraData.Applications = aura.IsUsingStacks() ? aura.StackAmount : aura.Charges;

		if (!aura.CasterGuid.IsUnit)
			auraData.CastUnit = ObjectGuid.Empty; // optional data is filled in, but cast unit contains empty guid in packet
		else if (!auraData.Flags.HasFlag(AuraFlags.NoCaster))
			auraData.CastUnit = aura.CasterGuid;

		if (auraData.Flags.HasFlag(AuraFlags.Duration))
		{
			auraData.Duration = aura.MaxDuration;
			auraData.Remaining = aura.Duration;
		}

		if (!auraData.Flags.HasFlag(AuraFlags.Scalable))
			return;

		var hasEstimatedAmounts = false;

		foreach (var effect in Base.AuraEffects)
		{
			if (!HasEffect(effect.Value.EffIndex))
				continue;

			auraData.Points.Add(effect.Value.Amount);

			if (effect.Value.GetEstimatedAmount().HasValue)
				hasEstimatedAmounts = true;
		}

		if (hasEstimatedAmounts)
			foreach (var effect in Base.AuraEffects)
				if (HasEffect(effect.Value.EffIndex))
					auraData.EstimatedPoints.Add(effect.Value.GetEstimatedAmount().GetValueOrDefault(effect.Value.Amount));
	}

	public void ClientUpdate(bool remove = false)
	{
		_needClientUpdate = false;

		AuraUpdate update = new();
		update.UpdateAll = false;
		update.UnitGUID = Target.GUID;

		AuraInfo auraInfo = new();
		BuildUpdatePacket(ref auraInfo, remove);
		update.Auras.Add(auraInfo);

		_target.SendMessageToSet(update, true);
	}

	public string GetDebugInfo()
	{
		return $"Base: {(Base != null ? Base.GetDebugInfo() : "NULL")}\nTarget: {(Target != null ? Target.GetDebugInfo() : "NULL")}";
	}

	public bool HasEffect(int effect)
	{
		return _effectMask.Contains(effect);
	}

	void _InitFlags(Unit caster, HashSet<int> effMask)
	{
		// mark as selfcasted if needed
		_flags |= (Base.CasterGuid == Target.GUID) ? AuraFlags.NoCaster : AuraFlags.None;

		// aura is casted by self or an enemy
		// one negative effect and we know aura is negative
		if (IsSelfcasted || caster == null || !caster.IsFriendlyTo(Target))
		{
			var negativeFound = false;

			foreach (var spellEffectInfo in Base.SpellInfo.Effects)
				if (effMask.Contains(spellEffectInfo.EffectIndex) && !Base.SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
				{
					negativeFound = true;

					break;
				}

			_flags |= negativeFound ? AuraFlags.Negative : AuraFlags.Positive;
		}
		// aura is casted by friend
		// one positive effect and we know aura is positive
		else
		{
			var positiveFound = false;

			foreach (var spellEffectInfo in Base.SpellInfo.Effects)
				if (effMask.Contains(spellEffectInfo.EffectIndex) && Base.SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
				{
					positiveFound = true;

					break;
				}

			_flags |= positiveFound ? AuraFlags.Positive : AuraFlags.Negative;
		}

		bool effectNeedsAmount(KeyValuePair<int, AuraEffect> effect) => EffectsToApply.Contains(effect.Value.EffIndex) && Aura.EffectTypeNeedsSendingAmount(effect.Value.AuraType);

		if (Base.SpellInfo.HasAttribute(SpellAttr8.AuraSendAmount) || Base.AuraEffects.Any(effectNeedsAmount))
			_flags |= AuraFlags.Scalable;
	}
}
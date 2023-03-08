// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Spells;

public class AuraApplication
{
	readonly Unit _target;
	readonly Aura _base;
	readonly byte _slot;        // Aura slot on unit
	AuraFlags _flags;           // Aura info flag
	uint _effectsToApply;       // Used only at spell hit to determine which effect should be applied
	bool _needClientUpdate;
	uint _effectMask;

	public Guid Guid { get; } = Guid.NewGuid();
	public HashSet<int> EffectIndexs { get; } = new();

	public Unit Target => _target;

	public Aura Base => _base;

	public byte Slot => _slot;

	public AuraFlags Flags => _flags;

	public uint EffectMask => _effectMask;

	public bool IsPositive => _flags.HasAnyFlag(AuraFlags.Positive);

	public uint EffectsToApply => _effectsToApply;

	public AuraRemoveMode RemoveMode { get; set; }  // Store info for know remove aura reason

    public bool HasRemoveMode => RemoveMode != 0;

	public bool IsNeedClientUpdate => _needClientUpdate;

	private bool IsSelfcasted => _flags.HasAnyFlag(AuraFlags.NoCaster);

	public AuraApplication(Unit target, Unit caster, Aura aura, uint effMask)
	{
		_target = target;
		_base = aura;
		RemoveMode = AuraRemoveMode.None;
		_slot = SpellConst.MaxAuras;
		_flags = AuraFlags.None;
		_effectsToApply = effMask;
		_needClientUpdate = false;

		Cypher.Assert(Target != null && Base != null);

		// Try find slot for aura
		byte slot = 0;

		// Lookup for auras already applied from spell
		foreach (var visibleAura in Target.GetVisibleAuras())
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
			Log.outDebug(LogFilter.Spells, "Aura: {0} Effect: {1} put to unit visible auras slot: {2}", Base.Id, EffectMask, slot);
		}
		else
		{
			Log.outError(LogFilter.Spells, "Aura: {0} Effect: {1} could not find empty unit visible slot", Base.Id, EffectMask);
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
			Log.outError(LogFilter.Spells, "Aura {0} has no effect at effectIndex {1} but _HandleEffect was called", Base.SpellInfo.Id, effIndex);

			return;
		}

		Cypher.Assert(aurEff != null);
		Cypher.Assert(HasEffect(effIndex) == (!apply));
		Cypher.Assert(Convert.ToBoolean((1 << effIndex) & _effectsToApply));
		Log.outDebug(LogFilter.Spells, "AuraApplication._HandleEffect: {0}, apply: {1}: amount: {2}", aurEff.AuraType, apply, aurEff.Amount);

		if (apply)
		{
			Cypher.Assert(!Convert.ToBoolean(_effectMask & (1 << effIndex)));
			_effectMask |= (uint)(1 << effIndex);
			aurEff.HandleEffect(this, AuraEffectHandleModes.Real, true);
			EffectIndexs.Add(effIndex);
		}
		else
		{
			Cypher.Assert(Convert.ToBoolean(_effectMask & (1 << effIndex)));
			_effectMask &= ~(uint)(1 << effIndex);
			aurEff.HandleEffect(this, AuraEffectHandleModes.Real, false);
			EffectIndexs.Remove(effIndex);
		}

		SetNeedClientUpdate();
	}

	public void UpdateApplyEffectMask(uint newEffMask, bool canHandleNewEffects)
	{
		if (_effectsToApply == newEffMask)
			return;

		var removeEffMask = (_effectsToApply ^ newEffMask) & (~newEffMask);
		var addEffMask = (_effectsToApply ^ newEffMask) & (~_effectsToApply);

		// quick check, removes application completely
		if (removeEffMask == _effectsToApply && addEffMask == 0)
		{
			_target._UnapplyAura(this, AuraRemoveMode.Default);

			return;
		}

		// update real effects only if they were applied already

		foreach (var eff in Base.AuraEffects)
		{
			if (HasEffect(eff.Key) && (removeEffMask & (1 << eff.Key)) != 0)
				_HandleEffect(eff.Key, false);

			if (canHandleNewEffects)
				if ((addEffMask & (1 << eff.Key)) != 0)
					_HandleEffect(eff.Key, true);
		}

		_effectsToApply = newEffMask;
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
		Cypher.Assert(_target.HasVisibleAura(this) != remove);

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

		if (!aura.CasterGuid.IsUnit())
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
		update.UnitGUID = Target.GetGUID();

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
		return Convert.ToBoolean(_effectMask & (1 << effect));
	}

	void _InitFlags(Unit caster, uint effMask)
	{
		// mark as selfcasted if needed
		_flags |= (Base.CasterGuid == Target.GetGUID()) ? AuraFlags.NoCaster : AuraFlags.None;

		// aura is casted by self or an enemy
		// one negative effect and we know aura is negative
		if (IsSelfcasted || caster == null || !caster.IsFriendlyTo(Target))
		{
			var negativeFound = false;

			foreach (var spellEffectInfo in Base.SpellInfo.Effects)
				if (((1 << spellEffectInfo.EffectIndex) & effMask) != 0 && !Base.SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
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
				if (((1 << spellEffectInfo.EffectIndex) & effMask) != 0 && Base.SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
				{
					positiveFound = true;

					break;
				}

			_flags |= positiveFound ? AuraFlags.Positive : AuraFlags.Negative;
		}

		bool effectNeedsAmount(KeyValuePair<int, AuraEffect> effect) => (EffectsToApply & (1 << effect.Value.EffIndex)) != 0 && Aura.EffectTypeNeedsSendingAmount(effect.Value.AuraType);

		if (Base.SpellInfo.HasAttribute(SpellAttr8.AuraSendAmount) || Base.AuraEffects.Any(effectNeedsAmount))
			_flags |= AuraFlags.Scalable;
	}
}
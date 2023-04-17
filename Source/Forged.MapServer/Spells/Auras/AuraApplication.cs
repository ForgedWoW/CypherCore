// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Spells.Auras;

public class AuraApplication
{
    public AuraApplication(Unit target, Unit caster, Aura aura, HashSet<int> effMask)
    {
        Target = target;
        Base = aura;
        RemoveMode = AuraRemoveMode.None;
        Slot = SpellConst.MaxAuras;
        Flags = AuraFlags.None;
        EffectsToApply = effMask;
        IsNeedClientUpdate = false;

        // Try find slot for aura
        byte slot = 0;

        // Lookup for auras already applied from spell
        foreach (var _ in Target.VisibleAuras.TakeWhile(visibleAura => slot >= visibleAura.Slot))
        {
            ++slot;
        }

        // Register Visible Aura
        if (slot < SpellConst.MaxAuras)
        {
            Slot = slot;
            Target.SetVisibleAura(this);
            IsNeedClientUpdate = true;
            Log.Logger.Debug("Aura: {0} Effect: {1} put to unit visible auras slot: {2}", Base.Id, EffectMask, slot);
        }
        else
            Log.Logger.Error("Aura: {0} Effect: {1} could not find empty unit visible slot", Base.Id, EffectMask);


        _InitFlags(caster, effMask);
    }

    public Aura Base { get; }
    public HashSet<int> EffectMask { get; } = new();
    public HashSet<int> EffectsToApply { get; private set; }
    public AuraFlags Flags { get; private set; }
    public Guid Guid { get; } = Guid.NewGuid();

    public bool HasRemoveMode => RemoveMode != 0;
    public bool IsNeedClientUpdate { get; private set; }
    public bool IsPositive => Flags.HasAnyFlag(AuraFlags.Positive);
    public AuraRemoveMode RemoveMode { get; set; }
    public byte Slot { get; }

    public Unit Target { get; }

    // Store info for know remove aura reason
    private bool IsSelfcasted => Flags.HasAnyFlag(AuraFlags.NoCaster);

    public void _HandleEffect(int effIndex, bool apply)
    {
        var aurEff = Base.GetEffect(effIndex);

        if (aurEff == null)
        {
            Log.Logger.Error("Aura {0} has no effect at effectIndex {1} but _HandleEffect was called", Base.SpellInfo.Id, effIndex);

            return;
        }

        if (HasEffect(effIndex) != !apply)
        {
            Log.Logger.Error("Aura {0} has effect at effectIndex {1}(has effect: {2}) but _HandleEffect with {3} was called", Base.SpellInfo.Id, effIndex, HasEffect(effIndex), apply);

            return;
        }

        Log.Logger.Debug("AuraApplication._HandleEffect: {0}, apply: {1}: amount: {2}", aurEff.AuraType, apply, aurEff.Amount);

        if (apply)
        {
            EffectMask.Add(effIndex);
            aurEff.HandleEffect(this, AuraEffectHandleModes.Real, true);
        }
        else
        {
            EffectMask.Remove(effIndex);
            aurEff.HandleEffect(this, AuraEffectHandleModes.Real, false);
        }

        SetNeedClientUpdate();
    }

    public void BuildUpdatePacket(ref AuraInfo auraInfo, bool remove)
    {
        auraInfo.Slot = Slot;

        if (remove)
            return;

        auraInfo.AuraData = new AuraDataInfo();

        var auraData = auraInfo.AuraData;
        auraData.CastID = Base.CastId;
        auraData.SpellID = (int)Base.Id;
        auraData.Visual = Base.SpellVisual;
        auraData.Flags = Flags;

        if (Base.AuraObjType != AuraObjectType.DynObj && Base.MaxDuration > 0 && !Base.SpellInfo.HasAttribute(SpellAttr5.DoNotDisplayDuration))
            auraData.Flags |= AuraFlags.Duration;

        auraData.ActiveFlags = EffectMask;

        if (!Base.SpellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel))
            auraData.CastLevel = Base.CasterLevel;
        else
            auraData.CastLevel = (ushort)Base.CastItemLevel;

        // send stack amount for aura which could be stacked (never 0 - causes incorrect display) or charges
        // stack amount has priority over charges (checked on retail with spell 50262)
        auraData.Applications = Base.IsUsingStacks() ? Base.StackAmount : Base.Charges;

        if (!Base.CasterGuid.IsUnit)
            auraData.CastUnit = ObjectGuid.Empty; // optional data is filled in, but cast unit contains empty guid in packet
        else if (!auraData.Flags.HasFlag(AuraFlags.NoCaster))
            auraData.CastUnit = Base.CasterGuid;

        if (auraData.Flags.HasFlag(AuraFlags.Duration))
        {
            auraData.Duration = Base.MaxDuration;
            auraData.Remaining = Base.Duration;
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

        if (!hasEstimatedAmounts)
            return;

        foreach (var effect in Base.AuraEffects.Where(effect => HasEffect(effect.Value.EffIndex)))
            auraData.EstimatedPoints.Add(effect.Value.GetEstimatedAmount().GetValueOrDefault(effect.Value.Amount));
    }

    public void ClientUpdate(bool remove = false)
    {
        IsNeedClientUpdate = false;

        AuraUpdate update = new()
        {
            UpdateAll = false,
            UnitGUID = Target.GUID
        };

        AuraInfo auraInfo = new();
        BuildUpdatePacket(ref auraInfo, remove);
        update.Auras.Add(auraInfo);

        Target.SendMessageToSet(update, true);
    }

    public string GetDebugInfo()
    {
        return $"Base: {(Base != null ? Base.GetDebugInfo() : "NULL")}\nTarget: {(Target != null ? Target.GetDebugInfo() : "NULL")}";
    }

    public bool HasEffect(int effect)
    {
        return EffectMask.Contains(effect);
    }

    public void Remove()
    {
        // update for out of range group members
        if (Slot >= SpellConst.MaxAuras)
            return;

        Target.RemoveVisibleAura(this);
        ClientUpdate(true);
    }
    public void SetNeedClientUpdate()
    {
        if (IsNeedClientUpdate || RemoveMode != AuraRemoveMode.None)
            return;

        IsNeedClientUpdate = true;
        Target.SetVisibleAuraUpdate(this);
    }

    public void UpdateApplyEffectMask(HashSet<int> newEffMask, bool canHandleNewEffects)
    {
        if (EffectsToApply.SetEquals(newEffMask))
            return;

        var toAdd = newEffMask.ToHashSet();
        var toRemove = EffectsToApply.ToHashSet();

        toAdd.SymmetricExceptWith(EffectsToApply);
        toRemove.SymmetricExceptWith(newEffMask);

        toAdd.ExceptWith(EffectsToApply);
        toRemove.ExceptWith(newEffMask);

        // quick check, removes application completely
        if (toAdd.SetEquals(toRemove) && toAdd.Count == 0)
        {
            Target.UnapplyAura(this, AuraRemoveMode.Default);

            return;
        }

        // update real effects only if they were applied already
        EffectsToApply = newEffMask;

        foreach (var eff in Base.AuraEffects)
        {
            if (HasEffect(eff.Key) && toRemove.Contains(eff.Key))
                _HandleEffect(eff.Key, false);

            if (!canHandleNewEffects)
                continue;

            if (toAdd.Contains(eff.Key))
                _HandleEffect(eff.Key, true);
        }
    }

    private void _InitFlags(Unit caster, HashSet<int> effMask)
    {
        // mark as selfcasted if needed
        Flags |= Base.CasterGuid == Target.GUID ? AuraFlags.NoCaster : AuraFlags.None;

        // aura is casted by self or an enemy
        // one negative effect and we know aura is negative
        if (IsSelfcasted || caster == null || !caster.WorldObjectCombat.IsFriendlyTo(Target))
        {
            var negativeFound = false;

            foreach (var spellEffectInfo in Base.SpellInfo.Effects)
                if (effMask.Contains(spellEffectInfo.EffectIndex) && !Base.SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
                {
                    negativeFound = true;

                    break;
                }

            Flags |= negativeFound ? AuraFlags.Negative : AuraFlags.Positive;
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

            Flags |= positiveFound ? AuraFlags.Positive : AuraFlags.Negative;
        }

        bool EffectNeedsAmount(KeyValuePair<int, AuraEffect> effect) => EffectsToApply.Contains(effect.Value.EffIndex) && Aura.EffectTypeNeedsSendingAmount(effect.Value.AuraType);

        if (Base.SpellInfo.HasAttribute(SpellAttr8.AuraSendAmount) || Base.AuraEffects.Any(EffectNeedsAmount))
            Flags |= AuraFlags.Scalable;
    }
}
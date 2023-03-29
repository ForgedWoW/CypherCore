// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Combat;

public class ThreatReference : IComparable<ThreatReference>
{
    public ThreatManager _mgr;
    public OnlineState Online;
    public int TempModifier; // Temporary effects (auras with SPELL_AURA_MOD_TOTAL_THREAT) - set from victim's threatmanager in ThreatManager::UpdateMyTempModifiers
    private readonly Creature _owner;
    private readonly Unit _victim;
    private double _baseAmount;
    private TauntState _taunted;

    public bool ShouldBeOffline
    {
        get
        {
            if (!_owner.CanSeeOrDetect(_victim))
                return true;

            if (!_owner._IsTargetAcceptable(_victim) || !_owner.CanCreatureAttack(_victim))
                return true;

            if (!FlagsAllowFighting(_owner, _victim) || !FlagsAllowFighting(_victim, _owner))
                return true;

            return false;
        }
    }

    public bool ShouldBeSuppressed
    {
        get
        {
            if (IsTaunting) // a taunting victim can never be suppressed
                return false;

            if (_victim.IsImmunedToDamage(_owner.GetMeleeDamageSchoolMask()))
                return true;

            if (_victim.HasAuraType(AuraType.ModConfuse))
                return true;

            if (_victim.HasBreakableByDamageAuraType(AuraType.ModStun))
                return true;

            return false;
        }
    }

    public Creature Owner => _owner;

    public Unit Victim => _victim;

    public double Threat => Math.Max(_baseAmount + TempModifier, 0.0f);

    public OnlineState OnlineState => Online;

    public bool IsOnline => Online >= OnlineState.Online;

    public bool IsAvailable => Online > OnlineState.Offline;

    public bool IsSuppressed => Online == OnlineState.Suppressed;

    public bool IsOffline => Online <= OnlineState.Offline;

    public TauntState TauntState => IsTaunting ? TauntState.Taunt : _taunted;

    public bool IsTaunting => _taunted >= TauntState.Taunt;

    public bool IsDetaunted => _taunted == TauntState.Detaunt;

    public ThreatReference(ThreatManager mgr, Unit victim)
    {
        _owner = mgr._owner as Creature;
        _mgr = mgr;
        _victim = victim;
        Online = OnlineState.Offline;
    }

    public int CompareTo(ThreatReference other)
    {
        return ThreatManager.CompareReferencesLT(this, other, 1.0f) ? 1 : -1;
    }

    public void AddThreat(double amount)
    {
        if (amount == 0.0f)
            return;

        _baseAmount = Math.Max(_baseAmount + amount, 0.0f);
        ListNotifyChanged();
        _mgr.NeedClientUpdate = true;
    }

    public void ScaleThreat(double factor)
    {
        if (factor == 1.0f)
            return;

        _baseAmount *= factor;
        ListNotifyChanged();
        _mgr.NeedClientUpdate = true;
    }

    public void UpdateOffline()
    {
        var shouldBeOffline = ShouldBeOffline;

        if (shouldBeOffline == IsOffline)
            return;

        if (shouldBeOffline)
        {
            Online = OnlineState.Offline;
            ListNotifyChanged();
            _mgr.SendRemoveToClients(_victim);
        }
        else
        {
            Online = ShouldBeSuppressed ? OnlineState.Suppressed : OnlineState.Online;
            ListNotifyChanged();
            _mgr.RegisterForAIUpdate(this);
        }
    }

    public static bool FlagsAllowFighting(Unit a, Unit b)
    {
        if (a.IsCreature && a.AsCreature.IsTrigger)
            return false;

        if (a.HasUnitFlag(UnitFlags.PlayerControlled))
        {
            if (b.HasUnitFlag(UnitFlags.ImmuneToPc))
                return false;
        }
        else
        {
            if (b.HasUnitFlag(UnitFlags.ImmuneToNpc))
                return false;
        }

        return true;
    }

    public void UpdateTauntState(TauntState state = TauntState.None)
    {
        // Check for SPELL_AURA_MOD_DETAUNT (applied from owner to victim)
        if (state < TauntState.Taunt && _victim.HasAuraTypeWithCaster(AuraType.ModDetaunt, _owner.GUID))
            state = TauntState.Detaunt;

        if (state == _taunted)
            return;

        Extensions.Swap(ref state, ref _taunted);

        ListNotifyChanged();
        _mgr.NeedClientUpdate = true;
    }

    public void ClearThreat()
    {
        _mgr.ClearThreat(this);
    }

    public void UnregisterAndFree()
    {
        _owner.GetThreatManager().PurgeThreatListRef(_victim.GUID);
        _victim.GetThreatManager().PurgeThreatenedByMeRef(_owner.GUID);
    }

    public void SetThreat(float amount)
    {
        _baseAmount = amount;
        ListNotifyChanged();
    }

    public void ModifyThreatByPercent(int percent)
    {
        if (percent != 0)
            ScaleThreat(0.01f * (100f + percent));
    }

    public void ListNotifyChanged()
    {
        _mgr.ListNotifyChanged();
    }
}
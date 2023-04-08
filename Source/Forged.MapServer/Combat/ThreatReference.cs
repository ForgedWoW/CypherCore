// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Combat;

public class ThreatReference : IComparable<ThreatReference>
{
    public ThreatManager ThreatManager;
    public OnlineState Online;
    public int TempModifier; // Temporary effects (auras with SPELL_AURA_MOD_TOTAL_THREAT) - set from victim's threatmanager in ThreatManager::UpdateMyTempModifiers
    private double _baseAmount;
    private TauntState _taunted;

    public ThreatReference(ThreatManager threatManager, Unit victim)
    {
        Owner = threatManager._owner as Creature;
        ThreatManager = threatManager;
        Victim = victim;
        Online = OnlineState.Offline;
    }

    public bool IsAvailable => Online > OnlineState.Offline;

    public bool IsDetaunted => _taunted == TauntState.Detaunt;

    public bool IsOffline => Online <= OnlineState.Offline;

    public bool IsOnline => Online >= OnlineState.Online;

    public bool IsSuppressed => Online == OnlineState.Suppressed;

    public bool IsTaunting => _taunted >= TauntState.Taunt;

    public OnlineState OnlineState => Online;

    public Creature Owner { get; }

    public bool ShouldBeOffline
    {
        get
        {
            if (!Owner.Visibility.CanSeeOrDetect(Victim))
                return true;

            if (!Owner._IsTargetAcceptable(Victim) || !Owner.CanCreatureAttack(Victim))
                return true;

            return !FlagsAllowFighting(Owner, Victim) || !FlagsAllowFighting(Victim, Owner);
        }
    }

    public bool ShouldBeSuppressed
    {
        get
        {
            if (IsTaunting) // a taunting victim can never be suppressed
                return false;

            if (Victim.IsImmunedToDamage(Owner.GetMeleeDamageSchoolMask()))
                return true;

            return Victim.HasAuraType(AuraType.ModConfuse) || Victim.HasBreakableByDamageAuraType(AuraType.ModStun);
        }
    }
    public TauntState TauntState => IsTaunting ? TauntState.Taunt : _taunted;
    public double Threat => Math.Max(_baseAmount + TempModifier, 0.0f);
    public Unit Victim { get; }
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

    public void AddThreat(double amount)
    {
        if (amount == 0.0f)
            return;

        _baseAmount = Math.Max(_baseAmount + amount, 0.0f);
        ListNotifyChanged();
        ThreatManager.NeedClientUpdate = true;
    }

    public void ClearThreat()
    {
        ThreatManager.ClearThreat(this);
    }

    public int CompareTo(ThreatReference other)
    {
        return ThreatManager.CompareReferencesLT(this, other, 1.0f) ? 1 : -1;
    }
    public void ListNotifyChanged()
    {
        ThreatManager.ListNotifyChanged();
    }

    public void ModifyThreatByPercent(int percent)
    {
        if (percent != 0)
            ScaleThreat(0.01f * (100f + percent));
    }

    public void ScaleThreat(double factor)
    {
        if (factor == 1.0f)
            return;

        _baseAmount *= factor;
        ListNotifyChanged();
        ThreatManager.NeedClientUpdate = true;
    }

    public void SetThreat(float amount)
    {
        _baseAmount = amount;
        ListNotifyChanged();
    }

    public void UnregisterAndFree()
    {
        Owner.GetThreatManager().PurgeThreatListRef(Victim.GUID);
        Victim.GetThreatManager().PurgeThreatenedByMeRef(Owner.GUID);
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
            ThreatManager.SendRemoveToClients(Victim);
        }
        else
        {
            Online = ShouldBeSuppressed ? OnlineState.Suppressed : OnlineState.Online;
            ListNotifyChanged();
            ThreatManager.RegisterForAIUpdate(this);
        }
    }
    public void UpdateTauntState(TauntState state = TauntState.None)
    {
        // Check for SPELL_AURA_MOD_DETAUNT (applied from owner to victim)
        if (state < TauntState.Taunt && Victim.HasAuraTypeWithCaster(AuraType.ModDetaunt, Owner.GUID))
            state = TauntState.Detaunt;

        if (state == _taunted)
            return;

        Extensions.Swap(ref state, ref _taunted);

        ListNotifyChanged();
        ThreatManager.NeedClientUpdate = true;
    }
}
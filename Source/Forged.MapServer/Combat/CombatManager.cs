// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Combat;

public class CombatManager
{
    public CombatManager(Unit owner)
    {
        Owner = owner;
    }

    public bool HasCombat => HasPvECombat() || HasPvPCombat();
    public Unit Owner { get; }
    public Dictionary<ObjectGuid, CombatReference> PvECombatRefs { get; } = new();

    public Dictionary<ObjectGuid, PvPCombatReference> PvPCombatRefs { get; } = new();
    public static bool CanBeginCombat(Unit a, Unit b)
    {
        // Checks combat validity before initial reference creation.
        // For the combat to be valid...
        // ...the two units need to be different
        if (a == b)
            return false;

        // ...the two units need to be in the world
        if (!a.Location.IsInWorld || !b.Location.IsInWorld)
            return false;

        // ...the two units need to both be alive
        if (!a.IsAlive || !b.IsAlive)
            return false;

        // ...the two units need to be on the same map
        if (a.Location.Map != b.Location.Map)
            return false;

        // ...the two units need to be in the same phase
        if (!WorldLocation.InSamePhase(a, b))
            return false;

        if (a.HasUnitState(UnitState.Evade) || b.HasUnitState(UnitState.Evade))
            return false;

        if (a.HasUnitState(UnitState.InFlight) || b.HasUnitState(UnitState.InFlight))
            return false;

        // ... both units must be allowed to enter combat
        if (a.IsCombatDisallowed || b.IsCombatDisallowed)
            return false;

        if (a.WorldObjectCombat.IsFriendlyTo(b) || b.WorldObjectCombat.IsFriendlyTo(a))
            return false;

        var playerA = a.CharmerOrOwnerPlayerOrPlayerItself;
        var playerB = b.CharmerOrOwnerPlayerOrPlayerItself;

        // ...neither of the two units must be (owned by) a player with .gm on
        if ((playerA && playerA.IsGameMaster) || (playerB && playerB.IsGameMaster))
            return false;

        return true;
    }

    public static void NotifyAICombat(Unit me, Unit other)
    {
        var ai = me.AI;

        ai?.JustEnteredCombat(other);
    }

    public void EndAllCombat()
    {
        EndAllPvECombat();
        EndAllPvPCombat();
    }

    public void EndAllPvECombat()
    {
        // cannot have threat without combat
        Owner.GetThreatManager().RemoveMeFromThreatLists();
        Owner.GetThreatManager().ClearAllThreat();

        lock (PvECombatRefs)
        {
            while (!PvECombatRefs.Empty())
                PvECombatRefs.First().Value.EndCombat();
        }
    }

    public void EndCombatBeyondRange(float range, bool includingPvP)
    {
        lock (PvECombatRefs)
        {
            foreach (var pair in PvECombatRefs.ToList())
            {
                var refe = pair.Value;

                if (!refe.First.Location.IsWithinDistInMap(refe.Second, range))
                {
                    PvECombatRefs.Remove(pair.Key);
                    refe.EndCombat();
                }
            }

            if (!includingPvP)
                return;

            foreach (var pair in PvPCombatRefs.ToList())
            {
                CombatReference refe = pair.Value;

                if (!refe.First.Location.IsWithinDistInMap(refe.Second, range))
                {
                    PvPCombatRefs.Remove(pair.Key);
                    refe.EndCombat();
                }
            }
        }
    }

    public Unit GetAnyTarget()
    {
        lock (PvECombatRefs)
        {
            foreach (var pair in PvECombatRefs)
                if (!pair.Value.IsSuppressedFor(Owner))
                    return pair.Value.GetOther(Owner);

            foreach (var pair in PvPCombatRefs)
                if (!pair.Value.IsSuppressedFor(Owner))
                    return pair.Value.GetOther(Owner);
        }

        return null;
    }

    public bool HasPvECombat()
    {
        lock (PvECombatRefs)
        {
            foreach (var (_, refe) in PvECombatRefs)
                if (!refe.IsSuppressedFor(Owner))
                    return true;
        }

        return false;
    }

    public bool HasPvECombatWithPlayers()
    {
        lock (PvECombatRefs)
        {
            foreach (var reference in PvECombatRefs)
                if (!reference.Value.IsSuppressedFor(Owner) && reference.Value.GetOther(Owner).IsPlayer)
                    return true;
        }

        return false;
    }

    public bool HasPvPCombat()
    {
        lock (PvECombatRefs)
        {
            foreach (var pair in PvPCombatRefs)
                if (!pair.Value.IsSuppressedFor(Owner))
                    return true;
        }

        return false;
    }

    public void InheritCombatStatesFrom(Unit who)
    {
        var mgr = who.GetCombatManager();

        lock (PvECombatRefs)
        {
            foreach (var refe in mgr.PvECombatRefs)
                if (!IsInCombatWith(refe.Key))
                {
                    var target = refe.Value.GetOther(who);

                    if ((Owner.IsImmuneToPc() && target.HasUnitFlag(UnitFlags.PlayerControlled)) ||
                        (Owner.IsImmuneToNPC() && !target.HasUnitFlag(UnitFlags.PlayerControlled)))
                        continue;

                    SetInCombatWith(target);
                }

            foreach (var refe in mgr.PvPCombatRefs)
            {
                var target = refe.Value.GetOther(who);

                if ((Owner.IsImmuneToPc() && target.HasUnitFlag(UnitFlags.PlayerControlled)) ||
                    (Owner.IsImmuneToNPC() && !target.HasUnitFlag(UnitFlags.PlayerControlled)))
                    continue;

                SetInCombatWith(target);
            }
        }
    }

    public bool IsInCombatWith(ObjectGuid guid)
    {
        lock (PvECombatRefs)
        {
            return PvECombatRefs.ContainsKey(guid) || PvPCombatRefs.ContainsKey(guid);
        }
    }

    public bool IsInCombatWith(Unit who)
    {
        return IsInCombatWith(who.GUID);
    }

    public void PurgeReference(ObjectGuid guid, bool pvp)
    {
        lock (PvECombatRefs)
        {
            if (pvp)
                PvPCombatRefs.Remove(guid);
            else
                PvECombatRefs.Remove(guid);
        }
    }

    public void RevalidateCombat()
    {
        lock (PvECombatRefs)
        {
            foreach (var (guid, refe) in PvECombatRefs.ToList())
                if (!CanBeginCombat(Owner, refe.GetOther(Owner)))
                {
                    PvECombatRefs.Remove(guid); // erase manually here to avoid iterator invalidation
                    refe.EndCombat();
                }

            foreach (var (guid, refe) in PvPCombatRefs.ToList())
                if (!CanBeginCombat(Owner, refe.GetOther(Owner)))
                {
                    PvPCombatRefs.Remove(guid); // erase manually here to avoid iterator invalidation
                    refe.EndCombat();
                }
        }
    }

    public bool SetInCombatWith(Unit who, bool addSecondUnitSuppressed = false)
    {
        // Are we already in combat? If yes, refresh pvp combat
        lock (PvECombatRefs)
        {
            var existingPvpRef = PvPCombatRefs.LookupByKey(who.GUID);

            if (existingPvpRef != null)
            {
                existingPvpRef.RefreshTimer();
                existingPvpRef.Refresh();

                return true;
            }


            var existingPveRef = PvECombatRefs.LookupByKey(who.GUID);

            if (existingPveRef != null)
            {
                existingPveRef.Refresh();

                return true;
            }
        }

        // Otherwise, check validity...
        if (!CanBeginCombat(Owner, who))
            return false;

        // ...then create new reference
        CombatReference refe;

        if (Owner.ControlledByPlayer && who.ControlledByPlayer)
            refe = new PvPCombatReference(Owner, who);
        else
            refe = new CombatReference(Owner, who);

        if (addSecondUnitSuppressed)
            refe.Suppress(who);

        // ...and insert it into both managers
        PutReference(who.GUID, refe);
        who.GetCombatManager().PutReference(Owner.GUID, refe);

        // now, sequencing is important - first we update the combat state, which will set both units in combat and do non-AI combat start stuff
        var needSelfAI = UpdateOwnerCombatState();
        var needOtherAI = who.GetCombatManager().UpdateOwnerCombatState();

        // then, we finally notify the AI (if necessary) and let it safely do whatever it feels like
        if (needSelfAI)
            NotifyAICombat(Owner, who);

        if (needOtherAI)
            NotifyAICombat(who, Owner);

        return IsInCombatWith(who);
    }

    public void SuppressPvPCombat()
    {
        lock (PvECombatRefs)
        {
            foreach (var pair in PvPCombatRefs)
                pair.Value.Suppress(Owner);
        }

        if (UpdateOwnerCombatState())
        {
            var ownerAI = Owner.AI;

            ownerAI?.JustExitedCombat();
        }
    }

    public void Update(uint tdiff)
    {
        foreach (var pair in PvPCombatRefs.ToList())
        {
            var refe = pair.Value;

            if (refe.First == Owner && !refe.Update(tdiff)) // only update if we're the first unit involved (otherwise double decrement)
            {
                PvPCombatRefs.Remove(pair.Key);
                refe.EndCombat(); // this will remove it from the other side
            }
        }
    }
    public bool UpdateOwnerCombatState()
    {
        var combatState = HasCombat;

        if (combatState == Owner.IsInCombat)
            return false;

        if (combatState)
        {
            Owner.SetUnitFlag(UnitFlags.InCombat);
            Owner.AtEnterCombat();

            if (!Owner.IsCreature)
                Owner.AtEngage(GetAnyTarget());
        }
        else
        {
            Owner.RemoveUnitFlag(UnitFlags.InCombat);
            Owner.AtExitCombat();

            if (!Owner.IsCreature)
                Owner.AtDisengage();
        }

        var master = Owner.CharmerOrOwner;

        master?.UpdatePetCombatState();

        return true;
    }
    private void EndAllPvPCombat()
    {
        lock (PvECombatRefs)
        {
            while (!PvPCombatRefs.Empty())
                PvPCombatRefs.First().Value.EndCombat();
        }
    }

    private void PutReference(ObjectGuid guid, CombatReference refe)
    {
        lock (PvECombatRefs)
        {
            if (refe.IsPvP)
                PvPCombatRefs[guid] = (PvPCombatReference)refe;
            else
                PvECombatRefs[guid] = refe;
        }
    }
}
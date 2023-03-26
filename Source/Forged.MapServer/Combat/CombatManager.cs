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
    private readonly Unit _owner;
    private readonly Dictionary<ObjectGuid, CombatReference> _pveRefs = new();
    private readonly Dictionary<ObjectGuid, PvPCombatReference> _pvpRefs = new();

	public Unit Owner => _owner;

	public bool HasCombat => HasPvECombat() || HasPvPCombat();

	public Dictionary<ObjectGuid, CombatReference> PvECombatRefs => _pveRefs;

	public Dictionary<ObjectGuid, PvPCombatReference> PvPCombatRefs => _pvpRefs;

	public CombatManager(Unit owner)
	{
		_owner = owner;
	}

	public static bool CanBeginCombat(Unit a, Unit b)
	{
		// Checks combat validity before initial reference creation.
		// For the combat to be valid...
		// ...the two units need to be different
		if (a == b)
			return false;

		// ...the two units need to be in the world
		if (!a.IsInWorld || !b.IsInWorld)
			return false;

		// ...the two units need to both be alive
		if (!a.IsAlive || !b.IsAlive)
			return false;

		// ...the two units need to be on the same map
		if (a.Map != b.Map)
			return false;

		// ...the two units need to be in the same phase
		if (!WorldObject.InSamePhase(a, b))
			return false;

		if (a.HasUnitState(UnitState.Evade) || b.HasUnitState(UnitState.Evade))
			return false;

		if (a.HasUnitState(UnitState.InFlight) || b.HasUnitState(UnitState.InFlight))
			return false;

		// ... both units must be allowed to enter combat
		if (a.IsCombatDisallowed || b.IsCombatDisallowed)
			return false;

		if (a.IsFriendlyTo(b) || b.IsFriendlyTo(a))
			return false;

		var playerA = a.CharmerOrOwnerPlayerOrPlayerItself;
		var playerB = b.CharmerOrOwnerPlayerOrPlayerItself;

		// ...neither of the two units must be (owned by) a player with .gm on
		if ((playerA && playerA.IsGameMaster) || (playerB && playerB.IsGameMaster))
			return false;

		return true;
	}

	public void Update(uint tdiff)
	{
		foreach (var pair in _pvpRefs.ToList())
		{
			var refe = pair.Value;

			if (refe.First == _owner && !refe.Update(tdiff)) // only update if we're the first unit involved (otherwise double decrement)
			{
				_pvpRefs.Remove(pair.Key);
				refe.EndCombat(); // this will remove it from the other side
			}
		}
	}

	public bool HasPvECombat()
	{
		lock (_pveRefs)
		{
			foreach (var (_, refe) in _pveRefs)
				if (!refe.IsSuppressedFor(_owner))
					return true;
		}

		return false;
	}

	public bool HasPvECombatWithPlayers()
	{
		lock (_pveRefs)
		{
			foreach (var reference in _pveRefs)
				if (!reference.Value.IsSuppressedFor(_owner) && reference.Value.GetOther(_owner).IsPlayer)
					return true;
		}

		return false;
	}

	public bool HasPvPCombat()
	{
		lock (_pveRefs)
		{
			foreach (var pair in _pvpRefs)
				if (!pair.Value.IsSuppressedFor(_owner))
					return true;
		}

		return false;
	}

	public Unit GetAnyTarget()
	{
		lock (_pveRefs)
		{
			foreach (var pair in _pveRefs)
				if (!pair.Value.IsSuppressedFor(_owner))
					return pair.Value.GetOther(_owner);

			foreach (var pair in _pvpRefs)
				if (!pair.Value.IsSuppressedFor(_owner))
					return pair.Value.GetOther(_owner);
		}

		return null;
	}

	public bool SetInCombatWith(Unit who, bool addSecondUnitSuppressed = false)
	{
		// Are we already in combat? If yes, refresh pvp combat
		lock (_pveRefs)
		{
			var existingPvpRef = _pvpRefs.LookupByKey(who.GUID);

			if (existingPvpRef != null)
			{
				existingPvpRef.RefreshTimer();
				existingPvpRef.Refresh();

				return true;
			}


			var existingPveRef = _pveRefs.LookupByKey(who.GUID);

			if (existingPveRef != null)
			{
				existingPveRef.Refresh();

				return true;
			}
		}

		// Otherwise, check validity...
		if (!CanBeginCombat(_owner, who))
			return false;

		// ...then create new reference
		CombatReference refe;

		if (_owner.IsControlledByPlayer && who.IsControlledByPlayer)
			refe = new PvPCombatReference(_owner, who);
		else
			refe = new CombatReference(_owner, who);

		if (addSecondUnitSuppressed)
			refe.Suppress(who);

		// ...and insert it into both managers
		PutReference(who.GUID, refe);
		who.GetCombatManager().PutReference(_owner.GUID, refe);

		// now, sequencing is important - first we update the combat state, which will set both units in combat and do non-AI combat start stuff
		var needSelfAI = UpdateOwnerCombatState();
		var needOtherAI = who.GetCombatManager().UpdateOwnerCombatState();

		// then, we finally notify the AI (if necessary) and let it safely do whatever it feels like
		if (needSelfAI)
			NotifyAICombat(_owner, who);

		if (needOtherAI)
			NotifyAICombat(who, _owner);

		return IsInCombatWith(who);
	}

	public bool IsInCombatWith(ObjectGuid guid)
	{
		lock (_pveRefs)
		{
			return _pveRefs.ContainsKey(guid) || _pvpRefs.ContainsKey(guid);
		}
	}

	public bool IsInCombatWith(Unit who)
	{
		return IsInCombatWith(who.GUID);
	}

	public void InheritCombatStatesFrom(Unit who)
	{
		var mgr = who.GetCombatManager();

		lock (_pveRefs)
		{
			foreach (var refe in mgr._pveRefs)
				if (!IsInCombatWith(refe.Key))
				{
					var target = refe.Value.GetOther(who);

					if ((_owner.IsImmuneToPC() && target.HasUnitFlag(UnitFlags.PlayerControlled)) ||
						(_owner.IsImmuneToNPC() && !target.HasUnitFlag(UnitFlags.PlayerControlled)))
						continue;

					SetInCombatWith(target);
				}

			foreach (var refe in mgr._pvpRefs)
			{
				var target = refe.Value.GetOther(who);

				if ((_owner.IsImmuneToPC() && target.HasUnitFlag(UnitFlags.PlayerControlled)) ||
					(_owner.IsImmuneToNPC() && !target.HasUnitFlag(UnitFlags.PlayerControlled)))
					continue;

				SetInCombatWith(target);
			}
		}
	}

	public void EndCombatBeyondRange(float range, bool includingPvP)
	{
		lock (_pveRefs)
		{
			foreach (var pair in _pveRefs.ToList())
			{
				var refe = pair.Value;

				if (!refe.First.IsWithinDistInMap(refe.Second, range))
				{
					_pveRefs.Remove(pair.Key);
					refe.EndCombat();
				}
			}

			if (!includingPvP)
				return;

			foreach (var pair in _pvpRefs.ToList())
			{
				CombatReference refe = pair.Value;

				if (!refe.First.IsWithinDistInMap(refe.Second, range))
				{
					_pvpRefs.Remove(pair.Key);
					refe.EndCombat();
				}
			}
		}
	}

	public void SuppressPvPCombat()
	{
		lock (_pveRefs)
		{
			foreach (var pair in _pvpRefs)
				pair.Value.Suppress(_owner);
		}

		if (UpdateOwnerCombatState())
		{
			var ownerAI = _owner.AI;

			if (ownerAI != null)
				ownerAI.JustExitedCombat();
		}
	}

	public void EndAllPvECombat()
	{
		// cannot have threat without combat
		_owner.GetThreatManager().RemoveMeFromThreatLists();
		_owner.GetThreatManager().ClearAllThreat();

		lock (_pveRefs)
		{
			while (!_pveRefs.Empty())
				_pveRefs.First().Value.EndCombat();
		}
	}

	public void RevalidateCombat()
	{
		lock (_pveRefs)
		{
			foreach (var (guid, refe) in _pveRefs.ToList())
				if (!CanBeginCombat(_owner, refe.GetOther(_owner)))
				{
					_pveRefs.Remove(guid); // erase manually here to avoid iterator invalidation
					refe.EndCombat();
				}

			foreach (var (guid, refe) in _pvpRefs.ToList())
				if (!CanBeginCombat(_owner, refe.GetOther(_owner)))
				{
					_pvpRefs.Remove(guid); // erase manually here to avoid iterator invalidation
					refe.EndCombat();
				}
		}
	}

	public static void NotifyAICombat(Unit me, Unit other)
	{
		var ai = me.AI;

		if (ai != null)
			ai.JustEnteredCombat(other);
	}

	public void PurgeReference(ObjectGuid guid, bool pvp)
	{
		lock (_pveRefs)
		{
			if (pvp)
				_pvpRefs.Remove(guid);
			else
				_pveRefs.Remove(guid);
		}
	}

	public bool UpdateOwnerCombatState()
	{
		var combatState = HasCombat;

		if (combatState == _owner.IsInCombat)
			return false;

		if (combatState)
		{
			_owner.SetUnitFlag(UnitFlags.InCombat);
			_owner.AtEnterCombat();

			if (!_owner.IsCreature)
				_owner.AtEngage(GetAnyTarget());
		}
		else
		{
			_owner.RemoveUnitFlag(UnitFlags.InCombat);
			_owner.AtExitCombat();

			if (!_owner.IsCreature)
				_owner.AtDisengage();
		}

		var master = _owner.CharmerOrOwner;

		if (master != null)
			master.UpdatePetCombatState();

		return true;
	}

	public void EndAllCombat()
	{
		EndAllPvECombat();
		EndAllPvPCombat();
	}

    private void EndAllPvPCombat()
	{
		lock (_pveRefs)
		{
			while (!_pvpRefs.Empty())
				_pvpRefs.First().Value.EndCombat();
		}
	}

    private void PutReference(ObjectGuid guid, CombatReference refe)
	{
		lock (_pveRefs)
		{
			if (refe.IsPvP)
				_pvpRefs[guid] = (PvPCombatReference)refe;
			else
				_pveRefs[guid] = refe;
		}
	}
}
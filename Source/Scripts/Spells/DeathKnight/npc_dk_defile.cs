// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class npc_dk_defile : ScriptedAI
{
	public npc_dk_defile(Creature creature) : base(creature)
	{
		SetCombatMovement(false);
		Me.ReactState = ReactStates.Passive;
		Me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
		Me.AddUnitState(UnitState.Root);
	}

	public override void Reset()
	{
		Me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(11));
	}
}
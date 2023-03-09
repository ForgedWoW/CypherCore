// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[CreatureScript(47649)]
public class npc_dru_efflorescence : ScriptedAI
{
	public npc_dru_efflorescence(Creature creature) : base(creature) { }

	public override void Reset()
	{
		Me.CastSpell(Me, EfflorescenceSpells.EFFLORESCENCE_DUMMY, true);
		Me.SetUnitFlag(UnitFlags.NonAttackable);
		Me.SetUnitFlag(UnitFlags.Uninteractible);
		Me.SetUnitFlag(UnitFlags.RemoveClientControl);
		Me.ReactState = ReactStates.Passive;
	}
}
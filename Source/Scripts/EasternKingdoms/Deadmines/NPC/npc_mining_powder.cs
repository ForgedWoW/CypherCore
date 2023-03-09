// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48284)]
public class npc_mining_powder : ScriptedAI
{
	private bool _damaged = false;

	public npc_mining_powder(Creature creature) : base(creature) { }

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		if (_damaged)
			return;

		_damaged = true;
		Me.CastSpell(Me, DMSpells.EXPLODE);
		Me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(100));
	}
}
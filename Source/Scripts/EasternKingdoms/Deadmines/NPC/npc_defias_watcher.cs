// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_foe_reaper_5000;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47404)]
public class npc_defias_watcher : ScriptedAI
{
	public InstanceScript Instance;
	public bool Status;

	public npc_defias_watcher(Creature creature) : base(creature)
	{
		Instance = creature.InstanceScript;
		Status = false;
	}

	public override void Reset()
	{
		if (!Me)
			return;

		Me.SetPower(PowerType.Energy, 100);
		Me.SetMaxPower(PowerType.Energy, 100);
		Me.SetPowerType(PowerType.Energy);

		if (Status == true)
		{
			if (!Me.HasAura(eSpell.ON_FIRE))
				Me.AddAura(eSpell.ON_FIRE, Me);

			Me.Faction = 35;
		}
	}

	public override void JustEnteredCombat(Unit who) { }

	public override void JustDied(Unit killer)
	{
		if (!Me || Status == true)
			return;

		Energizing();
	}

	public void Energizing()
	{
		Status = true;
		Me.SetHealth(15);
		Me.SetRegenerateHealth(false);
		Me.Faction = 35;
		Me.AddAura(eSpell.ON_FIRE, Me);
		Me.CastSpell(Me, eSpell.ON_FIRE);
		Me.SetInCombatWithZone();

		var reaper = Me.FindNearestCreature(DMCreatures.NPC_FOE_REAPER_5000, 200.0f);

		if (reaper != null)
			Me.CastSpell(reaper, eSpell.ENERGIZE);
	}

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		if (!Me || damage <= 0 || Status == true)
			return;

		if (Me.Health - damage <= Me.MaxHealth * 0.10)
		{
			damage = 0;
			Energizing();
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		DoMeleeAttackIfReady();
	}
}
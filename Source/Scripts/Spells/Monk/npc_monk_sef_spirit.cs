// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[CreatureScript(new uint[]
{
	69791, 69792
})]
public class npc_monk_sef_spirit : ScriptedAI
{
	public npc_monk_sef_spirit(Creature creature) : base(creature) { }

	public override void IsSummonedBy(WorldObject summoner)
	{
		Me.SetLevel(summoner.AsUnit.Level);
		Me.SetMaxHealth(summoner.AsUnit.MaxHealth / 3);
		Me.SetFullHealth();
		summoner.CastSpell(Me, MonkSpells.TRANSCENDENCE_CLONE_TARGET, true);
		Me.CastSpell(Me, Me.Entry == StormEarthAndFireSpells.NPC_FIRE_SPIRIT ? StormEarthAndFireSpells.SEF_FIRE_VISUAL : StormEarthAndFireSpells.SEF_EARTH_VISUAL, true);
		Me.CastSpell(Me, StormEarthAndFireSpells.SEF_SUMMONS_STATS, true);
		var attackPower = summoner.AsUnit.UnitData.AttackPower / 100 * 45.0f;
		var spellPower = summoner.AsUnit.SpellBaseDamageBonusDone(SpellSchoolMask.Nature) / 100 * 45.0f;

		var target = ObjectAccessor.Instance.GetUnit(summoner, summoner.AsUnit.Target);

		if (target != null)
		{
			Me.CastSpell(target, StormEarthAndFireSpells.SEF_CHARGE, true);
		}
		else
		{
			if (Me.Entry == StormEarthAndFireSpells.NPC_FIRE_SPIRIT)
				Me.MotionMaster.MoveFollow(summoner.AsUnit, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
			else
				Me.MotionMaster.MoveFollow(summoner.AsUnit, SharedConst.PetFollowDist, SharedConst.PetFollowAngle * 3);
		}
	}
}
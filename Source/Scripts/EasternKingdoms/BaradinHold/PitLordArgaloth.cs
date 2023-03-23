// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.EasternKingdoms.BaradinHold.PitLordArgaloth;

internal struct SpellIds
{
	public const uint MeteorSlash = 88942;
	public const uint ConsumingDarkness = 88954;
	public const uint FelFirestorm = 88972;
	public const uint Berserk = 47008;
}

[Script]
internal class boss_pit_lord_argaloth : BossAI
{
	private boss_pit_lord_argaloth(Creature creature) : base(creature, DataTypes.Argaloth) { }

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);
		Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							TimeSpan.FromSeconds(20),
							task =>
							{
								DoCastAOE(SpellIds.MeteorSlash);
								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(20),
							TimeSpan.FromSeconds(25),
							task =>
							{
								DoCastAOE(SpellIds.ConsumingDarkness, new CastSpellExtraArgs(true));
								task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25));
							});

		Scheduler.Schedule(TimeSpan.FromMinutes(5), task => { DoCast(Me, SpellIds.Berserk, new CastSpellExtraArgs(true)); });
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
		Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
		_DespawnAtEvade();
	}

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		if (Me.HealthBelowPctDamaged(33, damage) ||
			Me.HealthBelowPctDamaged(66, damage))
			DoCastAOE(SpellIds.FelFirestorm);
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
		Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}

[Script] // 88954 / 95173 - Consuming Darkness
internal class spell_argaloth_consuming_darkness_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RandomResize(Caster.Map.Is25ManRaid ? 8 : 3u);
	}
}

[Script] // 88942 / 95172 - Meteor Slash
internal class spell_argaloth_meteor_slash_SpellScript : SpellScript, ISpellOnHit, IHasSpellEffects
{
	private int _targetCount;
	public List<ISpellEffect> SpellEffects { get; } = new();

	public void OnHit()
	{
		if (_targetCount == 0)
			return;

		HitDamage = (HitDamage / _targetCount);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitConeCasterToDestEnemy));
	}

	private void CountTargets(List<WorldObject> targets)
	{
		_targetCount = targets.Count;
	}
}
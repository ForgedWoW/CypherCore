// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Karazhan.Curator;

internal struct SpellIds
{
	public const uint HatefulBolt = 30383;
	public const uint Evocation = 30254;
	public const uint ArcaneInfusion = 30403;
	public const uint Berserk = 26662;
	public const uint SummonAstralFlareNe = 30236;
	public const uint SummonAstralFlareNw = 30239;
	public const uint SummonAstralFlareSe = 30240;
	public const uint SummonAstralFlareSw = 30241;
}

internal struct TextIds
{
	public const uint SayAggro = 0;
	public const uint SaySummon = 1;
	public const uint SayEvocate = 2;
	public const uint SayEnrage = 3;
	public const uint SayKill = 4;
	public const uint SayDeath = 5;
}

internal struct MiscConst
{
	public const uint GroupAstralFlare = 1;
}

[Script]
internal class boss_curator : BossAI
{
	private bool _infused;

	public boss_curator(Creature creature) : base(creature, DataTypes.Curator) { }

	public override void Reset()
	{
		_Reset();
		_infused = false;
	}

	public override void KilledUnit(Unit victim)
	{
		if (victim.IsPlayer)
			Talk(TextIds.SayKill);
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
		Talk(TextIds.SayDeath);
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);
		Talk(TextIds.SayAggro);

		Scheduler.Schedule(TimeSpan.FromSeconds(12),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.MaxThreat, 1);

								if (target)
									DoCast(target, SpellIds.HatefulBolt);

								task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							MiscConst.GroupAstralFlare,
							task =>
							{
								if (RandomHelper.randChance(50))
									Talk(TextIds.SaySummon);

								DoCastSelf(RandomHelper.RAND(SpellIds.SummonAstralFlareNe, SpellIds.SummonAstralFlareNw, SpellIds.SummonAstralFlareSe, SpellIds.SummonAstralFlareSw), new CastSpellExtraArgs(true));

								var mana = (Me.GetMaxPower(PowerType.Mana) / 10);

								if (mana != 0)
								{
									Me.ModifyPower(PowerType.Mana, -mana);

									if (Me.GetPower(PowerType.Mana) * 100 / Me.GetMaxPower(PowerType.Mana) < 10)
									{
										Talk(TextIds.SayEvocate);
										Me.InterruptNonMeleeSpells(false);
										DoCastSelf(SpellIds.Evocation);
									}
								}

								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromMinutes(12),
							ScheduleTasks =>
							{
								Talk(TextIds.SayEnrage);
								DoCastSelf(SpellIds.Berserk, new CastSpellExtraArgs(true));
							});
	}

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		if (!HealthAbovePct(15) &&
			!_infused)
		{
			_infused = true;
			Scheduler.Schedule(TimeSpan.FromMilliseconds(1), task => DoCastSelf(SpellIds.ArcaneInfusion, new CastSpellExtraArgs(true)));
			Scheduler.CancelGroup(MiscConst.GroupAstralFlare);
		}
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}

[Script]
internal class npc_curator_astral_flare : ScriptedAI
{
	public npc_curator_astral_flare(Creature creature) : base(creature)
	{
		Me.ReactState = ReactStates.Passive;
	}

	public override void Reset()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								Me.ReactState = ReactStates.Aggressive;
								Me.RemoveUnitFlag(UnitFlags.Uninteractible);
								DoZoneInCombat();
							});
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff);
	}
}
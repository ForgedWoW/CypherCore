// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Gehennas;

internal struct SpellIds
{
	public const uint GehennasCurse = 19716;
	public const uint RainOfFire = 19717;
	public const uint ShadowBolt = 19728;
}

[Script]
internal class boss_gehennas : BossAI
{
	public boss_gehennas(Creature creature) : base(creature, DataTypes.Gehennas) { }

	public override void JustEngagedWith(Unit victim)
	{
		base.JustEngagedWith(victim);

		Scheduler.Schedule(TimeSpan.FromSeconds(12),
							task =>
							{
								DoCastVictim(SpellIds.GehennasCurse);
								task.Repeat(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(30));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0);

								if (target)
									DoCast(target, SpellIds.RainOfFire);

								task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 1);

								if (target)
									DoCast(target, SpellIds.ShadowBolt);

								task.Repeat(TimeSpan.FromSeconds(7));
							});
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}
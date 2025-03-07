// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Thebeast;

internal struct SpellIds
{
	public const uint Flamebreak = 16785;
	public const uint Immolate = 20294;
	public const uint Terrifyingroar = 14100;
}

[Script]
internal class boss_thebeast : BossAI
{
	public boss_thebeast(Creature creature) : base(creature, DataTypes.TheBeast) { }

	public override void Reset()
	{
		_Reset();
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);

		Scheduler.Schedule(TimeSpan.FromSeconds(12),
							task =>
							{
								DoCastVictim(SpellIds.Flamebreak);
								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(3),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

								if (target)
									DoCast(target, SpellIds.Immolate);

								task.Repeat(TimeSpan.FromSeconds(8));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(23),
							task =>
							{
								DoCastVictim(SpellIds.Terrifyingroar);
								task.Repeat(TimeSpan.FromSeconds(20));
							});
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}
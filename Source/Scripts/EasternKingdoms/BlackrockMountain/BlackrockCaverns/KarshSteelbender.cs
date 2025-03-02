// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.KarshSteelbender;

internal struct SpellIds
{
	public const uint Cleave = 15284;
	public const uint QuicksilverArmor = 75842;
	public const uint SuperheatedQuicksilverArmor = 75846;
}

internal struct TextIds
{
	public const uint YellAggro = 0;
	public const uint YellKill = 1;
	public const uint YellQuicksilverArmor = 2;
	public const uint YellDeath = 3;

	public const uint EmoteQuicksilverArmor = 4;
}

[Script]
internal class boss_karsh_steelbender : BossAI
{
	public boss_karsh_steelbender(Creature creature) : base(creature, DataTypes.KarshSteelbender) { }

	public override void Reset()
	{
		_Reset();
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);
		Talk(TextIds.YellAggro);

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCastVictim(SpellIds.Cleave);
								task.Repeat(TimeSpan.FromSeconds(10));
							});
	}

	public override void KilledUnit(Unit who)
	{
		if (who.IsPlayer)
			Talk(TextIds.YellKill);
	}

	public override void JustDied(Unit victim)
	{
		_JustDied();
		Talk(TextIds.YellDeath);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}
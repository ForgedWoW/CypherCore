// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Sulfuron;

internal struct SpellIds
{
	// Sulfuron Harbringer
	public const uint DarkStrike = 19777;
	public const uint DemoralizingShout = 19778;
	public const uint Inspire = 19779;
	public const uint Knockdown = 19780;
	public const uint Flamespear = 19781;

	// Adds
	public const uint Heal = 19775;
	public const uint Shadowwordpain = 19776;
	public const uint Immolate = 20294;
}

[Script]
internal class boss_sulfuron : BossAI
{
	public boss_sulfuron(Creature creature) : base(creature, DataTypes.SulfuronHarbinger) { }

	public override void JustEngagedWith(Unit victim)
	{
		base.JustEngagedWith(victim);

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCast(Me, SpellIds.DarkStrike);
								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(18));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							task =>
							{
								DoCastVictim(SpellIds.DemoralizingShout);
								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(13),
							task =>
							{
								var healers = DoFindFriendlyMissingBuff(45.0f, SpellIds.Inspire);

								if (!healers.Empty())
									DoCast(healers.SelectRandom(), SpellIds.Inspire);

								DoCast(Me, SpellIds.Inspire);
								task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(26));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.Knockdown);
								task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

								if (target)
									DoCast(target, SpellIds.Flamespear);

								task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16));
							});
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}

[Script]
internal class npc_flamewaker_priest : ScriptedAI
{
	public npc_flamewaker_priest(Creature creature) : base(creature) { }

	public override void Reset()
	{
		Scheduler.CancelAll();
	}

	public override void JustDied(Unit killer)
	{
		Scheduler.CancelAll();
	}

	public override void JustEngagedWith(Unit victim)
	{
		base.JustEngagedWith(victim);

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							TimeSpan.FromSeconds(30),
							task =>
							{
								var target = DoSelectLowestHpFriendly(60.0f, 1);

								if (target)
									DoCast(target, SpellIds.Heal);

								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.Shadowwordpain);

								if (target)
									DoCast(target, SpellIds.Shadowwordpain);

								task.Repeat(TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(26));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.Immolate);

								if (target)
									DoCast(target, SpellIds.Immolate);

								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
							});
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}
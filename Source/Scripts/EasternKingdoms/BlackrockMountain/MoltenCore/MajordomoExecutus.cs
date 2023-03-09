// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Majordomo;

internal struct SpellIds
{
	public const uint SummonRagnaros = 19774;
	public const uint BlastWave = 20229;
	public const uint Teleport = 20618;
	public const uint MagicReflection = 20619;
	public const uint AegisOfRagnaros = 20620;
	public const uint DamageReflection = 21075;
}

internal struct TextIds
{
	public const uint SayAggro = 0;
	public const uint SaySpawn = 1;
	public const uint SaySlay = 2;
	public const uint SaySpecial = 3;
	public const uint SayDefeat = 4;

	public const uint SaySummonMaj = 5;
	public const uint SayArrival2Maj = 6;

	public const uint OptionIdYouChallengedUs = 0;
	public const uint MenuOptionYouChallengedUs = 4108;
}

[Script]
internal class boss_majordomo : BossAI
{
	public boss_majordomo(Creature creature) : base(creature, DataTypes.MajordomoExecutus) { }

	public override void KilledUnit(Unit victim)
	{
		if (RandomHelper.URand(0, 99) < 25)
			Talk(TextIds.SaySlay);
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);
		Talk(TextIds.SayAggro);

		Scheduler.Schedule(TimeSpan.FromSeconds(30),
							task =>
							{
								DoCast(Me, SpellIds.MagicReflection);
								task.Repeat(TimeSpan.FromSeconds(30));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							task =>
							{
								DoCast(Me, SpellIds.DamageReflection);
								task.Repeat(TimeSpan.FromSeconds(30));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCastVictim(SpellIds.BlastWave);
								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(20),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 1);

								if (target)
									DoCast(target, SpellIds.Teleport);

								task.Repeat(TimeSpan.FromSeconds(20));
							});
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff);

		if (Instance.GetBossState(DataTypes.MajordomoExecutus) != EncounterState.Done)
		{
			if (!UpdateVictim())
				return;

			if (!Me.FindNearestCreature(MCCreatureIds.FlamewakerHealer, 100.0f) &&
				!Me.FindNearestCreature(MCCreatureIds.FlamewakerElite, 100.0f))
			{
				Instance.UpdateEncounterStateForKilledCreature(Me.Entry, Me);
				Me.Faction = (uint)FactionTemplates.Friendly;
				EnterEvadeMode();
				Talk(TextIds.SayDefeat);
				_JustDied();

				Scheduler.Schedule(TimeSpan.FromSeconds(32),
									(Action<Framework.Dynamic.TaskContext>)(task =>
																				{
																					Me.NearTeleportTo(MCMiscConst.RagnarosTelePos.X, MCMiscConst.RagnarosTelePos.Y, MCMiscConst.RagnarosTelePos.Z, MCMiscConst.RagnarosTelePos.Orientation);
																					Me.SetNpcFlag(NPCFlags.Gossip);
																				}));

				return;
			}

			if (Me.HasUnitState(UnitState.Casting))
				return;

			if (HealthBelowPct(50))
				DoCast(Me, SpellIds.AegisOfRagnaros, new CastSpellExtraArgs(true));

			DoMeleeAttackIfReady();
		}
	}

	public override void DoAction(int action)
	{
		if (action == ActionIds.StartRagnaros)
		{
			Me.RemoveNpcFlag(NPCFlags.Gossip);
			Talk(TextIds.SaySummonMaj);

			Scheduler.Schedule(TimeSpan.FromSeconds(8), task => { Instance.Instance.SummonCreature(MCCreatureIds.Ragnaros, MCMiscConst.RagnarosSummonPos); });
			Scheduler.Schedule(TimeSpan.FromSeconds(24), task => { Talk(TextIds.SayArrival2Maj); });
		}
		else if (action == ActionIds.StartRagnarosAlt)
		{
			Me.Faction = (uint)FactionTemplates.Friendly;
			Me.SetNpcFlag(NPCFlags.Gossip);
		}
	}

	public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
	{
		if (menuId == TextIds.MenuOptionYouChallengedUs &&
			gossipListId == TextIds.OptionIdYouChallengedUs)
		{
			player.CloseGossipMenu();
			DoAction(ActionIds.StartRagnaros);
		}

		return false;
	}
}
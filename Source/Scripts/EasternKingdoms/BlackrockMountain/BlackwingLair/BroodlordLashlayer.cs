// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Broodlord;

internal struct SpellIds
{
	public const uint Cleave = 26350;
	public const uint Blastwave = 23331;
	public const uint Mortalstrike = 24573;
	public const uint Knockback = 25778;
	public const uint SuppressionAura = 22247; // Suppression Device Spell
}

internal struct TextIds
{
	public const uint SayAggro = 0;
	public const uint SayLeash = 1;
}

internal struct EventIds
{
	// Suppression Device Events
	public const uint SuppressionCast = 1;
	public const uint SuppressionReset = 2;
}

internal struct ActionIds
{
	public const int Deactivate = 0;
}

[Script]
internal class boss_broodlord : BossAI
{
	public boss_broodlord(Creature creature) : base(creature, DataTypes.BroodlordLashlayer) { }

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);
		Talk(TextIds.SayAggro);

		SchedulerProtected.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								DoCastVictim(SpellIds.Cleave);
								task.Repeat(TimeSpan.FromSeconds(7));
							});

		SchedulerProtected.Schedule(TimeSpan.FromSeconds(12),
							task =>
							{
								DoCastVictim(SpellIds.Blastwave);
								task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16));
							});

		SchedulerProtected.Schedule(TimeSpan.FromSeconds(20),
							task =>
							{
								DoCastVictim(SpellIds.Mortalstrike);
								task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
							});

		SchedulerProtected.Schedule(TimeSpan.FromSeconds(30),
							task =>
							{
								DoCastVictim(SpellIds.Knockback);

								if (GetThreat(Me.Victim) != 0)
									ModifyThreatByPercent(Me.Victim, -50);

								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
							});

		SchedulerProtected.Schedule(TimeSpan.FromSeconds(1),
							task =>
							{
								if (Me.GetDistance(Me.HomePosition) > 150.0f)
								{
									Talk(TextIds.SayLeash);
									EnterEvadeMode(EvadeReason.Boundary);
								}

								task.Repeat(TimeSpan.FromSeconds(1));
							});
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();

		var _goList = Me.GetGameObjectListWithEntryInGrid(BWLGameObjectIds.SuppressionDevice, 200.0f);

		foreach (var go in _goList)
			go.			AI.DoAction(ActionIds.Deactivate);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		SchedulerProtected.Update(diff, () => DoMeleeAttackIfReady());
	}
}

[Script]
internal class go_suppression_device : GameObjectAI
{
	private readonly InstanceScript _instance;
	private bool _active;

	public go_suppression_device(GameObject go) : base(go)
	{
		_instance = go.InstanceScript;
		_active = true;
	}

	public override void InitializeAI()
	{
		if (_instance.GetBossState(DataTypes.BroodlordLashlayer) == EncounterState.Done)
		{
			Deactivate();

			return;
		}

		Events.ScheduleEvent(EventIds.SuppressionCast, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5));
	}

	public override void UpdateAI(uint diff)
	{
		Events.Update(diff);

		Events.ExecuteEvents(eventId =>
		{
			switch (eventId)
			{
				case EventIds.SuppressionCast:
					if (Me.GoState == GameObjectState.Ready)
					{
						Me.CastSpell(null, SpellIds.SuppressionAura, true);
						Me.SendCustomAnim(0);
					}

					Events.ScheduleEvent(EventIds.SuppressionCast, TimeSpan.FromSeconds(5));

					break;
				case EventIds.SuppressionReset:
					Activate();

					break;
			}
		});
	}

	public override void OnLootStateChanged(uint state, Unit unit)
	{
		switch ((LootState)state)
		{
			case LootState.Activated:
				Deactivate();
				Events.CancelEvent(EventIds.SuppressionCast);
				Events.ScheduleEvent(EventIds.SuppressionReset, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120));

				break;
			case LootState.JustDeactivated: // This case prevents the Gameobject despawn by Disarm Trap
				Me.SetLootState(LootState.Ready);

				break;
		}
	}

	public override void DoAction(int action)
	{
		if (action == ActionIds.Deactivate)
		{
			Deactivate();
			Events.CancelEvent(EventIds.SuppressionReset);
		}
	}

	private void Activate()
	{
		if (_active)
			return;

		_active = true;

		if (Me.GoState == GameObjectState.Active)
			Me.SetGoState(GameObjectState.Ready);

		Me.SetLootState(LootState.Ready);
		Me.RemoveFlag(GameObjectFlags.NotSelectable);
		Events.ScheduleEvent(EventIds.SuppressionCast, TimeSpan.FromSeconds(0));
	}

	private void Deactivate()
	{
		if (!_active)
			return;

		_active = false;
		Me.SetGoState(GameObjectState.Active);
		Me.SetFlag(GameObjectFlags.NotSelectable);
		Events.CancelEvent(EventIds.SuppressionCast);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;

namespace Scripts.EasternKingdoms.BaradinHold.Alizabal;

internal struct SpellIds
{
	public const uint BladeDance = 105784;
	public const uint BladeDanceDummy = 105828;
	public const uint SeethingHate = 105067;
	public const uint Skewer = 104936;
	public const uint Berserk = 47008;
}

internal struct TextIds
{
	public const uint SayIntro = 1;
	public const uint SayAggro = 2;
	public const uint SayHate = 3;
	public const uint SaySkewer = 4;
	public const uint SaySkewerAnnounce = 5;
	public const uint SayBladeStorm = 6;
	public const uint SaySlay = 10;
	public const uint SayDeath = 12;
}

internal struct ActionIds
{
	public const int Intro = 1;
}

internal struct PointIds
{
	public const uint Storm = 1;
}

internal struct EventIds
{
	public const uint RandomCast = 1;
	public const uint StopStorm = 2;
	public const uint MoveStorm = 3;
	public const uint CastStorm = 4;
}

[Script]
internal class at_alizabal_intro : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
	public at_alizabal_intro() : base("at_alizabal_intro") { }

	public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
	{
		var instance = player.InstanceScript;

		if (instance != null)
		{
			var alizabal = ObjectAccessor.GetCreature(player, instance.GetGuidData(DataTypes.Alizabal));

			if (alizabal)
				alizabal.AI.DoAction(ActionIds.Intro);
		}

		return true;
	}
}

[Script]
internal class boss_alizabal : BossAI
{
	private bool _hate;
	private bool _intro;
	private bool _skewer;

	public boss_alizabal(Creature creature) : base(creature, DataTypes.Alizabal) { }

	public override void Reset()
	{
		_Reset();
		_hate = false;
		_skewer = false;
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);
		Talk(TextIds.SayAggro);
		Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);
		Events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(10));
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
		Talk(TextIds.SayDeath);
		Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
	}

	public override void KilledUnit(Unit who)
	{
		if (who.IsPlayer)
			Talk(TextIds.SaySlay);
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
		Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
		Me.MotionMaster.MoveTargetedHome();
		_DespawnAtEvade();
	}

	public override void DoAction(int action)
	{
		switch (action)
		{
			case ActionIds.Intro:
				if (!_intro)
				{
					Talk(TextIds.SayIntro);
					_intro = true;
				}

				break;
		}
	}

	public override void MovementInform(MovementGeneratorType type, uint pointId)
	{
		switch (pointId)
		{
			case PointIds.Storm:
				Events.ScheduleEvent(EventIds.CastStorm, TimeSpan.FromMilliseconds(1));

				break;
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Events.Update(diff);

		Events.ExecuteEvents(eventId =>
		{
			switch (eventId)
			{
				case EventIds.RandomCast:
				{
					switch (RandomHelper.URand(0, 1))
					{
						case 0:
							if (!_skewer)
							{
								var target = SelectTarget(SelectTargetMethod.MaxThreat, 0);

								if (target)
								{
									DoCast(target, SpellIds.Skewer, new CastSpellExtraArgs(true));
									Talk(TextIds.SaySkewer);
									Talk(TextIds.SaySkewerAnnounce, target);
								}

								_skewer = true;
								Events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
							}
							else if (!_hate)
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(Me));

								if (target)
								{
									DoCast(target, SpellIds.SeethingHate, new CastSpellExtraArgs(true));
									Talk(TextIds.SayHate);
								}

								_hate = true;
								Events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
							}
							else if (_hate && _skewer)
							{
								Talk(TextIds.SayBladeStorm);
								DoCastAOE(SpellIds.BladeDanceDummy);
								DoCastAOE(SpellIds.BladeDance);
								Events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(21));
								Events.ScheduleEvent(EventIds.MoveStorm, TimeSpan.FromMilliseconds(4050));
								Events.ScheduleEvent(EventIds.StopStorm, TimeSpan.FromSeconds(13));
							}

							break;
						case 1:
							if (!_hate)
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(Me));

								if (target)
								{
									DoCast(target, SpellIds.SeethingHate, new CastSpellExtraArgs(true));
									Talk(TextIds.SayHate);
								}

								_hate = true;
								Events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
							}
							else if (!_skewer)
							{
								var target = SelectTarget(SelectTargetMethod.MaxThreat, 0);

								if (target)
								{
									DoCast(target, SpellIds.Skewer, new CastSpellExtraArgs(true));
									Talk(TextIds.SaySkewer);
									Talk(TextIds.SaySkewerAnnounce, target);
								}

								_skewer = true;
								Events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
							}
							else if (_hate && _skewer)
							{
								Talk(TextIds.SayBladeStorm);
								DoCastAOE(SpellIds.BladeDanceDummy);
								DoCastAOE(SpellIds.BladeDance);
								Events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(21));
								Events.ScheduleEvent(EventIds.MoveStorm, TimeSpan.FromMilliseconds(4050));
								Events.ScheduleEvent(EventIds.StopStorm, TimeSpan.FromSeconds(13));
							}

							break;
					}

					break;
				}
				case EventIds.MoveStorm:
				{
					Me.SetSpeedRate(UnitMoveType.Run, 4.0f);
					Me.SetSpeedRate(UnitMoveType.Walk, 4.0f);
					var target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(Me));

					if (target)
						Me.MotionMaster.MovePoint(PointIds.Storm, target.Location.X, target.Location.Y, target.Location.Z);

					Events.ScheduleEvent(EventIds.MoveStorm, TimeSpan.FromMilliseconds(4050));

					break;
				}
				case EventIds.StopStorm:
					Me.RemoveAura(SpellIds.BladeDance);
					Me.RemoveAura(SpellIds.BladeDanceDummy);
					Me.SetSpeedRate(UnitMoveType.Walk, 1.0f);
					Me.SetSpeedRate(UnitMoveType.Run, 1.14f);
					Me.MotionMaster.MoveChase(Me.Victim);
					_hate = false;
					_skewer = false;

					break;
				case EventIds.CastStorm:
					DoCastAOE(SpellIds.BladeDance);

					break;
			}
		});

		DoMeleeAttackIfReady();
	}
}
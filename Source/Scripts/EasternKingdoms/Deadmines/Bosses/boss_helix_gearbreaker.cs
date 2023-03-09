// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.Bosses;

[CreatureScript(47296)]
public class boss_helix_gearbreaker : BossAI
{
	public const string CHEST_BOMB = "Helix attaches a bomb to $N's chest.";

	public static readonly Position[] OafPos =
	{
		new(-289.809f, -527.215f, 49.8021f, 0), new(-289.587f, -489.575f, 49.9126f, 0)
	};

	public static readonly Position[] CrewSpawn =
	{
		new(-281.68f, -504.10f, 60.51f, 4.75f), new(-284.71f, -504.13f, 60.42f, 4.72f), new(-288.65f, -503.74f, 60.38f, 4.64f), new(-293.88f, -503.90f, 60.07f, 4.77f)
	};

	private Creature _oaf;

	public boss_helix_gearbreaker(Creature pCreature) : base(pCreature, DMData.DATA_HELIX) { }

	public override void Reset()
	{
		_Reset();

		if (!Me)
			return;

		Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
		Me.ReactState = ReactStates.Aggressive;
		Me.SetUnitFlag(UnitFlags.Uninteractible);
		Summons.DespawnAll();
		OafSupport();
	}

	public override void JustEnteredCombat(Unit who)
	{
		if (!Me)
			return;

		base.JustEnteredCombat(who);
		Talk(5);
		Me.SetInCombatWithZone();
		Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);
		Events.ScheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));

		if (IsHeroic())
		{
			SummonCrew();
			Events.ScheduleEvent(HelOaf_Events.EVENT_ACHIEVEVEMENT_BUFF, TimeSpan.FromMilliseconds(0));
		}
	}

	public void OafSupport()
	{
		_oaf = Me.VehicleCreatureBase;

		if (_oaf == null)
		{
			_oaf = Me.FindNearestCreature(DMCreatures.NPC_OAF, 30.0f);

			if (_oaf != null && _oaf.IsAlive)
			{
				Me.CastSpell(_oaf, eSpels.RIDE_VEHICLE_HARDCODED);
			}
			else
			{
				_oaf = Me.SummonCreature(DMCreatures.NPC_OAF, Me.HomePosition);

				if (_oaf != null && _oaf.IsAlive)
					Me.CastSpell(_oaf, eSpels.RIDE_VEHICLE_HARDCODED);
			}
		}
	}

	public override void JustSummoned(Creature summoned)
	{
		Summons.Summon(summoned);
	}

	public void SummonCrew()
	{
		for (byte i = 0; i < 4; ++i)
			Me.SummonCreature(DMCreatures.NPC_HELIX_CREW, CrewSpawn[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
	}

	public override void JustDied(Unit killer)
	{
		if (!Me)
			return;

		base.JustDied(killer);
		Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
		Talk(0);
		Summons.DespawnAll();
	}

	public override void JustReachedHome()
	{
		if (!Me)
			return;

		base.JustReachedHome();
		Talk(1);
		Instance.SetBossState(DMData.DATA_HELIX, EncounterState.Fail);
	}

	public void OafDead()
	{
		Events.ScheduleEvent(HelOaf_Events.EVENT_NO_OAF, TimeSpan.FromMilliseconds(100));
		Events.ScheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));

		if (IsHeroic())
			Events.ScheduleEvent(HelOaf_Events.EVENT_CHEST_BOMB, TimeSpan.FromMilliseconds(5000));
	}

	public override void UpdateAI(uint uiDiff)
	{
		if (!UpdateVictim())
			return;

		if (!Me)
			return;

		DoMeleeAttackIfReady();

		Events.Update(uiDiff);

		uint eventId;

		while ((eventId = Events.ExecuteEvent()) != 0)
			switch (eventId)
			{
				case HelOaf_Events.EVENT_THROW_BOMB:
					var target = SelectTarget(SelectTargetMethod.Random, 0, 150, true);

					if (target != null)
						Me.CastSpell(target, eSpels.THROW_BOMB, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle | TriggerCastFlags.IgnoreCasterAurastate));

					Events.ScheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));

					break;
				case HelOaf_Events.EVENT_CHEST_BOMB:
					var target1 = SelectTarget(SelectTargetMethod.Random, 0, 150, true);

					if (target1 != null)
					{
						Me.TextEmote(CHEST_BOMB, target1, true);
						Me.AddAura(eSpels.CHEST_BOMB, target1);
					}

					Events.ScheduleEvent(HelOaf_Events.EVENT_CHEST_BOMB, TimeSpan.FromMilliseconds(11000));

					break;
				case HelOaf_Events.EVENT_NO_OAF:
					Me.RemoveUnitFlag(UnitFlags.Uninteractible);
					Me.RemoveAura(eSpels.OAFQUARD);
					Talk(2);
					Events.RescheduleEvent(HelOaf_Events.EVENT_THROW_BOMB, TimeSpan.FromMilliseconds(3000));

					break;
				case HelOaf_Events.EVENT_ACHIEVEVEMENT_BUFF:
					var players = new List<Unit>();
					var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
					var searcher = new PlayerListSearcher(Me, players, checker);
					Cell.VisitGrid(Me, searcher, 150f);

					foreach (var item in players)
						Me.CastSpell(item, eSpels.HELIX_RIDE, true);

					Events.ScheduleEvent(HelOaf_Events.EVENT_ACHIEVEVEMENT_BUFF, TimeSpan.FromMilliseconds(60000));

					break;
			}
	}

	public struct eSpels
	{
		// Helix
		public const uint OAFQUARD = 90546;
		public const uint HELIX_RIDE = 88337;
		public const uint THROW_BOMB = 88264;

		// Oaf spell
		public const uint OAF_GRAB_TARGETING = 88289;
		public const uint RIDE_OAF = 88278; // 88277;
		public const uint RIDE_VEHICLE_HARDCODED = 46598;
		public const uint OAF_CHARGE = 88288;
		public const uint OAF_SMASH = 88300;
		public const uint OAF_SMASH_H = 91568;

		// BOMB
		public const uint STICKY_BOMB_EXPLODE = 95500; //88329; // 95500 -> 88321; 88974
		public const uint STICKY_BOMB_EXPLODE_H = 91566;
		public const uint ARMING_VISUAL_YELLOW = 88315;
		public const uint ARMING_VISUAL_ORANGE = 88316;
		public const uint ARMING_VISUAL_RED = 88317;
		public const uint BOMB_ARMED_STATE = 88319;
		public const uint CHEST_BOMB = 88352; // Unused
	}

	public struct HelOaf_Events
	{
		// Helix Events
		public const uint EVENT_CHEST_BOMB = 1;
		public const uint EVENT_THROW_BOMB = 2;
		public const uint EVENT_NO_OAF = 3;
		public const uint EVENT_ACHIEVEVEMENT_BUFF = 4;

		// Oaf Events
		public const uint EVENT_OAFQUARD = 5;
		public const uint EVENT_MOVE_TO_POINT = 6;
		public const uint EVENT_MOUNT_PLAYER = 7;
		public const uint EVEMT_CHARGE = 8;
		public const uint EVENT_FINISH = 9;
	}
}
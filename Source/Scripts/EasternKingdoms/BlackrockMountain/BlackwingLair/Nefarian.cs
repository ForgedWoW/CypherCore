// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.VictorNefarius;

internal struct SpellIds
{
	// Victor Nefarius
	// Ubrs Spells
	public const uint ChromaticChaos = 16337; // Self Cast hits 10339

	public const uint VaelastraszzSpawn = 16354; // Self Cast Depawn one sec after

	// Bwl Spells
	public const uint Shadowbolt = 22677;
	public const uint ShadowboltVolley = 22665;
	public const uint ShadowCommand = 22667;
	public const uint Fear = 22678;

	public const uint NefariansBarrier = 22663;

	// Nefarian
	public const uint ShadowflameInitial = 22992;
	public const uint Shadowflame = 22539;
	public const uint Bellowingroar = 22686;
	public const uint Veilofshadow = 7068;
	public const uint Cleave = 20691;
	public const uint Taillash = 23364;

	public const uint Mage = 23410;        // wild magic
	public const uint Warrior = 23397;     // beserk
	public const uint Druid = 23398;       // cat form
	public const uint Priest = 23401;      // corrupted healing
	public const uint Paladin = 23418;     // syphon blessing
	public const uint Shaman = 23425;      // totems
	public const uint Warlock = 23427;     // infernals
	public const uint Hunter = 23436;      // bow broke
	public const uint Rogue = 23414;       // Paralise
	public const uint DeathKnight = 49576; // Death Grip

	// 19484
	// 22664
	// 22674
	// 22666
}

internal struct TextIds
{
	// Nefarius
	// Ubrs
	public const uint SayChaosSpell = 9;
	public const uint SaySuccess = 10;

	public const uint SayFailure = 11;

	// Bwl
	public const uint SayGamesbegin1 = 12;

	public const uint SayGamesbegin2 = 13;
	// public const uint SayVaelIntro             = 14; Not used - when he corrupts Vaelastrasz

	// Nefarian
	public const uint SayRandom = 0;
	public const uint SayRaiseSkeletons = 1;
	public const uint SaySlay = 2;
	public const uint SayDeath = 3;

	public const uint SayMage = 4;
	public const uint SayWarrior = 5;
	public const uint SayDruid = 6;
	public const uint SayPriest = 7;
	public const uint SayPaladin = 8;
	public const uint SayShaman = 9;
	public const uint SayWarlock = 10;
	public const uint SayHunter = 11;
	public const uint SayRogue = 12;
	public const uint SayDeathKnight = 13;

	public const uint GossipId = 6045;
	public const uint GossipOptionId = 0;
}

internal struct CreatureIds
{
	public const uint BronzeDrakanoid = 14263;
	public const uint BlueDrakanoid = 14261;
	public const uint RedDrakanoid = 14264;
	public const uint GreenDrakanoid = 14262;
	public const uint BlackDrakanoid = 14265;
	public const uint ChromaticDrakanoid = 14302;

	public const uint BoneConstruct = 14605;

	// Ubrs
	public const uint Gyth = 10339;
}

internal struct GameObjectIds
{
	public const uint PortcullisActive = 164726;
	public const uint PortcullisTobossrooms = 175186;
}

internal struct MiscConst
{
	public const uint NefariusPath2 = 1379671;
	public const uint NefariusPath3 = 1379672;

	public static Position[] DrakeSpawnLoc = // drakonid
	{
		new(-7591.151855f, -1204.051880f, 476.800476f, 3.0f), new(-7514.598633f, -1150.448853f, 476.796570f, 3.0f)
	};

	public static Position[] NefarianLoc =
	{
		new(-7449.763672f, -1387.816040f, 526.783691f, 3.0f), // nefarian spawn
		new(-7535.456543f, -1279.562500f, 476.798706f, 3.0f)  // nefarian move
	};

	public static uint[] Entry =
	{
		CreatureIds.BronzeDrakanoid, CreatureIds.BlueDrakanoid, CreatureIds.RedDrakanoid, CreatureIds.GreenDrakanoid, CreatureIds.BlackDrakanoid
	};
}

internal struct EventIds
{
	// Victor Nefarius
	public const uint SpawnAdd = 1;
	public const uint ShadowBolt = 2;
	public const uint Fear = 3;

	public const uint MindControl = 4;

	// Nefarian
	public const uint Shadowflame = 5;
	public const uint Veilofshadow = 6;
	public const uint Cleave = 7;
	public const uint Taillash = 8;

	public const uint Classcall = 9;

	// Ubrs
	public const uint Chaos1 = 10;
	public const uint Chaos2 = 11;
	public const uint Path2 = 12;
	public const uint Path3 = 13;
	public const uint Success1 = 14;
	public const uint Success2 = 15;
	public const uint Success3 = 16;
}

[Script]
internal class boss_victor_nefarius : BossAI
{
	private uint SpawnedAdds;

	public boss_victor_nefarius(Creature creature) : base(creature, DataTypes.Nefarian)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		if (Me.Location.MapId == 469)
		{
			if (!Me.FindNearestCreature(BWLCreatureIds.Nefarian, 1000.0f, true))
				_Reset();

			Me.SetVisible(true);
			Me.SetNpcFlag(NPCFlags.Gossip);
			Me.Faction = (uint)FactionTemplates.Friendly;
			Me.SetStandState(UnitStandStateType.SitHighChair);
			Me.RemoveAura(SpellIds.NefariansBarrier);
		}
	}

	public override void JustReachedHome()
	{
		Reset();
	}

	public override void SummonedCreatureDies(Creature summon, Unit killer)
	{
		if (summon.Entry != BWLCreatureIds.Nefarian)
		{
			summon.UpdateEntry(CreatureIds.BoneConstruct);
			summon.SetUnitFlag(UnitFlags.Uninteractible);
			summon.ReactState = ReactStates.Passive;
			summon.SetStandState(UnitStandStateType.Dead);
		}
	}

	public override void JustSummoned(Creature summon) { }

	public override void SetData(uint type, uint data)
	{
		if (type == 1 &&
			data == 1)
		{
			Me.StopMoving();
			Events.ScheduleEvent(EventIds.Path2, TimeSpan.FromSeconds(9));
		}

		if (type == 1 &&
			data == 2)
			Events.ScheduleEvent(EventIds.Success1, TimeSpan.FromSeconds(5));
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
		{
			Events.Update(diff);

			Events.ExecuteEvents(eventId =>
			{
				switch (eventId)
				{
					case EventIds.Path2:
						Me.MotionMaster.MovePath(MiscConst.NefariusPath2, false);
						Events.ScheduleEvent(EventIds.Chaos1, TimeSpan.FromSeconds(7));

						break;
					case EventIds.Chaos1:
						var gyth = Me.FindNearestCreature(CreatureIds.Gyth, 75.0f, true);

						if (gyth)
						{
							Me.SetFacingToObject(gyth);
							Talk(TextIds.SayChaosSpell);
						}

						Events.ScheduleEvent(EventIds.Chaos2, TimeSpan.FromSeconds(2));

						break;
					case EventIds.Chaos2:
						DoCast(SpellIds.ChromaticChaos);
						Me.SetFacingTo(1.570796f);

						break;
					case EventIds.Success1:
						Unit player = Me.SelectNearestPlayer(60.0f);

						if (player)
						{
							Me.SetFacingToObject(player);
							Talk(TextIds.SaySuccess);
							var portcullis1 = Me.FindNearestGameObject(GameObjectIds.PortcullisActive, 65.0f);

							if (portcullis1)
								portcullis1.SetGoState(GameObjectState.Active);

							var portcullis2 = Me.FindNearestGameObject(GameObjectIds.PortcullisTobossrooms, 80.0f);

							if (portcullis2)
								portcullis2.SetGoState(GameObjectState.Active);
						}

						Events.ScheduleEvent(EventIds.Success2, TimeSpan.FromSeconds(4));

						break;
					case EventIds.Success2:
						DoCast(Me, SpellIds.VaelastraszzSpawn);
						Me.DespawnOrUnsummon(TimeSpan.FromSeconds(1));

						break;
					case EventIds.Path3:
						Me.MotionMaster.MovePath(MiscConst.NefariusPath3, false);

						break;
					default:
						break;
				}
			});

			return;
		}

		// Only do this if we haven't spawned nefarian yet
		if (UpdateVictim() &&
			SpawnedAdds <= 42)
		{
			Events.Update(diff);

			if (Me.HasUnitState(UnitState.Casting))
				return;

			Events.ExecuteEvents(eventId =>
			{
				switch (eventId)
				{
					case EventIds.ShadowBolt:
						switch (RandomHelper.URand(0, 1))
						{
							case 0:
								DoCastVictim(SpellIds.ShadowboltVolley);

								break;
							case 1:
								var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

								if (target)
									DoCast(target, SpellIds.Shadowbolt);

								break;
						}

						ResetThreatList();
						Events.ScheduleEvent(EventIds.ShadowBolt, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));

						break;
					case EventIds.Fear:
					{
						var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

						if (target)
							DoCast(target, SpellIds.Fear);

						Events.ScheduleEvent(EventIds.Fear, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));

						break;
					}
					case EventIds.MindControl:
					{
						var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

						if (target)
							DoCast(target, SpellIds.ShadowCommand);

						Events.ScheduleEvent(EventIds.MindControl, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));

						break;
					}
					case EventIds.SpawnAdd:
						for (byte i = 0; i < 2; ++i)
						{
							uint CreatureID;

							if (RandomHelper.URand(0, 2) == 0)
								CreatureID = CreatureIds.ChromaticDrakanoid;
							else
								CreatureID = MiscConst.Entry[RandomHelper.URand(0, 4)];

							Creature dragon = Me.SummonCreature(CreatureID, MiscConst.DrakeSpawnLoc[i]);

							if (dragon)
							{
								dragon.Faction = (uint)FactionTemplates.DragonflightBlack;
								dragon.AI.AttackStart(Me.Victim);
							}

							if (++SpawnedAdds >= 42)
							{
								Creature nefarian = Me.SummonCreature(BWLCreatureIds.Nefarian, MiscConst.NefarianLoc[0]);

								if (nefarian)
								{
									nefarian.SetActive(true);
									nefarian.SetFarVisible(true);
									nefarian.SetCanFly(true);
									nefarian.SetDisableGravity(true);
									nefarian.CastSpell(SpellIds.ShadowflameInitial);
									nefarian.MotionMaster.MovePoint(1, MiscConst.NefarianLoc[1]);
								}

								Events.CancelEvent(EventIds.MindControl);
								Events.CancelEvent(EventIds.Fear);
								Events.CancelEvent(EventIds.ShadowBolt);
								Me.SetVisible(false);

								return;
							}
						}

						Events.ScheduleEvent(EventIds.SpawnAdd, TimeSpan.FromSeconds(4));

						break;
				}

				if (Me.HasUnitState(UnitState.Casting))
					return;
			});
		}
	}

	public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
	{
		if (menuId == TextIds.GossipId &&
			gossipListId == TextIds.GossipOptionId)
		{
			player.CloseGossipMenu();
			Talk(TextIds.SayGamesbegin1);
			BeginEvent(player);
		}

		return false;
	}

	private void Initialize()
	{
		SpawnedAdds = 0;
	}

	private void BeginEvent(Player target)
	{
		_JustEngagedWith(target);

		Talk(TextIds.SayGamesbegin2);

		Me.Faction = (uint)FactionTemplates.DragonflightBlack;
		Me.RemoveNpcFlag(NPCFlags.Gossip);
		DoCast(Me, SpellIds.NefariansBarrier);
		Me.SetStandState(UnitStandStateType.Stand);
		Me.SetImmuneToPC(false);
		AttackStart(target);
		Events.ScheduleEvent(EventIds.ShadowBolt, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));
		Events.ScheduleEvent(EventIds.Fear, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
		//_events.ScheduleEvent(EventIds.MindControl, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
		Events.ScheduleEvent(EventIds.SpawnAdd, TimeSpan.FromSeconds(10));
	}
}

[Script]
internal class boss_nefarian : BossAI
{
	private bool canDespawn;
	private uint DespawnTimer;
	private bool Phase3;

	public boss_nefarian(Creature creature) : base(creature, DataTypes.Nefarian)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();
	}

	public override void JustReachedHome()
	{
		canDespawn = true;
	}

	public override void JustEngagedWith(Unit who)
	{
		Events.ScheduleEvent(EventIds.Shadowflame, TimeSpan.FromSeconds(12));
		Events.ScheduleEvent(EventIds.Fear, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
		Events.ScheduleEvent(EventIds.Veilofshadow, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
		Events.ScheduleEvent(EventIds.Cleave, TimeSpan.FromSeconds(7));
		//_events.ScheduleEvent(EventIds.Taillash, TimeSpan.FromSeconds(10));
		Events.ScheduleEvent(EventIds.Classcall, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
		Talk(TextIds.SayRandom);
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
		Talk(TextIds.SayDeath);
	}

	public override void KilledUnit(Unit victim)
	{
		if ((RandomHelper.Rand32() % 5) != 0)
			return;

		Talk(TextIds.SaySlay, victim);
	}

	public override void MovementInform(MovementGeneratorType type, uint id)
	{
		if (type != MovementGeneratorType.Point)
			return;

		if (id == 1)
		{
			DoZoneInCombat();

			if (Me.Victim)
				AttackStart(Me.Victim);
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (canDespawn && DespawnTimer <= diff)
		{
			Instance.SetBossState(DataTypes.Nefarian, EncounterState.Fail);

			var constructList = Me.GetCreatureListWithEntryInGrid(CreatureIds.BoneConstruct, 500.0f);

			foreach (var creature in constructList)
				creature.DespawnOrUnsummon();
		}
		else
		{
			DespawnTimer -= diff;
		}

		if (!UpdateVictim())
			return;

		if (canDespawn)
			canDespawn = false;

		Events.Update(diff);

		if (Me.HasUnitState(UnitState.Casting))
			return;

		Events.ExecuteEvents(eventId =>
		{
			switch (eventId)
			{
				case EventIds.Shadowflame:
					DoCastVictim(SpellIds.Shadowflame);
					Events.ScheduleEvent(EventIds.Shadowflame, TimeSpan.FromSeconds(12));

					break;
				case EventIds.Fear:
					DoCastVictim(SpellIds.Bellowingroar);
					Events.ScheduleEvent(EventIds.Fear, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));

					break;
				case EventIds.Veilofshadow:
					DoCastVictim(SpellIds.Veilofshadow);
					Events.ScheduleEvent(EventIds.Veilofshadow, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));

					break;
				case EventIds.Cleave:
					DoCastVictim(SpellIds.Cleave);
					Events.ScheduleEvent(EventIds.Cleave, TimeSpan.FromSeconds(7));

					break;
				case EventIds.Taillash:
					// Cast Nyi since we need a better check for behind Target
					DoCastVictim(SpellIds.Taillash);
					Events.ScheduleEvent(EventIds.Taillash, TimeSpan.FromSeconds(10));

					break;
				case EventIds.Classcall:
					var target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

					if (target)
						switch (target.Class)
						{
							case PlayerClass.Mage:
								Talk(TextIds.SayMage);
								DoCast(Me, SpellIds.Mage);

								break;
							case PlayerClass.Warrior:
								Talk(TextIds.SayWarrior);
								DoCast(Me, SpellIds.Warrior);

								break;
							case PlayerClass.Druid:
								Talk(TextIds.SayDruid);
								DoCast(target, SpellIds.Druid);

								break;
							case PlayerClass.Priest:
								Talk(TextIds.SayPriest);
								DoCast(Me, SpellIds.Priest);

								break;
							case PlayerClass.Paladin:
								Talk(TextIds.SayPaladin);
								DoCast(Me, SpellIds.Paladin);

								break;
							case PlayerClass.Shaman:
								Talk(TextIds.SayShaman);
								DoCast(Me, SpellIds.Shaman);

								break;
							case PlayerClass.Warlock:
								Talk(TextIds.SayWarlock);
								DoCast(Me, SpellIds.Warlock);

								break;
							case PlayerClass.Hunter:
								Talk(TextIds.SayHunter);
								DoCast(Me, SpellIds.Hunter);

								break;
							case PlayerClass.Rogue:
								Talk(TextIds.SayRogue);
								DoCast(Me, SpellIds.Rogue);

								break;
							case PlayerClass.Deathknight:
								Talk(TextIds.SayDeathKnight);
								DoCast(Me, SpellIds.DeathKnight);

								break;
							default:
								break;
						}

					Events.ScheduleEvent(EventIds.Classcall, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));

					break;
			}

			if (Me.HasUnitState(UnitState.Casting))
				return;
		});

		// Phase3 begins when health below 20 pct
		if (!Phase3 &&
			HealthBelowPct(20))
		{
			var constructList = Me.GetCreatureListWithEntryInGrid(CreatureIds.BoneConstruct, 500.0f);

			foreach (var creature in constructList)
				if (creature != null &&
					!creature.IsAlive)
				{
					creature.Respawn();
					DoZoneInCombat(creature);
					creature.RemoveUnitFlag(UnitFlags.Uninteractible);
					creature.ReactState = ReactStates.Aggressive;
					creature.SetStandState(UnitStandStateType.Stand);
				}

			Phase3 = true;
			Talk(TextIds.SayRaiseSkeletons);
		}

		DoMeleeAttackIfReady();
	}

	private void Initialize()
	{
		Phase3 = false;
		canDespawn = false;
		DespawnTimer = 30000;
	}
}
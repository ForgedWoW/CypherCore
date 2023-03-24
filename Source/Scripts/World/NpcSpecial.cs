﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Movement;
using Game.Scripting;
using Game.Spells;

namespace Scripts.World.NpcSpecial;

internal enum SpawnType
{
	Tripwire, // no warning, summon Creature at smaller range
	AlarmBot  // cast guards mark and summon npc - if player shows up with that buff duration < 5 seconds attack
}

internal class AirForceSpawn
{
	public uint myEntry;
	public uint otherEntry;
	public SpawnType spawnType;

	public AirForceSpawn(uint _myEntry, uint _otherEntry, SpawnType _spawnType)
	{
		myEntry = _myEntry;
		otherEntry = _otherEntry;
		spawnType = _spawnType;
	}
}

internal struct CreatureIds
{
	//Torchtossingtarget
	public const uint TorchTossingTargetBunny = 25535;

	//Garments
	public const uint Shaya = 12429;
	public const uint Roberts = 12423;
	public const uint Dolf = 12427;
	public const uint Korja = 12430;
	public const uint DgKel = 12428;

	//Doctor
	public const uint DoctorAlliance = 12939;
	public const uint DoctorHorde = 12920;

	//Fireworks
	public const uint Omen = 15467;
	public const uint MinionOfOmen = 15466;
	public const uint FireworkBlue = 15879;
	public const uint FireworkGreen = 15880;
	public const uint FireworkPurple = 15881;
	public const uint FireworkRed = 15882;
	public const uint FireworkYellow = 15883;
	public const uint FireworkWhite = 15884;
	public const uint FireworkBigBlue = 15885;
	public const uint FireworkBigGreen = 15886;
	public const uint FireworkBigPurple = 15887;
	public const uint FireworkBigRed = 15888;
	public const uint FireworkBigYellow = 15889;
	public const uint FireworkBigWhite = 15890;

	public const uint ClusterBlue = 15872;
	public const uint ClusterRed = 15873;
	public const uint ClusterGreen = 15874;
	public const uint ClusterPurple = 15875;
	public const uint ClusterWhite = 15876;
	public const uint ClusterYellow = 15877;
	public const uint ClusterBigBlue = 15911;
	public const uint ClusterBigGreen = 15912;
	public const uint ClusterBigPurple = 15913;
	public const uint ClusterBigRed = 15914;
	public const uint ClusterBigWhite = 15915;
	public const uint ClusterBigYellow = 15916;
	public const uint ClusterElune = 15918;

	// Rabbitspells
	public const uint SpringRabbit = 32791;

	// TrainWrecker
	public const uint ExultingWindUpTrainWrecker = 81071;

	// Argent squire/gruntling
	public const uint ArgentSquire = 33238;

	// BountifulTable
	public const uint TheTurkeyChair = 34812;
	public const uint TheCranberryChair = 34823;
	public const uint TheStuffingChair = 34819;
	public const uint TheSweetPotatoChair = 34824;
	public const uint ThePieChair = 34822;

	// TravelerTundraMammothNPCs
	public const uint HakmudOfArgus = 32638;
	public const uint Gnimo = 32639;
	public const uint DrixBlackwrench = 32641;
	public const uint Mojodishu = 32642;

	// BrewfestReveler2
	public const uint BrewfestReveler = 24484;
}

internal struct GameobjectIds
{
	//Fireworks
	public const uint FireworkLauncher1 = 180771;
	public const uint FireworkLauncher2 = 180868;
	public const uint FireworkLauncher3 = 180850;
	public const uint ClusterLauncher1 = 180772;
	public const uint ClusterLauncher2 = 180859;
	public const uint ClusterLauncher3 = 180869;
	public const uint ClusterLauncher4 = 180874;

	//TrainWrecker
	public const uint ToyTrain = 193963;

	//RibbonPole
	public const uint RibbonPole = 181605;
}

internal struct SpellIds
{
	public const uint GuardsMark = 38067;

	//Dancingflames
	public const uint SummonBrazier = 45423;
	public const uint BrazierDance = 45427;
	public const uint FierySeduction = 47057;

	//RibbonPole
	public const uint RibbonDanceCosmetic = 29726;
	public const uint RedFireRing = 46836;
	public const uint BlueFireRing = 46842;

	//Torchtossingtarget
	public const uint TargetIndicator = 45723;

	//Garments    
	public const uint LesserHealR2 = 2052;
	public const uint FortitudeR1 = 1243;

	//Guardianspells
	public const uint Deathtouch = 5;

	//Brewfestreveler
	public const uint BrewfestToast = 41586;

	//Wormholespells
	public const uint BoreanTundra = 67834;
	public const uint SholazarBasin = 67835;
	public const uint Icecrown = 67836;
	public const uint StormPeaks = 67837;
	public const uint HowlingFjord = 67838;
	public const uint Underground = 68081;

	//Rabbitspells
	public const uint SpringFling = 61875;
	public const uint SpringRabbitJump = 61724;
	public const uint SpringRabbitWander = 61726;
	public const uint SummonBabyBunny = 61727;
	public const uint SpringRabbitInLove = 61728;

	//TrainWrecker
	public const uint ToyTrainPulse = 61551;
	public const uint WreckTrain = 62943;

	//Argent squire/gruntling
	public const uint DarnassusPennant = 63443;
	public const uint ExodarPennant = 63439;
	public const uint GnomereganPennant = 63442;
	public const uint IronforgePennant = 63440;
	public const uint StormwindPennant = 62727;
	public const uint SenjinPennant = 63446;
	public const uint UndercityPennant = 63441;
	public const uint OrgrimmarPennant = 63444;
	public const uint SilvermoonPennant = 63438;
	public const uint ThunderbluffPennant = 63445;
	public const uint AuraPostmanS = 67376;
	public const uint AuraShopS = 67377;
	public const uint AuraBankS = 67368;
	public const uint AuraTiredS = 67401;
	public const uint AuraBankG = 68849;
	public const uint AuraPostmanG = 68850;
	public const uint AuraShopG = 68851;
	public const uint AuraTiredG = 68852;
	public const uint TiredPlayer = 67334;

	//BountifulTable
	public const uint CranberryServer = 61793;
	public const uint PieServer = 61794;
	public const uint StuffingServer = 61795;
	public const uint TurkeyServer = 61796;
	public const uint SweetPotatoesServer = 61797;

	//VoidZone
	public const uint Consumption = 28874;
}

internal struct QuestConst
{
	//Lunaclawspirit
	public const uint BodyHeartA = 6001;
	public const uint BodyHeartH = 6002;

	//ChickenCluck
	public const uint Cluck = 3861;

	//Garments
	public const uint Moon = 5621;
	public const uint Light1 = 5624;
	public const uint Light2 = 5625;
	public const uint Spirit = 5648;
	public const uint Darkness = 5650;
}

internal struct TextIds
{
	//Lunaclawspirit
	public const uint TextIdDefault = 4714;
	public const uint TextIdProgress = 4715;

	//Chickencluck
	public const uint EmoteHelloA = 0;
	public const uint EmoteHelloH = 1;
	public const uint EmoteCluck = 2;

	//Doctor
	public const uint SayDoc = 0;

	//    Garments
	// Used By 12429; 12423; 12427; 12430; 12428; But Signed For 12429
	public const uint SayThanks = 0;
	public const uint SayGoodbye = 1;
	public const uint SayHealed = 2;

	//Wormholespells
	public const uint Wormhole = 14785;

	//NpcExperience
	public const uint XpOnOff = 14736;
}

internal struct GossipMenus
{
	//Wormhole
	public const int MenuIdWormhole = 10668; // "This tear in the fabric of Time and space looks ominous."
	public const int OptionIdWormhole1 = 0;  // "Borean Tundra"
	public const int OptionIdWormhole2 = 1;  // "Howling Fjord"
	public const int OptionIdWormhole3 = 2;  // "Sholazar Basin"
	public const int OptionIdWormhole4 = 3;  // "Icecrown"
	public const int OptionIdWormhole5 = 4;  // "Storm Peaks"
	public const int OptionIdWormhole6 = 5;  // "Underground..."

	//Lunaclawspirit
	public const string ItemGrant = "You Have Thought Well; Spirit. I Ask You To Grant Me The Strength Of Your Body And The Strength Of Your Heart.";

	//Pettrainer
	public const uint MenuIdPetUnlearn = 6520;
	public const uint OptionIdPleaseDo = 0;

	//NpcExperience
	public const uint MenuIdXpOnOff = 10638;
	public const uint OptionIdXpOff = 0;
	public const uint OptionIdXpOn = 1;

	//Argent squire/gruntling
	public const uint OptionIdBank = 0;
	public const uint OptionIdShop = 1;
	public const uint OptionIdMail = 2;
	public const uint OptionIdDarnassusSenjinPennant = 3;
	public const uint OptionIdExodarUndercityPennant = 4;
	public const uint OptionIdGnomereganOrgrimmarPennant = 5;
	public const uint OptionIdIronforgeSilvermoonPennant = 6;
	public const uint OptionIdStormwindThunderbluffPennant = 7;
}

internal enum SeatIds
{
	//BountifulTable
	TurkeyChair = 0,
	CranberryChair = 1,
	StuffingChair = 2,
	SweetPotatoChair = 3,
	PieChair = 4,
	FoodHolder = 5,
	PlateHolder = 6
}

internal struct Misc
{
	public static AirForceSpawn[] AirforceSpawns =
	{
		new(2614, 15241, SpawnType.AlarmBot),  //Air Force Alarm Bot (Alliance)
		new(2615, 15242, SpawnType.AlarmBot),  //Air Force Alarm Bot (Horde)
		new(21974, 21976, SpawnType.AlarmBot), //Air Force Alarm Bot (Area 52)
		new(21993, 15242, SpawnType.AlarmBot), //Air Force Guard Post (Horde - Bat Rider)
		new(21996, 15241, SpawnType.AlarmBot), //Air Force Guard Post (Alliance - Gryphon)
		new(21997, 21976, SpawnType.AlarmBot), //Air Force Guard Post (Goblin - Area 52 - Zeppelin)
		new(21999, 15241, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Alliance)
		new(22001, 15242, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Horde)
		new(22002, 15242, SpawnType.Tripwire), //Air Force Trip Wire - Ground (Horde)
		new(22003, 15241, SpawnType.Tripwire), //Air Force Trip Wire - Ground (Alliance)
		new(22063, 21976, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Goblin - Area 52)
		new(22065, 22064, SpawnType.AlarmBot), //Air Force Guard Post (Ethereal - Stormspire)
		new(22066, 22067, SpawnType.AlarmBot), //Air Force Guard Post (Scryer - Dragonhawk)
		new(22068, 22064, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Ethereal - Stormspire)
		new(22069, 22064, SpawnType.AlarmBot), //Air Force Alarm Bot (Stormspire)
		new(22070, 22067, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Scryer)
		new(22071, 22067, SpawnType.AlarmBot), //Air Force Alarm Bot (Scryer)
		new(22078, 22077, SpawnType.AlarmBot), //Air Force Alarm Bot (Aldor)
		new(22079, 22077, SpawnType.AlarmBot), //Air Force Guard Post (Aldor - Gryphon)
		new(22080, 22077, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Aldor)
		new(22086, 22085, SpawnType.AlarmBot), //Air Force Alarm Bot (Sporeggar)
		new(22087, 22085, SpawnType.AlarmBot), //Air Force Guard Post (Sporeggar - Spore Bat)
		new(22088, 22085, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Sporeggar)
		new(22090, 22089, SpawnType.AlarmBot), //Air Force Guard Post (Toshley's Station - Flying Machine)
		new(22124, 22122, SpawnType.AlarmBot), //Air Force Alarm Bot (Cenarion)
		new(22125, 22122, SpawnType.AlarmBot), //Air Force Guard Post (Cenarion - Stormcrow)
		new(22126, 22122, SpawnType.AlarmBot)  //Air Force Trip Wire - Rooftop (Cenarion Expedition)
	};

	public const float RangeTripwire = 15.0f;
	public const float RangeAlarmbot = 100.0f;

	//ChickenCluck
	public const uint FactionFriendly = 35;
	public const uint FactionChicken = 31;

	//Doctor
	public static Position[] DoctorAllianceCoords =
	{
		new(-3757.38f, -4533.05f, 14.16f, 3.62f), // Top-far-right bunk as seen from entrance
		new(-3754.36f, -4539.13f, 14.16f, 5.13f), // Top-far-left bunk
		new(-3749.54f, -4540.25f, 14.28f, 3.34f), // Far-right bunk
		new(-3742.10f, -4536.85f, 14.28f, 3.64f), // Right bunk near entrance
		new(-3755.89f, -4529.07f, 14.05f, 0.57f), // Far-left bunk
		new(-3749.51f, -4527.08f, 14.07f, 5.26f), // Mid-left bunk
		new(-3746.37f, -4525.35f, 14.16f, 5.22f)  // Left bunk near entrance
	};

	//alliance run to where
	public static Position DoctorAllianceRunTo = new(-3742.96f, -4531.52f, 11.91f);

	public static Position[] DoctorHordeCoords =
	{
		new(-1013.75f, -3492.59f, 62.62f, 4.34f), // Left, Behind
		new(-1017.72f, -3490.92f, 62.62f, 4.34f), // Right, Behind
		new(-1015.77f, -3497.15f, 62.82f, 4.34f), // Left, Mid
		new(-1019.51f, -3495.49f, 62.82f, 4.34f), // Right, Mid
		new(-1017.25f, -3500.85f, 62.98f, 4.34f), // Left, front
		new(-1020.95f, -3499.21f, 62.98f, 4.34f)  // Right, Front
	};

	//horde run to where
	public static Position DoctorHordeRunTo = new(-1016.44f, -3508.48f, 62.96f);

	public static uint[] AllianceSoldierId =
	{
		12938, // 12938 Injured Alliance Soldier
		12936, // 12936 Badly injured Alliance Soldier
		12937  // 12937 Critically injured Alliance Soldier
	};

	public static uint[] HordeSoldierId =
	{
		12923, //12923 Injured Soldier
		12924, //12924 Badly injured Soldier
		12925  //12925 Critically injured Soldier
	};

	//    WormholeSpells
	public const uint DataShowUnderground = 1;

	//Fireworks
	public const uint AnimGoLaunchFirework = 3;
	public const uint ZoneMoonglade = 493;

	public static Position omenSummonPos = new(7558.993f, -2839.999f, 450.0214f, 4.46f);

	public const uint AuraDurationTimeLeft = 30000;

	//Argent squire/gruntling
	public const uint AchievementPonyUp = 3736;

	public static Tuple<uint, uint>[] bannerSpells =
	{
		Tuple.Create(SpellIds.DarnassusPennant, SpellIds.SenjinPennant), Tuple.Create(SpellIds.ExodarPennant, SpellIds.UndercityPennant), Tuple.Create(SpellIds.GnomereganPennant, SpellIds.OrgrimmarPennant), Tuple.Create(SpellIds.IronforgePennant, SpellIds.SilvermoonPennant), Tuple.Create(SpellIds.StormwindPennant, SpellIds.ThunderbluffPennant)
	};
}

[Script]
internal class npc_air_force_bots : NullCreatureAI
{
	private readonly AirForceSpawn _spawn;
	private readonly List<ObjectGuid> _toAttack = new();
	private ObjectGuid _myGuard;

	public npc_air_force_bots(Creature creature) : base(creature)
	{
		_spawn = FindSpawnFor(creature.Entry);
	}

	public override void UpdateAI(uint diff)
	{
		if (_toAttack.Empty())
			return;

		var guard = GetOrSummonGuard();

		if (guard == null)
			return;

		// Keep the list of targets for later on when the guards will be alive
		if (!guard.IsAlive)
			return;

		for (var i = 0; i < _toAttack.Count; ++i)
		{
			var guid = _toAttack[i];

			var target = Global.ObjAccessor.GetUnit(Me, guid);

			if (!target)
				continue;

			if (guard.IsEngagedBy(target))
				continue;

			guard.EngageWithTarget(target);

			if (_spawn.spawnType == SpawnType.AlarmBot)
				guard.CastSpell(target, SpellIds.GuardsMark, true);
		}

		_toAttack.Clear();
	}

	public override void MoveInLineOfSight(Unit who)
	{
		// guards are only spawned against players
		if (!who.IsPlayer)
			return;

		// we're already scheduled to attack this player on our next tick, don't bother checking
		if (_toAttack.Contains(who.GUID))
			return;

		// check if they're in range
		if (!who.IsWithinDistInMap(Me, (_spawn.spawnType == SpawnType.AlarmBot) ? Misc.RangeAlarmbot : Misc.RangeTripwire))
			return;

		// check if they're hostile
		if (!(Me.IsHostileTo(who) || who.IsHostileTo(Me)))
			return;

		// check if they're a valid attack Target
		if (!Me.IsValidAttackTarget(who))
			return;

		if ((_spawn.spawnType == SpawnType.Tripwire) &&
			who.IsFlying)
			return;

		_toAttack.Add(who.GUID);
	}

	private static AirForceSpawn FindSpawnFor(uint entry)
	{
		foreach (var spawn in Misc.AirforceSpawns)
			if (spawn.myEntry == entry)
				return spawn;

		return null;
	}

	private Creature GetOrSummonGuard()
	{
		var guard = ObjectAccessor.GetCreature(Me, _myGuard);

		if (guard == null &&
			(guard = Me.SummonCreature(_spawn.otherEntry, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromMinutes(5))))
			_myGuard = guard.GUID;

		return guard;
	}
}

[Script]
internal class npc_chicken_cluck : ScriptedAI
{
	private uint ResetFlagTimer;

	public npc_chicken_cluck(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();
		Me.Faction = Misc.FactionChicken;
		Me.RemoveNpcFlag(NPCFlags.QuestGiver);
	}

	public override void JustEngagedWith(Unit who) { }

	public override void UpdateAI(uint diff)
	{
		// Reset flags after a certain Time has passed so that the next player has to start the 'event' again
		if (Me.HasNpcFlag(NPCFlags.QuestGiver))
		{
			if (ResetFlagTimer <= diff)
			{
				EnterEvadeMode();

				return;
			}
			else
			{
				ResetFlagTimer -= diff;
			}
		}

		if (UpdateVictim())
			DoMeleeAttackIfReady();
	}

	public override void ReceiveEmote(Player player, TextEmotes emote)
	{
		switch (emote)
		{
			case TextEmotes.Chicken:
				if (player.GetQuestStatus(QuestConst.Cluck) == QuestStatus.None &&
					RandomHelper.Rand32() % 30 == 1)
				{
					Me.SetNpcFlag(NPCFlags.QuestGiver);
					Me.Faction = Misc.FactionFriendly;
					Talk(player.Team == TeamFaction.Horde ? TextIds.EmoteHelloH : TextIds.EmoteHelloA);
				}

				break;
			case TextEmotes.Cheer:
				if (player.GetQuestStatus(QuestConst.Cluck) == QuestStatus.Complete)
				{
					Me.SetNpcFlag(NPCFlags.QuestGiver);
					Me.Faction = Misc.FactionFriendly;
					Talk(TextIds.EmoteCluck);
				}

				break;
		}
	}

	public override void OnQuestAccept(Player player, Quest quest)
	{
		if (quest.Id == QuestConst.Cluck)
			Reset();
	}

	public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
	{
		if (quest.Id == QuestConst.Cluck)
			Reset();
	}

	private void Initialize()
	{
		ResetFlagTimer = 120000;
	}
}

[Script]
internal class npc_dancing_flames : ScriptedAI
{
	public npc_dancing_flames(Creature creature) : base(creature) { }

	public override void Reset()
	{
		DoCastSelf(SpellIds.SummonBrazier, new CastSpellExtraArgs(true));
		DoCastSelf(SpellIds.BrazierDance, new CastSpellExtraArgs(false));
		Me.EmoteState = Emote.StateDance;
		Me.Location.Relocate(Me.Location.X, Me.Location.Y, Me.Location.Z + 1.05f);
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff);
	}

	public override void ReceiveEmote(Player player, TextEmotes emote)
	{
		if (Me.IsWithinLOS(player.Location.X, player.Location.Y, player.Location.Z) &&
			Me.IsWithinDistInMap(player, 30.0f))
		{
			// She responds to emotes not instantly but ~1500ms later
			// If you first /bow, then /wave before dancing flames bow back, it doesnt bow at all and only does wave
			// If you're performing emotes too fast, she will not respond to them
			// Means she just replaces currently scheduled event with new after receiving new Emote
			Scheduler.CancelAll();

			switch (emote)
			{
				case TextEmotes.Kiss:
					Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotShy));

					break;
				case TextEmotes.Wave:
					Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotWave));

					break;
				case TextEmotes.Bow:
					Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotBow));

					break;
				case TextEmotes.Joke:
					Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotLaugh));

					break;
				case TextEmotes.Dance:
					if (!player.HasAura(SpellIds.FierySeduction))
					{
						DoCast(player, SpellIds.FierySeduction, new CastSpellExtraArgs(true));
						Me.SetFacingTo(Me.Location.GetAbsoluteAngle(player.Location));
					}

					break;
			}
		}
	}
}

[Script]
internal class npc_torch_tossing_target_bunny_controller : ScriptedAI
{
	private ObjectGuid _lastTargetGUID;

	private uint _targetTimer;

	public npc_torch_tossing_target_bunny_controller(Creature creature) : base(creature)
	{
		_targetTimer = 3000;
	}

	public override void UpdateAI(uint diff)
	{
		if (_targetTimer < diff)
		{
			var target = Global.ObjAccessor.GetUnit(Me, DoSearchForTargets(_lastTargetGUID));

			if (target)
				target.CastSpell(target, SpellIds.TargetIndicator, true);

			_targetTimer = 3000;
		}
		else
		{
			_targetTimer -= diff;
		}
	}

	private ObjectGuid DoSearchForTargets(ObjectGuid lastTargetGUID)
	{
		var targets = Me.GetCreatureListWithEntryInGrid(CreatureIds.TorchTossingTargetBunny, 60.0f);
		targets.RemoveAll(creature => creature.GUID == lastTargetGUID);

		if (!targets.Empty())
		{
			_lastTargetGUID = targets.SelectRandom().GUID;

			return _lastTargetGUID;
		}

		return ObjectGuid.Empty;
	}
}

[Script]
internal class npc_midsummer_bunny_pole : ScriptedAI
{
	private bool running;

	public npc_midsummer_bunny_pole(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		Scheduler.SetValidator(() => running);

		Scheduler.Schedule(TimeSpan.FromMilliseconds(1),
							task =>
							{
								if (checkNearbyPlayers())
								{
									Reset();

									return;
								}

								var go = Me.FindNearestGameObject(GameobjectIds.RibbonPole, 10.0f);

								if (go)
									Me.CastSpell(go, SpellIds.RedFireRing, true);

								task.Schedule(TimeSpan.FromSeconds(5),
											task1 =>
											{
												if (checkNearbyPlayers())
												{
													Reset();

													return;
												}

												go = Me.FindNearestGameObject(GameobjectIds.RibbonPole, 10.0f);

												if (go)
													Me.CastSpell(go, SpellIds.BlueFireRing, true);

												task.Repeat(TimeSpan.FromSeconds(5));
											});
							});
	}

	public override void DoAction(int action)
	{
		// Don't start event if it's already running.
		if (running)
			return;

		running = true;
		//events.ScheduleEvent(EVENT_CAST_RED_FIRE_RING, 1);
	}

	public override void UpdateAI(uint diff)
	{
		if (!running)
			return;

		Scheduler.Update(diff);
	}

	private void Initialize()
	{
		Scheduler.CancelAll();
		running = false;
	}

	private bool checkNearbyPlayers()
	{
		// Returns true if no nearby player has aura "Test Ribbon Pole Channel".
		List<Unit> players = new();
		var check = new UnitAuraCheck<Player>(true, SpellIds.RibbonDanceCosmetic);
		var searcher = new PlayerListSearcher(Me, players, check);
		Cell.VisitGrid(Me, searcher, 10.0f);

		return players.Empty();
	}
}

[Script]
internal class npc_doctor : ScriptedAI
{
	private readonly List<Position> Coordinates = new();

	private readonly List<ObjectGuid> Patients = new();

	private bool Event;
	private uint PatientDiedCount;
	private uint PatientSavedCount;

	private ObjectGuid PlayerGUID;
	private uint SummonPatientCount;

	private uint SummonPatientTimer;

	public npc_doctor(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();
		Me.RemoveUnitFlag(UnitFlags.Uninteractible);
	}

	public void BeginEvent(Player player)
	{
		PlayerGUID = player.GUID;

		SummonPatientTimer = 10000;
		SummonPatientCount = 0;
		PatientDiedCount = 0;
		PatientSavedCount = 0;

		switch (Me.Entry)
		{
			case CreatureIds.DoctorAlliance:
				foreach (var coord in Misc.DoctorAllianceCoords)
					Coordinates.Add(coord);

				break;
			case CreatureIds.DoctorHorde:
				foreach (var coord in Misc.DoctorHordeCoords)
					Coordinates.Add(coord);

				break;
		}

		Event = true;
		Me.SetUnitFlag(UnitFlags.Uninteractible);
	}

	public void PatientDied(Position point)
	{
		var player = Global.ObjAccessor.GetPlayer(Me, PlayerGUID);

		if (player && ((player.GetQuestStatus(6624) == QuestStatus.Incomplete) || (player.GetQuestStatus(6622) == QuestStatus.Incomplete)))
		{
			++PatientDiedCount;

			if (PatientDiedCount > 5 && Event)
			{
				if (player.GetQuestStatus(6624) == QuestStatus.Incomplete)
					player.FailQuest(6624);
				else if (player.GetQuestStatus(6622) == QuestStatus.Incomplete)
					player.FailQuest(6622);

				Reset();

				return;
			}

			Coordinates.Add(point);
		}
		else
			// If no player or player abandon quest in progress
		{
			Reset();
		}
	}

	public void PatientSaved(Creature soldier, Player player, Position point)
	{
		if (player && PlayerGUID == player.GUID)
			if ((player.GetQuestStatus(6624) == QuestStatus.Incomplete) ||
				(player.GetQuestStatus(6622) == QuestStatus.Incomplete))
			{
				++PatientSavedCount;

				if (PatientSavedCount == 15)
				{
					if (!Patients.Empty())
						foreach (var guid in Patients)
						{
							var patient = ObjectAccessor.GetCreature(Me, guid);

							if (patient)
								patient.SetDeathState(DeathState.JustDied);
						}

					if (player.GetQuestStatus(6624) == QuestStatus.Incomplete)
						player.AreaExploredOrEventHappens(6624);
					else if (player.GetQuestStatus(6622) == QuestStatus.Incomplete)
						player.AreaExploredOrEventHappens(6622);

					Reset();

					return;
				}

				Coordinates.Add(point);
			}
	}

	public override void UpdateAI(uint diff)
	{
		if (Event && SummonPatientCount >= 20)
		{
			Reset();

			return;
		}

		if (Event)
		{
			if (SummonPatientTimer <= diff)
			{
				if (Coordinates.Empty())
					return;

				uint patientEntry;

				switch (Me.Entry)
				{
					case CreatureIds.DoctorAlliance:
						patientEntry = Misc.AllianceSoldierId[RandomHelper.Rand32() % 3];

						break;
					case CreatureIds.DoctorHorde:
						patientEntry = Misc.HordeSoldierId[RandomHelper.Rand32() % 3];

						break;
					default:
						Log.Logger.Error("Invalid entry for Triage doctor. Please check your database");

						return;
				}

				var index = RandomHelper.IRand(0, Coordinates.Count - 1);

				Creature Patient = Me.SummonCreature(patientEntry, Coordinates[index], TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(5));

				if (Patient)
				{
					//303, this flag appear to be required for client side Item.spell to work (TARGET_SINGLE_FRIEND)
					Patient.SetUnitFlag(UnitFlags.PlayerControlled);

					Patients.Add(Patient.GUID);
					((npc_injured_patient)Patient.AI).DoctorGUID = Me.GUID;
					((npc_injured_patient)Patient.AI).Coord = Coordinates[index];

					Coordinates.RemoveAt(index);
				}

				SummonPatientTimer = 10000;
				++SummonPatientCount;
			}
			else
			{
				SummonPatientTimer -= diff;
			}
		}
	}

	public override void JustEngagedWith(Unit who) { }

	public override void OnQuestAccept(Player player, Quest quest)
	{
		if ((quest.Id == 6624) ||
			(quest.Id == 6622))
			BeginEvent(player);
	}

	private void Initialize()
	{
		PlayerGUID.Clear();

		SummonPatientTimer = 10000;
		SummonPatientCount = 0;
		PatientDiedCount = 0;
		PatientSavedCount = 0;

		Patients.Clear();
		Coordinates.Clear();

		Event = false;
	}
}

[Script]
public class npc_injured_patient : ScriptedAI
{
	public Position Coord;

	public ObjectGuid DoctorGUID;

	public npc_injured_patient(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		//no select
		Me.RemoveUnitFlag(UnitFlags.Uninteractible);

		//no regen health
		Me.SetUnitFlag(UnitFlags.InCombat);

		//to make them lay with face down
		Me.SetStandState(UnitStandStateType.Dead);

		var mobId = Me.Entry;

		switch (mobId)
		{
			//lower max health
			case 12923:
			case 12938: //Injured Soldier
				Me.SetHealth(Me.CountPctFromMaxHealth(75));

				break;
			case 12924:
			case 12936: //Badly injured Soldier
				Me.SetHealth(Me.CountPctFromMaxHealth(50));

				break;
			case 12925:
			case 12937: //Critically injured Soldier
				Me.SetHealth(Me.CountPctFromMaxHealth(25));

				break;
		}
	}

	public override void JustEngagedWith(Unit who) { }

	public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
	{
		var player = caster.AsPlayer;

		if (!player ||
			!Me.IsAlive ||
			spellInfo.Id != 20804)
			return;

		if (player.GetQuestStatus(6624) == QuestStatus.Incomplete ||
			player.GetQuestStatus(6622) == QuestStatus.Incomplete)
			if (!DoctorGUID.IsEmpty)
			{
				var doctor = ObjectAccessor.GetCreature(Me, DoctorGUID);

				if (doctor)
					((npc_doctor)doctor.AI).PatientSaved(Me, player, Coord);
			}

		//make not selectable
		Me.SetUnitFlag(UnitFlags.Uninteractible);

		//regen health
		Me.RemoveUnitFlag(UnitFlags.InCombat);

		//stand up
		Me.SetStandState(UnitStandStateType.Stand);

		Talk(TextIds.SayDoc);

		var mobId = Me.Entry;
		Me.SetWalk(false);

		switch (mobId)
		{
			case 12923:
			case 12924:
			case 12925:
				Me.MotionMaster.MovePoint(0, Misc.DoctorHordeRunTo);

				break;
			case 12936:
			case 12937:
			case 12938:
				Me.MotionMaster.MovePoint(0, Misc.DoctorAllianceRunTo);

				break;
		}
	}

	public override void UpdateAI(uint diff)
	{
		//lower HP on every world tick makes it a useful counter, not officlone though
		if (Me.IsAlive &&
			Me.Health > 6)
			Me.ModifyHealth(-5);

		if (Me.IsAlive &&
			Me.Health <= 6)
		{
			Me.RemoveUnitFlag(UnitFlags.InCombat);
			Me.SetUnitFlag(UnitFlags.Uninteractible);
			Me.SetDeathState(DeathState.JustDied);
			Me.SetUnitFlag3(UnitFlags3.FakeDead);

			if (!DoctorGUID.IsEmpty)
			{
				var doctor = ObjectAccessor.GetCreature((Me), DoctorGUID);

				if (doctor)
					((npc_doctor)doctor.AI).PatientDied(Coord);
			}
		}
	}

	private void Initialize()
	{
		DoctorGUID.Clear();
		Coord = null;
	}
}

[Script]
internal class npc_garments_of_quests : EscortAI
{
	private readonly uint quest;
	private bool CanRun;
	private ObjectGuid CasterGUID;

	private bool IsHealed;

	private uint RunAwayTimer;

	public npc_garments_of_quests(Creature creature) : base(creature)
	{
		switch (Me.Entry)
		{
			case CreatureIds.Shaya:
				quest = QuestConst.Moon;

				break;
			case CreatureIds.Roberts:
				quest = QuestConst.Light1;

				break;
			case CreatureIds.Dolf:
				quest = QuestConst.Light2;

				break;
			case CreatureIds.Korja:
				quest = QuestConst.Spirit;

				break;
			case CreatureIds.DgKel:
				quest = QuestConst.Darkness;

				break;
			default:
				quest = 0;

				break;
		}

		Initialize();
	}

	public override void Reset()
	{
		CasterGUID.Clear();

		Initialize();

		Me.SetStandState(UnitStandStateType.Kneel);
		// expect database to have RegenHealth=0
		Me.SetHealth(Me.CountPctFromMaxHealth(70));
	}

	public override void JustEngagedWith(Unit who) { }

	public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
	{
		if (spellInfo.Id == SpellIds.LesserHealR2 ||
			spellInfo.Id == SpellIds.FortitudeR1)
		{
			//not while in combat
			if (Me.IsInCombat)
				return;

			//nothing to be done now
			if (IsHealed && CanRun)
				return;

			var player = caster.AsPlayer;

			if (player)
			{
				if (quest != 0 &&
					player.GetQuestStatus(quest) == QuestStatus.Incomplete)
				{
					if (IsHealed &&
						!CanRun &&
						spellInfo.Id == SpellIds.FortitudeR1)
					{
						Talk(TextIds.SayThanks, player);
						CanRun = true;
					}
					else if (!IsHealed &&
							spellInfo.Id == SpellIds.LesserHealR2)
					{
						CasterGUID = player.GUID;
						Me.SetStandState(UnitStandStateType.Stand);
						Talk(TextIds.SayHealed, player);
						IsHealed = true;
					}
				}

				// give quest credit, not expect any special quest objectives
				if (CanRun)
					player.TalkedToCreature(Me.Entry, Me.GUID);
			}
		}
	}

	public override void WaypointReached(uint waypointId, uint pathId) { }

	public override void UpdateAI(uint diff)
	{
		if (CanRun && !Me.IsInCombat)
		{
			if (RunAwayTimer <= diff)
			{
				var unit = Global.ObjAccessor.GetUnit(Me, CasterGUID);

				if (unit)
				{
					switch (Me.Entry)
					{
						case CreatureIds.Shaya:
						case CreatureIds.Roberts:
						case CreatureIds.Dolf:
						case CreatureIds.Korja:
						case CreatureIds.DgKel:
							Talk(TextIds.SayGoodbye, unit);

							break;
					}

					Start(false, true);
				}
				else
				{
					EnterEvadeMode(); //something went wrong
				}

				RunAwayTimer = 30000;
			}
			else
			{
				RunAwayTimer -= diff;
			}
		}

		base.UpdateAI(diff);
	}

	private void Initialize()
	{
		IsHealed = false;
		CanRun = false;

		RunAwayTimer = 5000;
	}
}

[Script]
internal class npc_guardian : ScriptedAI
{
	public npc_guardian(Creature creature) : base(creature) { }

	public override void Reset()
	{
		Me.SetUnitFlag(UnitFlags.NonAttackable);
	}

	public override void JustEngagedWith(Unit who) { }

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		if (Me.IsAttackReady())
		{
			DoCastVictim(SpellIds.Deathtouch, new CastSpellExtraArgs(true));
			Me.ResetAttackTimer();
		}
	}
}

[Script]
internal class npc_steam_tonk : ScriptedAI
{
	public npc_steam_tonk(Creature creature) : base(creature) { }

	public override void Reset() { }

	public override void JustEngagedWith(Unit who) { }

	public void OnPossess(bool apply)
	{
		if (apply)
		{
			// Initialize the Action bar without the melee attack command
			Me.InitCharmInfo();
			Me.GetCharmInfo().InitEmptyActionBar(false);

			Me.ReactState = ReactStates.Passive;
		}
		else
		{
			Me.ReactState = ReactStates.Aggressive;
		}
	}
}

[Script]
internal class npc_brewfest_reveler : ScriptedAI
{
	public npc_brewfest_reveler(Creature creature) : base(creature) { }

	public override void ReceiveEmote(Player player, TextEmotes emote)
	{
		if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
			return;

		if (emote == TextEmotes.Dance)
			Me.CastSpell(player, SpellIds.BrewfestToast, false);
	}
}

[Script]
internal class npc_brewfest_reveler_2 : ScriptedAI
{
	private readonly List<ObjectGuid> _revelerGuids = new();

	private readonly Emote[] BrewfestRandomEmote =
	{
		Emote.OneshotQuestion, Emote.OneshotApplaud, Emote.OneshotShout, Emote.OneshotEatNoSheathe, Emote.OneshotLaughNoSheathe
	};

	public npc_brewfest_reveler_2(Creature creature) : base(creature) { }

	public override void Reset()
	{
		Scheduler.CancelAll();

		Scheduler.Schedule(TimeSpan.FromSeconds(1),
							TimeSpan.FromSeconds(2),
							fillListTask =>
							{
								var creatureList = Me.GetCreatureListWithEntryInGrid(CreatureIds.BrewfestReveler, 5.0f);

								foreach (var creature in creatureList)
									if (creature != Me)
										_revelerGuids.Add(creature.GUID);

								fillListTask.Schedule(TimeSpan.FromSeconds(1),
													TimeSpan.FromSeconds(2),
													faceToTask =>
													{
														// Turn to random brewfest reveler within set range
														if (!_revelerGuids.Empty())
														{
															var creature = ObjectAccessor.GetCreature(Me, _revelerGuids.SelectRandom());

															if (creature != null)
																Me.SetFacingToObject(creature);
														}

														Scheduler.Schedule(TimeSpan.FromSeconds(2),
																			TimeSpan.FromSeconds(6),
																			emoteTask =>
																			{
																				var nextTask = (TaskContext task) =>
																				{
																					// If dancing stop before next random State
																					if (Me.EmoteState == Emote.StateDance)
																						Me.EmoteState = Emote.OneshotNone;

																					// Random EVENT_EMOTE or EVENT_FACETO
																					if (RandomHelper.randChance(50))
																						faceToTask.Repeat(TimeSpan.FromSeconds(1));
																					else
																						emoteTask.Repeat(TimeSpan.FromSeconds(1));
																				};

																				// Play random Emote or dance
																				if (RandomHelper.randChance(50))
																				{
																					Me.HandleEmoteCommand(BrewfestRandomEmote.SelectRandom());
																					Scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6), nextTask);
																				}
																				else
																				{
																					Me.EmoteState = Emote.StateDance;
																					Scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12), nextTask);
																				}
																			});
													});
							});
	}

	// Copied from old script. I don't know if this is 100% correct.
	public override void ReceiveEmote(Player player, TextEmotes emote)
	{
		if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
			return;

		if (emote == TextEmotes.Dance)
			Me.CastSpell(player, SpellIds.BrewfestToast, false);
	}

	public override void UpdateAI(uint diff)
	{
		UpdateVictim();

		Scheduler.Update(diff);
	}
}

[Script]
internal class npc_wormhole : PassiveAI
{
	private bool _showUnderground;

	public npc_wormhole(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void InitializeAI()
	{
		Initialize();
	}

	public override bool OnGossipHello(Player player)
	{
		player.InitGossipMenu(GossipMenus.MenuIdWormhole);

		if (Me.IsSummon)
			if (player == Me.ToTempSummon().GetSummoner())
			{
				player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole1, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);
				player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole2, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 2);
				player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole3, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 3);
				player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole4, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 4);
				player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole5, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 5);

				if (_showUnderground)
					player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole6, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 6);

				player.SendGossipMenu(TextIds.Wormhole, Me.GUID);
			}

		return true;
	}

	public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
	{
		var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
		player.PlayerTalkClass.ClearMenus();

		switch (action)
		{
			case GossipAction.GOSSIP_ACTION_INFO_DEF + 1: // Borean Tundra
				player.CloseGossipMenu();
				DoCast(player, SpellIds.BoreanTundra, new CastSpellExtraArgs(false));

				break;
			case GossipAction.GOSSIP_ACTION_INFO_DEF + 2: // Howling Fjord
				player.CloseGossipMenu();
				DoCast(player, SpellIds.HowlingFjord, new CastSpellExtraArgs(false));

				break;
			case GossipAction.GOSSIP_ACTION_INFO_DEF + 3: // Sholazar Basin
				player.CloseGossipMenu();
				DoCast(player, SpellIds.SholazarBasin, new CastSpellExtraArgs(false));

				break;
			case GossipAction.GOSSIP_ACTION_INFO_DEF + 4: // Icecrown
				player.CloseGossipMenu();
				DoCast(player, SpellIds.Icecrown, new CastSpellExtraArgs(false));

				break;
			case GossipAction.GOSSIP_ACTION_INFO_DEF + 5: // Storm peaks
				player.CloseGossipMenu();
				DoCast(player, SpellIds.StormPeaks, new CastSpellExtraArgs(false));

				break;
			case GossipAction.GOSSIP_ACTION_INFO_DEF + 6: // Underground
				player.CloseGossipMenu();
				DoCast(player, SpellIds.Underground, new CastSpellExtraArgs(false));

				break;
		}

		return true;
	}

	private void Initialize()
	{
		_showUnderground = RandomHelper.URand(0, 100) == 0; // Guessed value, it is really rare though
	}
}

[Script]
internal class npc_spring_rabbit : ScriptedAI
{
	private uint bunnyTimer;

	private bool inLove;
	private uint jumpTimer;
	private ObjectGuid rabbitGUID;
	private uint searchTimer;

	public npc_spring_rabbit(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();
		var owner = Me.OwnerUnit;

		if (owner)
			Me.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
	}

	public override void JustEngagedWith(Unit who) { }

	public override void DoAction(int param)
	{
		inLove = true;
		var owner = Me.OwnerUnit;

		if (owner)
			owner.CastSpell(owner, SpellIds.SpringFling, true);
	}

	public override void UpdateAI(uint diff)
	{
		if (inLove)
		{
			if (jumpTimer <= diff)
			{
				var rabbit = Global.ObjAccessor.GetUnit(Me, rabbitGUID);

				if (rabbit)
					DoCast(rabbit, SpellIds.SpringRabbitJump);

				jumpTimer = RandomHelper.URand(5000, 10000);
			}
			else
			{
				jumpTimer -= diff;
			}

			if (bunnyTimer <= diff)
			{
				DoCast(SpellIds.SummonBabyBunny);
				bunnyTimer = RandomHelper.URand(20000, 40000);
			}
			else
			{
				bunnyTimer -= diff;
			}
		}
		else
		{
			if (searchTimer <= diff)
			{
				var rabbit = Me.FindNearestCreature(CreatureIds.SpringRabbit, 10.0f);

				if (rabbit)
				{
					if (rabbit == Me ||
						rabbit.HasAura(SpellIds.SpringRabbitInLove))
						return;

					Me.AddAura(SpellIds.SpringRabbitInLove, Me);
					DoAction(1);
					rabbit.AddAura(SpellIds.SpringRabbitInLove, rabbit);
					rabbit.AI.DoAction(1);
					rabbit.CastSpell(rabbit, SpellIds.SpringRabbitJump, true);
					rabbitGUID = rabbit.GUID;
				}

				searchTimer = RandomHelper.URand(5000, 10000);
			}
			else
			{
				searchTimer -= diff;
			}
		}
	}

	private void Initialize()
	{
		inLove = false;
		rabbitGUID.Clear();
		jumpTimer = RandomHelper.URand(5000, 10000);
		bunnyTimer = RandomHelper.URand(10000, 20000);
		searchTimer = RandomHelper.URand(5000, 10000);
	}
}

[Script]
internal class npc_imp_in_a_ball : ScriptedAI
{
	private ObjectGuid summonerGUID;

	public npc_imp_in_a_ball(Creature creature) : base(creature)
	{
		summonerGUID.Clear();
	}

	public override void IsSummonedBy(WorldObject summoner)
	{
		if (summoner.IsTypeId(TypeId.Player))
		{
			summonerGUID = summoner.GUID;

			Scheduler.Schedule(TimeSpan.FromSeconds(3),
								task =>
								{
									var owner = Global.ObjAccessor.GetPlayer(Me, summonerGUID);

									if (owner)
										Global.CreatureTextMgr.SendChat(Me, 0, owner, owner.Group ? ChatMsg.MonsterParty : ChatMsg.MonsterWhisper, Language.Addon, CreatureTextRange.Normal);
								});
		}
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff);
	}
}

internal struct TrainWrecker
{
	public const int EventDoJump = 1;
	public const int EventDoFacing = 2;
	public const int EventDoWreck = 3;
	public const int EventDoDance = 4;
	public const uint MoveidChase = 1;
	public const uint MoveidJump = 2;
}

[Script]
internal class npc_train_wrecker : NullCreatureAI
{
	private bool _isSearching;
	private byte _nextAction;
	private ObjectGuid _target;
	private uint _timer;

	public npc_train_wrecker(Creature creature) : base(creature)
	{
		_isSearching = true;
		_nextAction = 0;
		_timer = 1 * Time.InMilliseconds;
	}

	public override void UpdateAI(uint diff)
	{
		if (_isSearching)
		{
			if (diff < _timer)
			{
				_timer -= diff;
			}
			else
			{
				var target = Me.FindNearestGameObject(GameobjectIds.ToyTrain, 15.0f);

				if (target)
				{
					_isSearching = false;
					_target = target.GUID;
					Me.SetWalk(true);
					Me.MotionMaster.MovePoint(TrainWrecker.MoveidChase, target.GetNearPosition(3.0f, target.Location.GetAbsoluteAngle(Me.Location)));
				}
				else
				{
					_timer = 3 * Time.InMilliseconds;
				}
			}
		}
		else
		{
			switch (_nextAction)
			{
				case TrainWrecker.EventDoJump:
				{
					var target = VerifyTarget();

					if (target)
						Me.MotionMaster.MoveJump(target.Location, 5.0f, 10.0f, TrainWrecker.MoveidJump);

					_nextAction = 0;
				}

					break;
				case TrainWrecker.EventDoFacing:
				{
					var target = VerifyTarget();

					if (target)
					{
						Me.SetFacingTo(target.Location.Orientation);
						Me.HandleEmoteCommand(Emote.OneshotAttack1h);
						_timer = (uint)(1.5 * Time.InMilliseconds);
						_nextAction = TrainWrecker.EventDoWreck;
					}
					else
					{
						_nextAction = 0;
					}
				}

					break;
				case TrainWrecker.EventDoWreck:
				{
					if (diff < _timer)
					{
						_timer -= diff;

						break;
					}

					var target = VerifyTarget();

					if (target)
					{
						Me.CastSpell(target, SpellIds.WreckTrain, false);
						_timer = 2 * Time.InMilliseconds;
						_nextAction = TrainWrecker.EventDoDance;
					}
					else
					{
						_nextAction = 0;
					}
				}

					break;
				case TrainWrecker.EventDoDance:
					if (diff < _timer)
					{
						_timer -= diff;

						break;
					}

					Me.UpdateEntry(CreatureIds.ExultingWindUpTrainWrecker);
					Me.EmoteState = Emote.OneshotDance;
					Me.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
					_nextAction = 0;

					break;
				default:
					break;
			}
		}
	}

	public override void MovementInform(MovementGeneratorType type, uint id)
	{
		if (id == TrainWrecker.MoveidChase)
			_nextAction = TrainWrecker.EventDoJump;
		else if (id == TrainWrecker.MoveidJump)
			_nextAction = TrainWrecker.EventDoFacing;
	}

	private GameObject VerifyTarget()
	{
		var target = ObjectAccessor.GetGameObject(Me, _target);

		if (target)
			return target;

		Me.HandleEmoteCommand(Emote.OneshotRude);
		Me.DespawnOrUnsummon(TimeSpan.FromSeconds(3));

		return null;
	}
}

[Script]
internal class npc_argent_squire_gruntling : ScriptedAI
{
	public npc_argent_squire_gruntling(Creature creature) : base(creature) { }

	public override void Reset()
	{
		var owner = Me.OwnerUnit?.AsPlayer;

		if (owner != null)
		{
			var ownerTired = owner.GetAura(SpellIds.TiredPlayer);

			if (ownerTired != null)
			{
				var squireTired = Me.AddAura(IsArgentSquire() ? SpellIds.AuraTiredS : SpellIds.AuraTiredG, Me);

				squireTired?.SetDuration(ownerTired.Duration);
			}

			if (owner.HasAchieved(Misc.AchievementPonyUp) &&
				!Me.HasAura(SpellIds.AuraTiredS) &&
				!Me.HasAura(SpellIds.AuraTiredG))
			{
				Me.SetNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor);

				return;
			}
		}

		Me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor);
	}

	public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
	{
		switch (gossipListId)
		{
			case GossipMenus.OptionIdBank:
			{
				Me.RemoveNpcFlag(NPCFlags.Mailbox | NPCFlags.Vendor);
				var _bankAura = IsArgentSquire() ? SpellIds.AuraBankS : SpellIds.AuraBankG;

				if (!Me.HasAura(_bankAura))
					DoCastSelf(_bankAura);

				if (!player.HasAura(SpellIds.TiredPlayer))
					player.CastSpell(player, SpellIds.TiredPlayer, true);

				break;
			}
			case GossipMenus.OptionIdShop:
			{
				Me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox);
				var _shopAura = IsArgentSquire() ? SpellIds.AuraShopS : SpellIds.AuraShopG;

				if (!Me.HasAura(_shopAura))
					DoCastSelf(_shopAura);

				if (!player.HasAura(SpellIds.TiredPlayer))
					player.CastSpell(player, SpellIds.TiredPlayer, true);

				break;
			}
			case GossipMenus.OptionIdMail:
			{
				Me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Vendor);

				var _mailAura = IsArgentSquire() ? SpellIds.AuraPostmanS : SpellIds.AuraPostmanG;

				if (!Me.HasAura(_mailAura))
					DoCastSelf(_mailAura);

				if (!player.HasAura(SpellIds.TiredPlayer))
					player.CastSpell(player, SpellIds.TiredPlayer, true);

				break;
			}
			case GossipMenus.OptionIdDarnassusSenjinPennant:
			case GossipMenus.OptionIdExodarUndercityPennant:
			case GossipMenus.OptionIdGnomereganOrgrimmarPennant:
			case GossipMenus.OptionIdIronforgeSilvermoonPennant:
			case GossipMenus.OptionIdStormwindThunderbluffPennant:
				if (IsArgentSquire())
					DoCastSelf(Misc.bannerSpells[gossipListId - 3].Item1, new CastSpellExtraArgs(true));
				else
					DoCastSelf(Misc.bannerSpells[gossipListId - 3].Item2, new CastSpellExtraArgs(true));

				player.PlayerTalkClass.SendCloseGossip();

				break;
			default:
				break;
		}

		return false;
	}

	private bool IsArgentSquire()
	{
		return Me.Entry == CreatureIds.ArgentSquire;
	}
}

[Script]
internal class npc_bountiful_table : PassiveAI
{
	private readonly Dictionary<uint, uint> ChairSpells = new()
	{
		{
			CreatureIds.TheCranberryChair, SpellIds.CranberryServer
		},
		{
			CreatureIds.ThePieChair, SpellIds.PieServer
		},
		{
			CreatureIds.TheStuffingChair, SpellIds.StuffingServer
		},
		{
			CreatureIds.TheTurkeyChair, SpellIds.TurkeyServer
		},
		{
			CreatureIds.TheSweetPotatoChair, SpellIds.SweetPotatoesServer
		}
	};

	public npc_bountiful_table(Creature creature) : base(creature) { }

	public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
	{
		var x = 0.0f;
		var y = 0.0f;
		var z = 0.0f;
		var o = 0.0f;

		switch ((SeatIds)seatId)
		{
			case SeatIds.TurkeyChair:
				x = 3.87f;
				y = 2.07f;
				o = 3.700098f;

				break;
			case SeatIds.CranberryChair:
				x = 3.87f;
				y = -2.07f;
				o = 2.460914f;

				break;
			case SeatIds.StuffingChair:
				x = -2.52f;

				break;
			case SeatIds.SweetPotatoChair:
				x = -0.09f;
				y = -3.24f;
				o = 1.186824f;

				break;
			case SeatIds.PieChair:
				x = -0.18f;
				y = 3.24f;
				o = 5.009095f;

				break;
			case SeatIds.FoodHolder:
			case SeatIds.PlateHolder:
				var holders = who.VehicleKit1;

				if (holders)
					holders.InstallAllAccessories(true);

				return;
			default:
				break;
		}

		var initializer = (MoveSplineInit init) =>
		{
			init.DisableTransportPathTransformations();
			init.MoveTo(x, y, z, false);
			init.SetFacing(o);
		};

		who.MotionMaster.LaunchMoveSpline(initializer, EventId.VehicleBoard, MovementGeneratorPriority.Highest);
		who.Events.AddEvent(new CastFoodSpell(who, ChairSpells[who.Entry]), who.Events.CalculateTime(TimeSpan.FromSeconds(1)));
		var creature = who.AsCreature;

		if (creature)
			creature.SetDisplayFromModel(0);
	}
}

[Script]
internal class npc_gen_void_zone : ScriptedAI
{
	public npc_gen_void_zone(Creature creature) : base(creature) { }

	public override void InitializeAI()
	{
		Me.ReactState = ReactStates.Passive;
	}

	public override void JustAppeared()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(2), task => { DoCastSelf(SpellIds.Consumption); });
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff);
	}
}

internal class CastFoodSpell : BasicEvent
{
	private readonly Unit _owner;
	private readonly uint _spellId;

	public CastFoodSpell(Unit owner, uint spellId)
	{
		_owner = owner;
		_spellId = spellId;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		_owner.CastSpell(_owner, _spellId, true);

		return true;
	}
}
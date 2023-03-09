// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.MagistersTerrace.PriestessDelrissa;

internal struct TextIds
{
	public const uint SayAggro = 0;
	public const uint SayDeath = 10;
}

internal struct SpellIds
{
	public const uint DispelMagic = 27609;
	public const uint FlashHeal = 17843;
	public const uint SwPainNormal = 14032;
	public const uint SwPainHeroic = 15654;
	public const uint Shield = 44291;
	public const uint RenewNormal = 44174;
	public const uint RenewHeroic = 46192;

	// Apoko
	public const uint WindfuryTotem = 27621;
	public const uint WarStomp = 46026;
	public const uint Purge = 27626;
	public const uint LesserHealingWave = 44256;
	public const uint FrostShock = 21401;
	public const uint FireNovaTotem = 44257;
	public const uint EarthbindTotem = 15786;

	public const uint HealingPotion = 15503;

	// RogueSpells
	public const uint KidneyShot = 27615;
	public const uint Gouge = 12540;
	public const uint Kick = 27613;
	public const uint Vanish = 44290;
	public const uint Backstab = 15657;
	public const uint Eviscerate = 27611;

	// WarlockSpells
	public const uint Immolate = 44267;
	public const uint ShadowBolt = 12471;
	public const uint SeedOfCorruption = 44141;
	public const uint CurseOfAgony = 14875;
	public const uint Fear = 38595;
	public const uint ImpFireball = 44164;
	public const uint SummonImp = 44163;

	// KickDown
	public const uint Knockdown = 11428;
	public const uint SnapKick = 46182;

	// MageSpells
	public const uint Polymorph = 13323;
	public const uint IceBlock = 27619;
	public const uint Blizzard = 44178;
	public const uint IceLance = 46194;
	public const uint ConeOfCold = 38384;
	public const uint Frostbolt = 15043;
	public const uint Blink = 14514;

	// WarriorSpells
	public const uint InterceptStun = 27577;
	public const uint Disarm = 27581;
	public const uint PiercingHowl = 23600;
	public const uint FrighteningShout = 19134;
	public const uint Hamstring = 27584;
	public const uint BattleShout = 27578;
	public const uint MortalStrike = 44268;

	// HunterSpells
	public const uint AimedShot = 44271;
	public const uint Shoot = 15620;
	public const uint ConcussiveShot = 27634;
	public const uint MultiShot = 31942;
	public const uint WingClip = 44286;
	public const uint FreezingTrap = 44136;

	// EngineerSpells
	public const uint GoblinDragonGun = 44272;
	public const uint RocketLaunch = 44137;
	public const uint Recombobulate = 44274;
	public const uint HighExplosiveSheep = 44276;
	public const uint FelIronBomb = 46024;
	public const uint SheepExplosion = 44279;
}

internal struct CreatureIds
{
	public const uint Sliver = 24552;
}

internal struct MiscConst
{
	public const uint MaxActiveLackey = 4;

	public const float fOrientation = 4.98f;
	public const float fZLocation = -19.921f;

	public static float[][] LackeyLocations =
	{
		new float[]
		{
			123.77f, 17.6007f
		},
		new float[]
		{
			131.731f, 15.0827f
		},
		new float[]
		{
			121.563f, 15.6213f
		},
		new float[]
		{
			129.988f, 17.2355f
		}
	};

	public static uint[] _auiAddEntries =
	{
		24557, //Kagani Nightstrike
		24558, //Elris Duskhallow
		24554, //Eramas Brightblaze
		24561, //Yazzaj
		24559, //Warlord Salaris
		24555, //Garaxxas
		24553, //Apoko
		24556  //Zelfan
	};

	public static uint[] LackeyDeath =
	{
		1, 2, 3, 4
	};

	public static uint[] PlayerDeath =
	{
		5, 6, 7, 8, 9
	};
}

[Script]
internal class boss_priestess_delrissa : BossAI
{
	public ObjectGuid[] _auiLackeyGUID = new ObjectGuid[MiscConst.MaxActiveLackey];
	private readonly List<uint> LackeyEntryList = new();

	private byte PlayersKilled;

	public boss_priestess_delrissa(Creature creature) : base(creature, DataTypes.PriestessDelrissa)
	{
		Initialize();
		LackeyEntryList.Clear();
	}

	public override void Reset()
	{
		Initialize();

		InitializeLackeys();
	}

	//this mean she at some point evaded
	public override void JustReachedHome()
	{
		Instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.Fail);
	}

	public override void JustEngagedWith(Unit who)
	{
		Talk(TextIds.SayAggro);

		foreach (var lackeyGuid in _auiLackeyGUID)
		{
			var pAdd = Global.ObjAccessor.GetUnit(Me, lackeyGuid);

			if (pAdd && !pAdd.IsEngaged)
				AddThreat(who, 0.0f, pAdd);
		}

		Instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.InProgress);
	}

	public override void KilledUnit(Unit victim)
	{
		if (!victim.IsPlayer)
			return;

		Talk(MiscConst.PlayerDeath[PlayersKilled]);

		if (PlayersKilled < 4)
			++PlayersKilled;
	}

	public override void JustDied(Unit killer)
	{
		Talk(TextIds.SayDeath);

		if (Instance.GetData(DataTypes.DelrissaDeathCount) == MiscConst.MaxActiveLackey)
			Instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.Done);
		else
			Me.RemoveDynamicFlag(UnitDynFlags.Lootable);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		PlayersKilled = 0;

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							task =>
							{
								var health = Me.Health;
								Unit target = Me;

								for (byte i = 0; i < _auiLackeyGUID.Length; ++i)
								{
									var pAdd = Global.ObjAccessor.GetUnit(Me, _auiLackeyGUID[i]);

									if (pAdd != null &&
										pAdd.IsAlive &&
										pAdd.										Health < health)
										target = pAdd;
								}

								DoCast(target, SpellIds.FlashHeal);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								Unit target = Me;

								if (RandomHelper.URand(0, 1) != 0)
								{
									var pAdd = Global.ObjAccessor.GetUnit(Me, _auiLackeyGUID[RandomHelper.Rand32() % _auiLackeyGUID.Length]);

									if (pAdd != null &&
										pAdd.IsAlive)
										target = pAdd;
								}

								DoCast(target, SpellIds.RenewNormal);
								task.Repeat(TimeSpan.FromSeconds(5));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								Unit target = Me;

								if (RandomHelper.URand(0, 1) != 0)
								{
									var pAdd = Global.ObjAccessor.GetUnit(Me, _auiLackeyGUID[RandomHelper.Rand32() % _auiLackeyGUID.Length]);

									if (pAdd != null &&
										pAdd.IsAlive &&
										!pAdd.HasAura(SpellIds.Shield))
										target = pAdd;
								}

								DoCast(target, SpellIds.Shield);
								task.Repeat(TimeSpan.FromSeconds(7.5));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(5),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

								if (target != null)
									DoCast(target, SpellIds.SwPainNormal);

								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(7.5),
							task =>
							{
								Unit target = null;

								if (RandomHelper.URand(0, 1) != 0)
								{
									target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
								}
								else
								{
									if (RandomHelper.URand(0, 1) != 0)
									{
										target = Me;
									}
									else
									{
										var pAdd = Global.ObjAccessor.GetUnit(Me, _auiLackeyGUID[RandomHelper.Rand32() % _auiLackeyGUID.Length]);

										if (pAdd != null &&
											pAdd.IsAlive)
											target = pAdd;
									}
								}

								if (target)
									DoCast(target, SpellIds.DispelMagic);

								task.Repeat(TimeSpan.FromSeconds(12));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(5),
							task =>
							{
								var homePos = Me.HomePosition;

								if (Me.Location.Z >= homePos.Z + 10)
								{
									EnterEvadeMode();

									return;
								}

								task.Repeat();
							});
	}

	private void InitializeLackeys()
	{
		//can be called if Creature are dead, so avoid
		if (!Me.IsAlive)
			return;

		byte j = 0;

		//it's empty, so first Time
		if (LackeyEntryList.Empty())
		{
			//fill vector array with entries from Creature array
			for (byte i = 0; i < LackeyEntryList.Count; ++i)
				LackeyEntryList[i] = MiscConst._auiAddEntries[i];

			//remove random entries
			LackeyEntryList.RandomResize(MiscConst.MaxActiveLackey);

			//summon all the remaining in vector
			foreach (var guid in LackeyEntryList)
			{
				Creature pAdd = Me.SummonCreature(guid, MiscConst.LackeyLocations[j][0], MiscConst.LackeyLocations[j][1], MiscConst.fZLocation, MiscConst.fOrientation, TempSummonType.CorpseDespawn);

				if (pAdd != null)
					_auiLackeyGUID[j] = pAdd.GUID;

				++j;
			}
		}
		else
		{
			foreach (var guid in LackeyEntryList)
			{
				var pAdd = Global.ObjAccessor.GetUnit(Me, _auiLackeyGUID[j]);

				//object already removed, not exist
				if (!pAdd)
				{
					pAdd = Me.SummonCreature(guid, MiscConst.LackeyLocations[j][0], MiscConst.LackeyLocations[j][1], MiscConst.fZLocation, MiscConst.fOrientation, TempSummonType.CorpseDespawn);

					if (pAdd != null)
						_auiLackeyGUID[j] = pAdd.GUID;
				}

				++j;
			}
		}
	}
}

//all 8 possible lackey use this common
internal class boss_priestess_lackey_common : ScriptedAI
{
	public ObjectGuid[] _auiLackeyGUIDs = new ObjectGuid[MiscConst.MaxActiveLackey];
	private readonly InstanceScript instance;
	private bool UsedPotion;

	public boss_priestess_lackey_common(Creature creature) : base(creature)
	{
		Initialize();
		instance = creature.InstanceScript;
	}

	public override void Reset()
	{
		Initialize();
		AcquireGUIDs();

		// in case she is not alive and Reset was for some reason called, respawn her (most likely party wipe after killing her)
		var delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);

		if (delrissa)
			if (!delrissa.IsAlive)
				delrissa.Respawn();
	}

	public override void JustEngagedWith(Unit who)
	{
		if (!who)
			return;

		foreach (var guid in _auiLackeyGUIDs)
		{
			var pAdd = Global.ObjAccessor.GetUnit(Me, guid);

			if (pAdd != null &&
				!pAdd.IsEngaged &&
				pAdd != Me)
				AddThreat(who, 0.0f, pAdd);
		}

		var delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);

		if (delrissa)
			if (delrissa.IsAlive &&
				!delrissa.IsEngaged)
				AddThreat(who, 0.0f, delrissa);
	}

	public override void JustDied(Unit killer)
	{
		var delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);
		var uiLackeyDeathCount = instance.GetData(DataTypes.DelrissaDeathCount);

		if (!delrissa)
			return;

		//should delrissa really yell if dead?
		delrissa.
			//should delrissa really yell if dead?
			AI.Talk(MiscConst.LackeyDeath[uiLackeyDeathCount]);

		instance.SetData(DataTypes.DelrissaDeathCount, (uint)EncounterState.Special);

		//increase local var, since we now may have four dead
		++uiLackeyDeathCount;

		if (uiLackeyDeathCount == MiscConst.MaxActiveLackey)
			//Time to make her lootable and complete event if she died before lackeys
			if (!delrissa.IsAlive)
			{
				delrissa.SetDynamicFlag(UnitDynFlags.Lootable);

				instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.Done);
			}
	}

	public override void KilledUnit(Unit victim)
	{
		var delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);

		if (delrissa)
			delrissa.AI.KilledUnit(victim);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UsedPotion &&
			HealthBelowPct(25))
		{
			DoCast(Me, SpellIds.HealingPotion);
			UsedPotion = true;
		}

		Scheduler.Update(diff);
	}

	private void Initialize()
	{
		UsedPotion = false;

		// These guys does not follow normal threat system rules
		// For later development, some alternative threat system should be made
		// We do not know what this system is based upon, but one theory is class (healers=high threat, dps=medium, etc)
		// We reset their threat frequently as an alternative until such a system exist
		Scheduler.Schedule(TimeSpan.FromSeconds(5),
							TimeSpan.FromSeconds(20),
							task =>
							{
								ResetThreatList();
								task.Repeat();
							});
	}

	private void AcquireGUIDs()
	{
		var delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);

		if (delrissa)
			for (byte i = 0; i < MiscConst.MaxActiveLackey; ++i)
				_auiLackeyGUIDs[i] = (delrissa.AI as boss_priestess_delrissa)._auiLackeyGUID[i];
	}
}

[Script]
internal class boss_kagani_nightstrike : boss_priestess_lackey_common
{
	private bool InVanish;

	//Rogue
	public boss_kagani_nightstrike(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();
		Me.SetVisible(true);

		base.Reset();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		Scheduler.Update(diff);

		if (!InVanish)
			DoMeleeAttackIfReady();
	}

	private void Initialize()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(5.5),
							task =>
							{
								DoCastVictim(SpellIds.Gouge);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(7),
							task =>
							{
								DoCastVictim(SpellIds.Kick);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								DoCast(Me, SpellIds.Vanish);

								var unit = SelectTarget(SelectTargetMethod.Random, 0);

								ResetThreatList();

								if (unit)
									AddThreat(unit, 1000.0f);

								InVanish = true;
								task.Repeat(TimeSpan.FromSeconds(30));

								task.Schedule(TimeSpan.FromSeconds(10),
											waitTask =>
											{
												if (InVanish)
												{
													DoCastVictim(SpellIds.Backstab, new CastSpellExtraArgs(true));
													DoCastVictim(SpellIds.KidneyShot, new CastSpellExtraArgs(true));
													Me.SetVisible(true); // ...? Hacklike
													InVanish = false;
												}

												waitTask.Repeat();
											});
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.Eviscerate);
								task.Repeat(TimeSpan.FromSeconds(4));
							});

		InVanish = false;
	}
}

[Script]
internal class boss_ellris_duskhallow : boss_priestess_lackey_common
{
	//Warlock
	public boss_ellris_duskhallow(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		base.Reset();
	}

	public override void JustEngagedWith(Unit who)
	{
		DoCast(Me, SpellIds.SummonImp);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.Immolate);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(3),
							task =>
							{
								DoCastVictim(SpellIds.ShadowBolt);
								task.Repeat(TimeSpan.FromSeconds(5));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								var unit = SelectTarget(SelectTargetMethod.Random, 0);

								if (unit)
									DoCast(unit, SpellIds.SeedOfCorruption);

								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(1),
							task =>
							{
								var unit = SelectTarget(SelectTargetMethod.Random, 0);

								if (unit)
									DoCast(unit, SpellIds.CurseOfAgony);

								task.Repeat(TimeSpan.FromSeconds(13));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								var unit = SelectTarget(SelectTargetMethod.Random, 0);

								if (unit)
									DoCast(unit, SpellIds.Fear);

								task.Repeat();
							});
	}
}

[Script]
internal class boss_eramas_brightblaze : boss_priestess_lackey_common
{
	//Monk
	public boss_eramas_brightblaze(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		base.Reset();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.Knockdown);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(4.5),
							task =>
							{
								DoCastVictim(SpellIds.SnapKick);
								task.Repeat();
							});
	}
}

[Script]
internal class boss_yazzai : boss_priestess_lackey_common
{
	private bool HasIceBlocked;

	//Mage
	public boss_yazzai(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		base.Reset();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		if (HealthBelowPct(35) &&
			!HasIceBlocked)
		{
			DoCast(Me, SpellIds.IceBlock);
			HasIceBlocked = true;
		}

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		HasIceBlocked = false;

		Scheduler.Schedule(TimeSpan.FromSeconds(1),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0);

								if (target)
								{
									DoCast(target, SpellIds.Polymorph);
									task.Repeat(TimeSpan.FromSeconds(20));
								}
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								var unit = SelectTarget(SelectTargetMethod.Random, 0);

								if (unit)
									DoCast(unit, SpellIds.Blizzard);

								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(12),
							task =>
							{
								DoCastVictim(SpellIds.IceLance);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCastVictim(SpellIds.ConeOfCold);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(3),
							task =>
							{
								DoCastVictim(SpellIds.Frostbolt);
								task.Repeat(TimeSpan.FromSeconds(8));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								var InMeleeRange = false;

								foreach (var pair in Me.GetCombatManager().PvECombatRefs)
									if (pair.Value.GetOther(Me).IsWithinMeleeRange(Me))
									{
										InMeleeRange = true;

										break;
									}

								//if anybody is in melee range than escape by blink
								if (InMeleeRange)
									DoCast(Me, SpellIds.Blink);

								task.Repeat();
							});
	}
}

[Script]
internal class boss_warlord_salaris : boss_priestess_lackey_common
{
	//Warrior
	public boss_warlord_salaris(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		base.Reset();
	}

	public override void JustEngagedWith(Unit who)
	{
		DoCast(Me, SpellIds.BattleShout);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		Scheduler.Schedule(TimeSpan.FromMilliseconds(500),
							task =>
							{
								var InMeleeRange = false;

								foreach (var pair in Me.GetCombatManager().PvECombatRefs)
									if (pair.Value.GetOther(Me).IsWithinMeleeRange(Me))
									{
										InMeleeRange = true;

										break;
									}

								//if nobody is in melee range than try to use Intercept
								if (!InMeleeRange)
								{
									var unit = SelectTarget(SelectTargetMethod.Random, 0);

									if (unit)
										DoCast(unit, SpellIds.InterceptStun);
								}

								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.Disarm);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCastVictim(SpellIds.PiercingHowl);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(18),
							task =>
							{
								DoCastVictim(SpellIds.FrighteningShout);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(4.5),
							task =>
							{
								DoCastVictim(SpellIds.Hamstring);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								DoCastVictim(SpellIds.MortalStrike);
								task.Repeat(TimeSpan.FromSeconds(4.5));
							});
	}
}

[Script]
internal class boss_garaxxas : boss_priestess_lackey_common
{
	private readonly TaskScheduler _meleeScheduler = new();

	private ObjectGuid _uiPetGUID;

	//Hunter
	public boss_garaxxas(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		var pPet = Global.ObjAccessor.GetUnit(Me, _uiPetGUID);

		if (!pPet)
			Me.SummonCreature(CreatureIds.Sliver, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.CorpseDespawn);

		base.Reset();
	}

	public override void JustSummoned(Creature summoned)
	{
		_uiPetGUID = summoned.GUID;
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		if (Me.IsWithinDistInMap(Me.Victim, SharedConst.AttackDistance))
			_meleeScheduler.Update(diff, () => DoMeleeAttackIfReady());
		else
			Scheduler.Update(diff);
	}

	private void Initialize()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.AimedShot);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(2.5),
							task =>
							{
								DoCastVictim(SpellIds.Shoot);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								DoCastVictim(SpellIds.ConcussiveShot);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCastVictim(SpellIds.MultiShot);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(4),
							task =>
							{
								DoCastVictim(SpellIds.WingClip);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							task =>
							{
								//attempt find go summoned from spell (cast by me)
								var go = Me.GetGameObject(SpellIds.FreezingTrap);

								//if we have a go, we need to wait (only one trap at a Time)
								if (go)
								{
									task.Repeat(TimeSpan.FromSeconds(2.5));
								}
								else
								{
									//if go does not exist, then we can cast
									DoCastVictim(SpellIds.FreezingTrap);
									task.Repeat();
								}
							});
	}
}

[Script]
internal class boss_apoko : boss_priestess_lackey_common
{
	private byte Totem_Amount;

	//Shaman
	public boss_apoko(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		base.Reset();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		Totem_Amount = 1;

		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								DoCast(Me, RandomHelper.RAND(SpellIds.WindfuryTotem, SpellIds.FireNovaTotem, SpellIds.EarthbindTotem));
								++Totem_Amount;
								task.Repeat(TimeSpan.FromMilliseconds(Totem_Amount * 2000));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCast(Me, SpellIds.WarStomp);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								var unit = SelectTarget(SelectTargetMethod.Random, 0);

								if (unit)
									DoCast(unit, SpellIds.Purge);

								task.Repeat(TimeSpan.FromSeconds(15));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(5),
							task =>
							{
								DoCast(Me, SpellIds.LesserHealingWave);
								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(7),
							task =>
							{
								DoCastVictim(SpellIds.FrostShock);
								task.Repeat();
							});
	}
}

[Script]
internal class boss_zelfan : boss_priestess_lackey_common
{
	//Engineer
	public boss_zelfan(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();

		base.Reset();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		base.UpdateAI(diff);

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(20),
							task =>
							{
								DoCastVictim(SpellIds.GoblinDragonGun);
								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(7),
							task =>
							{
								DoCastVictim(SpellIds.RocketLaunch);
								task.Repeat(TimeSpan.FromSeconds(9));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(4),
							task =>
							{
								foreach (var guid in _auiLackeyGUIDs)
								{
									var pAdd = Global.ObjAccessor.GetUnit(Me, guid);

									if (pAdd != null &&
										pAdd.										IsPolymorphed)
									{
										DoCast(pAdd, SpellIds.Recombobulate);

										break;
									}
								}

								task.Repeat(TimeSpan.FromSeconds(2));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCast(Me, SpellIds.HighExplosiveSheep);
								task.Repeat(TimeSpan.FromSeconds(65));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							task =>
							{
								DoCastVictim(SpellIds.FelIronBomb);
								task.Repeat();
							});
	}
}
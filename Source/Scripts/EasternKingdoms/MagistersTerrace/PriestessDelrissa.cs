// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Dynamic;

namespace Scripts.EasternKingdoms.MagistersTerrace.PriestessDelrissa;

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_DEATH = 10;
}

internal struct SpellIds
{
    public const uint DISPEL_MAGIC = 27609;
    public const uint FLASH_HEAL = 17843;
    public const uint SW_PAIN_NORMAL = 14032;
    public const uint SW_PAIN_HEROIC = 15654;
    public const uint SHIELD = 44291;
    public const uint RENEW_NORMAL = 44174;
    public const uint RENEW_HEROIC = 46192;

    // Apoko
    public const uint WINDFURY_TOTEM = 27621;
    public const uint WAR_STOMP = 46026;
    public const uint PURGE = 27626;
    public const uint LESSER_HEALING_WAVE = 44256;
    public const uint FROST_SHOCK = 21401;
    public const uint FIRE_NOVA_TOTEM = 44257;
    public const uint EARTHBIND_TOTEM = 15786;

    public const uint HEALING_POTION = 15503;

    // RogueSpells
    public const uint KIDNEY_SHOT = 27615;
    public const uint GOUGE = 12540;
    public const uint KICK = 27613;
    public const uint VANISH = 44290;
    public const uint BACKSTAB = 15657;
    public const uint EVISCERATE = 27611;

    // WarlockSpells
    public const uint IMMOLATE = 44267;
    public const uint SHADOW_BOLT = 12471;
    public const uint SEED_OF_CORRUPTION = 44141;
    public const uint CURSE_OF_AGONY = 14875;
    public const uint FEAR = 38595;
    public const uint IMP_FIREBALL = 44164;
    public const uint SUMMON_IMP = 44163;

    // KickDown
    public const uint KNOCKDOWN = 11428;
    public const uint SNAP_KICK = 46182;

    // MageSpells
    public const uint POLYMORPH = 13323;
    public const uint ICE_BLOCK = 27619;
    public const uint BLIZZARD = 44178;
    public const uint ICE_LANCE = 46194;
    public const uint CONE_OF_COLD = 38384;
    public const uint FROSTBOLT = 15043;
    public const uint BLINK = 14514;

    // WarriorSpells
    public const uint INTERCEPT_STUN = 27577;
    public const uint DISARM = 27581;
    public const uint PIERCING_HOWL = 23600;
    public const uint FRIGHTENING_SHOUT = 19134;
    public const uint HAMSTRING = 27584;
    public const uint BATTLE_SHOUT = 27578;
    public const uint MORTAL_STRIKE = 44268;

    // HunterSpells
    public const uint AIMED_SHOT = 44271;
    public const uint SHOOT = 15620;
    public const uint CONCUSSIVE_SHOT = 27634;
    public const uint MULTI_SHOT = 31942;
    public const uint WING_CLIP = 44286;
    public const uint FREEZING_TRAP = 44136;

    // EngineerSpells
    public const uint GOBLIN_DRAGON_GUN = 44272;
    public const uint ROCKET_LAUNCH = 44137;
    public const uint RECOMBOBULATE = 44274;
    public const uint HIGH_EXPLOSIVE_SHEEP = 44276;
    public const uint FEL_IRON_BOMB = 46024;
    public const uint SHEEP_EXPLOSION = 44279;
}

internal struct CreatureIds
{
    public const uint SLIVER = 24552;
}

internal struct MiscConst
{
    public const uint MAX_ACTIVE_LACKEY = 4;

    public const float F_ORIENTATION = 4.98f;
    public const float F_Z_LOCATION = -19.921f;

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

    public static uint[] AuiAddEntries =
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
internal class BossPriestessDelrissa : BossAI
{
    public ObjectGuid[] AuiLackeyGUID = new ObjectGuid[MiscConst.MAX_ACTIVE_LACKEY];
    private readonly List<uint> _lackeyEntryList = new();

    private byte _playersKilled;

    public BossPriestessDelrissa(Creature creature) : base(creature, DataTypes.PRIESTESS_DELRISSA)
    {
        Initialize();
        _lackeyEntryList.Clear();
    }

    public override void Reset()
    {
        Initialize();

        InitializeLackeys();
    }

    //this mean she at some point evaded
    public override void JustReachedHome()
    {
        Instance.SetBossState(DataTypes.PRIESTESS_DELRISSA, EncounterState.Fail);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);

        foreach (var lackeyGuid in AuiLackeyGUID)
        {
            var pAdd = Global.ObjAccessor.GetUnit(Me, lackeyGuid);

            if (pAdd && !pAdd.IsEngaged)
                AddThreat(who, 0.0f, pAdd);
        }

        Instance.SetBossState(DataTypes.PRIESTESS_DELRISSA, EncounterState.InProgress);
    }

    public override void KilledUnit(Unit victim)
    {
        if (!victim.IsPlayer)
            return;

        Talk(MiscConst.PlayerDeath[_playersKilled]);

        if (_playersKilled < 4)
            ++_playersKilled;
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DEATH);

        if (Instance.GetData(DataTypes.DELRISSA_DEATH_COUNT) == MiscConst.MAX_ACTIVE_LACKEY)
            Instance.SetBossState(DataTypes.PRIESTESS_DELRISSA, EncounterState.Done);
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
        _playersKilled = 0;

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               var health = Me.Health;
                               Unit target = Me;

                               for (byte i = 0; i < AuiLackeyGUID.Length; ++i)
                               {
                                   var pAdd = Global.ObjAccessor.GetUnit(Me, AuiLackeyGUID[i]);

                                   if (pAdd != null &&
                                       pAdd.IsAlive &&
                                       pAdd.Health < health)
                                       target = pAdd;
                               }

                               DoCast(target, SpellIds.FLASH_HEAL);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               Unit target = Me;

                               if (RandomHelper.URand(0, 1) != 0)
                               {
                                   var pAdd = Global.ObjAccessor.GetUnit(Me, AuiLackeyGUID[RandomHelper.Rand32() % AuiLackeyGUID.Length]);

                                   if (pAdd != null &&
                                       pAdd.IsAlive)
                                       target = pAdd;
                               }

                               DoCast(target, SpellIds.RENEW_NORMAL);
                               task.Repeat(TimeSpan.FromSeconds(5));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               Unit target = Me;

                               if (RandomHelper.URand(0, 1) != 0)
                               {
                                   var pAdd = Global.ObjAccessor.GetUnit(Me, AuiLackeyGUID[RandomHelper.Rand32() % AuiLackeyGUID.Length]);

                                   if (pAdd != null &&
                                       pAdd.IsAlive &&
                                       !pAdd.HasAura(SpellIds.SHIELD))
                                       target = pAdd;
                               }

                               DoCast(target, SpellIds.SHIELD);
                               task.Repeat(TimeSpan.FromSeconds(7.5));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                               if (target != null)
                                   DoCast(target, SpellIds.SW_PAIN_NORMAL);

                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(7.5),
                           task =>
                           {
                               Unit target = null;

                               if (RandomHelper.URand(0, 1) != 0)
                                   target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                               else
                               {
                                   if (RandomHelper.URand(0, 1) != 0)
                                       target = Me;
                                   else
                                   {
                                       var pAdd = Global.ObjAccessor.GetUnit(Me, AuiLackeyGUID[RandomHelper.Rand32() % AuiLackeyGUID.Length]);

                                       if (pAdd != null &&
                                           pAdd.IsAlive)
                                           target = pAdd;
                                   }
                               }

                               if (target)
                                   DoCast(target, SpellIds.DISPEL_MAGIC);

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
        if (_lackeyEntryList.Empty())
        {
            //fill vector array with entries from Creature array
            for (byte i = 0; i < _lackeyEntryList.Count; ++i)
                _lackeyEntryList[i] = MiscConst.AuiAddEntries[i];

            //remove random entries
            _lackeyEntryList.RandomResize(MiscConst.MAX_ACTIVE_LACKEY);

            //summon all the remaining in vector
            foreach (var guid in _lackeyEntryList)
            {
                Creature pAdd = Me.SummonCreature(guid, MiscConst.LackeyLocations[j][0], MiscConst.LackeyLocations[j][1], MiscConst.F_Z_LOCATION, MiscConst.F_ORIENTATION, TempSummonType.CorpseDespawn);

                if (pAdd != null)
                    AuiLackeyGUID[j] = pAdd.GUID;

                ++j;
            }
        }
        else
            foreach (var guid in _lackeyEntryList)
            {
                var pAdd = Global.ObjAccessor.GetUnit(Me, AuiLackeyGUID[j]);

                //object already removed, not exist
                if (!pAdd)
                {
                    pAdd = Me.SummonCreature(guid, MiscConst.LackeyLocations[j][0], MiscConst.LackeyLocations[j][1], MiscConst.F_Z_LOCATION, MiscConst.F_ORIENTATION, TempSummonType.CorpseDespawn);

                    if (pAdd != null)
                        AuiLackeyGUID[j] = pAdd.GUID;
                }

                ++j;
            }
    }
}

//all 8 possible lackey use this common
internal class BossPriestessLackeyCommon : ScriptedAI
{
    public ObjectGuid[] AuiLackeyGuiDs = new ObjectGuid[MiscConst.MAX_ACTIVE_LACKEY];
    private readonly InstanceScript _instance;
    private bool _usedPotion;

    public BossPriestessLackeyCommon(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
        AcquireGuiDs();

        // in case she is not alive and Reset was for some reason called, respawn her (most likely party wipe after killing her)
        var delrissa = _instance.GetCreature(DataTypes.PRIESTESS_DELRISSA);

        if (delrissa)
            if (!delrissa.IsAlive)
                delrissa.Respawn();
    }

    public override void JustEngagedWith(Unit who)
    {
        if (!who)
            return;

        foreach (var guid in AuiLackeyGuiDs)
        {
            var pAdd = Global.ObjAccessor.GetUnit(Me, guid);

            if (pAdd != null &&
                !pAdd.IsEngaged &&
                pAdd != Me)
                AddThreat(who, 0.0f, pAdd);
        }

        var delrissa = _instance.GetCreature(DataTypes.PRIESTESS_DELRISSA);

        if (delrissa)
            if (delrissa.IsAlive &&
                !delrissa.IsEngaged)
                AddThreat(who, 0.0f, delrissa);
    }

    public override void JustDied(Unit killer)
    {
        var delrissa = _instance.GetCreature(DataTypes.PRIESTESS_DELRISSA);
        var uiLackeyDeathCount = _instance.GetData(DataTypes.DELRISSA_DEATH_COUNT);

        if (!delrissa)
            return;

        //should delrissa really yell if dead?
        delrissa.
            //should delrissa really yell if dead?
            AI.Talk(MiscConst.LackeyDeath[uiLackeyDeathCount]);

        _instance.SetData(DataTypes.DELRISSA_DEATH_COUNT, (uint)EncounterState.Special);

        //increase local var, since we now may have four dead
        ++uiLackeyDeathCount;

        if (uiLackeyDeathCount == MiscConst.MAX_ACTIVE_LACKEY)
            //Time to make her lootable and complete event if she died before lackeys
            if (!delrissa.IsAlive)
            {
                delrissa.SetDynamicFlag(UnitDynFlags.Lootable);

                _instance.SetBossState(DataTypes.PRIESTESS_DELRISSA, EncounterState.Done);
            }
    }

    public override void KilledUnit(Unit victim)
    {
        var delrissa = _instance.GetCreature(DataTypes.PRIESTESS_DELRISSA);

        if (delrissa)
            delrissa.AI.KilledUnit(victim);
    }

    public override void UpdateAI(uint diff)
    {
        if (!_usedPotion &&
            HealthBelowPct(25))
        {
            DoCast(Me, SpellIds.HEALING_POTION);
            _usedPotion = true;
        }

        Scheduler.Update(diff);
    }

    private void Initialize()
    {
        _usedPotion = false;

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

    private void AcquireGuiDs()
    {
        var delrissa = _instance.GetCreature(DataTypes.PRIESTESS_DELRISSA);

        if (delrissa)
            for (byte i = 0; i < MiscConst.MAX_ACTIVE_LACKEY; ++i)
                AuiLackeyGuiDs[i] = (delrissa.AI as BossPriestessDelrissa).AuiLackeyGUID[i];
    }
}

[Script]
internal class BossKaganiNightstrike : BossPriestessLackeyCommon
{
    private bool _inVanish;

    //Rogue
    public BossKaganiNightstrike(Creature creature) : base(creature)
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

        if (!_inVanish)
            DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(5.5),
                           task =>
                           {
                               DoCastVictim(SpellIds.GOUGE);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           task =>
                           {
                               DoCastVictim(SpellIds.KICK);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCast(Me, SpellIds.VANISH);

                               var unit = SelectTarget(SelectTargetMethod.Random, 0);

                               ResetThreatList();

                               if (unit)
                                   AddThreat(unit, 1000.0f);

                               _inVanish = true;
                               task.Repeat(TimeSpan.FromSeconds(30));

                               task.Schedule(TimeSpan.FromSeconds(10),
                                             waitTask =>
                                             {
                                                 if (_inVanish)
                                                 {
                                                     DoCastVictim(SpellIds.BACKSTAB, new CastSpellExtraArgs(true));
                                                     DoCastVictim(SpellIds.KIDNEY_SHOT, new CastSpellExtraArgs(true));
                                                     Me.SetVisible(true); // ...? Hacklike
                                                     _inVanish = false;
                                                 }

                                                 waitTask.Repeat();
                                             });
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.EVISCERATE);
                               task.Repeat(TimeSpan.FromSeconds(4));
                           });

        _inVanish = false;
    }
}

[Script]
internal class BossEllrisDuskhallow : BossPriestessLackeyCommon
{
    //Warlock
    public BossEllrisDuskhallow(Creature creature) : base(creature)
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
        DoCast(Me, SpellIds.SUMMON_IMP);
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
                               DoCastVictim(SpellIds.IMMOLATE);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHADOW_BOLT);
                               task.Repeat(TimeSpan.FromSeconds(5));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               var unit = SelectTarget(SelectTargetMethod.Random, 0);

                               if (unit)
                                   DoCast(unit, SpellIds.SEED_OF_CORRUPTION);

                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               var unit = SelectTarget(SelectTargetMethod.Random, 0);

                               if (unit)
                                   DoCast(unit, SpellIds.CURSE_OF_AGONY);

                               task.Repeat(TimeSpan.FromSeconds(13));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               var unit = SelectTarget(SelectTargetMethod.Random, 0);

                               if (unit)
                                   DoCast(unit, SpellIds.FEAR);

                               task.Repeat();
                           });
    }
}

[Script]
internal class BossEramasBrightblaze : BossPriestessLackeyCommon
{
    //Monk
    public BossEramasBrightblaze(Creature creature) : base(creature)
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
                               DoCastVictim(SpellIds.KNOCKDOWN);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4.5),
                           task =>
                           {
                               DoCastVictim(SpellIds.SNAP_KICK);
                               task.Repeat();
                           });
    }
}

[Script]
internal class BossYazzai : BossPriestessLackeyCommon
{
    private bool _hasIceBlocked;

    //Mage
    public BossYazzai(Creature creature) : base(creature)
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
            !_hasIceBlocked)
        {
            DoCast(Me, SpellIds.ICE_BLOCK);
            _hasIceBlocked = true;
        }

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        _hasIceBlocked = false;

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                               {
                                   DoCast(target, SpellIds.POLYMORPH);
                                   task.Repeat(TimeSpan.FromSeconds(20));
                               }
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               var unit = SelectTarget(SelectTargetMethod.Random, 0);

                               if (unit)
                                   DoCast(unit, SpellIds.BLIZZARD);

                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.ICE_LANCE);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.CONE_OF_COLD);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               DoCastVictim(SpellIds.FROSTBOLT);
                               task.Repeat(TimeSpan.FromSeconds(8));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               var inMeleeRange = false;

                               foreach (var pair in Me.GetCombatManager().PvECombatRefs)
                                   if (pair.Value.GetOther(Me).IsWithinMeleeRange(Me))
                                   {
                                       inMeleeRange = true;

                                       break;
                                   }

                               //if anybody is in melee range than escape by blink
                               if (inMeleeRange)
                                   DoCast(Me, SpellIds.BLINK);

                               task.Repeat();
                           });
    }
}

[Script]
internal class BossWarlordSalaris : BossPriestessLackeyCommon
{
    //Warrior
    public BossWarlordSalaris(Creature creature) : base(creature)
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
        DoCast(Me, SpellIds.BATTLE_SHOUT);
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
                               var inMeleeRange = false;

                               foreach (var pair in Me.GetCombatManager().PvECombatRefs)
                                   if (pair.Value.GetOther(Me).IsWithinMeleeRange(Me))
                                   {
                                       inMeleeRange = true;

                                       break;
                                   }

                               //if nobody is in melee range than try to use Intercept
                               if (!inMeleeRange)
                               {
                                   var unit = SelectTarget(SelectTargetMethod.Random, 0);

                                   if (unit)
                                       DoCast(unit, SpellIds.INTERCEPT_STUN);
                               }

                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.DISARM);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.PIERCING_HOWL);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(18),
                           task =>
                           {
                               DoCastVictim(SpellIds.FRIGHTENING_SHOUT);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4.5),
                           task =>
                           {
                               DoCastVictim(SpellIds.HAMSTRING);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.MORTAL_STRIKE);
                               task.Repeat(TimeSpan.FromSeconds(4.5));
                           });
    }
}

[Script]
internal class BossGaraxxas : BossPriestessLackeyCommon
{
    private readonly TaskScheduler _meleeScheduler = new();

    private ObjectGuid _uiPetGUID;

    //Hunter
    public BossGaraxxas(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        var pPet = Global.ObjAccessor.GetUnit(Me, _uiPetGUID);

        if (!pPet)
            Me.SummonCreature(CreatureIds.SLIVER, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.CorpseDespawn);

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
                               DoCastVictim(SpellIds.AIMED_SHOT);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2.5),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHOOT);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.CONCUSSIVE_SHOT);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.MULTI_SHOT);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               DoCastVictim(SpellIds.WING_CLIP);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               //attempt find go summoned from spell (cast by me)
                               var go = Me.GetGameObject(SpellIds.FREEZING_TRAP);

                               //if we have a go, we need to wait (only one trap at a Time)
                               if (go)
                                   task.Repeat(TimeSpan.FromSeconds(2.5));
                               else
                               {
                                   //if go does not exist, then we can cast
                                   DoCastVictim(SpellIds.FREEZING_TRAP);
                                   task.Repeat();
                               }
                           });
    }
}

[Script]
internal class BossApoko : BossPriestessLackeyCommon
{
    private byte _totemAmount;

    //Shaman
    public BossApoko(Creature creature) : base(creature)
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
        _totemAmount = 1;

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCast(Me, RandomHelper.RAND(SpellIds.WINDFURY_TOTEM, SpellIds.FIRE_NOVA_TOTEM, SpellIds.EARTHBIND_TOTEM));
                               ++_totemAmount;
                               task.Repeat(TimeSpan.FromMilliseconds(_totemAmount * 2000));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCast(Me, SpellIds.WAR_STOMP);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               var unit = SelectTarget(SelectTargetMethod.Random, 0);

                               if (unit)
                                   DoCast(unit, SpellIds.PURGE);

                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               DoCast(Me, SpellIds.LESSER_HEALING_WAVE);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           task =>
                           {
                               DoCastVictim(SpellIds.FROST_SHOCK);
                               task.Repeat();
                           });
    }
}

[Script]
internal class BossZelfan : BossPriestessLackeyCommon
{
    //Engineer
    public BossZelfan(Creature creature) : base(creature)
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
                               DoCastVictim(SpellIds.GOBLIN_DRAGON_GUN);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           task =>
                           {
                               DoCastVictim(SpellIds.ROCKET_LAUNCH);
                               task.Repeat(TimeSpan.FromSeconds(9));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               foreach (var guid in AuiLackeyGuiDs)
                               {
                                   var pAdd = Global.ObjAccessor.GetUnit(Me, guid);

                                   if (pAdd != null &&
                                       pAdd.IsPolymorphed)
                                   {
                                       DoCast(pAdd, SpellIds.RECOMBOBULATE);

                                       break;
                                   }
                               }

                               task.Repeat(TimeSpan.FromSeconds(2));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCast(Me, SpellIds.HIGH_EXPLOSIVE_SHEEP);
                               task.Repeat(TimeSpan.FromSeconds(65));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.FEL_IRON_BOMB);
                               task.Repeat();
                           });
    }
}
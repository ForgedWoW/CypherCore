// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;
using Serilog;

namespace Scripts.EasternKingdoms.Karazhan.EsOpera;

internal struct TextIds
{
    public const uint SAY_DOROTHEE_DEATH = 0;
    public const uint SAY_DOROTHEE_SUMMON = 1;
    public const uint SAY_DOROTHEE_TITO_DEATH = 2;
    public const uint SAY_DOROTHEE_AGGRO = 3;

    public const uint SAY_ROAR_AGGRO = 0;
    public const uint SAY_ROAR_DEATH = 1;
    public const uint SAY_ROAR_SLAY = 2;

    public const uint SAY_STRAWMAN_AGGRO = 0;
    public const uint SAY_STRAWMAN_DEATH = 1;
    public const uint SAY_STRAWMAN_SLAY = 2;

    public const uint SAY_TINHEAD_AGGRO = 0;
    public const uint SAY_TINHEAD_DEATH = 1;
    public const uint SAY_TINHEAD_SLAY = 2;
    public const uint EMOTE_RUST = 3;

    public const uint SAY_CRONE_AGGRO = 0;
    public const uint SAY_CRONE_DEATH = 1;
    public const uint SAY_CRONE_SLAY = 2;

    //RedRidingHood
    public const uint SAY_WOLF_AGGRO = 0;
    public const uint SAY_WOLF_SLAY = 1;
    public const uint SAY_WOLF_HOOD = 2;
    public const uint OPTION_WHAT_PHAT_LEWTS_YOU_HAVE = 7443;

    //Romulo & Julianne
    public const uint SAY_JULIANNE_AGGRO = 0;
    public const uint SAY_JULIANNE_ENTER = 1;
    public const uint SAY_JULIANNE_DEATH01 = 2;
    public const uint SAY_JULIANNE_DEATH02 = 3;
    public const uint SAY_JULIANNE_RESURRECT = 4;
    public const uint SAY_JULIANNE_SLAY = 5;

    public const uint SAY_ROMULO_AGGRO = 0;
    public const uint SAY_ROMULO_DEATH = 1;
    public const uint SAY_ROMULO_ENTER = 2;
    public const uint SAY_ROMULO_RESURRECT = 3;
    public const uint SAY_ROMULO_SLAY = 4;
}

internal struct SpellIds
{
    // Dorothee
    public const uint WATERBOLT = 31012;
    public const uint SCREAM = 31013;
    public const uint SUMMONTITO = 31014;

    // Tito
    public const uint YIPPING = 31015;

    // Strawman
    public const uint BRAIN_BASH = 31046;
    public const uint BRAIN_WIPE = 31069;
    public const uint BURNING_STRAW = 31075;

    // Tinhead
    public const uint CLEAVE = 31043;
    public const uint RUST = 31086;

    // Roar
    public const uint MANGLE = 31041;
    public const uint SHRED = 31042;
    public const uint FRIGHTENED_SCREAM = 31013;

    // Crone
    public const uint CHAIN_LIGHTNING = 32337;

    // Cyclone
    public const uint KNOCKBACK = 32334;
    public const uint CYCLONE_VISUAL = 32332;

    //Red Riding Hood
    public const uint LITTLE_RED_RIDING_HOOD = 30768;
    public const uint TERRIFYING_HOWL = 30752;
    public const uint WIDE_SWIPE = 30761;

    //Romulo & Julianne
    public const uint BLINDING_PASSION = 30890;
    public const uint DEVOTION = 30887;
    public const uint ETERNAL_AFFECTION = 30878;
    public const uint POWERFUL_ATTRACTION = 30889;
    public const uint DRINK_POISON = 30907;

    public const uint BACKWARD_LUNGE = 30815;
    public const uint DARING = 30841;
    public const uint DEADLY_SWATHE = 30817;
    public const uint POISON_THRUST = 30822;

    public const uint UNDYING_LOVE = 30951;
    public const uint RES_VISUAL = 24171;
}

internal struct CreatureIds
{
    public const uint TITO = 17548;
    public const uint CYCLONE = 18412;
    public const uint CRONE = 18168;

    //Red Riding Hood
    public const uint BIG_BAD_WOLF = 17521;

    //Romulo & Julianne
    public const uint ROMULO = 17533;
}

internal struct MiscConst
{
    //Red Riding Hood
    public const uint SOUND_WOLF_DEATH = 9275;

    //Romulo & Julianne
    public const int ROMULO_X = -10900;
    public const int ROMULO_Y = -1758;

    public static void SummonCroneIfReady(InstanceScript instance, Creature creature)
    {
        instance.SetData(DataTypes.OPERA_OZ_DEATHCOUNT, (uint)EncounterState.Special); // Increment DeathCount

        if (instance.GetData(DataTypes.OPERA_OZ_DEATHCOUNT) == 4)
        {
            Creature pCrone = creature.SummonCreature(CreatureIds.CRONE, -10891.96f, -1755.95f, creature.Location.Z, 4.64f, TempSummonType.TimedOrDeadDespawn, TimeSpan.FromHours(2));

            if (pCrone)
                if (creature.Victim)
                    pCrone.AI.AttackStart(creature.Victim);
        }
    }

    public static void PretendToDie(Creature creature)
    {
        creature.InterruptNonMeleeSpells(true);
        creature.RemoveAllAuras();
        creature.SetHealth(0);
        creature.SetUnitFlag(UnitFlags.Uninteractible);
        creature.MotionMaster.Clear();
        creature.MotionMaster.MoveIdle();
        creature.SetStandState(UnitStandStateType.Dead);
    }

    public static void Resurrect(Creature target)
    {
        target.RemoveUnitFlag(UnitFlags.Uninteractible);
        target.SetFullHealth();
        target.SetStandState(UnitStandStateType.Stand);
        target.SpellFactory.CastSpell(target, SpellIds.RES_VISUAL, true);

        if (target.Victim)
        {
            target.MotionMaster.MoveChase(target.Victim);
            target.AI.AttackStart(target.Victim);
        }
        else
            target.MotionMaster.Initialize();
    }
}

internal enum RajPhase
{
    Julianne = 0,
    Romulo = 1,
    Both = 2
}

[Script]
internal class BossDorothee : ScriptedAI
{
    public bool SummonedTito;
    public bool TitoDied;
    private readonly InstanceScript _instance;
    private uint _aggroTimer;
    private uint _fearTimer;
    private uint _summonTitoTimer;

    private uint _waterBoltTimer;

    public BossDorothee(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_DOROTHEE_AGGRO);
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DOROTHEE_DEATH);

        MiscConst.SummonCroneIfReady(_instance, Me);
    }

    public override void AttackStart(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.AttackStart(who);
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void UpdateAI(uint diff)
    {
        if (_aggroTimer != 0)
        {
            if (_aggroTimer <= diff)
            {
                Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                _aggroTimer = 0;
            }
            else
                _aggroTimer -= diff;
        }

        if (!UpdateVictim())
            return;

        if (_waterBoltTimer <= diff)
        {
            DoCast(SelectTarget(SelectTargetMethod.Random, 0), SpellIds.WATERBOLT);
            _waterBoltTimer = TitoDied ? 1500 : 5000u;
        }
        else
            _waterBoltTimer -= diff;

        if (_fearTimer <= diff)
        {
            DoCastVictim(SpellIds.SCREAM);
            _fearTimer = 30000;
        }
        else
            _fearTimer -= diff;

        if (!SummonedTito)
        {
            if (_summonTitoTimer <= diff)
                SummonTito();
            else _summonTitoTimer -= diff;
        }

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _aggroTimer = 500;

        _waterBoltTimer = 5000;
        _fearTimer = 15000;
        _summonTitoTimer = 47500;

        SummonedTito = false;
        TitoDied = false;
    }

    private void SummonTito()
    {
        Creature pTito = Me.SummonCreature(CreatureIds.TITO, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));

        if (pTito)
        {
            Talk(TextIds.SAY_DOROTHEE_SUMMON);
            pTito.GetAI<NPCTito>().DorotheeGUID = Me.GUID;
            pTito.AI.AttackStart(Me.Victim);
            SummonedTito = true;
            TitoDied = false;
        }
    }
}

[Script]
internal class NPCTito : ScriptedAI
{
    public ObjectGuid DorotheeGUID;
    private uint _yipTimer;

    public NPCTito(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustEngagedWith(Unit who) { }

    public override void JustDied(Unit killer)
    {
        if (!DorotheeGUID.IsEmpty)
        {
            var dorothee = ObjectAccessor.GetCreature(Me, DorotheeGUID);

            if (dorothee && dorothee.IsAlive)
            {
                dorothee.GetAI<BossDorothee>().TitoDied = true;
                Talk(TextIds.SAY_DOROTHEE_TITO_DEATH, dorothee);
            }
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (_yipTimer <= diff)
        {
            DoCastVictim(SpellIds.YIPPING);
            _yipTimer = 10000;
        }
        else
            _yipTimer -= diff;

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        DorotheeGUID.Clear();
        _yipTimer = 10000;
    }
}

[Script]
internal class BossStrawman : ScriptedAI
{
    private readonly InstanceScript _instance;
    private uint _aggroTimer;
    private uint _brainBashTimer;
    private uint _brainWipeTimer;

    public BossStrawman(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void AttackStart(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.AttackStart(who);
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_STRAWMAN_AGGRO);
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if ((spellInfo.SchoolMask == SpellSchoolMask.Fire) &&
            ((RandomHelper.Rand32() % 10) == 0))
            DoCast(Me, SpellIds.BURNING_STRAW, new CastSpellExtraArgs(true));
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_STRAWMAN_DEATH);

        MiscConst.SummonCroneIfReady(_instance, Me);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_STRAWMAN_SLAY);
    }

    public override void UpdateAI(uint diff)
    {
        if (_aggroTimer != 0)
        {
            if (_aggroTimer <= diff)
            {
                Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                _aggroTimer = 0;
            }
            else
                _aggroTimer -= diff;
        }

        if (!UpdateVictim())
            return;

        if (_brainBashTimer <= diff)
        {
            DoCastVictim(SpellIds.BRAIN_BASH);
            _brainBashTimer = 15000;
        }
        else
            _brainBashTimer -= diff;

        if (_brainWipeTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target)
                DoCast(target, SpellIds.BRAIN_WIPE);

            _brainWipeTimer = 20000;
        }
        else
            _brainWipeTimer -= diff;

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _aggroTimer = 13000;
        _brainBashTimer = 5000;
        _brainWipeTimer = 7000;
    }
}

[Script]
internal class BossTinhead : ScriptedAI
{
    private readonly InstanceScript _instance;
    private uint _aggroTimer;
    private uint _cleaveTimer;

    private byte _rustCount;
    private uint _rustTimer;

    public BossTinhead(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_TINHEAD_AGGRO);
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void AttackStart(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.AttackStart(who);
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_TINHEAD_DEATH);

        MiscConst.SummonCroneIfReady(_instance, Me);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_TINHEAD_SLAY);
    }

    public override void UpdateAI(uint diff)
    {
        if (_aggroTimer != 0)
        {
            if (_aggroTimer <= diff)
            {
                Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                _aggroTimer = 0;
            }
            else
                _aggroTimer -= diff;
        }

        if (!UpdateVictim())
            return;

        if (_cleaveTimer <= diff)
        {
            DoCastVictim(SpellIds.CLEAVE);
            _cleaveTimer = 5000;
        }
        else
            _cleaveTimer -= diff;

        if (_rustCount < 8)
        {
            if (_rustTimer <= diff)
            {
                ++_rustCount;
                Talk(TextIds.EMOTE_RUST);
                DoCast(Me, SpellIds.RUST);
                _rustTimer = 6000;
            }
            else
                _rustTimer -= diff;
        }

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _aggroTimer = 15000;
        _cleaveTimer = 5000;
        _rustTimer = 30000;

        _rustCount = 0;
    }
}

[Script]
internal class BossRoar : ScriptedAI
{
    private readonly InstanceScript _instance;
    private uint _aggroTimer;
    private uint _mangleTimer;
    private uint _screamTimer;
    private uint _shredTimer;

    public BossRoar(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void MoveInLineOfSight(Unit who)

    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void AttackStart(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.AttackStart(who);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_ROAR_AGGRO);
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_ROAR_DEATH);

        MiscConst.SummonCroneIfReady(_instance, Me);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_ROAR_SLAY);
    }

    public override void UpdateAI(uint diff)
    {
        if (_aggroTimer != 0)
        {
            if (_aggroTimer <= diff)
            {
                Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                _aggroTimer = 0;
            }
            else
                _aggroTimer -= diff;
        }

        if (!UpdateVictim())
            return;

        if (_mangleTimer <= diff)
        {
            DoCastVictim(SpellIds.MANGLE);
            _mangleTimer = RandomHelper.URand(5000, 8000);
        }
        else
            _mangleTimer -= diff;

        if (_shredTimer <= diff)
        {
            DoCastVictim(SpellIds.SHRED);
            _shredTimer = RandomHelper.URand(10000, 15000);
        }
        else
            _shredTimer -= diff;

        if (_screamTimer <= diff)
        {
            DoCastVictim(SpellIds.FRIGHTENED_SCREAM);
            _screamTimer = RandomHelper.URand(20000, 30000);
        }
        else
            _screamTimer -= diff;

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _aggroTimer = 20000;
        _mangleTimer = 5000;
        _shredTimer = 10000;
        _screamTimer = 15000;
    }
}

[Script]
internal class BossCrone : ScriptedAI
{
    private readonly InstanceScript _instance;
    private uint _chainLightningTimer;

    private uint _cycloneTimer;

    public BossCrone(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_CRONE_SLAY);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_CRONE_AGGRO);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_CRONE_DEATH);
        _instance.SetBossState(DataTypes.OPERA_PERFORMANCE, EncounterState.Done);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (_cycloneTimer <= diff)
        {
            var cyclone = DoSpawnCreature(CreatureIds.CYCLONE, RandomHelper.URand(0, 9), RandomHelper.URand(0, 9), 0, 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(15));

            if (cyclone)
                cyclone.SpellFactory.CastSpell(cyclone, SpellIds.CYCLONE_VISUAL, true);

            _cycloneTimer = 30000;
        }
        else
            _cycloneTimer -= diff;

        if (_chainLightningTimer <= diff)
        {
            DoCastVictim(SpellIds.CHAIN_LIGHTNING);
            _chainLightningTimer = 15000;
        }
        else
            _chainLightningTimer -= diff;

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        // Hello, developer from the future! It's me again!
        // This Time, you're fixing Karazhan scripts. Awesome. These are a mess of hacks. An amalgamation of hacks, so to speak. Maybe even a Patchwerk thereof.
        // Anyway, I digress.
        // @todo This line below is obviously a hack. Duh. I'm just coming in here to hackfix the encounter to actually be completable.
        // It needs a rewrite. Badly. Please, take good care of it.
        Me.RemoveUnitFlag(UnitFlags.NonAttackable);
        Me.SetImmuneToPC(false);
        _cycloneTimer = 30000;
        _chainLightningTimer = 10000;
    }
}

[Script]
internal class NPCCyclone : ScriptedAI
{
    private uint _moveTimer;

    public NPCCyclone(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustEngagedWith(Unit who) { }

    public override void MoveInLineOfSight(Unit who) { }

    public override void UpdateAI(uint diff)
    {
        if (!Me.HasAura(SpellIds.KNOCKBACK))
            DoCast(Me, SpellIds.KNOCKBACK, new CastSpellExtraArgs(true));

        if (_moveTimer <= diff)
        {
            var pos = Me.GetRandomNearPosition(10);
            Me.MotionMaster.MovePoint(0, pos);
            _moveTimer = RandomHelper.URand(5000, 8000);
        }
        else
            _moveTimer -= diff;
    }

    private void Initialize()
    {
        _moveTimer = 1000;
    }
}

[Script]
internal class NPCGrandmother : ScriptedAI
{
    public NPCGrandmother(Creature creature) : base(creature) { }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        if (menuId == TextIds.OPTION_WHAT_PHAT_LEWTS_YOU_HAVE &&
            gossipListId == 0)
        {
            player.CloseGossipMenu();

            Creature pBigBadWolf = Me.SummonCreature(CreatureIds.BIG_BAD_WOLF, Me.Location.X, Me.Location.Y, Me.Location.Z, Me.Location.Orientation, TempSummonType.TimedOrDeadDespawn, TimeSpan.FromHours(2));

            if (pBigBadWolf)
                pBigBadWolf.AI.AttackStart(player);

            Me.DespawnOrUnsummon();
        }

        return false;
    }
}

[Script]
internal class BossBigbadwolf : ScriptedAI
{
    private readonly InstanceScript _instance;
    private uint _chaseTimer;
    private uint _fearTimer;

    private ObjectGuid _hoodGUID;

    private bool _isChasing;
    private uint _swipeTimer;
    private double _tempThreat;

    public BossBigbadwolf(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_WOLF_AGGRO);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_WOLF_SLAY);
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void JustDied(Unit killer)
    {
        DoPlaySoundToSet(Me, MiscConst.SOUND_WOLF_DEATH);
        _instance.SetBossState(DataTypes.OPERA_PERFORMANCE, EncounterState.Done);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();

        if (_chaseTimer <= diff)
        {
            if (!_isChasing)
            {
                var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                if (target)
                {
                    Talk(TextIds.SAY_WOLF_HOOD);
                    DoCast(target, SpellIds.LITTLE_RED_RIDING_HOOD, new CastSpellExtraArgs(true));
                    _tempThreat = GetThreat(target);

                    if (_tempThreat != 0f)
                        ModifyThreatByPercent(target, -100);

                    _hoodGUID = target.GUID;
                    AddThreat(target, 1000000.0f);
                    _chaseTimer = 20000;
                    _isChasing = true;
                }
            }
            else
            {
                _isChasing = false;

                var target = Global.ObjAccessor.GetUnit(Me, _hoodGUID);

                if (target)
                {
                    _hoodGUID.Clear();

                    if (GetThreat(target) != 0f)
                        ModifyThreatByPercent(target, -100);

                    AddThreat(target, _tempThreat);
                    _tempThreat = 0;
                }

                _chaseTimer = 40000;
            }
        }
        else
            _chaseTimer -= diff;

        if (_isChasing)
            return;

        if (_fearTimer <= diff)
        {
            DoCastVictim(SpellIds.TERRIFYING_HOWL);
            _fearTimer = RandomHelper.URand(25000, 35000);
        }
        else
            _fearTimer -= diff;

        if (_swipeTimer <= diff)
        {
            DoCastVictim(SpellIds.WIDE_SWIPE);
            _swipeTimer = RandomHelper.URand(25000, 30000);
        }
        else
            _swipeTimer -= diff;
    }

    private void Initialize()
    {
        _chaseTimer = 30000;
        _fearTimer = RandomHelper.URand(25000, 35000);
        _swipeTimer = 5000;

        _hoodGUID.Clear();
        _tempThreat = 0;

        _isChasing = false;
    }
}

[Script]
internal class BossJulianne : ScriptedAI
{
    public bool IsFakingDeath;
    public uint ResurrectSelfTimer;
    public uint ResurrectTimer;
    public bool RomuloDead;
    private readonly InstanceScript _instance;
    private uint _aggroYellTimer;

    private uint _blindingPassionTimer;
    private uint _devotionTimer;
    private uint _drinkPoisonTimer;

    private uint _entryYellTimer;
    private uint _eternalAffectionTimer;

    private RajPhase _phase;
    private uint _powerfulAttractionTimer;

    private ObjectGuid _romuloGUID;
    private bool _summonedRomulo;
    private uint _summonRomuloTimer;

    public BossJulianne(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
        _entryYellTimer = 1000;
        _aggroYellTimer = 10000;
        IsFakingDeath = false;
        ResurrectTimer = 0;
    }

    public override void Reset()
    {
        Initialize();

        if (IsFakingDeath)
        {
            MiscConst.Resurrect(Me);
            IsFakingDeath = false;
        }
    }

    public override void JustEngagedWith(Unit who) { }

    public override void AttackStart(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.AttackStart(who);
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.DRINK_POISON)
        {
            Talk(TextIds.SAY_JULIANNE_DEATH01);
            _drinkPoisonTimer = 2500;
        }
    }

    public override void DamageTaken(Unit doneBy, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (damage < Me.Health)
            return;

        //anything below only used if incoming Damage will kill

        if (_phase == RajPhase.Julianne)
        {
            damage = 0;

            //this means already drinking, so return
            if (IsFakingDeath)
                return;

            Me.InterruptNonMeleeSpells(true);
            DoCast(Me, SpellIds.DRINK_POISON);

            IsFakingDeath = true;

            //Is This Usefull? Creature Julianne = (ObjectAccessor.GetCreature((me), JulianneGUID));
            return;
        }

        if (_phase == RajPhase.Romulo)
        {
            Log.Logger.Error("boss_julianneAI: cannot take Damage in PhaseRomulo, why was i here?");
            damage = 0;

            return;
        }

        if (_phase == RajPhase.Both)
        {
            //if this is true then we have to kill romulo too
            if (RomuloDead)
            {
                var romulo = ObjectAccessor.GetCreature(Me, _romuloGUID);

                if (romulo)
                {
                    romulo.RemoveUnitFlag(UnitFlags.Uninteractible);
                    romulo.MotionMaster.Clear();
                    romulo.SetDeathState(DeathState.JustDied);
                    romulo.CombatStop(true);
                    romulo.ReplaceAllDynamicFlags(UnitDynFlags.Lootable);
                }

                return;
            }

            //if not already returned, then romulo is alive and we can pretend die
            var romulo1 = (ObjectAccessor.GetCreature((Me), _romuloGUID));

            if (romulo1)
            {
                MiscConst.PretendToDie(Me);
                IsFakingDeath = true;
                romulo1.GetAI<BossRomulo>().ResurrectTimer = 10000;
                romulo1.GetAI<BossRomulo>().JulianneDead = true;
                damage = 0;

                return;
            }
        }

        Log.Logger.Error("boss_julianneAI: DamageTaken reach end of code, that should not happen.");
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_JULIANNE_DEATH02);
        _instance.SetBossState(DataTypes.OPERA_PERFORMANCE, EncounterState.Done);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_JULIANNE_SLAY);
    }

    public override void UpdateAI(uint diff)
    {
        if (_entryYellTimer != 0)
        {
            if (_entryYellTimer <= diff)
            {
                Talk(TextIds.SAY_JULIANNE_ENTER);
                _entryYellTimer = 0;
            }
            else
                _entryYellTimer -= diff;
        }

        if (_aggroYellTimer != 0)
        {
            if (_aggroYellTimer <= diff)
            {
                Talk(TextIds.SAY_JULIANNE_AGGRO);
                Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                Me.Faction = (uint)FactionTemplates.Monster2;
                _aggroYellTimer = 0;
            }
            else
                _aggroYellTimer -= diff;
        }

        if (_drinkPoisonTimer != 0)
        {
            //will do this TimeSpan.FromSeconds(2s)ecs after spell hit. this is Time to display visual as expected
            if (_drinkPoisonTimer <= diff)
            {
                MiscConst.PretendToDie(Me);
                _phase = RajPhase.Romulo;
                _summonRomuloTimer = 10000;
                _drinkPoisonTimer = 0;
            }
            else
                _drinkPoisonTimer -= diff;
        }

        if (_phase == RajPhase.Romulo &&
            !_summonedRomulo)
        {
            if (_summonRomuloTimer <= diff)
            {
                Creature pRomulo = Me.SummonCreature(CreatureIds.ROMULO, MiscConst.ROMULO_X, MiscConst.ROMULO_Y, Me.Location.Z, 0, TempSummonType.TimedOrDeadDespawn, TimeSpan.FromHours(2));

                if (pRomulo)
                {
                    _romuloGUID = pRomulo.GUID;
                    pRomulo.GetAI<BossRomulo>().JulianneGUID = Me.GUID;
                    pRomulo.GetAI<BossRomulo>().Phase = RajPhase.Romulo;
                    DoZoneInCombat(pRomulo);

                    pRomulo.Faction = (uint)FactionTemplates.Monster2;
                }

                _summonedRomulo = true;
            }
            else
                _summonRomuloTimer -= diff;
        }

        if (ResurrectSelfTimer != 0)
        {
            if (ResurrectSelfTimer <= diff)
            {
                MiscConst.Resurrect(Me);
                _phase = RajPhase.Both;
                IsFakingDeath = false;

                if (Me.Victim)
                    AttackStart(Me.Victim);

                ResurrectSelfTimer = 0;
                ResurrectTimer = 1000;
            }
            else
                ResurrectSelfTimer -= diff;
        }

        if (!UpdateVictim() || IsFakingDeath)
            return;

        if (RomuloDead)
        {
            if (ResurrectTimer <= diff)
            {
                var romulo = ObjectAccessor.GetCreature(Me, _romuloGUID);

                if (romulo && romulo.GetAI<BossRomulo>().IsFakingDeath)
                {
                    Talk(TextIds.SAY_JULIANNE_RESURRECT);
                    MiscConst.Resurrect(romulo);
                    romulo.GetAI<BossRomulo>().IsFakingDeath = false;
                    RomuloDead = false;
                    ResurrectTimer = 10000;
                }
            }
            else
                ResurrectTimer -= diff;
        }

        if (_blindingPassionTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target)
                DoCast(target, SpellIds.BLINDING_PASSION);

            _blindingPassionTimer = RandomHelper.URand(30000, 45000);
        }
        else
            _blindingPassionTimer -= diff;

        if (_devotionTimer <= diff)
        {
            DoCast(Me, SpellIds.DEVOTION);
            _devotionTimer = RandomHelper.URand(15000, 45000);
        }
        else
            _devotionTimer -= diff;

        if (_powerfulAttractionTimer <= diff)
        {
            DoCast(SelectTarget(SelectTargetMethod.Random, 0), SpellIds.POWERFUL_ATTRACTION);
            _powerfulAttractionTimer = RandomHelper.URand(5000, 30000);
        }
        else
            _powerfulAttractionTimer -= diff;

        if (_eternalAffectionTimer <= diff)
        {
            if (RandomHelper.URand(0, 1) != 0 && _summonedRomulo)
            {
                var romulo = (ObjectAccessor.GetCreature((Me), _romuloGUID));

                if (romulo &&
                    romulo.IsAlive &&
                    !RomuloDead)
                    DoCast(romulo, SpellIds.ETERNAL_AFFECTION);
            }
            else
                DoCast(Me, SpellIds.ETERNAL_AFFECTION);

            _eternalAffectionTimer = RandomHelper.URand(45000, 60000);
        }
        else
            _eternalAffectionTimer -= diff;

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _romuloGUID.Clear();
        _phase = RajPhase.Julianne;

        _blindingPassionTimer = 30000;
        _devotionTimer = 15000;
        _eternalAffectionTimer = 25000;
        _powerfulAttractionTimer = 5000;
        _summonRomuloTimer = 10000;
        _drinkPoisonTimer = 0;
        ResurrectSelfTimer = 0;

        _summonedRomulo = false;
        RomuloDead = false;
    }
}

[Script]
internal class BossRomulo : ScriptedAI
{
    public bool IsFakingDeath;
    public bool JulianneDead;

    public ObjectGuid JulianneGUID;
    public RajPhase Phase;
    public uint ResurrectTimer;
    private readonly InstanceScript _instance;
    private uint _backwardLungeTimer;
    private uint _daringTimer;
    private uint _deadlySwatheTimer;
    private uint _poisonThrustTimer;

    public BossRomulo(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustReachedHome()
    {
        Me.DespawnOrUnsummon();
    }

    public override void DamageTaken(Unit doneBy, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (damage < Me.Health)
            return;

        //anything below only used if incoming Damage will kill

        if (Phase == RajPhase.Romulo)
        {
            Talk(TextIds.SAY_ROMULO_DEATH);
            MiscConst.PretendToDie(Me);
            IsFakingDeath = true;
            Phase = RajPhase.Both;

            var julianne = ObjectAccessor.GetCreature(Me, JulianneGUID);

            if (julianne)
            {
                julianne.GetAI<BossJulianne>().RomuloDead = true;
                julianne.GetAI<BossJulianne>().ResurrectSelfTimer = 10000;
            }

            damage = 0;

            return;
        }

        if (Phase == RajPhase.Both)
        {
            if (JulianneDead)
            {
                var julianne = ObjectAccessor.GetCreature(Me, JulianneGUID);

                if (julianne)
                {
                    julianne.RemoveUnitFlag(UnitFlags.Uninteractible);
                    julianne.MotionMaster.Clear();
                    julianne.SetDeathState(DeathState.JustDied);
                    julianne.CombatStop(true);
                    julianne.ReplaceAllDynamicFlags(UnitDynFlags.Lootable);
                }

                return;
            }

            var julianne1 = ObjectAccessor.GetCreature(Me, JulianneGUID);

            if (julianne1)
            {
                MiscConst.PretendToDie(Me);
                IsFakingDeath = true;
                julianne1.GetAI<BossJulianne>().ResurrectTimer = 10000;
                julianne1.GetAI<BossJulianne>().RomuloDead = true;
                damage = 0;

                return;
            }
        }

        Log.Logger.Error("boss_romulo: DamageTaken reach end of code, that should not happen.");
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_ROMULO_AGGRO);

        if (!JulianneGUID.IsEmpty)
        {
            var julianne = ObjectAccessor.GetCreature(Me, JulianneGUID);

            if (julianne && julianne.Victim)
            {
                AddThreat(julianne.Victim, 1.0f);
                AttackStart(julianne.Victim);
            }
        }
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (Me.HasUnitFlag(UnitFlags.NonAttackable))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_ROMULO_DEATH);
        _instance.SetBossState(DataTypes.OPERA_PERFORMANCE, EncounterState.Done);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_ROMULO_SLAY);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim() || IsFakingDeath)
            return;

        if (JulianneDead)
        {
            if (ResurrectTimer <= diff)
            {
                var julianne = (ObjectAccessor.GetCreature((Me), JulianneGUID));

                if (julianne && julianne.GetAI<BossJulianne>().IsFakingDeath)
                {
                    Talk(TextIds.SAY_ROMULO_RESURRECT);
                    MiscConst.Resurrect(julianne);
                    julianne.GetAI<BossJulianne>().IsFakingDeath = false;
                    JulianneDead = false;
                    ResurrectTimer = 10000;
                }
            }
            else
                ResurrectTimer -= diff;
        }

        if (_backwardLungeTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 1, 100, true);

            if (target && !Me.Location.HasInArc(MathF.PI, target.Location))
            {
                DoCast(target, SpellIds.BACKWARD_LUNGE);
                _backwardLungeTimer = RandomHelper.URand(15000, 30000);
            }
        }
        else
            _backwardLungeTimer -= diff;

        if (_daringTimer <= diff)
        {
            DoCast(Me, SpellIds.DARING);
            _daringTimer = RandomHelper.URand(20000, 40000);
        }
        else
            _daringTimer -= diff;

        if (_deadlySwatheTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target)
                DoCast(target, SpellIds.DEADLY_SWATHE);

            _deadlySwatheTimer = RandomHelper.URand(15000, 25000);
        }
        else
            _deadlySwatheTimer -= diff;

        if (_poisonThrustTimer <= diff)
        {
            DoCastVictim(SpellIds.POISON_THRUST);
            _poisonThrustTimer = RandomHelper.URand(10000, 20000);
        }
        else
            _poisonThrustTimer -= diff;

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        JulianneGUID.Clear();
        Phase = RajPhase.Romulo;

        _backwardLungeTimer = 15000;
        _daringTimer = 20000;
        _deadlySwatheTimer = 25000;
        _poisonThrustTimer = 10000;
        ResurrectTimer = 10000;

        IsFakingDeath = false;
        JulianneDead = false;
    }
}
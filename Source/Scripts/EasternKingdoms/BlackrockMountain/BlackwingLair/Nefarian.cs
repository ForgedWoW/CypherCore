// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.VictorNefarius;

internal struct SpellIds
{
    // Victor Nefarius
    // Ubrs Spells
    public const uint CHROMATIC_CHAOS = 16337; // Self Cast hits 10339

    public const uint VAELASTRASZZ_SPAWN = 16354; // Self Cast Depawn one sec after

    // Bwl Spells
    public const uint SHADOWBOLT = 22677;
    public const uint SHADOWBOLT_VOLLEY = 22665;
    public const uint SHADOW_COMMAND = 22667;
    public const uint FEAR = 22678;

    public const uint NEFARIANS_BARRIER = 22663;

    // Nefarian
    public const uint SHADOWFLAME_INITIAL = 22992;
    public const uint SHADOWFLAME = 22539;
    public const uint BELLOWINGROAR = 22686;
    public const uint VEILOFSHADOW = 7068;
    public const uint CLEAVE = 20691;
    public const uint TAILLASH = 23364;

    public const uint MAGE = 23410;         // wild magic
    public const uint WARRIOR = 23397;      // beserk
    public const uint DRUID = 23398;        // cat form
    public const uint PRIEST = 23401;       // corrupted healing
    public const uint PALADIN = 23418;      // syphon blessing
    public const uint SHAMAN = 23425;       // totems
    public const uint WARLOCK = 23427;      // infernals
    public const uint HUNTER = 23436;       // bow broke
    public const uint ROGUE = 23414;        // Paralise
    public const uint DEATH_KNIGHT = 49576; // Death Grip

    // 19484
    // 22664
    // 22674
    // 22666
}

internal struct TextIds
{
    // Nefarius
    // Ubrs
    public const uint SAY_CHAOS_SPELL = 9;
    public const uint SAY_SUCCESS = 10;

    public const uint SAY_FAILURE = 11;

    // Bwl
    public const uint SAY_GAMESBEGIN1 = 12;

    public const uint SAY_GAMESBEGIN2 = 13;
    // public const uint SayVaelIntro             = 14; Not used - when he corrupts Vaelastrasz

    // Nefarian
    public const uint SAY_RANDOM = 0;
    public const uint SAY_RAISE_SKELETONS = 1;
    public const uint SAY_SLAY = 2;
    public const uint SAY_DEATH = 3;

    public const uint SAY_MAGE = 4;
    public const uint SAY_WARRIOR = 5;
    public const uint SAY_DRUID = 6;
    public const uint SAY_PRIEST = 7;
    public const uint SAY_PALADIN = 8;
    public const uint SAY_SHAMAN = 9;
    public const uint SAY_WARLOCK = 10;
    public const uint SAY_HUNTER = 11;
    public const uint SAY_ROGUE = 12;
    public const uint SAY_DEATH_KNIGHT = 13;

    public const uint GOSSIP_ID = 6045;
    public const uint GOSSIP_OPTION_ID = 0;
}

internal struct CreatureIds
{
    public const uint BRONZE_DRAKANOID = 14263;
    public const uint BLUE_DRAKANOID = 14261;
    public const uint RED_DRAKANOID = 14264;
    public const uint GREEN_DRAKANOID = 14262;
    public const uint BLACK_DRAKANOID = 14265;
    public const uint CHROMATIC_DRAKANOID = 14302;

    public const uint BONE_CONSTRUCT = 14605;

    // Ubrs
    public const uint GYTH = 10339;
}

internal struct GameObjectIds
{
    public const uint PORTCULLIS_ACTIVE = 164726;
    public const uint PORTCULLIS_TOBOSSROOMS = 175186;
}

internal struct MiscConst
{
    public const uint NEFARIUS_PATH2 = 1379671;
    public const uint NEFARIUS_PATH3 = 1379672;

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
        CreatureIds.BRONZE_DRAKANOID, CreatureIds.BLUE_DRAKANOID, CreatureIds.RED_DRAKANOID, CreatureIds.GREEN_DRAKANOID, CreatureIds.BLACK_DRAKANOID
    };
}

internal struct EventIds
{
    // Victor Nefarius
    public const uint SPAWN_ADD = 1;
    public const uint SHADOW_BOLT = 2;
    public const uint FEAR = 3;

    public const uint MIND_CONTROL = 4;

    // Nefarian
    public const uint SHADOWFLAME = 5;
    public const uint VEILOFSHADOW = 6;
    public const uint CLEAVE = 7;
    public const uint TAILLASH = 8;

    public const uint CLASSCALL = 9;

    // Ubrs
    public const uint CHAOS1 = 10;
    public const uint CHAOS2 = 11;
    public const uint PATH2 = 12;
    public const uint PATH3 = 13;
    public const uint SUCCESS1 = 14;
    public const uint SUCCESS2 = 15;
    public const uint SUCCESS3 = 16;
}

[Script]
internal class BossVictorNefarius : BossAI
{
    private uint _spawnedAdds;

    public BossVictorNefarius(Creature creature) : base(creature, DataTypes.NEFARIAN)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        if (Me.Location.MapId == 469)
        {
            if (!Me.FindNearestCreature(BwlCreatureIds.NEFARIAN, 1000.0f, true))
                _Reset();

            Me.SetVisible(true);
            Me.SetNpcFlag(NPCFlags.Gossip);
            Me.Faction = (uint)FactionTemplates.Friendly;
            Me.SetStandState(UnitStandStateType.SitHighChair);
            Me.RemoveAura(SpellIds.NEFARIANS_BARRIER);
        }
    }

    public override void JustReachedHome()
    {
        Reset();
    }

    public override void SummonedCreatureDies(Creature summon, Unit killer)
    {
        if (summon.Entry != BwlCreatureIds.NEFARIAN)
        {
            summon.UpdateEntry(CreatureIds.BONE_CONSTRUCT);
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
            Events.ScheduleEvent(EventIds.PATH2, TimeSpan.FromSeconds(9));
        }

        if (type == 1 &&
            data == 2)
            Events.ScheduleEvent(EventIds.SUCCESS1, TimeSpan.FromSeconds(5));
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
                    case EventIds.PATH2:
                        Me.MotionMaster.MovePath(MiscConst.NEFARIUS_PATH2, false);
                        Events.ScheduleEvent(EventIds.CHAOS1, TimeSpan.FromSeconds(7));

                        break;
                    case EventIds.CHAOS1:
                        var gyth = Me.FindNearestCreature(CreatureIds.GYTH, 75.0f, true);

                        if (gyth)
                        {
                            Me.SetFacingToObject(gyth);
                            Talk(TextIds.SAY_CHAOS_SPELL);
                        }

                        Events.ScheduleEvent(EventIds.CHAOS2, TimeSpan.FromSeconds(2));

                        break;
                    case EventIds.CHAOS2:
                        DoCast(SpellIds.CHROMATIC_CHAOS);
                        Me.SetFacingTo(1.570796f);

                        break;
                    case EventIds.SUCCESS1:
                        Unit player = Me.SelectNearestPlayer(60.0f);

                        if (player)
                        {
                            Me.SetFacingToObject(player);
                            Talk(TextIds.SAY_SUCCESS);
                            var portcullis1 = Me.FindNearestGameObject(GameObjectIds.PORTCULLIS_ACTIVE, 65.0f);

                            if (portcullis1)
                                portcullis1.SetGoState(GameObjectState.Active);

                            var portcullis2 = Me.FindNearestGameObject(GameObjectIds.PORTCULLIS_TOBOSSROOMS, 80.0f);

                            if (portcullis2)
                                portcullis2.SetGoState(GameObjectState.Active);
                        }

                        Events.ScheduleEvent(EventIds.SUCCESS2, TimeSpan.FromSeconds(4));

                        break;
                    case EventIds.SUCCESS2:
                        DoCast(Me, SpellIds.VAELASTRASZZ_SPAWN);
                        Me.DespawnOrUnsummon(TimeSpan.FromSeconds(1));

                        break;
                    case EventIds.PATH3:
                        Me.MotionMaster.MovePath(MiscConst.NEFARIUS_PATH3, false);

                        break;
                }
            });

            return;
        }

        // Only do this if we haven't spawned nefarian yet
        if (UpdateVictim() &&
            _spawnedAdds <= 42)
        {
            Events.Update(diff);

            if (Me.HasUnitState(UnitState.Casting))
                return;

            Events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.SHADOW_BOLT:
                        switch (RandomHelper.URand(0, 1))
                        {
                            case 0:
                                DoCastVictim(SpellIds.SHADOWBOLT_VOLLEY);

                                break;
                            case 1:
                                var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

                                if (target)
                                    DoCast(target, SpellIds.SHADOWBOLT);

                                break;
                        }

                        ResetThreatList();
                        Events.ScheduleEvent(EventIds.SHADOW_BOLT, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));

                        break;
                    case EventIds.FEAR:
                    {
                        var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

                        if (target)
                            DoCast(target, SpellIds.FEAR);

                        Events.ScheduleEvent(EventIds.FEAR, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));

                        break;
                    }
                    case EventIds.MIND_CONTROL:
                    {
                        var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

                        if (target)
                            DoCast(target, SpellIds.SHADOW_COMMAND);

                        Events.ScheduleEvent(EventIds.MIND_CONTROL, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));

                        break;
                    }
                    case EventIds.SPAWN_ADD:
                        for (byte i = 0; i < 2; ++i)
                        {
                            uint creatureID;

                            if (RandomHelper.URand(0, 2) == 0)
                                creatureID = CreatureIds.CHROMATIC_DRAKANOID;
                            else
                                creatureID = MiscConst.Entry[RandomHelper.URand(0, 4)];

                            Creature dragon = Me.SummonCreature(creatureID, MiscConst.DrakeSpawnLoc[i]);

                            if (dragon)
                            {
                                dragon.Faction = (uint)FactionTemplates.DragonflightBlack;
                                dragon.AI.AttackStart(Me.Victim);
                            }

                            if (++_spawnedAdds >= 42)
                            {
                                Creature nefarian = Me.SummonCreature(BwlCreatureIds.NEFARIAN, MiscConst.NefarianLoc[0]);

                                if (nefarian)
                                {
                                    nefarian.SetActive(true);
                                    nefarian.SetFarVisible(true);
                                    nefarian.SetCanFly(true);
                                    nefarian.SetDisableGravity(true);
                                    nefarian.SpellFactory.CastSpell(SpellIds.SHADOWFLAME_INITIAL);
                                    nefarian.MotionMaster.MovePoint(1, MiscConst.NefarianLoc[1]);
                                }

                                Events.CancelEvent(EventIds.MIND_CONTROL);
                                Events.CancelEvent(EventIds.FEAR);
                                Events.CancelEvent(EventIds.SHADOW_BOLT);
                                Me.SetVisible(false);

                                return;
                            }
                        }

                        Events.ScheduleEvent(EventIds.SPAWN_ADD, TimeSpan.FromSeconds(4));

                        break;
                }

                if (Me.HasUnitState(UnitState.Casting))
                    return;
            });
        }
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        if (menuId == TextIds.GOSSIP_ID &&
            gossipListId == TextIds.GOSSIP_OPTION_ID)
        {
            player.CloseGossipMenu();
            Talk(TextIds.SAY_GAMESBEGIN1);
            BeginEvent(player);
        }

        return false;
    }

    private void Initialize()
    {
        _spawnedAdds = 0;
    }

    private void BeginEvent(Player target)
    {
        _JustEngagedWith(target);

        Talk(TextIds.SAY_GAMESBEGIN2);

        Me.Faction = (uint)FactionTemplates.DragonflightBlack;
        Me.RemoveNpcFlag(NPCFlags.Gossip);
        DoCast(Me, SpellIds.NEFARIANS_BARRIER);
        Me.SetStandState(UnitStandStateType.Stand);
        Me.SetImmuneToPC(false);
        AttackStart(target);
        Events.ScheduleEvent(EventIds.SHADOW_BOLT, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));
        Events.ScheduleEvent(EventIds.FEAR, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
        //_events.ScheduleEvent(EventIds.MindControl, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
        Events.ScheduleEvent(EventIds.SPAWN_ADD, TimeSpan.FromSeconds(10));
    }
}

[Script]
internal class BossNefarian : BossAI
{
    private bool _canDespawn;
    private uint _despawnTimer;
    private bool _phase3;

    public BossNefarian(Creature creature) : base(creature, DataTypes.NEFARIAN)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void JustReachedHome()
    {
        _canDespawn = true;
    }

    public override void JustEngagedWith(Unit who)
    {
        Events.ScheduleEvent(EventIds.SHADOWFLAME, TimeSpan.FromSeconds(12));
        Events.ScheduleEvent(EventIds.FEAR, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
        Events.ScheduleEvent(EventIds.VEILOFSHADOW, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
        Events.ScheduleEvent(EventIds.CLEAVE, TimeSpan.FromSeconds(7));
        //_events.ScheduleEvent(EventIds.Taillash, TimeSpan.FromSeconds(10));
        Events.ScheduleEvent(EventIds.CLASSCALL, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
        Talk(TextIds.SAY_RANDOM);
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.SAY_DEATH);
    }

    public override void KilledUnit(Unit victim)
    {
        if ((RandomHelper.Rand32() % 5) != 0)
            return;

        Talk(TextIds.SAY_SLAY, victim);
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
        if (_canDespawn && _despawnTimer <= diff)
        {
            Instance.SetBossState(DataTypes.NEFARIAN, EncounterState.Fail);

            var constructList = Me.GetCreatureListWithEntryInGrid(CreatureIds.BONE_CONSTRUCT, 500.0f);

            foreach (var creature in constructList)
                creature.DespawnOrUnsummon();
        }
        else
            _despawnTimer -= diff;

        if (!UpdateVictim())
            return;

        if (_canDespawn)
            _canDespawn = false;

        Events.Update(diff);

        if (Me.HasUnitState(UnitState.Casting))
            return;

        Events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case EventIds.SHADOWFLAME:
                    DoCastVictim(SpellIds.SHADOWFLAME);
                    Events.ScheduleEvent(EventIds.SHADOWFLAME, TimeSpan.FromSeconds(12));

                    break;
                case EventIds.FEAR:
                    DoCastVictim(SpellIds.BELLOWINGROAR);
                    Events.ScheduleEvent(EventIds.FEAR, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));

                    break;
                case EventIds.VEILOFSHADOW:
                    DoCastVictim(SpellIds.VEILOFSHADOW);
                    Events.ScheduleEvent(EventIds.VEILOFSHADOW, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));

                    break;
                case EventIds.CLEAVE:
                    DoCastVictim(SpellIds.CLEAVE);
                    Events.ScheduleEvent(EventIds.CLEAVE, TimeSpan.FromSeconds(7));

                    break;
                case EventIds.TAILLASH:
                    // Cast Nyi since we need a better check for behind Target
                    DoCastVictim(SpellIds.TAILLASH);
                    Events.ScheduleEvent(EventIds.TAILLASH, TimeSpan.FromSeconds(10));

                    break;
                case EventIds.CLASSCALL:
                    var target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

                    if (target)
                        switch (target.Class)
                        {
                            case PlayerClass.Mage:
                                Talk(TextIds.SAY_MAGE);
                                DoCast(Me, SpellIds.MAGE);

                                break;
                            case PlayerClass.Warrior:
                                Talk(TextIds.SAY_WARRIOR);
                                DoCast(Me, SpellIds.WARRIOR);

                                break;
                            case PlayerClass.Druid:
                                Talk(TextIds.SAY_DRUID);
                                DoCast(target, SpellIds.DRUID);

                                break;
                            case PlayerClass.Priest:
                                Talk(TextIds.SAY_PRIEST);
                                DoCast(Me, SpellIds.PRIEST);

                                break;
                            case PlayerClass.Paladin:
                                Talk(TextIds.SAY_PALADIN);
                                DoCast(Me, SpellIds.PALADIN);

                                break;
                            case PlayerClass.Shaman:
                                Talk(TextIds.SAY_SHAMAN);
                                DoCast(Me, SpellIds.SHAMAN);

                                break;
                            case PlayerClass.Warlock:
                                Talk(TextIds.SAY_WARLOCK);
                                DoCast(Me, SpellIds.WARLOCK);

                                break;
                            case PlayerClass.Hunter:
                                Talk(TextIds.SAY_HUNTER);
                                DoCast(Me, SpellIds.HUNTER);

                                break;
                            case PlayerClass.Rogue:
                                Talk(TextIds.SAY_ROGUE);
                                DoCast(Me, SpellIds.ROGUE);

                                break;
                            case PlayerClass.Deathknight:
                                Talk(TextIds.SAY_DEATH_KNIGHT);
                                DoCast(Me, SpellIds.DEATH_KNIGHT);

                                break;
                        }

                    Events.ScheduleEvent(EventIds.CLASSCALL, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));

                    break;
            }

            if (Me.HasUnitState(UnitState.Casting))
                return;
        });

        // Phase3 begins when health below 20 pct
        if (!_phase3 &&
            HealthBelowPct(20))
        {
            var constructList = Me.GetCreatureListWithEntryInGrid(CreatureIds.BONE_CONSTRUCT, 500.0f);

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

            _phase3 = true;
            Talk(TextIds.SAY_RAISE_SKELETONS);
        }

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _phase3 = false;
        _canDespawn = false;
        _despawnTimer = 30000;
    }
}
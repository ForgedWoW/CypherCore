// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.ShadeOfAran;

internal struct SpellIds
{
    public const uint FROSTBOLT = 29954;
    public const uint FIREBALL = 29953;
    public const uint ARCMISSLE = 29955;
    public const uint CHAINSOFICE = 29991;
    public const uint DRAGONSBREATH = 29964;
    public const uint MASSSLOW = 30035;
    public const uint FLAME_WREATH = 29946;
    public const uint AOE_CS = 29961;
    public const uint PLAYERPULL = 32265;
    public const uint AEXPLOSION = 29973;
    public const uint MASS_POLY = 29963;
    public const uint BLINK_CENTER = 29967;
    public const uint ELEMENTALS = 29962;
    public const uint CONJURE = 29975;
    public const uint DRINK = 30024;
    public const uint POTION = 32453;
    public const uint AOE_PYROBLAST = 29978;

    public const uint CIRCULAR_BLIZZARD = 29951;
    public const uint WATERBOLT = 31012;
    public const uint SHADOW_PYRO = 29978;
}

internal struct CreatureIds
{
    public const uint WATER_ELEMENTAL = 17167;
    public const uint SHADOW_OF_ARAN = 18254;
    public const uint ARAN_BLIZZARD = 17161;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_FLAMEWREATH = 1;
    public const uint SAY_BLIZZARD = 2;
    public const uint SAY_EXPLOSION = 3;
    public const uint SAY_DRINK = 4;
    public const uint SAY_ELEMENTALS = 5;
    public const uint SAY_KILL = 6;
    public const uint SAY_TIMEOVER = 7;
    public const uint SAY_DEATH = 8;
    public const uint SAY_ATIESH = 9;
}

internal enum SuperSpell
{
    Flame = 0,
    Blizzard,
    Ae
}

[Script]
internal class BossAran : ScriptedAI
{
    private static readonly uint[] AtieshStaves =
    {
        22589, //ItemAtieshMage,
        22630, //ItemAtieshWarlock,
        22631, //ItemAtieshPriest,
        22632  //ItemAtieshDruid,
    };

    private readonly ObjectGuid[] _flameWreathTarget = new ObjectGuid[3];
    private readonly float[] _fwTargPosX = new float[3];
    private readonly float[] _fwTargPosY = new float[3];

    private readonly InstanceScript _instance;

    private uint _arcaneCooldown;
    private uint _berserkTimer;
    private uint _closeDoorTimer; // Don't close the door right on aggro in case some people are still entering.

    private uint _currentNormalSpell;
    private bool _drinking;

    private uint _drinkInterruptTimer;
    private bool _drinkInturrupted;

    private bool _elementalsSpawned;
    private uint _fireCooldown;
    private uint _flameWreathCheckTime;

    private uint _flameWreathTimer;
    private uint _frostCooldown;

    private SuperSpell _lastSuperSpell;
    private uint _normalCastTimer;

    private uint _secondarySpellTimer;
    private bool _seenAtiesh;
    private uint _superCastTimer;

    public BossAran(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();

        // Not in progress
        _instance.SetBossState(DataTypes.ARAN, EncounterState.NotStarted);
        _instance.HandleGameObject(_instance.GetGuidData(DataTypes.GO_LIBRARY_DOOR), true);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_KILL);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DEATH);

        _instance.SetBossState(DataTypes.ARAN, EncounterState.Done);
        _instance.HandleGameObject(_instance.GetGuidData(DataTypes.GO_LIBRARY_DOOR), true);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);

        _instance.SetBossState(DataTypes.ARAN, EncounterState.InProgress);
        _instance.HandleGameObject(_instance.GetGuidData(DataTypes.GO_LIBRARY_DOOR), false);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (_closeDoorTimer != 0)
        {
            if (_closeDoorTimer <= diff)
            {
                _instance.HandleGameObject(_instance.GetGuidData(DataTypes.GO_LIBRARY_DOOR), false);
                _closeDoorTimer = 0;
            }
            else
            {
                _closeDoorTimer -= diff;
            }
        }

        //Cooldowns for casts
        if (_arcaneCooldown != 0)
        {
            if (_arcaneCooldown >= diff)
                _arcaneCooldown -= diff;
            else _arcaneCooldown = 0;
        }

        if (_fireCooldown != 0)
        {
            if (_fireCooldown >= diff)
                _fireCooldown -= diff;
            else _fireCooldown = 0;
        }

        if (_frostCooldown != 0)
        {
            if (_frostCooldown >= diff)
                _frostCooldown -= diff;
            else _frostCooldown = 0;
        }

        if (!_drinking &&
            Me.GetMaxPower(PowerType.Mana) != 0 &&
            Me.GetPowerPct(PowerType.Mana) < 20.0f)
        {
            _drinking = true;
            Me.InterruptNonMeleeSpells(false);

            Talk(TextIds.SAY_DRINK);

            if (!_drinkInturrupted)
            {
                DoCast(Me, SpellIds.MASS_POLY, new CastSpellExtraArgs(true));
                DoCast(Me, SpellIds.CONJURE, new CastSpellExtraArgs(false));
                DoCast(Me, SpellIds.DRINK, new CastSpellExtraArgs(false));
                Me.SetStandState(UnitStandStateType.Sit);
                _drinkInterruptTimer = 10000;
            }
        }

        //Drink Interrupt
        if (_drinking && _drinkInturrupted)
        {
            _drinking = false;
            Me.RemoveAura(SpellIds.DRINK);
            Me.SetStandState(UnitStandStateType.Stand);
            Me.SetPower(PowerType.Mana, Me.GetMaxPower(PowerType.Mana) - 32000);
            DoCast(Me, SpellIds.POTION, new CastSpellExtraArgs(false));
        }

        //Drink Interrupt Timer
        if (_drinking && !_drinkInturrupted)
        {
            if (_drinkInterruptTimer >= diff)
            {
                _drinkInterruptTimer -= diff;
            }
            else
            {
                Me.SetStandState(UnitStandStateType.Stand);
                DoCast(Me, SpellIds.POTION, new CastSpellExtraArgs(true));
                DoCast(Me, SpellIds.AOE_PYROBLAST, new CastSpellExtraArgs(false));
                _drinkInturrupted = true;
                _drinking = false;
            }
        }

        //Don't execute any more code if we are drinking
        if (_drinking)
            return;

        //Normal casts
        if (_normalCastTimer <= diff)
        {
            if (!Me.IsNonMeleeSpellCast(false))
            {
                var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                if (!target)
                    return;

                var spells = new uint[3];
                byte availableSpells = 0;

                //Check for what spells are not on cooldown
                if (_arcaneCooldown == 0)
                {
                    spells[availableSpells] = SpellIds.ARCMISSLE;
                    ++availableSpells;
                }

                if (_fireCooldown == 0)
                {
                    spells[availableSpells] = SpellIds.FIREBALL;
                    ++availableSpells;
                }

                if (_frostCooldown == 0)
                {
                    spells[availableSpells] = SpellIds.FROSTBOLT;
                    ++availableSpells;
                }

                //If no available spells wait 1 second and try again
                if (availableSpells != 0)
                {
                    _currentNormalSpell = spells[RandomHelper.Rand32() % availableSpells];
                    DoCast(target, _currentNormalSpell);
                }
            }

            _normalCastTimer = 1000;
        }
        else
        {
            _normalCastTimer -= diff;
        }

        if (_secondarySpellTimer <= diff)
        {
            switch (RandomHelper.URand(0, 1))
            {
                case 0:
                    DoCast(Me, SpellIds.AOE_CS);

                    break;
                case 1:
                    var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                    if (target)
                        DoCast(target, SpellIds.CHAINSOFICE);

                    break;
            }

            _secondarySpellTimer = RandomHelper.URand(5000, 20000);
        }
        else
        {
            _secondarySpellTimer -= diff;
        }

        if (_superCastTimer <= diff)
        {
            var available = new SuperSpell[2];

            switch (_lastSuperSpell)
            {
                case SuperSpell.Ae:
                    available[0] = SuperSpell.Flame;
                    available[1] = SuperSpell.Blizzard;

                    break;
                case SuperSpell.Flame:
                    available[0] = SuperSpell.Ae;
                    available[1] = SuperSpell.Blizzard;

                    break;
                case SuperSpell.Blizzard:
                    available[0] = SuperSpell.Flame;
                    available[1] = SuperSpell.Ae;

                    break;
                default:
                    available[0] = 0;
                    available[1] = 0;

                    break;
            }

            _lastSuperSpell = available[RandomHelper.URand(0, 1)];

            switch (_lastSuperSpell)
            {
                case SuperSpell.Ae:
                    Talk(TextIds.SAY_EXPLOSION);

                    DoCast(Me, SpellIds.BLINK_CENTER, new CastSpellExtraArgs(true));
                    DoCast(Me, SpellIds.PLAYERPULL, new CastSpellExtraArgs(true));
                    DoCast(Me, SpellIds.MASSSLOW, new CastSpellExtraArgs(true));
                    DoCast(Me, SpellIds.AEXPLOSION, new CastSpellExtraArgs(false));

                    break;

                case SuperSpell.Flame:
                    Talk(TextIds.SAY_FLAMEWREATH);

                    _flameWreathTimer = 20000;
                    _flameWreathCheckTime = 500;

                    _flameWreathTarget[0].Clear();
                    _flameWreathTarget[1].Clear();
                    _flameWreathTarget[2].Clear();

                    FlameWreathEffect();

                    break;

                case SuperSpell.Blizzard:
                    Talk(TextIds.SAY_BLIZZARD);

                    Creature pSpawn = Me.SummonCreature(CreatureIds.ARAN_BLIZZARD, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(25));

                    if (pSpawn)
                    {
                        pSpawn.Faction = Me.Faction;
                        pSpawn.SpellFactory.CastSpell(pSpawn, SpellIds.CIRCULAR_BLIZZARD, false);
                    }

                    break;
            }

            _superCastTimer = RandomHelper.URand(35000, 40000);
        }
        else
        {
            _superCastTimer -= diff;
        }

        if (!_elementalsSpawned &&
            HealthBelowPct(40))
        {
            _elementalsSpawned = true;

            for (uint i = 0; i < 4; ++i)
            {
                Creature unit = Me.SummonCreature(CreatureIds.WATER_ELEMENTAL, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(90));

                if (unit)
                {
                    unit.Attack(Me.Victim, true);
                    unit.Faction = Me.Faction;
                }
            }

            Talk(TextIds.SAY_ELEMENTALS);
        }

        if (_berserkTimer <= diff)
        {
            for (uint i = 0; i < 5; ++i)
            {
                Creature unit = Me.SummonCreature(CreatureIds.SHADOW_OF_ARAN, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(5));

                if (unit)
                {
                    unit.Attack(Me.Victim, true);
                    unit.Faction = Me.Faction;
                }
            }

            Talk(TextIds.SAY_TIMEOVER);

            _berserkTimer = 60000;
        }
        else
        {
            _berserkTimer -= diff;
        }

        //Flame Wreath check
        if (_flameWreathTimer != 0)
        {
            if (_flameWreathTimer >= diff)
                _flameWreathTimer -= diff;
            else _flameWreathTimer = 0;

            if (_flameWreathCheckTime <= diff)
            {
                for (byte i = 0; i < 3; ++i)
                {
                    if (_flameWreathTarget[i].IsEmpty)
                        continue;

                    var unit = Global.ObjAccessor.GetUnit(Me, _flameWreathTarget[i]);

                    if (unit && !unit.IsWithinDist2d(_fwTargPosX[i], _fwTargPosY[i], 3))
                    {
                        unit.SpellFactory.CastSpell(unit,
                                       20476,
                                       new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                           .SetOriginalCaster(Me.GUID));

                        unit.SpellFactory.CastSpell(unit, 11027, true);
                        _flameWreathTarget[i].Clear();
                    }
                }

                _flameWreathCheckTime = 500;
            }
            else
            {
                _flameWreathCheckTime -= diff;
            }
        }

        if (_arcaneCooldown != 0 &&
            _fireCooldown != 0 &&
            _frostCooldown != 0)
            DoMeleeAttackIfReady();
    }

    public override void DamageTaken(Unit pAttacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!_drinkInturrupted &&
            _drinking &&
            damage != 0)
            _drinkInturrupted = true;
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        //We only care about interrupt effects and only if they are durring a spell currently being cast
        if (!spellInfo.HasEffect(SpellEffectName.InterruptCast) ||
            !Me.IsNonMeleeSpellCast(false))
            return;

        //Interrupt effect
        Me.InterruptNonMeleeSpells(false);

        //Normally we would set the cooldown equal to the spell duration
        //but we do not have access to the DurationStore

        switch (_currentNormalSpell)
        {
            case SpellIds.ARCMISSLE:
                _arcaneCooldown = 5000;

                break;
            case SpellIds.FIREBALL:
                _fireCooldown = 5000;

                break;
            case SpellIds.FROSTBOLT:
                _frostCooldown = 5000;

                break;
        }
    }

    public override void MoveInLineOfSight(Unit who)
    {
        base.MoveInLineOfSight(who);

        if (_seenAtiesh ||
            Me.IsInCombat ||
            Me.GetDistance2d(who) > Me.GetAttackDistance(who) + 10.0f)
            return;

        var player = who.AsPlayer;

        if (!player)
            return;

        foreach (var id in AtieshStaves)
        {
            if (!PlayerHasWeaponEquipped(player, id))
                continue;

            _seenAtiesh = true;
            Talk(TextIds.SAY_ATIESH);
            Me.SetFacingTo(Me.Location.GetAbsoluteAngle(player.Location));
            Me.ClearUnitState(UnitState.Moving);
            Me.MotionMaster.MoveDistract(7 * Time.IN_MILLISECONDS, Me.Location.GetAbsoluteAngle(who.Location));

            break;
        }
    }

    private void Initialize()
    {
        _secondarySpellTimer = 5000;
        _normalCastTimer = 0;
        _superCastTimer = 35000;
        _berserkTimer = 720000;
        _closeDoorTimer = 15000;

        _lastSuperSpell = (SuperSpell)(RandomHelper.Rand32() % 3);

        _flameWreathTimer = 0;
        _flameWreathCheckTime = 0;

        _currentNormalSpell = 0;
        _arcaneCooldown = 0;
        _fireCooldown = 0;
        _frostCooldown = 0;

        _drinkInterruptTimer = 10000;

        _elementalsSpawned = false;
        _drinking = false;
        _drinkInturrupted = false;
    }

    private void FlameWreathEffect()
    {
        List<Unit> targets = new();

        //store the threat list in a different container
        foreach (var refe in Me.GetThreatManager().SortedThreatList)
        {
            var target = refe.Victim;

            if (refe.Victim.IsPlayer &&
                refe.Victim.IsAlive)
                targets.Add(target);
        }

        //cut down to size if we have more than 3 targets
        targets.RandomResize(3);

        uint i = 0;

        foreach (var unit in targets)
            if (unit)
            {
                _flameWreathTarget[i] = unit.GUID;
                _fwTargPosX[i] = unit.Location.X;
                _fwTargPosY[i] = unit.Location.Y;
                DoCast(unit, SpellIds.FLAME_WREATH, new CastSpellExtraArgs(true));
                ++i;
            }
    }

    private bool PlayerHasWeaponEquipped(Player player, uint itemEntry)
    {
        var item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

        if (item && item.Entry == itemEntry)
            return true;

        return false;
    }
}

[Script]
internal class WaterElemental : ScriptedAI
{
    public WaterElemental(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromMilliseconds(2000 + (RandomHelper.Rand32() % 3000)),
                           task =>
                           {
                               DoCastVictim(SpellIds.WATERBOLT);
                               task.Repeat(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
                           });
    }

    public override void JustEngagedWith(Unit who) { }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff);
    }
}
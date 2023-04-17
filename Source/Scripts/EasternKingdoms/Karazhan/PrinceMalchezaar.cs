// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.PrinceMalchezaar;

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_AXE_TOSS1 = 1;

    public const uint SAY_AXE_TOSS2 = 2;

    //public const uint SaySpecial1                = 3; Not used, needs to be implemented, but I don't know where it should be used.
    //public const uint SaySpecial2                = 4; Not used, needs to be implemented, but I don't know where it should be used.
    //public const uint SaySpecial3                = 5; Not used, needs to be implemented, but I don't know where it should be used.
    public const uint SAY_SLAY = 6;
    public const uint SAY_SUMMON = 7;
    public const uint SAY_DEATH = 8;
}

internal struct SpellIds
{
    public const uint ENFEEBLE = 30843; //Enfeeble during phase 1 and 2
    public const uint ENFEEBLE_EFFECT = 41624;

    public const uint SHADOWNOVA = 30852;    //Shadownova used during all phases
    public const uint SW_PAIN = 30854;        //Shadow word pain during phase 1 and 3 (different targeting rules though)
    public const uint THRASH_PASSIVE = 12787; //Extra attack chance during phase 2
    public const uint SUNDER_ARMOR = 30901;   //Sunder armor during phase 2
    public const uint THRASH_AURA = 12787;    //Passive proc chance for thrash
    public const uint EQUIP_AXES = 30857;     //Visual for axe equiping
    public const uint AMPLIFY_DAMAGE = 39095; //Amplifiy during phase 3
    public const uint CLEAVE = 30131;        //Same as Nightbane.
    public const uint HELLFIRE = 30859;      //Infenals' hellfire aura

    public const uint INFERNAL_RELAY = 30834;
}

internal struct MiscConst
{
    public const uint TOTAL_INFERNAL_POINTS = 18;
    public const uint NETHERSPITE_INFERNAL = 17646; //The netherspite infernal creature
    public const uint MALCHEZARS_AXE = 17650;       //Malchezar's axes (creatures), summoned during phase 3

    public const uint INFERNAL_MODEL_INVISIBLE = 11686; //Infernal Effects
    public const int EQUIP_ID_AXE = 33542;              //Axes info
}

[Script]
internal class NetherspiteInfernal : ScriptedAI
{
    public ObjectGuid Malchezaar;
    public Vector2 Point;

    public NetherspiteInfernal(Creature creature) : base(creature) { }

    public override void Reset() { }

    public override void JustEngagedWith(Unit who) { }

    public override void MoveInLineOfSight(Unit who) { }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }

    public override void KilledUnit(Unit who)
    {
        var unit = Global.ObjAccessor.GetUnit(Me, Malchezaar);

        if (unit)
        {
            var creature = unit.AsCreature;

            if (creature)
                creature.AI.KilledUnit(who);
        }
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.INFERNAL_RELAY)
        {
            Me.SetDisplayId(Me.NativeDisplayId);
            Me.SetUnitFlag(UnitFlags.Uninteractible);

            Scheduler.Schedule(TimeSpan.FromSeconds(4), task => DoCast(Me, SpellIds.HELLFIRE));

            Scheduler.Schedule(TimeSpan.FromSeconds(170),
                               task =>
                               {
                                   var pMalchezaar = ObjectAccessor.GetCreature(Me, Malchezaar);

                                   if (pMalchezaar && pMalchezaar.IsAlive)
                                       pMalchezaar.GetAI<BossMalchezaar>().Cleanup(Me, Point);
                               });
        }
    }

    public override void DamageTaken(Unit doneBy, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!doneBy ||
            doneBy.GUID != Malchezaar)
            damage = 0;
    }
}

[Script]
internal class BossMalchezaar : ScriptedAI
{
    private static readonly Vector2[] InfernalPoints =
    {
        new(-10922.8f, -1985.2f), new(-10916.2f, -1996.2f), new(-10932.2f, -2008.1f), new(-10948.8f, -2022.1f), new(-10958.7f, -1997.7f), new(-10971.5f, -1997.5f), new(-10990.8f, -1995.1f), new(-10989.8f, -1976.5f), new(-10971.6f, -1973.0f), new(-10955.5f, -1974.0f), new(-10939.6f, -1969.8f), new(-10958.0f, -1952.2f), new(-10941.7f, -1954.8f), new(-10943.1f, -1988.5f), new(-10948.8f, -2005.1f), new(-10984.0f, -2019.3f), new(-10932.8f, -1979.6f), new(-10935.7f, -1996.0f)
    };

    private readonly ObjectGuid[] _axes = new ObjectGuid[2];
    private readonly long[] _enfeebleHealth = new long[5];
    private readonly ObjectGuid[] _enfeebleTargets = new ObjectGuid[5];

    private readonly List<ObjectGuid> _infernals = new();

    private readonly InstanceScript _instance;
    private readonly List<Vector2> _positions = new();

    private uint _amplifyDamageTimer;
    private uint _axesTargetSwitchTimer;
    private uint _cleaveTimer;
    private uint _enfeebleResetTimer;
    private uint _enfeebleTimer;
    private uint _infernalTimer;

    private uint _phase;
    private uint _shadowNovaTimer;
    private uint _sunderArmorTimer;
    private uint _swPainTimer;

    public BossMalchezaar(Creature creature) : base(creature)
    {
        Initialize();

        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        AxesCleanup();
        ClearWeapons();
        InfernalCleanup();
        _positions.Clear();

        Initialize();

        for (byte i = 0; i < MiscConst.TOTAL_INFERNAL_POINTS; ++i)
            _positions.Add(InfernalPoints[i]);

        _instance.HandleGameObject(_instance.GetGuidData(DataTypes.GO_NETHER_DOOR), true);
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_SLAY);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DEATH);

        AxesCleanup();
        ClearWeapons();
        InfernalCleanup();
        _positions.Clear();

        for (byte i = 0; i < MiscConst.TOTAL_INFERNAL_POINTS; ++i)
            _positions.Add(InfernalPoints[i]);

        _instance.HandleGameObject(_instance.GetGuidData(DataTypes.GO_NETHER_DOOR), true);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);

        _instance.HandleGameObject(_instance.GetGuidData(DataTypes.GO_NETHER_DOOR), false); // Open the door leading further in
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (_enfeebleResetTimer != 0 &&
            _enfeebleResetTimer <= diff) // Let's not forget to reset that
        {
            EnfeebleResetHealth();
            _enfeebleResetTimer = 0;
        }
        else
        {
            _enfeebleResetTimer -= diff;
        }

        if (Me.HasUnitState(UnitState.Stunned)) // While shifting to phase 2 malchezaar stuns himself
            return;

        if (Me.Victim &&
            Me.Target != Me.Victim.GUID)
            Me.SetTarget(Me.Victim.GUID);

        if (_phase == 1)
        {
            if (HealthBelowPct(60))
            {
                Me.InterruptNonMeleeSpells(false);

                _phase = 2;

                //animation
                DoCast(Me, SpellIds.EQUIP_AXES);

                //text
                Talk(TextIds.SAY_AXE_TOSS1);

                //passive thrash aura
                DoCast(Me, SpellIds.THRASH_AURA, new CastSpellExtraArgs(true));

                //models
                SetEquipmentSlots(false, MiscConst.EQUIP_ID_AXE, MiscConst.EQUIP_ID_AXE);

                Me.SetBaseAttackTime(WeaponAttackType.OffAttack, (Me.GetBaseAttackTime(WeaponAttackType.BaseAttack) * 150) / 100);
                Me.SetCanDualWield(true);
            }
        }
        else if (_phase == 2)
        {
            if (HealthBelowPct(30))
            {
                _infernalTimer = 15000;

                _phase = 3;

                ClearWeapons();

                //remove thrash
                Me.RemoveAura(SpellIds.THRASH_AURA);

                Talk(TextIds.SAY_AXE_TOSS2);

                var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                for (byte i = 0; i < 2; ++i)
                {
                    Creature axe = Me.SummonCreature(MiscConst.MALCHEZARS_AXE, Me.Location.X, Me.Location.Y, Me.Location.Z, 0, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));

                    if (axe)
                    {
                        axe.SetUnitFlag(UnitFlags.Uninteractible);
                        axe.Faction = Me.Faction;
                        _axes[i] = axe.GUID;

                        if (target)
                        {
                            axe.AI.AttackStart(target);
                            AddThreat(target, 10000000.0f, axe);
                        }
                    }
                }

                if (_shadowNovaTimer > 35000)
                    _shadowNovaTimer = _enfeebleTimer + 5000;

                return;
            }

            if (_sunderArmorTimer <= diff)
            {
                DoCastVictim(SpellIds.SUNDER_ARMOR);
                _sunderArmorTimer = RandomHelper.URand(10000, 18000);
            }
            else
            {
                _sunderArmorTimer -= diff;
            }

            if (_cleaveTimer <= diff)
            {
                DoCastVictim(SpellIds.CLEAVE);
                _cleaveTimer = RandomHelper.URand(6000, 12000);
            }
            else
            {
                _cleaveTimer -= diff;
            }
        }
        else
        {
            if (_axesTargetSwitchTimer <= diff)
            {
                _axesTargetSwitchTimer = RandomHelper.URand(7500, 20000);

                var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                if (target)
                    for (byte i = 0; i < 2; ++i)
                    {
                        var axe = Global.ObjAccessor.GetUnit(Me, _axes[i]);

                        if (axe)
                        {
                            if (axe.Victim)
                                ResetThreat(axe.Victim, axe);

                            AddThreat(target, 1000000.0f, axe);
                        }
                    }
            }
            else
            {
                _axesTargetSwitchTimer -= diff;
            }

            if (_amplifyDamageTimer <= diff)
            {
                var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                if (target)
                    DoCast(target, SpellIds.AMPLIFY_DAMAGE);

                _amplifyDamageTimer = RandomHelper.URand(20000, 30000);
            }
            else
            {
                _amplifyDamageTimer -= diff;
            }
        }

        //Time for global and double timers
        if (_infernalTimer <= diff)
        {
            SummonInfernal(diff);
            _infernalTimer = _phase == 3 ? 14500 : 44500u; // 15 secs in phase 3, 45 otherwise
        }
        else
        {
            _infernalTimer -= diff;
        }

        if (_shadowNovaTimer <= diff)
        {
            DoCastVictim(SpellIds.SHADOWNOVA);
            _shadowNovaTimer = _phase == 3 ? 31000 : uint.MaxValue;
        }
        else
        {
            _shadowNovaTimer -= diff;
        }

        if (_phase != 2)
        {
            if (_swPainTimer <= diff)
            {
                Unit target;

                if (_phase == 1)
                    target = Me.Victim; // the tank
                else                    // anyone but the tank
                    target = SelectTarget(SelectTargetMethod.Random, 1, 100, true);

                if (target)
                    DoCast(target, SpellIds.SW_PAIN);

                _swPainTimer = 20000;
            }
            else
            {
                _swPainTimer -= diff;
            }
        }

        if (_phase != 3)
        {
            if (_enfeebleTimer <= diff)
            {
                EnfeebleHealthEffect();
                _enfeebleTimer = 30000;
                _shadowNovaTimer = 5000;
                _enfeebleResetTimer = 9000;
            }
            else
            {
                _enfeebleTimer -= diff;
            }
        }

        if (_phase == 2)
            DoMeleeAttacksIfReady();
        else
            DoMeleeAttackIfReady();
    }

    public void Cleanup(Creature infernal, Vector2 point)
    {
        foreach (var guid in _infernals)
            if (guid == infernal.GUID)
            {
                _infernals.Remove(guid);

                break;
            }

        _positions.Add(point);
    }

    private void Initialize()
    {
        _enfeebleTimer = 30000;
        _enfeebleResetTimer = 38000;
        _shadowNovaTimer = 35500;
        _swPainTimer = 20000;
        _amplifyDamageTimer = 5000;
        _cleaveTimer = 8000;
        _infernalTimer = 40000;
        _axesTargetSwitchTimer = RandomHelper.URand(7500, 20000);
        _sunderArmorTimer = RandomHelper.URand(5000, 10000);
        _phase = 1;

        for (byte i = 0; i < 5; ++i)
        {
            _enfeebleTargets[i].Clear();
            _enfeebleHealth[i] = 0;
        }
    }

    private void InfernalCleanup()
    {
        //Infernal Cleanup
        foreach (var guid in _infernals)
        {
            var pInfernal = Global.ObjAccessor.GetUnit(Me, guid);

            if (pInfernal && pInfernal.IsAlive)
            {
                pInfernal.SetVisible(false);
                pInfernal.SetDeathState(DeathState.JustDied);
            }
        }

        _infernals.Clear();
    }

    private void AxesCleanup()
    {
        for (byte i = 0; i < 2; ++i)
        {
            var axe = Global.ObjAccessor.GetUnit(Me, _axes[i]);

            if (axe && axe.IsAlive)
                axe.KillSelf();

            _axes[i].Clear();
        }
    }

    private void ClearWeapons()
    {
        SetEquipmentSlots(false, 0, 0);
        Me.SetCanDualWield(false);
    }

    private void EnfeebleHealthEffect()
    {
        var info = Global.SpellMgr.GetSpellInfo(SpellIds.ENFEEBLE_EFFECT, GetDifficulty());

        if (info == null)
            return;

        var tank = Me.GetThreatManager().CurrentVictim;
        List<Unit> targets = new();

        foreach (var refe in Me.GetThreatManager().SortedThreatList)
        {
            var target = refe.Victim;

            if (target != tank &&
                target.IsAlive &&
                target.IsPlayer)
                targets.Add(target);
        }

        if (targets.Empty())
            return;

        //cut down to size if we have more than 5 targets
        targets.RandomResize(5);

        uint i = 0;

        foreach (var target in targets)
        {
            if (target)
            {
                _enfeebleTargets[i] = target.GUID;
                _enfeebleHealth[i] = target.Health;

                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.OriginalCaster = Me.GUID;
                target.SpellFactory.CastSpell(target, SpellIds.ENFEEBLE, args);
                target.SetHealth(1);
            }

            i++;
        }
    }

    private void EnfeebleResetHealth()
    {
        for (byte i = 0; i < 5; ++i)
        {
            var target = Global.ObjAccessor.GetUnit(Me, _enfeebleTargets[i]);

            if (target && target.IsAlive)
                target.SetHealth(_enfeebleHealth[i]);

            _enfeebleTargets[i].Clear();
            _enfeebleHealth[i] = 0;
        }
    }

    private void SummonInfernal(uint diff)
    {
        var point = Vector2.Zero;
        Position pos = null;

        if ((Me.Location.MapId != 532) ||
            _positions.Empty())
        {
            pos = Me.GetRandomNearPosition(60);
        }
        else
        {
            point = _positions.SelectRandom();
            pos.Relocate(point.X, point.Y, 275.5f, RandomHelper.FRand(0.0f, (MathF.PI * 2)));
        }

        Creature infernal = Me.SummonCreature(MiscConst.NETHERSPITE_INFERNAL, pos, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(3));

        if (infernal)
        {
            infernal.SetDisplayId(MiscConst.INFERNAL_MODEL_INVISIBLE);
            infernal.Faction = Me.Faction;

            if (point != Vector2.Zero)
                infernal.GetAI<NetherspiteInfernal>().Point = point;

            infernal.GetAI<NetherspiteInfernal>().Malchezaar = Me.GUID;

            _infernals.Add(infernal.GUID);
            DoCast(infernal, SpellIds.INFERNAL_RELAY);
        }

        Talk(TextIds.SAY_SUMMON);
    }

    private void DoMeleeAttacksIfReady()
    {
        if (Me.IsWithinMeleeRange(Me.Victim) &&
            !Me.IsNonMeleeSpellCast(false))
        {
            //Check for base attack
            if (Me.IsAttackReady() &&
                Me.Victim)
            {
                Me.AttackerStateUpdate(Me.Victim);
                Me.ResetAttackTimer();
            }

            //Check for offhand attack
            if (Me.IsAttackReady(WeaponAttackType.OffAttack) &&
                Me.Victim)
            {
                Me.AttackerStateUpdate(Me.Victim, WeaponAttackType.OffAttack);
                Me.ResetAttackTimer(WeaponAttackType.OffAttack);
            }
        }
    }
}
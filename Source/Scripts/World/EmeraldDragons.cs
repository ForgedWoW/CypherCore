// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.World.EmeraldDragons;

internal struct CreatureIds
{
    public const uint DRAGON_YSONDRE = 14887;
    public const uint DRAGON_LETHON = 14888;
    public const uint DRAGON_EMERISS = 14889;
    public const uint DRAGON_TAERAR = 14890;
    public const uint DREAM_FOG = 15224;

    //Ysondre
    public const uint DEMENTED_DRUID = 15260;

    //Lethon
    public const uint SPIRIT_SHADE = 15261;
}

internal struct SpellIds
{
    public const uint TAIL_SWEEP = 15847;       // Tail Sweep - Slap Everything Behind Dragon (2 Seconds Interval)
    public const uint SUMMON_PLAYER = 24776;    // Teleport Highest Threat Player In Front Of Dragon If Wandering Off
    public const uint DREAM_FOG = 24777;        // Auraspell For Dream Fog Npc (15224)
    public const uint SLEEP = 24778;           // Sleep Triggerspell (Used For Dream Fog)
    public const uint SEEPING_FOG_LEFT = 24813;  // Dream Fog - Summon Left
    public const uint SEEPING_FOG_RIGHT = 24814; // Dream Fog - Summon Right
    public const uint NOXIOUS_BREATH = 24818;
    public const uint MARK_OF_NATURE = 25040;     // Mark Of Nature Trigger (Applied On Target Death - 15 Minutes Of Being Suspectible To Aura Of Nature)
    public const uint MARK_OF_NATURE_AURA = 25041; // Mark Of Nature (Passive Marker-Test; Ticks Every 10 Seconds From Boss; Triggers Spellid 25042 (Scripted)
    public const uint AURA_OF_NATURE = 25043;     // Stun For 2 Minutes (Used When public const uint MarkOfNature Exists On The Target)

    //Ysondre
    public const uint LIGHTNING_WAVE = 24819;
    public const uint SUMMON_DRUID_SPIRITS = 24795;

    //Lethon
    public const uint DRAW_SPIRIT = 24811;
    public const uint SHADOW_BOLT_WHIRL = 24834;
    public const uint DARK_OFFERING = 24804;

    //Emeriss
    public const uint PUTRID_MUSHROOM = 24904;
    public const uint CORRUPTION_OF_EARTH = 24910;
    public const uint VOLATILE_INFECTION = 24928;

    //Taerar
    public const uint BELLOWING_ROAR = 22686;
    public const uint SHADE = 24313;
    public const uint ARCANE_BLAST = 24857;

    public static uint[] TaerarShadeSpells = new uint[]
    {
        24841, 24842, 24843
    };
}

internal struct TextIds
{
    //Ysondre
    public const uint SAY_YSONDRE_AGGRO = 0;
    public const uint SAY_YSONDRE_SUMMON_DRUIDS = 1;

    //Lethon
    public const uint SAY_LETHON_AGGRO = 0;
    public const uint SAY_LETHON_DRAW_SPIRIT = 1;

    //Emeriss
    public const uint SAY_EMERISS_AGGRO = 0;
    public const uint SAY_EMERISS_CAST_CORRUPTION = 1;

    //Taerar
    public const uint SAY_TAERAR_AGGRO = 0;
    public const uint SAY_TAERAR_SUMMON_SHADES = 1;
}

internal class EmeraldDragonAI : WorldBossAI
{
    public EmeraldDragonAI(Creature creature) : base(creature) { }

    public override void Reset()
    {
        base.Reset();
        Me.RemoveUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
        Me.ReactState = ReactStates.Aggressive;
        DoCast(Me, SpellIds.MARK_OF_NATURE_AURA, new CastSpellExtraArgs(true));

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               // Tail Sweep is cast every two seconds, no matter what goes on in front of the dragon
                               DoCast(Me, SpellIds.TAIL_SWEEP);
                               task.Repeat(TimeSpan.FromSeconds(2));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(7.5),
                           TimeSpan.FromSeconds(15),
                           task =>
                           {
                               // Noxious Breath is cast on random intervals, no less than 7.5 seconds between
                               DoCast(Me, SpellIds.NOXIOUS_BREATH);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12.5),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               // Seeping Fog appears only as "pairs", and only ONE pair at any given Time!
                               // Despawntime is 2 minutes, so reschedule it for new cast after 2 minutes + a minor "random Time" (30 seconds at max)
                               DoCast(Me, SpellIds.SEEPING_FOG_LEFT, new CastSpellExtraArgs(true));
                               DoCast(Me, SpellIds.SEEPING_FOG_RIGHT, new CastSpellExtraArgs(true));
                               task.Repeat(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2.5));
                           });
    }

    // Target killed during encounter, mark them as suspectible for Aura Of Nature
    public override void KilledUnit(Unit who)
    {
        if (who.IsTypeId(TypeId.Player))
            who.SpellFactory.CastSpell(who, SpellIds.MARK_OF_NATURE, true);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (Me.HasUnitState(UnitState.Casting))
            return;

        Scheduler.Update(diff);

        var target = SelectTarget(SelectTargetMethod.MaxThreat, 0, -50.0f, true);

        if (target)
            DoCast(target, SpellIds.SUMMON_PLAYER);

        DoMeleeAttackIfReady();
    }
}

[Script]
internal class NPCDreamFog : ScriptedAI
{
    private uint _roamTimer;

    public NPCDreamFog(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (_roamTimer == 0)
        {
            // Chase Target, but don't attack - otherwise just roam around
            var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

            if (target)
            {
                _roamTimer = RandomHelper.URand(15000, 30000);
                Me.MotionMaster.Clear();
                Me.MotionMaster.MoveChase(target, 0.2f);
            }
            else
            {
                _roamTimer = 2500;
                Me.MotionMaster.Clear();
                Me.MotionMaster.MoveRandom(25.0f);
            }

            // Seeping fog movement is slow enough for a player to be able to walk backwards and still outpace it
            Me.SetWalk(true);
            Me.SetSpeedRate(UnitMoveType.Walk, 0.75f);
        }
        else
        {
            _roamTimer -= diff;
        }
    }

    private void Initialize()
    {
        _roamTimer = 0;
    }
}

[Script]
internal class BossYsondre : EmeraldDragonAI
{
    private byte _stage;

    public BossYsondre(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        base.Reset();

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.LIGHTNING_WAVE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
                           });
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_YSONDRE_AGGRO);
        base.JustEngagedWith(who);
    }

    // Summon druid spirits on 75%, 50% and 25% health
    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!HealthAbovePct(100 - 25 * _stage))
        {
            Talk(TextIds.SAY_YSONDRE_SUMMON_DRUIDS);

            for (byte i = 0; i < 10; ++i)
                DoCast(Me, SpellIds.SUMMON_DRUID_SPIRITS, new CastSpellExtraArgs(true));

            ++_stage;
        }
    }

    private void Initialize()
    {
        _stage = 1;
    }
}

[Script]
internal class BossLethon : EmeraldDragonAI
{
    private byte _stage;

    public BossLethon(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        base.Reset();

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               Me.SpellFactory.CastSpell((Unit)null, SpellIds.SHADOW_BOLT_WHIRL, false);
                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
                           });
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_LETHON_AGGRO);
        base.JustEngagedWith(who);
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!HealthAbovePct(100 - 25 * _stage))
        {
            Talk(TextIds.SAY_LETHON_DRAW_SPIRIT);
            DoCast(Me, SpellIds.DRAW_SPIRIT);
            ++_stage;
        }
    }

    public override void SpellHitTarget(WorldObject target, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.DRAW_SPIRIT &&
            target.IsPlayer)
        {
            Position targetPos = target.Location;
            Me.SummonCreature(CreatureIds.SPIRIT_SHADE, targetPos, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(50));
        }
    }

    private void Initialize()
    {
        _stage = 1;
    }
}

[Script]
internal class NPCSpiritShade : PassiveAI
{
    private ObjectGuid _summonerGuid;

    public NPCSpiritShade(Creature creature) : base(creature) { }

    public override void IsSummonedBy(WorldObject summoner)
    {
        var unitSummoner = summoner.AsUnit;

        if (unitSummoner == null)
            return;

        _summonerGuid = summoner.GUID;
        Me.MotionMaster.MoveFollow(unitSummoner, 0.0f, 0.0f);
    }

    public override void MovementInform(MovementGeneratorType moveType, uint data)
    {
        if (moveType == MovementGeneratorType.Follow &&
            data == _summonerGuid.Counter)
        {
            Me.SpellFactory.CastSpell((Unit)null, SpellIds.DARK_OFFERING, false);
            Me.DespawnOrUnsummon(TimeSpan.FromSeconds(1));
        }
    }
}

[Script]
internal class BossEmeriss : EmeraldDragonAI
{
    private byte _stage;

    public BossEmeriss(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        base.Reset();

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.VOLATILE_INFECTION);
                               task.Repeat(TimeSpan.FromSeconds(120));
                           });
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsTypeId(TypeId.Player))
            DoCast(who, SpellIds.PUTRID_MUSHROOM, new CastSpellExtraArgs(true));

        base.KilledUnit(who);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_EMERISS_AGGRO);
        base.JustEngagedWith(who);
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!HealthAbovePct(100 - 25 * _stage))
        {
            Talk(TextIds.SAY_EMERISS_CAST_CORRUPTION);
            DoCast(Me, SpellIds.CORRUPTION_OF_EARTH, new CastSpellExtraArgs(true));
            ++_stage;
        }
    }

    private void Initialize()
    {
        _stage = 1;
    }
}

[Script]
internal class BossTaerar : EmeraldDragonAI
{
    private bool _banished;      // used for shades activation testing
    private uint _banishedTimer; // counter for banishment timeout
    private byte _shades;        // keep track of how many shades are dead
    private byte _stage;         // check which "shade phase" we're at (75-50-25 percentage counters)

    public BossTaerar(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Me.RemoveAura(SpellIds.SHADE);

        Initialize();
        base.Reset();

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCast(SpellIds.ARCANE_BLAST);
                               task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(12));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               DoCast(SpellIds.BELLOWING_ROAR);
                               task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
                           });
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_TAERAR_AGGRO);
        base.JustEngagedWith(who);
    }

    public override void SummonedCreatureDies(Creature summon, Unit killer)
    {
        --_shades;
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        // At 75, 50 or 25 percent health, we need to activate the shades and go "banished"
        // Note: _stage holds the amount of times they have been summoned
        if (!_banished &&
            !HealthAbovePct(100 - 25 * _stage))
        {
            _banished = true;
            _banishedTimer = 60000;

            Me.InterruptNonMeleeSpells(false);
            DoStopAttack();

            Talk(TextIds.SAY_TAERAR_SUMMON_SHADES);

            foreach (var spell in SpellIds.TaerarShadeSpells)
                DoCastVictim(spell, new CastSpellExtraArgs(true));

            _shades += (byte)SpellIds.TaerarShadeSpells.Length;

            DoCast(SpellIds.SHADE);
            Me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
            Me.ReactState = ReactStates.Passive;

            ++_stage;
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!Me.IsInCombat)
            return;

        if (_banished)
        {
            // If all three shades are dead, Or it has taken too long, end the current event and get Taerar back into business
            if (_banishedTimer <= diff ||
                _shades == 0)
            {
                _banished = false;

                Me.RemoveUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
                Me.RemoveAura(SpellIds.SHADE);
                Me.ReactState = ReactStates.Aggressive;
            }
            // _banishtimer has not expired, and we still have active shades:
            else
            {
                _banishedTimer -= diff;
            }

            // Update the Scheduler before we return (handled under emerald_dragonAI.UpdateAI(diff); if we're not inside this check)
            Scheduler.Update(diff);

            return;
        }

        base.UpdateAI(diff);
    }

    private void Initialize()
    {
        _stage = 1;
        _shades = 0;
        _banished = false;
        _banishedTimer = 0;
    }
}

[Script] // 24778 - Sleep
internal class SpellDreamFogSleepSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(obj =>
        {
            var unit = obj.AsUnit;

            if (unit)
                return unit.HasAura(SpellIds.SLEEP);

            return true;
        });
    }
}

[Script] // 25042 - Triggerspell - Mark of Nature
internal class SpellMarkOfNatureSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(obj =>
        {
            // return those not tagged or already under the influence of Aura of Nature
            var unit = obj.AsUnit;

            if (unit)
                return !(unit.HasAura(SpellIds.MARK_OF_NATURE) && !unit.HasAura(SpellIds.AURA_OF_NATURE));

            return true;
        });
    }

    private void HandleEffect(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        HitUnit.SpellFactory.CastSpell(HitUnit, SpellIds.AURA_OF_NATURE, true);
    }
}
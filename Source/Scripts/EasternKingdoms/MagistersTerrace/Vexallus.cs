// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.MagistersTerrace.Vexallus;

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_ENERGY = 1;
    public const uint SAY_OVERLOAD = 2;
    public const uint SAY_KILL = 3;
    public const uint EMOTE_DISCHARGE_ENERGY = 4;
}

internal struct SpellIds
{
    public const uint CHAIN_LIGHTNING = 44318;
    public const uint OVERLOAD = 44353;
    public const uint ARCANE_SHOCK = 44319;

    public const uint SUMMON_PURE_ENERGY = 44322;    // mod scale -10
    public const uint H_SUMMON_PURE_ENERGY1 = 46154; // mod scale -5
    public const uint H_SUMMON_PURE_ENERGY2 = 46159; // mod scale -5

    // NpcPureEnergy
    public const uint ENERGY_BOLT = 46156;
    public const uint ENERGY_FEEDBACK = 44335;
    public const uint PURE_ENERGY_PASSIVE = 44326;
}

internal struct MiscConst
{
    public const uint INTERVAL_MODIFIER = 15;
    public const uint INTERVAL_SWITCH = 6;
}

[Script]
internal class BossVexallus : BossAI
{
    private bool _enraged;
    private uint _intervalHealthAmount;

    public BossVexallus(Creature creature) : base(creature, DataTypes.VEXALLUS)
    {
        _intervalHealthAmount = 1;
        _enraged = false;
    }

    public override void Reset()
    {
        _Reset();
        _intervalHealthAmount = 1;
        _enraged = false;
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_KILL);
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.CHAIN_LIGHTNING);

                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 20.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.ARCANE_SHOCK);

                               task.Repeat(TimeSpan.FromSeconds(8));
                           });
    }

    public override void JustSummoned(Creature summoned)
    {
        var temp = SelectTarget(SelectTargetMethod.Random, 0);

        if (temp)
            summoned.MotionMaster.MoveFollow(temp, 0, 0);

        Summons.Summon(summoned);
    }

    public override void DamageTaken(Unit who, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (_enraged)
            return;

        // 85%, 70%, 55%, 40%, 25%
        if (!HealthAbovePct((int)(100 - MiscConst.INTERVAL_MODIFIER * _intervalHealthAmount)))
        {
            // increase amount, unless we're at 10%, then we switch and return
            if (_intervalHealthAmount == MiscConst.INTERVAL_SWITCH)
            {
                _enraged = true;
                Scheduler.CancelAll();

                Scheduler.Schedule(TimeSpan.FromSeconds(1.2),
                                   task =>
                                   {
                                       DoCastVictim(SpellIds.OVERLOAD);
                                       task.Repeat(TimeSpan.FromSeconds(2));
                                   });

                return;
            }
            else
                ++_intervalHealthAmount;

            Talk(TextIds.SAY_ENERGY);
            Talk(TextIds.EMOTE_DISCHARGE_ENERGY);

            if (IsHeroic())
            {
                DoCast(Me, SpellIds.H_SUMMON_PURE_ENERGY1);
                DoCast(Me, SpellIds.H_SUMMON_PURE_ENERGY2);
            }
            else
                DoCast(Me, SpellIds.SUMMON_PURE_ENERGY);
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}

[Script]
internal class NPCPureEnergy : ScriptedAI
{
    public NPCPureEnergy(Creature creature) : base(creature)
    {
        Me.SetDisplayFromModel(1);
    }

    public override void JustDied(Unit killer)
    {
        if (killer)
            killer.SpellFactory.CastSpell(killer, SpellIds.ENERGY_FEEDBACK, true);

        Me.RemoveAura(SpellIds.PURE_ENERGY_PASSIVE);
    }
}
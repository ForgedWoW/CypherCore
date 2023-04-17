// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.GeneralAngerforge;

internal struct SpellIds
{
    public const uint MIGHTYBLOW = 14099;
    public const uint HAMSTRING = 9080;
    public const uint CLEAVE = 20691;
}

internal enum Phases
{
    One = 1,
    Two = 2
}

[Script]
internal class BossGeneralAngerforge : ScriptedAI
{
    private Phases _phase;

    public BossGeneralAngerforge(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        _phase = Phases.One;

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.MIGHTYBLOW);
                               task.Repeat(TimeSpan.FromSeconds(18));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.HAMSTRING);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(9));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (Me.HealthBelowPctDamaged(20, damage) &&
            _phase == Phases.One)
        {
            _phase = Phases.Two;

            Scheduler.Schedule(TimeSpan.FromSeconds(0),
                               task =>
                               {
                                   for (byte i = 0; i < 2; ++i)
                                       SummonMedic(Me.Victim);
                               });

            Scheduler.Schedule(TimeSpan.FromSeconds(0),
                               task =>
                               {
                                   for (byte i = 0; i < 3; ++i)
                                       SummonAdd(Me.Victim);

                                   task.Repeat(TimeSpan.FromSeconds(25));
                               });
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void SummonAdd(Unit victim)
    {
        var summonedAdd = DoSpawnCreature(8901, RandomHelper.IRand(-14, 14), RandomHelper.IRand(-14, 14), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(120));

        if (summonedAdd)
            summonedAdd.AI.AttackStart(victim);
    }

    private void SummonMedic(Unit victim)
    {
        var summonedMedic = DoSpawnCreature(8894, RandomHelper.IRand(-9, 9), RandomHelper.IRand(-9, 9), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(120));

        if (summonedMedic)
            summonedMedic.AI.AttackStart(victim);
    }
}
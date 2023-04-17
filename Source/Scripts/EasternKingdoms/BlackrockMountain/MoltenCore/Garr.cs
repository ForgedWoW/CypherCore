// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Garr;

internal struct SpellIds
{
    // Garr
    public const uint ANTIMAGIC_PULSE = 19492;
    public const uint MAGMA_SHACKLES = 19496;
    public const uint ENRAGE = 19516;
    public const uint SEPARATION_ANXIETY = 23492;

    // Adds
    public const uint ERUPTION = 19497;
    public const uint IMMOLATE = 15732;
}

[Script]
internal class BossGarr : BossAI
{
    public BossGarr(Creature creature) : base(creature, DataTypes.GARR) { }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(25),
                           task =>
                           {
                               DoCast(Me, SpellIds.ANTIMAGIC_PULSE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCast(Me, SpellIds.MAGMA_SHACKLES);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}

[Script]
internal class NPCFiresworn : ScriptedAI
{
    public NPCFiresworn(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        ScheduleTasks();
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        var health10Pct = Me.CountPctFromMaxHealth(10);
        var health = Me.Health;

        if (health - damage < health10Pct)
        {
            damage = 0;
            DoCastVictim(SpellIds.ERUPTION);
            Me.DespawnOrUnsummon();
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void ScheduleTasks()
    {
        // Timers for this are probably wrong
        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                                   DoCast(target, SpellIds.IMMOLATE);

                               task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
                           });

        // Separation Anxiety - Periodically check if Garr is nearby
        // ...and enrage if he is not.
        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          if (!Me.FindNearestCreature(McCreatureIds.GARR, 20.0f))
                                                                              DoCastSelf(SpellIds.SEPARATION_ANXIETY);
                                                                          else if (Me.HasAura(SpellIds.SEPARATION_ANXIETY))
                                                                              Me.RemoveAura(SpellIds.SEPARATION_ANXIETY);

                                                                          task.Repeat();
                                                                      }));
    }
}
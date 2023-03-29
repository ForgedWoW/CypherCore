// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.AlteracValley.Vanndar;

internal struct SpellIds
{
    public const uint Avatar = 19135;
    public const uint Thunderclap = 15588;
    public const uint Stormbolt = 20685; // not sure
}

internal struct TextIds
{
    public const uint YellAggro = 0;

    public const uint YellEvade = 1;

    //public const uint YellRespawn1                                 = -1810010; // Missing in database
    //public const uint YellRespawn2                                 = -1810011; // Missing in database
    public const uint YellRandom = 2;
    public const uint YellSpell = 3;
}

[Script]
internal class boss_vanndar : ScriptedAI
{
    public boss_vanndar(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               DoCastVictim(SpellIds.Avatar);
                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               DoCastVictim(SpellIds.Thunderclap);
                               task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.Stormbolt);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           TimeSpan.FromSeconds(30),
                           task =>
                           {
                               Talk(TextIds.YellRandom);
                               task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          if (Me.GetDistance2d(Me.HomePosition.X, Me.HomePosition.Y) > 50)
                                                                          {
                                                                              base.EnterEvadeMode();
                                                                              Talk(TextIds.YellEvade);
                                                                          }

                                                                          task.Repeat();
                                                                      }));

        Talk(TextIds.YellAggro);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
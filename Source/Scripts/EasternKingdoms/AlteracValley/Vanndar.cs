// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.AlteracValley.Vanndar;

internal struct SpellIds
{
    public const uint AVATAR = 19135;
    public const uint THUNDERCLAP = 15588;
    public const uint STORMBOLT = 20685; // not sure
}

internal struct TextIds
{
    public const uint YELL_AGGRO = 0;

    public const uint YELL_EVADE = 1;

    //public const uint YellRespawn1                                 = -1810010; // Missing in database
    //public const uint YellRespawn2                                 = -1810011; // Missing in database
    public const uint YELL_RANDOM = 2;
    public const uint YELL_SPELL = 3;
}

[Script]
internal class BossVanndar : ScriptedAI
{
    public BossVanndar(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               DoCastVictim(SpellIds.AVATAR);
                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               DoCastVictim(SpellIds.THUNDERCLAP);
                               task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.STORMBOLT);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           TimeSpan.FromSeconds(30),
                           task =>
                           {
                               Talk(TextIds.YELL_RANDOM);
                               task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          if (Me.GetDistance2d(Me.HomePosition.X, Me.HomePosition.Y) > 50)
                                                                          {
                                                                              base.EnterEvadeMode();
                                                                              Talk(TextIds.YELL_EVADE);
                                                                          }

                                                                          task.Repeat();
                                                                      }));

        Talk(TextIds.YELL_AGGRO);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
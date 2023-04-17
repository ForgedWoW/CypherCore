// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.AlteracValley.Drekthar;

internal struct SpellIds
{
    public const uint WHIRLWIND = 15589;
    public const uint WHIRLWIND2 = 13736;
    public const uint KNOCKDOWN = 19128;
    public const uint FRENZY = 8269;
    public const uint SWEEPING_STRIKES = 18765; // not sure
    public const uint CLEAVE = 20677;          // not sure
    public const uint WINDFURY = 35886;        // not sure
    public const uint STORMPIKE = 51876;       // not sure
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_EVADE = 1;
    public const uint SAY_RESPAWN = 2;
    public const uint SAY_RANDOM = 3;
}

[Script]
internal class BossDrekthar : ScriptedAI
{
    public BossDrekthar(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.WHIRLWIND);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(18));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.WHIRLWIND2);
                               task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.KNOCKDOWN);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.FRENZY);
                               task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           TimeSpan.FromSeconds(30),
                           task =>
                           {
                               Talk(TextIds.SAY_RANDOM);
                               task.Repeat();
                           });
    }

    public override void JustAppeared()
    {
        Reset();
        Talk(TextIds.SAY_RESPAWN);
    }

    public override bool CheckInRoom()
    {
        if (Me.GetDistance2d(Me.HomePosition.X, Me.HomePosition.Y) > 50)
        {
            EnterEvadeMode();
            Talk(TextIds.SAY_EVADE);

            return false;
        }

        return true;
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim() ||
            !CheckInRoom())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
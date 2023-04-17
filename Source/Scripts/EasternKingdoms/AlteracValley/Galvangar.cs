// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.AlteracValley.Galvangar;

internal struct SpellIds
{
    public const uint CLEAVE = 15284;
    public const uint FRIGHTENING_SHOUT = 19134;
    public const uint WHIRLWIND1 = 15589;
    public const uint WHIRLWIND2 = 13736;
    public const uint MORTAL_STRIKE = 16856;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_EVADE = 1;
    public const uint SAY_BUFF = 2;
}

internal struct ActionIds
{
    public const int BUFF_YELL = -30001; // shared from Battleground
}

[Script]
internal class BossGalvangar : ScriptedAI
{
    public BossGalvangar(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           TimeSpan.FromSeconds(9),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           TimeSpan.FromSeconds(19),
                           task =>
                           {
                               DoCastVictim(SpellIds.FRIGHTENING_SHOUT);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           TimeSpan.FromSeconds(13),
                           task =>
                           {
                               DoCastVictim(SpellIds.WHIRLWIND1);
                               task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.WHIRLWIND2);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.MORTAL_STRIKE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                           });
    }

    public override void DoAction(int actionId)
    {
        if (actionId == ActionIds.BUFF_YELL)
            Talk(TextIds.SAY_BUFF);
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
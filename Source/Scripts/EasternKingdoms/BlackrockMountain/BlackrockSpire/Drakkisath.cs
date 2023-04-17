// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Drakkisath;

internal struct SpellIds
{
    public const uint FIRENOVA = 23462;
    public const uint CLEAVE = 20691;
    public const uint CONFLIGURATION = 16805;
    public const uint THUNDERCLAP = 15548; //Not sure if right Id. 23931 would be a harder possibility.
}

[Script]
internal class BossDrakkisath : BossAI
{
    public BossDrakkisath(Creature creature) : base(creature, DataTypes.GENERAL_DRAKKISATH) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.FIRENOVA);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(8));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.CONFLIGURATION);
                               task.Repeat(TimeSpan.FromSeconds(18));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(17),
                           task =>
                           {
                               DoCastVictim(SpellIds.THUNDERCLAP);
                               task.Repeat(TimeSpan.FromSeconds(20));
                           });
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
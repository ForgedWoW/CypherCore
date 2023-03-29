// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Firemaw;

internal struct SpellIds
{
    public const uint Shadowflame = 22539;
    public const uint Wingbuffet = 23339;
    public const uint Flamebuffet = 23341;
}

[Script]
internal class boss_firemaw : BossAI
{
    public boss_firemaw(Creature creature) : base(creature, DataTypes.Firemaw) { }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.Shadowflame);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               DoCastVictim(SpellIds.Wingbuffet);

                               if (GetThreat(Me.Victim) != 0)
                                   ModifyThreatByPercent(Me.Victim, -75);

                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               DoCastVictim(SpellIds.Flamebuffet);
                               task.Repeat(TimeSpan.FromSeconds(5));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
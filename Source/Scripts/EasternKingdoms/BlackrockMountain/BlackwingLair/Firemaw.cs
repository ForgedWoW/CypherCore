// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Firemaw;

internal struct SpellIds
{
    public const uint SHADOWFLAME = 22539;
    public const uint WINGBUFFET = 23339;
    public const uint FLAMEBUFFET = 23341;
}

[Script]
internal class BossFiremaw : BossAI
{
    public BossFiremaw(Creature creature) : base(creature, DataTypes.FIREMAW) { }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHADOWFLAME);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               DoCastVictim(SpellIds.WINGBUFFET);

                               if (GetThreat(Me.Victim) != 0)
                                   ModifyThreatByPercent(Me.Victim, -75);

                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               DoCastVictim(SpellIds.FLAMEBUFFET);
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
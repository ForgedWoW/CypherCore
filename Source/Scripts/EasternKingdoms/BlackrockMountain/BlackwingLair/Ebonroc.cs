// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Ebonroc;

internal struct SpellIds
{
    public const uint SHADOWFLAME = 22539;
    public const uint WINGBUFFET = 23339;
    public const uint SHADOWOFEBONROC = 23340;
}

[Script]
internal class BossEbonroc : BossAI
{
    public BossEbonroc(Creature creature) : base(creature, DataTypes.EBONROC) { }

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
                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHADOWOFEBONROC);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
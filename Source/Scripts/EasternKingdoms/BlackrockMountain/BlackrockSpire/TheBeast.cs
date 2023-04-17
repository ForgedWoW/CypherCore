// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Thebeast;

internal struct SpellIds
{
    public const uint FLAMEBREAK = 16785;
    public const uint IMMOLATE = 20294;
    public const uint TERRIFYINGROAR = 14100;
}

[Script]
internal class BossThebeast : BossAI
{
    public BossThebeast(Creature creature) : base(creature, DataTypes.THE_BEAST) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.FLAMEBREAK);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                               if (target)
                                   DoCast(target, SpellIds.IMMOLATE);

                               task.Repeat(TimeSpan.FromSeconds(8));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(23),
                           task =>
                           {
                               DoCastVictim(SpellIds.TERRIFYINGROAR);
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
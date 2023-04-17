// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.HighInterrogatorGerstahn;

internal struct SpellIds
{
    public const uint SHADOWWORDPAIN = 10894;
    public const uint MANABURN = 10876;
    public const uint PSYCHICSCREAM = 8122;
    public const uint SHADOWSHIELD = 22417;
}

[Script]
internal class BossHighInterrogatorGerstahn : ScriptedAI
{
    public BossHighInterrogatorGerstahn(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.SHADOWWORDPAIN);

                               task.Repeat(TimeSpan.FromSeconds(7));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(14),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.MANABURN);

                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(32),
                           task =>
                           {
                               DoCastVictim(SpellIds.PSYCHICSCREAM);
                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCast(Me, SpellIds.SHADOWSHIELD);
                               task.Repeat(TimeSpan.FromSeconds(25));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Gehennas;

internal struct SpellIds
{
    public const uint GEHENNAS_CURSE = 19716;
    public const uint RAIN_OF_FIRE = 19717;
    public const uint SHADOW_BOLT = 19728;
}

[Script]
internal class BossGehennas : BossAI
{
    public BossGehennas(Creature creature) : base(creature, DataTypes.GEHENNAS) { }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.GEHENNAS_CURSE);
                               task.Repeat(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                                   DoCast(target, SpellIds.RAIN_OF_FIRE);

                               task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 1);

                               if (target)
                                   DoCast(target, SpellIds.SHADOW_BOLT);

                               task.Repeat(TimeSpan.FromSeconds(7));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
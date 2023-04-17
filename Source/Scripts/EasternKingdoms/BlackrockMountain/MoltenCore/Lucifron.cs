// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Lucifron;

internal struct SpellIds
{
    public const uint IMPENDING_DOOM = 19702;
    public const uint LUCIFRON_CURSE = 19703;
    public const uint SHADOW_SHOCK = 20603;
}

[Script]
internal class BossLucifron : BossAI
{
    public BossLucifron(Creature creature) : base(creature, DataTypes.LUCIFRON) { }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.IMPENDING_DOOM);
                               task.Repeat(TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.LUCIFRON_CURSE);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHADOW_SHOCK);
                               task.Repeat(TimeSpan.FromSeconds(6));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}
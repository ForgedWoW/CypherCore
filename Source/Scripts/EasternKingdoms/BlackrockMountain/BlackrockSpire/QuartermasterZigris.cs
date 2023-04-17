// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.QuartermasterZigris;

internal struct SpellIds
{
    public const uint SHOOT = 16496;
    public const uint STUNBOMB = 16497;
    public const uint HEALING_POTION = 15504;
    public const uint HOOKEDNET = 15609;
}

[Script]
internal class QuartermasterZigris : BossAI
{
    public QuartermasterZigris(Creature creature) : base(creature, DataTypes.QUARTERMASTER_ZIGRIS) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHOOT);
                               task.Repeat(TimeSpan.FromMilliseconds(500));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCastVictim(SpellIds.STUNBOMB);
                               task.Repeat(TimeSpan.FromSeconds(14));
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
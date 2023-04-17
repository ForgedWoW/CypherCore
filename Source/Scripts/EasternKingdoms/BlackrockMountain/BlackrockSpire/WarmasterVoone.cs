// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.WarmasterVoone;

internal struct SpellIds
{
    public const uint SNAPKICK = 15618;
    public const uint CLEAVE = 15284;
    public const uint UPPERCUT = 10966;
    public const uint MORTALSTRIKE = 16856;
    public const uint PUMMEL = 15615;
    public const uint THROWAXE = 16075;
}

[Script]
internal class BossWarmasterVoone : BossAI
{
    public BossWarmasterVoone(Creature creature) : base(creature, DataTypes.WARMASTER_VOONE) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.SNAPKICK);
                               task.Repeat(TimeSpan.FromSeconds(6));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(14),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(12));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.UPPERCUT);
                               task.Repeat(TimeSpan.FromSeconds(14));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.MORTALSTRIKE);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(32),
                           task =>
                           {
                               DoCastVictim(SpellIds.PUMMEL);
                               task.Repeat(TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               DoCastVictim(SpellIds.THROWAXE);
                               task.Repeat(TimeSpan.FromSeconds(8));
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
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.MoiraBronzebeard;

internal struct SpellIds
{
    public const uint HEAL = 10917;
    public const uint RENEW = 10929;
    public const uint SHIELD = 10901;
    public const uint MINDBLAST = 10947;
    public const uint SHADOWWORDPAIN = 10894;
    public const uint SMITE = 10934;
}

[Script]
internal class BossMoiraBronzebeard : ScriptedAI
{
    public BossMoiraBronzebeard(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        //Scheduler.Schedule(EventHeal, TimeSpan.FromSeconds(12s)); // not used atm // These times are probably wrong
        Scheduler.Schedule(TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCastVictim(SpellIds.MINDBLAST);
                               task.Repeat(TimeSpan.FromSeconds(14));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHADOWWORDPAIN);
                               task.Repeat(TimeSpan.FromSeconds(18));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.SMITE);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff);
    }
}
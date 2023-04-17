// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.AmbassadorFlamelash;

internal struct SpellIds
{
    public const uint FIREBLAST = 15573;
}

[Script]
internal class BossAmbassadorFlamelash : ScriptedAI
{
    public BossAmbassadorFlamelash(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCastVictim(SpellIds.FIREBLAST);
                               task.Repeat(TimeSpan.FromSeconds(7));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(24),
                           task =>
                           {
                               for (uint i = 0; i < 4; ++i)
                                   SummonSpirit(Me.Victim);

                               task.Repeat(TimeSpan.FromSeconds(30));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void SummonSpirit(Unit victim)
    {
        var spirit = DoSpawnCreature(9178, RandomHelper.FRand(-9, 9), RandomHelper.FRand(-9, 9), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(60));

        if (spirit)
            spirit.AI.AttackStart(victim);
    }
}
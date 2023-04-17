// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.ShadowHunterVoshgajin;

internal struct SpellIds
{
    public const uint CURSEOFBLOOD = 24673;
    public const uint HEX = 16708;
    public const uint CLEAVE = 20691;
}

[Script]
internal class BossShadowHunterVoshgajin : BossAI
{
    public BossShadowHunterVoshgajin(Creature creature) : base(creature, DataTypes.SHADOW_HUNTER_VOSHGAJIN) { }

    public override void Reset()
    {
        _Reset();
        //DoCast(me, SpellIcearmor, true);
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCastVictim(SpellIds.CURSEOFBLOOD);
                               task.Repeat(TimeSpan.FromSeconds(45));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                               if (target)
                                   DoCast(target, SpellIds.HEX);

                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(14),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(7));
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
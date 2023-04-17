// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.HighlordOmokk;

internal struct SpellIds
{
    public const uint FRENZY = 8269;
    public const uint KNOCK_AWAY = 10101;
}

[Script]
internal class BossHighlordOmokk : BossAI
{
    public BossHighlordOmokk(Creature creature) : base(creature, DataTypes.HIGHLORD_OMOKK) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.FRENZY);
                               task.Repeat(TimeSpan.FromMinutes(1));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(18),
                           task =>
                           {
                               DoCastVictim(SpellIds.KNOCK_AWAY);
                               task.Repeat(TimeSpan.FromSeconds(12));
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
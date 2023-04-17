// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.UrokDoomhowl;

internal struct SpellIds
{
    public const uint REND = 16509;
    public const uint STRIKE = 15580;
    public const uint INTIMIDATING_ROAR = 16508;
}

internal struct TextIds
{
    public const uint SAY_SUMMON = 0;
    public const uint SAY_AGGRO = 1;
}

[Script]
internal class BossUrokDoomhowl : BossAI
{
    public BossUrokDoomhowl(Creature creature) : base(creature, DataTypes.UROK_DOOMHOWL) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(17),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.REND);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.STRIKE);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
                           });

        Talk(TextIds.SAY_AGGRO);
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
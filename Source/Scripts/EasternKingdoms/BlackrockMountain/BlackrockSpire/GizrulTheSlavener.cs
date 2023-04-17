// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.GizrulTheSlavener;

internal struct SpellIds
{
    public const uint FATAL_BITE = 16495;
    public const uint INFECTED_BITE = 16128;
    public const uint FRENZY = 8269;
}

internal struct PathIds
{
    public const uint GIZRUL = 402450;
}

[Script]
internal class BossGizrulTheSlavener : BossAI
{
    public BossGizrulTheSlavener(Creature creature) : base(creature, DataTypes.GIZRUL_THE_SLAVENER) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void IsSummonedBy(WorldObject summoner)
    {
        Me.MotionMaster.MovePath(PathIds.GIZRUL, false);
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(17),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.FATAL_BITE);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCast(Me, SpellIds.INFECTED_BITE);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
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
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Halycon;

internal struct SpellIds
{
    public const uint REND = 13738;
    public const uint THRASH = 3391;
}

internal struct TextIds
{
    public const uint EMOTE_DEATH = 0;
}

[Script]
internal class BossHalycon : BossAI
{
    private static readonly Position SummonLocation = new(-167.9561f, -411.7844f, 76.23057f, 1.53589f);


    public BossHalycon(Creature creature) : base(creature, DataTypes.HALYCON)
    {
        Initialize();
    }

    public override void Reset()
    {
        _Reset();
        Initialize();
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

        Scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), task => { DoCast(Me, SpellIds.THRASH); });
    }

    public override void JustDied(Unit killer)
    {
        Me.SummonCreature(CreaturesIds.GIZRUL_THE_SLAVENER, SummonLocation, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));
        Talk(TextIds.EMOTE_DEATH);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize() { }
}
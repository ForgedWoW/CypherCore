// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Halycon;

internal struct SpellIds
{
    public const uint Rend = 13738;
    public const uint Thrash = 3391;
}

internal struct TextIds
{
    public const uint EmoteDeath = 0;
}

[Script]
internal class boss_halycon : BossAI
{
    private static readonly Position SummonLocation = new(-167.9561f, -411.7844f, 76.23057f, 1.53589f);


    public boss_halycon(Creature creature) : base(creature, DataTypes.Halycon)
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
                               DoCastVictim(SpellIds.Rend);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), task => { DoCast(Me, SpellIds.Thrash); });
    }

    public override void JustDied(Unit killer)
    {
        Me.SummonCreature(CreaturesIds.GizrulTheSlavener, SummonLocation, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));
        Talk(TextIds.EmoteDeath);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize() { }
}
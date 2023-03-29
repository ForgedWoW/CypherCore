// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Gyth;

internal struct SpellIds
{
    public const uint RendMounts = 16167;    // Change model
    public const uint CorrosiveAcid = 16359; // Combat (self cast)
    public const uint Flamebreath = 16390;   // Combat (Self cast)
    public const uint Freeze = 16350;        // Combat (Self cast)
    public const uint KnockAway = 10101;     // Combat
    public const uint SummonRend = 16328;    // Summons Rend near death
}

internal struct MiscConst
{
    public const uint NefariusPath2 = 1379671;
    public const uint NefariusPath3 = 1379672;
    public const uint GythPath1 = 1379681;
}

[Script]
internal class boss_gyth : BossAI
{
    private bool SummonedRend;

    public boss_gyth(Creature creature) : base(creature, DataTypes.Gyth)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        if (Instance.GetBossState(DataTypes.Gyth) == EncounterState.InProgress)
        {
            Instance.SetBossState(DataTypes.Gyth, EncounterState.Done);
            Me.DespawnOrUnsummon();
        }
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.CancelAll();

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCast(Me, SpellIds.CorrosiveAcid);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCast(Me, SpellIds.Freeze);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCast(Me, SpellIds.Flamebreath);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           TimeSpan.FromSeconds(18),
                           task =>
                           {
                               DoCastVictim(SpellIds.KnockAway);
                               task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(20));
                           });
    }

    public override void JustDied(Unit killer)
    {
        Instance.SetBossState(DataTypes.Gyth, EncounterState.Done);
    }

    public override void SetData(uint type, uint data)
    {
        switch (data)
        {
            case 1:
                Scheduler.Schedule(TimeSpan.FromSeconds(1),
                                   task =>
                                   {
                                       Me.AddAura(SpellIds.RendMounts, Me);
                                       var portcullis = Me.FindNearestGameObject(GameObjectsIds.DrPortcullis, 40.0f);

                                       if (portcullis)
                                           portcullis.UseDoorOrButton();

                                       var victor = Me.FindNearestCreature(CreaturesIds.LordVictorNefarius, 75.0f, true);

                                       if (victor)
                                           victor.AI.SetData(1, 1);

                                       task.Schedule(TimeSpan.FromSeconds(2), summonTask2 => { Me.MotionMaster.MovePath(MiscConst.GythPath1, false); });
                                   });

                break;
            default:
                break;
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!SummonedRend &&
            HealthBelowPct(5))
        {
            DoCast(Me, SpellIds.SummonRend);
            Me.RemoveAura(SpellIds.RendMounts);
            SummonedRend = true;
        }

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        SummonedRend = false;
    }
}
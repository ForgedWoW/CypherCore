// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Gyth;

internal struct SpellIds
{
    public const uint REND_MOUNTS = 16167;    // Change model
    public const uint CORROSIVE_ACID = 16359; // Combat (self cast)
    public const uint FLAMEBREATH = 16390;   // Combat (Self cast)
    public const uint FREEZE = 16350;        // Combat (Self cast)
    public const uint KNOCK_AWAY = 10101;     // Combat
    public const uint SUMMON_REND = 16328;    // Summons Rend near death
}

internal struct MiscConst
{
    public const uint NEFARIUS_PATH2 = 1379671;
    public const uint NEFARIUS_PATH3 = 1379672;
    public const uint GYTH_PATH1 = 1379681;
}

[Script]
internal class BossGyth : BossAI
{
    private bool _summonedRend;

    public BossGyth(Creature creature) : base(creature, DataTypes.GYTH)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        if (Instance.GetBossState(DataTypes.GYTH) == EncounterState.InProgress)
        {
            Instance.SetBossState(DataTypes.GYTH, EncounterState.Done);
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
                               DoCast(Me, SpellIds.CORROSIVE_ACID);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCast(Me, SpellIds.FREEZE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCast(Me, SpellIds.FLAMEBREATH);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           TimeSpan.FromSeconds(18),
                           task =>
                           {
                               DoCastVictim(SpellIds.KNOCK_AWAY);
                               task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(20));
                           });
    }

    public override void JustDied(Unit killer)
    {
        Instance.SetBossState(DataTypes.GYTH, EncounterState.Done);
    }

    public override void SetData(uint type, uint data)
    {
        switch (data)
        {
            case 1:
                Scheduler.Schedule(TimeSpan.FromSeconds(1),
                                   task =>
                                   {
                                       Me.AddAura(SpellIds.REND_MOUNTS, Me);
                                       var portcullis = Me.FindNearestGameObject(GameObjectsIds.DR_PORTCULLIS, 40.0f);

                                       if (portcullis)
                                           portcullis.UseDoorOrButton();

                                       var victor = Me.FindNearestCreature(CreaturesIds.LORD_VICTOR_NEFARIUS, 75.0f, true);

                                       if (victor)
                                           victor.AI.SetData(1, 1);

                                       task.Schedule(TimeSpan.FromSeconds(2), summonTask2 => { Me.MotionMaster.MovePath(MiscConst.GYTH_PATH1, false); });
                                   });

                break;
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!_summonedRend &&
            HealthBelowPct(5))
        {
            DoCast(Me, SpellIds.SUMMON_REND);
            Me.RemoveAura(SpellIds.REND_MOUNTS);
            _summonedRend = true;
        }

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        _summonedRend = false;
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Broodlord;

internal struct SpellIds
{
    public const uint CLEAVE = 26350;
    public const uint BLASTWAVE = 23331;
    public const uint MORTALSTRIKE = 24573;
    public const uint KNOCKBACK = 25778;
    public const uint SUPPRESSION_AURA = 22247; // Suppression Device Spell
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_LEASH = 1;
}

internal struct EventIds
{
    // Suppression Device Events
    public const uint SUPPRESSION_CAST = 1;
    public const uint SUPPRESSION_RESET = 2;
}

internal struct ActionIds
{
    public const int DEACTIVATE = 0;
}

[Script]
internal class BossBroodlord : BossAI
{
    public BossBroodlord(Creature creature) : base(creature, DataTypes.BROODLORD_LASHLAYER) { }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.SAY_AGGRO);

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(8),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.CLEAVE);
                                        task.Repeat(TimeSpan.FromSeconds(7));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(12),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.BLASTWAVE);
                                        task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(20),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.MORTALSTRIKE);
                                        task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(30),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.KNOCKBACK);

                                        if (GetThreat(Me.Victim) != 0)
                                            ModifyThreatByPercent(Me.Victim, -50);

                                        task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(1),
                                    task =>
                                    {
                                        if (Me.GetDistance(Me.HomePosition) > 150.0f)
                                        {
                                            Talk(TextIds.SAY_LEASH);
                                            EnterEvadeMode(EvadeReason.Boundary);
                                        }

                                        task.Repeat(TimeSpan.FromSeconds(1));
                                    });
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();

        var goList = Me.GetGameObjectListWithEntryInGrid(BwlGameObjectIds.SUPPRESSION_DEVICE, 200.0f);

        foreach (var go in goList)
            go.AI.DoAction(ActionIds.DEACTIVATE);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        SchedulerProtected.Update(diff, () => DoMeleeAttackIfReady());
    }
}

[Script]
internal class GOSuppressionDevice : GameObjectAI
{
    private readonly InstanceScript _instance;
    private bool _active;

    public GOSuppressionDevice(GameObject go) : base(go)
    {
        _instance = go.InstanceScript;
        _active = true;
    }

    public override void InitializeAI()
    {
        if (_instance.GetBossState(DataTypes.BROODLORD_LASHLAYER) == EncounterState.Done)
        {
            Deactivate();

            return;
        }

        Events.ScheduleEvent(EventIds.SUPPRESSION_CAST, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5));
    }

    public override void UpdateAI(uint diff)
    {
        Events.Update(diff);

        Events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case EventIds.SUPPRESSION_CAST:
                    if (Me.GoState == GameObjectState.Ready)
                    {
                        Me.SpellFactory.CastSpell(null, SpellIds.SUPPRESSION_AURA, true);
                        Me.SendCustomAnim(0);
                    }

                    Events.ScheduleEvent(EventIds.SUPPRESSION_CAST, TimeSpan.FromSeconds(5));

                    break;
                case EventIds.SUPPRESSION_RESET:
                    Activate();

                    break;
            }
        });
    }

    public override void OnLootStateChanged(uint state, Unit unit)
    {
        switch ((LootState)state)
        {
            case LootState.Activated:
                Deactivate();
                Events.CancelEvent(EventIds.SUPPRESSION_CAST);
                Events.ScheduleEvent(EventIds.SUPPRESSION_RESET, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120));

                break;
            case LootState.JustDeactivated: // This case prevents the Gameobject despawn by Disarm Trap
                Me.SetLootState(LootState.Ready);

                break;
        }
    }

    public override void DoAction(int action)
    {
        if (action == ActionIds.DEACTIVATE)
        {
            Deactivate();
            Events.CancelEvent(EventIds.SUPPRESSION_RESET);
        }
    }

    private void Activate()
    {
        if (_active)
            return;

        _active = true;

        if (Me.GoState == GameObjectState.Active)
            Me.SetGoState(GameObjectState.Ready);

        Me.SetLootState(LootState.Ready);
        Me.RemoveFlag(GameObjectFlags.NotSelectable);
        Events.ScheduleEvent(EventIds.SUPPRESSION_CAST, TimeSpan.FromSeconds(0));
    }

    private void Deactivate()
    {
        if (!_active)
            return;

        _active = false;
        Me.SetGoState(GameObjectState.Active);
        Me.SetFlag(GameObjectFlags.NotSelectable);
        Events.CancelEvent(EventIds.SUPPRESSION_CAST);
    }
}
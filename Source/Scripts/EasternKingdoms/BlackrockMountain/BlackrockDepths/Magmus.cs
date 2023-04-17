// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.Magmus;

internal struct SpellIds
{
    //Magmus
    public const uint FIERYBURST = 13900;
    public const uint WARSTOMP = 24375;

    //IronhandGuardian
    public const uint GOUTOFFLAME = 15529;
}

internal enum Phases
{
    One = 1,
    Two = 2
}

[Script]
internal class BossMagmus : ScriptedAI
{
    private Phases _phase;

    public BossMagmus(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        var instance = Me.InstanceScript;

        instance?.SetData(DataTypes.TYPE_IRON_HALL, (uint)EncounterState.InProgress);

        _phase = Phases.One;

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               DoCastVictim(SpellIds.FIERYBURST);
                               task.Repeat(TimeSpan.FromSeconds(6));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (Me.HealthBelowPctDamaged(50, damage) &&
            _phase == Phases.One)
        {
            _phase = Phases.Two;

            Scheduler.Schedule(TimeSpan.FromSeconds(0),
                               task =>
                               {
                                   DoCastVictim(SpellIds.WARSTOMP);
                                   task.Repeat(TimeSpan.FromSeconds(8));
                               });
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    public override void JustDied(Unit killer)
    {
        var instance = Me.InstanceScript;

        if (instance != null)
        {
            instance.HandleGameObject(instance.GetGuidData(DataTypes.DATA_THRONE_DOOR), true);
            instance.SetData(DataTypes.TYPE_IRON_HALL, (uint)EncounterState.Done);
        }
    }
}

[Script]
internal class NPCIronhandGuardian : ScriptedAI
{
    private readonly InstanceScript _instance;
    private bool _active;

    public NPCIronhandGuardian(Creature creature) : base(creature)
    {
        _instance = Me.InstanceScript;
        _active = false;
    }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void UpdateAI(uint diff)
    {
        if (!_active)
        {
            if (_instance.GetData(DataTypes.TYPE_IRON_HALL) == (uint)EncounterState.NotStarted)
                return;

            // Once the boss is engaged, the guardians will stay activated until the next instance reset
            Scheduler.Schedule(TimeSpan.FromSeconds(0),
                               TimeSpan.FromSeconds(10),
                               task =>
                               {
                                   DoCastAOE(SpellIds.GOUTOFFLAME);
                                   task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(21));
                               });

            _active = true;
        }

        Scheduler.Update(diff);
    }
}
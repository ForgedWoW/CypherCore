// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.AlteracValley.Balinda;

internal struct SpellIds
{
    public const uint ArcaneExplosion = 46608;
    public const uint ConeOfCold = 38384;
    public const uint Fireball = 46988;
    public const uint Frostbolt = 46987;
    public const uint SummonWaterElemental = 45067;
    public const uint Iceblock = 46604;
}

internal struct TextIds
{
    public const uint SayAggro = 0;
    public const uint SayEvade = 1;
    public const uint SaySalvation = 2;
}

internal struct ActionIds
{
    public const int BuffYell = -30001; // shared from Battleground
}

[Script]
internal class boss_balinda : ScriptedAI
{
    private readonly SummonList summons;
    private bool HasCastIceblock;
    private ObjectGuid WaterElementalGUID;

    public boss_balinda(Creature creature) : base(creature)
    {
        summons = new SummonList(Me);
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        Scheduler.CancelAll();
        summons.DespawnAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SayAggro);

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.ArcaneExplosion);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.ConeOfCold);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               DoCastVictim(SpellIds.Fireball);
                               task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(9));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               DoCastVictim(SpellIds.Frostbolt);
                               task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               if (summons.Empty())
                                   DoCast(SpellIds.SummonWaterElemental);

                               task.Repeat(TimeSpan.FromSeconds(50));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          if (Me.GetDistance2d(Me.HomePosition.X, Me.HomePosition.Y) > 50)
                                                                          {
                                                                              base.EnterEvadeMode();
                                                                              Talk(TextIds.SayEvade);
                                                                          }

                                                                          var elemental = ObjectAccessor.GetCreature(Me, WaterElementalGUID);

                                                                          if (elemental != null)
                                                                              if (elemental.GetDistance2d(Me.HomePosition.X, Me.HomePosition.Y) > 50)
                                                                                  elemental.AI.EnterEvadeMode();

                                                                          task.Repeat();
                                                                      }));
    }

    public override void JustSummoned(Creature summoned)
    {
        summoned.AI.AttackStart(SelectTarget(SelectTargetMethod.Random, 0, 50, true));
        summoned.Faction = Me.Faction;
        WaterElementalGUID = summoned.GUID;
        summons.Summon(summoned);
    }

    public override void SummonedCreatureDespawn(Creature summoned)
    {
        summons.Despawn(summoned);
    }

    public override void JustDied(Unit killer)
    {
        summons.DespawnAll();
    }

    public override void DoAction(int actionId)
    {
        if (actionId == ActionIds.BuffYell)
            Talk(TextIds.SayAggro);
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (Me.HealthBelowPctDamaged(40, damage) &&
            !HasCastIceblock)
        {
            DoCast(SpellIds.Iceblock);
            HasCastIceblock = true;
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        WaterElementalGUID.Clear();
        HasCastIceblock = false;
    }
}
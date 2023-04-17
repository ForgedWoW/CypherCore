// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.AlteracValley.Balinda;

internal struct SpellIds
{
    public const uint ARCANE_EXPLOSION = 46608;
    public const uint CONE_OF_COLD = 38384;
    public const uint FIREBALL = 46988;
    public const uint FROSTBOLT = 46987;
    public const uint SUMMON_WATER_ELEMENTAL = 45067;
    public const uint ICEBLOCK = 46604;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_EVADE = 1;
    public const uint SAY_SALVATION = 2;
}

internal struct ActionIds
{
    public const int BUFF_YELL = -30001; // shared from Battleground
}

[Script]
internal class BossBalinda : ScriptedAI
{
    private readonly SummonList _summons;
    private bool _hasCastIceblock;
    private ObjectGuid _waterElementalGUID;

    public BossBalinda(Creature creature) : base(creature)
    {
        _summons = new SummonList(Me);
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        Scheduler.CancelAll();
        _summons.DespawnAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.ARCANE_EXPLOSION);
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.CONE_OF_COLD);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               DoCastVictim(SpellIds.FIREBALL);
                               task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(9));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               DoCastVictim(SpellIds.FROSTBOLT);
                               task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               if (_summons.Empty())
                                   DoCast(SpellIds.SUMMON_WATER_ELEMENTAL);

                               task.Repeat(TimeSpan.FromSeconds(50));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          if (Me.GetDistance2d(Me.HomePosition.X, Me.HomePosition.Y) > 50)
                                                                          {
                                                                              base.EnterEvadeMode();
                                                                              Talk(TextIds.SAY_EVADE);
                                                                          }

                                                                          var elemental = ObjectAccessor.GetCreature(Me, _waterElementalGUID);

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
        _waterElementalGUID = summoned.GUID;
        _summons.Summon(summoned);
    }

    public override void SummonedCreatureDespawn(Creature summoned)
    {
        _summons.Despawn(summoned);
    }

    public override void JustDied(Unit killer)
    {
        _summons.DespawnAll();
    }

    public override void DoAction(int actionId)
    {
        if (actionId == ActionIds.BUFF_YELL)
            Talk(TextIds.SAY_AGGRO);
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (Me.HealthBelowPctDamaged(40, damage) &&
            !_hasCastIceblock)
        {
            DoCast(SpellIds.ICEBLOCK);
            _hasCastIceblock = true;
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
        _waterElementalGUID.Clear();
        _hasCastIceblock = false;
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class CasterAI : CombatAI
{
    private float _attackDistance;

    public CasterAI(Creature creature) : base(creature)
    {
        _attackDistance = SharedConst.MeleeRange;
    }

    public override void AttackStart(Unit victim)
    {
        AttackStartCaster(victim, _attackDistance);
    }

    public override void InitializeAI()
    {
        base.InitializeAI();

        _attackDistance = 30.0f;

        foreach (var id in Spells)
        {
            var info = GetAISpellInfo(id, Me.Location.Map.DifficultyID);

            if (info is { Condition: AICondition.Combat } && _attackDistance > info.MaxRange)
                _attackDistance = info.MaxRange;
        }

        if (_attackDistance == 30.0f)
            _attackDistance = SharedConst.MeleeRange;
    }

    public override void JustEngagedWith(Unit victim)
    {
        if (Spells.Empty())
            return;

        var spell = (int)(RandomHelper.Rand32() % Spells.Count);
        uint count = 0;

        foreach (var id in Spells)
        {
            var info = GetAISpellInfo(id, Me.Location.Map.DifficultyID);

            if (info == null)
                continue;

            switch (info.Condition)
            {
                case AICondition.Aggro:
                    Me.SpellFactory.CastSpell(victim, id);

                    break;

                case AICondition.Combat:
                {
                    var cooldown = info.RealCooldown;

                    if (count == spell)
                    {
                        DoCast(Spells[spell]);
                        cooldown += TimeSpan.FromMilliseconds(Me.GetCurrentSpellCastTime(id));
                    }

                    Events.ScheduleEvent(id, cooldown);

                    break;
                }
            }
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.Victim != null)
            if (Me.Victim.HasBreakableByDamageCrowdControlAura(Me))
            {
                Me.InterruptNonMeleeSpells(false);

                return;
            }

        if (Me.HasUnitState(UnitState.Casting))
            return;

        var spellId = Events.ExecuteEvent();

        if (spellId != 0)
        {
            DoCast(spellId);
            var casttime = (uint)Me.GetCurrentSpellCastTime(spellId);
            var info = GetAISpellInfo(spellId, Me.Location.Map.DifficultyID);

            if (info != null)
                Events.ScheduleEvent(spellId, TimeSpan.FromMilliseconds(casttime != 0 ? casttime : 500) + info.RealCooldown);
        }
    }
}
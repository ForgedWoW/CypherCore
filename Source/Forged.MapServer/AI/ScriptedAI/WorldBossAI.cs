// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.ScriptedAI;

public class WorldBossAI : ScriptedAI
{
    private readonly SummonList _summons;

    public WorldBossAI(Creature creature) : base(creature)
    {
        _summons = new SummonList(creature);
    }

    // Hook used to execute events scheduled into EventMap without the need
    // to override UpdateAI
    // note: You must re-schedule the event within this method if the event
    // is supposed to run more than once
    public virtual void ExecuteEvent(uint eventId) { }

    public override void JustDied(Unit killer)
    {
        JustDiedInternal();
    }

    public override void JustEngagedWith(Unit who)
    {
        JustEngagedWithInternal();
    }

    public override void JustSummoned(Creature summon)
    {
        _summons.Summon(summon);
        var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

        if (target != null)
            summon.AI.AttackStart(target);
    }

    public override void Reset()
    {
        ResetInternal();
    }

    public override void SummonedCreatureDespawn(Creature summon)
    {
        _summons.Despawn(summon);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.HasUnitState(UnitState.Casting))
            return;

        var hasSpell = false;

        Events.ExecuteEvents(eventId =>
        {
            ExecuteEvent(eventId);

            if (Me.HasUnitState(UnitState.Casting))
                hasSpell = true;
        });

        if (!hasSpell)
            DoMeleeAttackIfReady();
    }

    private void JustDiedInternal()
    {
        Events.Reset();
        _summons.DespawnAll();
    }

    private void JustEngagedWithInternal()
    {
        var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

        if (target != null)
            AttackStart(target);
    }

    private void ResetInternal()
    {
        if (!Me.IsAlive)
            return;

        Events.Reset();
        _summons.DespawnAll();
    }
}
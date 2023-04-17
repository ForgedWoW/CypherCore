// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Shazzrah;

internal struct SpellIds
{
    public const uint ARCANE_EXPLOSION = 19712;
    public const uint SHAZZRAH_CURSE = 19713;
    public const uint MAGIC_GROUNDING = 19714;
    public const uint COUNTERSPELL = 19715;
    public const uint SHAZZRAH_GATE_DUMMY = 23138; // Teleports to and attacks a random Target.
    public const uint SHAZZRAH_GATE = 23139;
}

internal struct EventIds
{
    public const uint ARCANE_EXPLOSION = 1;
    public const uint ARCANE_EXPLOSION_TRIGGERED = 2;
    public const uint SHAZZRAH_CURSE = 3;
    public const uint MAGIC_GROUNDING = 4;
    public const uint COUNTERSPELL = 5;
    public const uint SHAZZRAH_GATE = 6;
}

[Script]
internal class BossShazzrah : BossAI
{
    public BossShazzrah(Creature creature) : base(creature, DataTypes.SHAZZRAH) { }

    public override void JustEngagedWith(Unit target)
    {
        base.JustEngagedWith(target);
        Events.ScheduleEvent(EventIds.ARCANE_EXPLOSION, TimeSpan.FromSeconds(6));
        Events.ScheduleEvent(EventIds.SHAZZRAH_CURSE, TimeSpan.FromSeconds(10));
        Events.ScheduleEvent(EventIds.MAGIC_GROUNDING, TimeSpan.FromSeconds(24));
        Events.ScheduleEvent(EventIds.COUNTERSPELL, TimeSpan.FromSeconds(15));
        Events.ScheduleEvent(EventIds.SHAZZRAH_GATE, TimeSpan.FromSeconds(45));
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.HasUnitState(UnitState.Casting))
            return;

        Events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case EventIds.ARCANE_EXPLOSION:
                    DoCastVictim(SpellIds.ARCANE_EXPLOSION);
                    Events.ScheduleEvent(EventIds.ARCANE_EXPLOSION, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(7));

                    break;
                // Triggered subsequent to using "Gate of Shazzrah".
                case EventIds.ARCANE_EXPLOSION_TRIGGERED:
                    DoCastVictim(SpellIds.ARCANE_EXPLOSION);

                    break;
                case EventIds.SHAZZRAH_CURSE:
                    var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.SHAZZRAH_CURSE);

                    if (target)
                        DoCast(target, SpellIds.SHAZZRAH_CURSE);

                    Events.ScheduleEvent(EventIds.SHAZZRAH_CURSE, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(30));

                    break;
                case EventIds.MAGIC_GROUNDING:
                    DoCast(Me, SpellIds.MAGIC_GROUNDING);
                    Events.ScheduleEvent(EventIds.MAGIC_GROUNDING, TimeSpan.FromSeconds(35));

                    break;
                case EventIds.COUNTERSPELL:
                    DoCastVictim(SpellIds.COUNTERSPELL);
                    Events.ScheduleEvent(EventIds.COUNTERSPELL, TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(20));

                    break;
                case EventIds.SHAZZRAH_GATE:
                    ResetThreatList();
                    DoCastAOE(SpellIds.SHAZZRAH_GATE_DUMMY);
                    Events.ScheduleEvent(EventIds.ARCANE_EXPLOSION_TRIGGERED, TimeSpan.FromSeconds(2));
                    Events.RescheduleEvent(EventIds.ARCANE_EXPLOSION, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6));
                    Events.ScheduleEvent(EventIds.SHAZZRAH_GATE, TimeSpan.FromSeconds(45));

                    break;
            }

            if (Me.HasUnitState(UnitState.Casting))
                return;
        });


        DoMeleeAttackIfReady();
    }
}

[Script] // 23138 - Gate of Shazzrah
internal class SpellShazzrahGateDummy : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Empty())
            return;

        var target = targets.SelectRandom();
        targets.Clear();
        targets.Add(target);
    }

    private void HandleScript(int effIndex)
    {
        var target = HitUnit;

        if (target)
        {
            target.SpellFactory.CastSpell(Caster, SpellIds.SHAZZRAH_GATE, true);
            var creature = Caster.AsCreature;

            if (creature)
                creature.AI.AttackStart(target); // Attack the Target which caster will teleport to.
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[CreatureScript(EvokerNpCs.CYCLE_OF_LIFE)]
public class NPCEvokerCycleOfLife : ScriptedAI
{
    Aura _cycleOfLife;

    public NPCEvokerCycleOfLife(Creature creature) : base(creature) { }

    public override void JustSummoned(Creature summon)
    {
        base.JustSummoned(summon);
        var owner = Me.OwnerUnit;

        if (owner == null)
            return;

        _cycleOfLife = owner.GetAura(EvokerSpells.CYCLE_OF_LIFE_AURA);
    }

    public override void OnDespawn()
    {
        CastSpellExtraArgs args = new(true);
        args.SpellValueOverrides[SpellValueMod.BasePoint0] = _cycleOfLife.AuraEffects[0].Amount;
        Me.SpellFactory.CastSpell(Me.HomePosition, EvokerSpells.CYCLE_OF_LIFE_HEAL, args);
        base.OnDespawn();
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[CreatureScript(EvokerNPCs.CYCLE_OF_LIFE)]
public class npc_evoker_cycle_of_life : ScriptedAI
{
    Aura _cycleOfLife;

    public npc_evoker_cycle_of_life(Creature creature) : base(creature) { }

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
        Me.CastSpell(Me.HomePosition, EvokerSpells.CYCLE_OF_LIFE_HEAL, args);
        base.OnDespawn();
    }
}
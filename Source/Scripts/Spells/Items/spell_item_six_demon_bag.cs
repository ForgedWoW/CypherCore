// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 14537 Six Demon Bag
internal class SpellItemSixDemonBag : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (target)
        {
            uint spellId;
            var rand = RandomHelper.URand(0, 99);

            if (rand < 25) // Fireball (25% chance)
                spellId = ItemSpellIds.FIREBALL;
            else if (rand < 50) // Frostball (25% chance)
                spellId = ItemSpellIds.FROSTBOLT;
            else if (rand < 70) // Chain Lighting (20% chance)
                spellId = ItemSpellIds.CHAIN_LIGHTNING;
            else if (rand < 80) // Polymorph (10% chance)
            {
                spellId = ItemSpellIds.POLYMORPH;

                if (RandomHelper.URand(0, 100) <= 30) // 30% chance to self-cast
                    target = caster;
            }
            else if (rand < 95) // Enveloping Winds (15% chance)
                spellId = ItemSpellIds.ENVELOPING_WINDS;
            else // Summon Felhund minion (5% chance)
            {
                spellId = ItemSpellIds.SUMMON_FELHOUND_MINION;
                target = caster;
            }

            caster.SpellFactory.CastSpell(target, spellId, new CastSpellExtraArgs(CastItem));
        }
    }
}
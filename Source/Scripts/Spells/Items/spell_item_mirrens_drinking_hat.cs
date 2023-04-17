// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 29830 - Mirren's Drinking Hat
internal class SpellItemMirrensDrinkingHat : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        uint spellId = 0;

        switch (RandomHelper.URand(1, 6))
        {
            case 1:
            case 2:
            case 3:
                spellId = ItemSpellIds.LOCH_MODAN_LAGER;

                break;
            case 4:
            case 5:
                spellId = ItemSpellIds.STOUTHAMMER_LITE;

                break;
            case 6:
                spellId = ItemSpellIds.AERIE_PEAK_PALE_ALE;

                break;
            default:
                return;
        }

        var caster = Caster;
        caster.SpellFactory.CastSpell(caster, spellId, new CastSpellExtraArgs(Spell));
    }
}
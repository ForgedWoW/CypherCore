// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script("spell_item_goblin_jumper_cables", 33u, ItemSpellIds.GOBLIN_JUMPER_CABLES_FAIL)]
[Script("spell_item_goblin_jumper_cables_xl", 50u, ItemSpellIds.GOBLIN_JUMPER_CABLES_XL_FAIL)]
[Script("spell_item_gnomish_army_knife", 67u, 0u)]
internal class SpellItemDefibrillate : SpellScript, IHasSpellEffects
{
    private readonly uint _chance;
    private readonly uint _failSpell;

    public SpellItemDefibrillate(uint chance, uint failSpell)
    {
        _chance = chance;
        _failSpell = failSpell;
    }

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Resurrect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        if (RandomHelper.randChance(_chance))
        {
            PreventHitDefaultEffect(effIndex);

            if (_failSpell != 0)
                Caster.SpellFactory.CastSpell(Caster, _failSpell, new CastSpellExtraArgs(CastItem));
        }
    }
}
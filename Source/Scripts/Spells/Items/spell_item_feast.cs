// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script("spell_item_great_feast", TextIds.GREAT_FEAST)]
[Script("spell_item_fish_feast", TextIds.TEXT_FISH_FEAST)]
[Script("spell_item_gigantic_feast", TextIds.TEXT_GIGANTIC_FEAST)]
[Script("spell_item_small_feast", TextIds.SMALL_FEAST)]
[Script("spell_item_bountiful_feast", TextIds.BOUNTIFUL_FEAST)]
internal class SpellItemFeast : SpellScript, IHasSpellEffects
{
    private readonly uint _text;

    public SpellItemFeast(uint text)
    {
        _text = text;
    }

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;
        caster.TextEmote(_text, caster, false);
    }
}
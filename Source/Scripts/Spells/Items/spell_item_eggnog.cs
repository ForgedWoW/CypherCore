// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 21149 - Egg Nog
internal class SpellItemEggnog : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 2, SpellEffectName.Inebriate, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        if (RandomHelper.randChance(40))
            Caster.SpellFactory.CastSpell(HitUnit, RandomHelper.randChance(50) ? ItemSpellIds.EGG_NOG_REINDEER : ItemSpellIds.EGG_NOG_SNOWMAN, CastItem);
    }
}
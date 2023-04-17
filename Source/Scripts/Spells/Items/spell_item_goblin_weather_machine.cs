// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 46203 - Goblin Weather Machine
internal class SpellItemGoblinWeatherMachine : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var target = HitUnit;

        var spellId = RandomHelper.RAND(ItemSpellIds.PERSONALIZED_WEATHER1, ItemSpellIds.PERSONALIZED_WEATHER2, ItemSpellIds.PERSONALIZED_WEATHER3, ItemSpellIds.PERSONALIZED_WEATHER4);
        target.SpellFactory.CastSpell(target, spellId, new CastSpellExtraArgs(Spell));
    }
}
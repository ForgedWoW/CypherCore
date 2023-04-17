// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 40802 Mingo's Fortune Generator
internal class SpellItemMingosFortuneGenerator : SpellScript, IHasSpellEffects
{
    private readonly uint[] _createFortuneSpells =
    {
        ItemSpellIds.CREATE_FORTUNE1, ItemSpellIds.CREATE_FORTUNE2, ItemSpellIds.CREATE_FORTUNE3, ItemSpellIds.CREATE_FORTUNE4, ItemSpellIds.CREATE_FORTUNE5, ItemSpellIds.CREATE_FORTUNE6, ItemSpellIds.CREATE_FORTUNE7, ItemSpellIds.CREATE_FORTUNE8, ItemSpellIds.CREATE_FORTUNE9, ItemSpellIds.CREATE_FORTUNE10, ItemSpellIds.CREATE_FORTUNE11, ItemSpellIds.CREATE_FORTUNE12, ItemSpellIds.CREATE_FORTUNE13, ItemSpellIds.CREATE_FORTUNE14, ItemSpellIds.CREATE_FORTUNE15, ItemSpellIds.CREATE_FORTUNE16, ItemSpellIds.CREATE_FORTUNE17, ItemSpellIds.CREATE_FORTUNE18, ItemSpellIds.CREATE_FORTUNE19, ItemSpellIds.CREATE_FORTUNE20
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        Caster.SpellFactory.CastSpell(Caster, _createFortuneSpells.SelectRandom(), true);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script("spell_item_arcanite_dragonling", ItemSpellIds.ARCANITE_DRAGONLING)]
[Script("spell_item_gnomish_battle_chicken", ItemSpellIds.BATTLE_CHICKEN)]
[Script("spell_item_mechanical_dragonling", ItemSpellIds.MECHANICAL_DRAGONLING)]
[Script("spell_item_mithril_mechanical_dragonling", ItemSpellIds.MITHRIL_MECHANICAL_DRAGONLING)]
internal class SpellItemTriggerSpell : SpellScript, IHasSpellEffects
{
    private readonly uint _triggeredSpellId;

    public SpellItemTriggerSpell(uint triggeredSpellId)
    {
        _triggeredSpellId = triggeredSpellId;
    }

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var item = CastItem;

        if (item)
            caster.SpellFactory.CastSpell(caster, _triggeredSpellId, new CastSpellExtraArgs(item));
    }
}
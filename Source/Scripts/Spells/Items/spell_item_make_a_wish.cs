// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 33060 Make a Wish
internal class SpellItemMakeAWish : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var spellId = ItemSpellIds.MR_PINCHYS_GIFT;

        switch (RandomHelper.URand(1, 5))
        {
            case 1:
                spellId = ItemSpellIds.MR_PINCHYS_BLESSING;

                break;
            case 2:
                spellId = ItemSpellIds.SUMMON_MIGHTY_MR_PINCHY;

                break;
            case 3:
                spellId = ItemSpellIds.SUMMON_FURIOUS_MR_PINCHY;

                break;
            case 4:
                spellId = ItemSpellIds.TINY_MAGICAL_CRAWDAD;

                break;
        }

        caster.SpellFactory.CastSpell(caster, spellId, true);
    }
}
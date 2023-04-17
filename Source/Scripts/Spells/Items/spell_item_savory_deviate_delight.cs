// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 8213 Savory Deviate Delight
internal class SpellItemSavoryDeviateDelight : SpellScript, IHasSpellEffects
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
        uint spellId = 0;

        switch (RandomHelper.URand(1, 2))
        {
            // Flip Out - ninja
            case 1:
                spellId = (caster.NativeGender == Gender.Male ? ItemSpellIds.FLIP_OUT_MALE : ItemSpellIds.FLIP_OUT_FEMALE);

                break;
            // Yaaarrrr - pirate
            case 2:
                spellId = (caster.NativeGender == Gender.Male ? ItemSpellIds.YAAARRRR_MALE : ItemSpellIds.YAAARRRR_FEMALE);

                break;
        }

        caster.SpellFactory.CastSpell(caster, spellId, true);
    }
}
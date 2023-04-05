// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_feast : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var target = HitUnit;

        switch (SpellInfo.Id)
        {
            case GenericSpellIds.GreatFeast:
                target.CastSpell(target, GenericSpellIds.FeastFood);
                target.CastSpell(target, GenericSpellIds.FeastDrink);
                target.CastSpell(target, GenericSpellIds.GreatFeastRefreshment);

                break;
            case GenericSpellIds.FishFeast:
                target.CastSpell(target, GenericSpellIds.FeastFood);
                target.CastSpell(target, GenericSpellIds.FeastDrink);
                target.CastSpell(target, GenericSpellIds.FishFeastRefreshment);

                break;
            case GenericSpellIds.GiganticFeast:
                target.CastSpell(target, GenericSpellIds.FeastFood);
                target.CastSpell(target, GenericSpellIds.FeastDrink);
                target.CastSpell(target, GenericSpellIds.GiganticFeastRefreshment);

                break;
            case GenericSpellIds.SmallFeast:
                target.CastSpell(target, GenericSpellIds.FeastFood);
                target.CastSpell(target, GenericSpellIds.FeastDrink);
                target.CastSpell(target, GenericSpellIds.SmallFeastRefreshment);

                break;
            case GenericSpellIds.BountifulFeast:
                target.CastSpell(target, GenericSpellIds.BountifulFeastRefreshment);
                target.CastSpell(target, GenericSpellIds.BountifulFeastDrink);
                target.CastSpell(target, GenericSpellIds.BountifulFeastFood);

                break;
            
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenFeast : SpellScript, IHasSpellEffects
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
            case GenericSpellIds.GREAT_FEAST:
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_FOOD);
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_DRINK);
                target.SpellFactory.CastSpell(target, GenericSpellIds.GREAT_FEAST_REFRESHMENT);

                break;
            case GenericSpellIds.FISH_FEAST:
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_FOOD);
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_DRINK);
                target.SpellFactory.CastSpell(target, GenericSpellIds.FISH_FEAST_REFRESHMENT);

                break;
            case GenericSpellIds.GIGANTIC_FEAST:
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_FOOD);
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_DRINK);
                target.SpellFactory.CastSpell(target, GenericSpellIds.GIGANTIC_FEAST_REFRESHMENT);

                break;
            case GenericSpellIds.SMALL_FEAST:
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_FOOD);
                target.SpellFactory.CastSpell(target, GenericSpellIds.FEAST_DRINK);
                target.SpellFactory.CastSpell(target, GenericSpellIds.SMALL_FEAST_REFRESHMENT);

                break;
            case GenericSpellIds.BOUNTIFUL_FEAST:
                target.SpellFactory.CastSpell(target, GenericSpellIds.BOUNTIFUL_FEAST_REFRESHMENT);
                target.SpellFactory.CastSpell(target, GenericSpellIds.BOUNTIFUL_FEAST_DRINK);
                target.SpellFactory.CastSpell(target, GenericSpellIds.BOUNTIFUL_FEAST_FOOD);

                break;
        }
    }
}
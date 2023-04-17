// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 190336 - Conjure Refreshment
internal class SpellMageConjureRefreshment : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster.AsPlayer;

        if (caster)
        {
            var group = caster.Group;

            if (group)
                caster.SpellFactory.CastSpell(caster, MageSpells.CONJURE_REFRESHMENT_TABLE, true);
            else
                caster.SpellFactory.CastSpell(caster, MageSpells.CONJURE_REFRESHMENT, true);
        }
    }
}
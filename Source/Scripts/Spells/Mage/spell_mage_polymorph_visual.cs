// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 32826 - Polymorph (Visual)
internal class SpellMagePolymorphVisual : SpellScript, IHasSpellEffects
{
    private const uint NPCAurosalia = 18744;

    private readonly uint[] _polymorhForms =
    {
        MageSpells.SquirrelForm, MageSpells.GiraffeForm, MageSpells.SerpentForm, MageSpells.DRADONHAWK_FORM, MageSpells.WorgenForm, MageSpells.SheepForm
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        // add dummy effect spell handler to Polymorph visual
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        Unit target = Caster.FindNearestCreature(NPCAurosalia, 30.0f);

        if (target)
            if (target.IsTypeId(TypeId.Unit))
                target.SpellFactory.CastSpell(target, _polymorhForms[RandomHelper.IRand(0, 5)], true);
    }
}
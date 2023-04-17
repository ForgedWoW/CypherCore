// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemPygmyOil : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var aura = caster.GetAura(ItemSpellIds.PYGMY_OIL_PYGMY_AURA);

        if (aura != null)
            aura.RefreshDuration();
        else
        {
            aura = caster.GetAura(ItemSpellIds.PYGMY_OIL_SMALLER_AURA);

            if (aura == null ||
                aura.StackAmount < 5 ||
                !RandomHelper.randChance(50))
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.PYGMY_OIL_SMALLER_AURA, true);
            else
            {
                aura.Remove();
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.PYGMY_OIL_PYGMY_AURA, true);
            }
        }
    }
}
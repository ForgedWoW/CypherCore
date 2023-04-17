// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemSocretharsStone : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return (Caster.Area == 3900 || Caster.Area == 3742);
    }


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        switch (caster.Area)
        {
            case 3900:
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.SOCRETHAR_TO_SEAT, true);

                break;
            case 3742:
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.SOCRETHAR_FROM_SEAT, true);

                break;
            default:
                return;
        }
    }
}
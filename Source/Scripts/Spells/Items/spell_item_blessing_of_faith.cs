// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

internal class SpellItemBlessingOfFaith : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var unitTarget = HitUnit;

        if (unitTarget != null)
        {
            uint spellId = 0;

            switch (unitTarget.Class)
            {
                case PlayerClass.Druid:
                    spellId = ItemSpellIds.BLESSING_OF_LOWER_CITY_DRUID;

                    break;
                case PlayerClass.Paladin:
                    spellId = ItemSpellIds.BLESSING_OF_LOWER_CITY_PALADIN;

                    break;
                case PlayerClass.Priest:
                    spellId = ItemSpellIds.BLESSING_OF_LOWER_CITY_PRIEST;

                    break;
                case PlayerClass.Shaman:
                    spellId = ItemSpellIds.BLESSING_OF_LOWER_CITY_SHAMAN;

                    break;
                default:
                    return; // ignore for non-healing classes
            }

            var caster = Caster;
            caster.SpellFactory.CastSpell(caster, spellId, true);
        }
    }
}
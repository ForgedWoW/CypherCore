// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 58886 - Food
internal class SpellMagicEaterFood : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTriggerSpell, 1, AuraType.PeriodicTriggerSpell));
    }

    private void HandleTriggerSpell(AuraEffect aurEff)
    {
        PreventDefaultAction();
        var target = Target;

        switch (RandomHelper.URand(0, 5))
        {
            case 0:
                target.SpellFactory.CastSpell(target, ItemSpellIds.WILD_MAGIC, true);

                break;
            case 1:
                target.SpellFactory.CastSpell(target, ItemSpellIds.WELL_FED1, true);

                break;
            case 2:
                target.SpellFactory.CastSpell(target, ItemSpellIds.WELL_FED2, true);

                break;
            case 3:
                target.SpellFactory.CastSpell(target, ItemSpellIds.WELL_FED3, true);

                break;
            case 4:
                target.SpellFactory.CastSpell(target, ItemSpellIds.WELL_FED4, true);

                break;
            case 5:
                target.SpellFactory.CastSpell(target, ItemSpellIds.WELL_FED5, true);

                break;
        }
    }
}
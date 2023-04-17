// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenMarkOfKazrogalHellfireAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PowerBurn));
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        var target = Target;

        if (target.GetPower(PowerType.Mana) == 0)
        {
            target.SpellFactory.CastSpell(target, GenericSpellIds.MARK_OF_KAZROGAL_DAMAGE_HELLFIRE, new CastSpellExtraArgs(aurEff));
            // Remove aura
            SetDuration(0);
        }
    }
}
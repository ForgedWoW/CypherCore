// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(114918)] // 114918 - Light's Hammer (Periodic)
internal class SpellPalLightHammerPeriodic : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        var lightHammer = Target;
        var originalCaster = lightHammer.OwnerUnit;

        if (originalCaster != null)
        {
            originalCaster.SpellFactory.CastSpell(lightHammer.Location, PaladinSpells.LIGHT_HAMMER_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
            originalCaster.SpellFactory.CastSpell(lightHammer.Location, PaladinSpells.LIGHT_HAMMER_HEALING, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }
    }
}
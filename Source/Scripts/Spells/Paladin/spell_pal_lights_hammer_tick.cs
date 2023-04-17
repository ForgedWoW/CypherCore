// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// Light's Hammer (Periodic Dummy) - 114918
[SpellScript(114918)]
public class SpellPalLightsHammerTick : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
    }

    private void OnTick(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster != null)
            if (caster.OwnerUnit)
            {
                var args = new CastSpellExtraArgs();
                args.SetTriggerFlags(TriggerCastFlags.FullMask);
                args.SetOriginalCaster(caster.OwnerUnit.GUID);
                caster.SpellFactory.CastSpell(new Position(caster.Location.X, caster.Location.Y, caster.Location.Z), PaladinSpells.ARCING_LIGHT_HEAL, args);
                caster.SpellFactory.CastSpell(new Position(caster.Location.X, caster.Location.Y, caster.Location.Z), PaladinSpells.ARCING_LIGHT_DAMAGE, args);
            }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(191587)]
public class AuraDkVirulentPlague : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleEffectRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var removeMode = TargetApplication.RemoveMode;

        if (removeMode == AuraRemoveMode.Death)
        {
            var caster = Caster;

            if (caster != null)
                caster.SpellFactory.CastSpell(Target, DeathKnightSpells.VIRULENT_ERUPTION, true);
        }
    }
}
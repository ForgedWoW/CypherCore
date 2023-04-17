// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemNitroBoostsBackfire : AuraScript, IHasAuraEffects
{
    private double _lastZ = MapConst.InvalidHeight;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicDummy, 1, AuraType.PeriodicTriggerSpell));
    }

    private void HandleApply(AuraEffect effect, AuraEffectHandleModes mode)
    {
        _lastZ = Target.Location.Z;
    }

    private void HandlePeriodicDummy(AuraEffect effect)
    {
        PreventDefaultAction();
        var curZ = Target.Location.Z;

        if (curZ < _lastZ)
        {
            if (RandomHelper.randChance(80)) // we don't have enough sniffs to verify this, guesstimate
                Target.SpellFactory.CastSpell(Target, ItemSpellIds.NITRO_BOOSTS_PARACHUTE, new CastSpellExtraArgs(effect));

            Aura.Remove();
        }
        else
            _lastZ = curZ;
    }
}
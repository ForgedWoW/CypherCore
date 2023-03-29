// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_nitro_boosts_backfire : AuraScript, IHasAuraEffects
{
    private double lastZ = MapConst.InvalidHeight;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicDummy, 1, AuraType.PeriodicTriggerSpell));
    }

    private void HandleApply(AuraEffect effect, AuraEffectHandleModes mode)
    {
        lastZ = Target.Location.Z;
    }

    private void HandlePeriodicDummy(AuraEffect effect)
    {
        PreventDefaultAction();
        var curZ = Target.Location.Z;

        if (curZ < lastZ)
        {
            if (RandomHelper.randChance(80)) // we don't have enough sniffs to verify this, guesstimate
                Target.CastSpell(Target, ItemSpellIds.NitroBoostsParachute, new CastSpellExtraArgs(effect));

            Aura.Remove();
        }
        else
        {
            lastZ = curZ;
        }
    }
}
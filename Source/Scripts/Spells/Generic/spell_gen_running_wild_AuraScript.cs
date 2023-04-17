// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenRunningWildAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleMount, 1, AuraType.Mounted, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
    }

    private void HandleMount(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        PreventDefaultAction();

        target.Mount(SharedConst.DisplayIdHiddenMount, 0, 0);

        // cast speed aura
        var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(aurEff.Amount);

        if (mountCapability != null)
            target.SpellFactory.CastSpell(target, mountCapability.ModSpellAuraID, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 69641 - Gryphon/Wyvern Pet - Mounting Check Aura
internal class SpellGenGryphonWyvernMountCheck : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        var target = Target;
        var owner = target.OwnerUnit;

        if (owner == null)
            return;

        if (owner.IsMounted)
            target.SetDisableGravity(true);
        else
            target.SetDisableGravity(false);
    }
}
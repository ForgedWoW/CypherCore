// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 189063 - Lightning Vortex (proc 185881 Item - Shaman T18 Elemental 4P Bonus)
[SpellScript(189063)]
internal class SpellShaItemT18Elemental4PBonus : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(DiminishHaste, 1, AuraType.PeriodicDummy));
    }

    private void DiminishHaste(AuraEffect aurEff)
    {
        PreventDefaultAction();
        var hasteBuff = GetEffect(0);

        hasteBuff?.ChangeAmount(hasteBuff.Amount - aurEff.Amount);
    }
}
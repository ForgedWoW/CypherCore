// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(152279)]
public class SpellDkBreathOfSindragosa : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicTriggerSpell));
    }

    private void OnTick(AuraEffect unnamedParameter)
    {
        var lCaster = Caster;

        if (lCaster == null)
            return;

        var lPlayer = lCaster.AsPlayer;

        if (lPlayer == null)
            return;

        lCaster.ModifyPower(PowerType.RunicPower, -130);
        /*if (l_Caster->ToPlayer())
                l_Caster->ToPlayer()->SendPowerUpdate(PowerType.RunicPower, l_Caster->GetPower(PowerType.RunicPower));*/

        if (lCaster.GetPower(PowerType.RunicPower) <= 130)
            lCaster.RemoveAura(DeathKnightSpells.BREATH_OF_SINDRAGOSA);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

//210378
[SpellScript(210378)]
public class aura_darkest_before_the_dawn : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
    }

    private void OnTick(AuraEffect UnnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var dawnTrigger = caster.GetAura(PaladinSpells.DARKEST_BEFORE_THE_DAWN);

        if (dawnTrigger != null)
            caster.AddAura(PaladinSpells.DARKEST_BEFORE_THE_DAWN_BUFF, caster);
    }
}
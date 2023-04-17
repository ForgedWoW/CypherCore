// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

//210378
[SpellScript(210378)]
public class AuraDarkestBeforeTheDawn : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
    }

    private void OnTick(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var dawnTrigger = caster.GetAura(PaladinSpells.DARKEST_BEFORE_THE_DAWN);

        if (dawnTrigger != null)
            caster.AddAura(PaladinSpells.DARKEST_BEFORE_THE_DAWN_BUFF, caster);
    }
}
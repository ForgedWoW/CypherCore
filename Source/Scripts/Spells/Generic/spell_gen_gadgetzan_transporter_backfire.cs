// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenGadgetzanTransporterBackfire : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var r = RandomHelper.IRand(0, 119);

        if (r < 20) // Transporter Malfunction - 1/6 polymorph
            caster.SpellFactory.CastSpell(caster, GenericSpellIds.TRANSPORTER_MALFUNCTION_POLYMORPH, true);
        else if (r < 100) // Evil Twin               - 4/6 evil twin
            caster.SpellFactory.CastSpell(caster, GenericSpellIds.TRANSPORTER_EVILTWIN, true);
        else // Transporter Malfunction - 1/6 miss the Target
            caster.SpellFactory.CastSpell(caster, GenericSpellIds.TRANSPORTER_MALFUNCTION_MISS, true);
    }
}
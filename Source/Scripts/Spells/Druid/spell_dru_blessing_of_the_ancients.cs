// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(202360)]
public class SpellDruBlessingOfTheAncients : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }


    private void HandleDummy(int effIndex)
    {
        var removeAura = Caster.HasAura(DruidSpells.BlessingOfElune) ? (uint)DruidSpells.BlessingOfElune : (uint)DruidSpells.BlessingOfAnshe;
        var addAura = Caster.HasAura(DruidSpells.BlessingOfElune) ? (uint)DruidSpells.BlessingOfAnshe : (uint)DruidSpells.BlessingOfElune;

        Caster.RemoveAura(removeAura);
        Caster.SpellFactory.CastSpell(null, addAura, true);
    }
}
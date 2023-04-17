// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(208244)]
public class SpellRogRollTheBonesVisualSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(Prevent, (byte)255, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
    }


    private void Prevent(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.AsPlayer)
        {
            PreventHitAura();
            PreventHitDamage();
            PreventHitDefaultEffect(effIndex);
            PreventHitEffect(effIndex);
        }
    }
}
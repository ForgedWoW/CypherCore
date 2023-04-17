// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[]
{
    703, 1833, 8676
})]
public class SpellRogCloakAndDaggerSpellScript : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (caster != null)
            if (caster.HasAuraType(AuraType.ModStealth))
                if (caster.HasAura(138106))
                {
                    var target = ExplTargetUnit;

                    if (target != null)
                        caster.SpellFactory.CastSpell(target, 138916, true);
                }

        return SpellCastResult.SpellCastOk;
    }
}
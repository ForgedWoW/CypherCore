// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(211881)]
public class SpellDhFelEruption : SpellScript, ISpellBeforeHit, ISpellOnHit
{
    public void BeforeHit(SpellMissInfo missInfo)
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null || target == null)
            return;

        if (missInfo == SpellMissInfo.Immune || missInfo == SpellMissInfo.Immune2)
            caster.SpellFactory.CastSpell(target, DemonHunterSpells.FEL_ERUPTION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, 2));
    }

    public void OnHit()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null || target == null)
            return;

        caster.SpellFactory.CastSpell(target, DemonHunterSpells.FEL_ERUPTION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, 1));
    }
}
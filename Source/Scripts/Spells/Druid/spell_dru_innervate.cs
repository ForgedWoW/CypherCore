// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 29166 - Innervate
internal class SpellDruInnervate : SpellScript, ISpellCheckCast, ISpellOnHit
{
    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit?.AsPlayer;

        if (target == null)
            return SpellCastResult.BadTargets;

        var spec = CliDB.ChrSpecializationStorage.LookupByKey(target.GetPrimarySpecialization());

        if (spec == null ||
            spec.Role != 1)
            return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }

    public void OnHit()
    {
        var caster = Caster;

        if (caster != HitUnit)
        {
            var innervateR2 = caster.GetAuraEffect(DruidSpellIds.InnervateRank2, 0);

            if (innervateR2 != null)
                caster.SpellFactory.CastSpell(caster,
                                 DruidSpellIds.Innervate,
                                 new CastSpellExtraArgs(TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress)
                                     .SetTriggeringSpell(Spell)
                                     .AddSpellMod(SpellValueMod.BasePoint0, -innervateR2.Amount));
        }
    }
}
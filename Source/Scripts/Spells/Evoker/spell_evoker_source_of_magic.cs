// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_SOURCE_OF_MAGIC)]
public class SpellEvokerSourceOfMagic : SpellScript, ISpellCheckCast, ISpellOnCast
{
    public SpellCastResult CheckCast()
    {
        if (ExplTargetUnit == null || !ExplTargetUnit.TryGetAsPlayer(out var player) || !Caster.IsInPartyWith(player) || player.GetPrimarySpecialization() == 0 || CliDB.ChrSpecializationStorage.LookupByKey(player.GetPrimarySpecialization()).Role != 1)
            return SpellCastResult.NoValidTargets;

        return SpellCastResult.SpellCastOk;
    }

    public void OnCast()
    {
        ExplTargetUnit.AddAura(EvokerSpells.BLUE_SOURCE_OF_MAGIC);
        var aura = Caster.AddAura(EvokerSpells.BLUE_SOURCE_OF_MAGIC_AURA);

        aura.ForEachAuraScript<IAuraScriptValues>(a =>
        {
            if (a.ScriptValues.TryGetValue("target", out var unit))
                ((Unit)unit).RemoveAura(EvokerSpells.BLUE_SOURCE_OF_MAGIC);

            a.ScriptValues["target"] = ExplTargetUnit;
        });
    }
}
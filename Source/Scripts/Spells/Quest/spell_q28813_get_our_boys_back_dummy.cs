// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 93072 - Get Our Boys Back Dummy
internal class SpellQ28813GetOurBoysBackDummy : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;
        var injuredStormwindInfantry = caster.FindNearestCreature(CreatureIds.INJURED_STORMWIND_INFANTRY, 5.0f, true);

        if (injuredStormwindInfantry)
        {
            injuredStormwindInfantry.SetCreatorGUID(caster.GUID);
            injuredStormwindInfantry.SpellFactory.CastSpell(injuredStormwindInfantry, QuestSpellIds.RENEWED_LIFE, true);
        }
    }
}
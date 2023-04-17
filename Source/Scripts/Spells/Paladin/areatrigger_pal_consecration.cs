// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 26573 - Consecration
[Script] //  9228 - AreaTriggerId
internal class AreatriggerPalConsecration : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster != null)
        {
            // 243597 is also being cast as protection, but CreateObject is not sent, either serverside areatrigger for this aura or unused - also no visual is seen
            if (unit == caster &&
                caster.IsPlayer &&
                caster.AsPlayer.GetPrimarySpecialization() == TalentSpecialization.PaladinProtection)
                caster.SpellFactory.CastSpell(caster, PaladinSpells.CONSECRATION_PROTECTION_AURA);

            if (caster.IsValidAttackTarget(unit))
                if (caster.HasAura(PaladinSpells.CONSECRATED_GROUND_PASSIVE))
                    caster.SpellFactory.CastSpell(unit, PaladinSpells.CONSECRATED_GROUND_SLOW);
        }
    }

    public void OnUnitExit(Unit unit)
    {
        if (At.CasterGuid == unit.GUID)
            unit.RemoveAurasDueToSpell(PaladinSpells.CONSECRATION_PROTECTION_AURA, At.CasterGuid);

        unit.RemoveAurasDueToSpell(PaladinSpells.CONSECRATED_GROUND_SLOW, At.CasterGuid);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(217200)]
public class SpellHunBarbedShot : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            caster.SpellFactory.CastSpell(caster, HunterSpells.BARBED_SHOT_PLAYERAURA, true);

            if (caster.IsPlayer)
            {
                Unit pet = caster.GetGuardianPet();

                if (pet != null)
                {
                    if (!pet)
                        return;

                    caster.SpellFactory.CastSpell(pet, HunterSpells.BARBED_SHOT_PETAURA, true);
                }
            }
        }
    }
}
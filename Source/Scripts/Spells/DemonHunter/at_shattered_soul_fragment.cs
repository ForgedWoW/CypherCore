// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[Script]
public class AtShatteredSoulFragment : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit unit)
    {
        if (unit != At.GetCaster() || !unit.IsPlayer || unit.AsPlayer.Class != PlayerClass.DemonHunter)
            return;

        switch (At.Entry)
        {
            case 10665:
                if (At.GetCaster().AsPlayer.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
                    At.GetCaster().SpellFactory.CastSpell(At.GetCaster(), ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_25_HAVOC, true);

                At.Remove();

                break;

            case 10666:
                if (At.GetCaster().AsPlayer.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
                    At.GetCaster().SpellFactory.CastSpell(At.GetCaster(), ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_25_HAVOC, true);

                At.Remove();

                break;
        }
    }
}
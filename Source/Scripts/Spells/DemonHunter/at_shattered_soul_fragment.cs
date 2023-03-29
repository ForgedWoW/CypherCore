// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_shattered_soul_fragment : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit unit)
    {
        if (unit != At.GetCaster() || !unit.IsPlayer || unit.AsPlayer.Class != PlayerClass.DemonHunter)
            return;

        switch (At.Entry)
        {
            case 10665:
                if (At.GetCaster().AsPlayer.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
                    At.GetCaster().CastSpell(At.GetCaster(), ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_25_HAVOC, true);

                At.Remove();

                break;

            case 10666:
                if (At.GetCaster().AsPlayer.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
                    At.GetCaster().CastSpell(At.GetCaster(), ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_25_HAVOC, true);

                At.Remove();

                break;
        }
    }
}
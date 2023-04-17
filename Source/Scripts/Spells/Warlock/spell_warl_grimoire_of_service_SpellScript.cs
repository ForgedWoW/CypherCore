// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Grimoire of Service summons - 111859, 111895, 111896, 111897, 111898
[SpellScript(new uint[]
{
    111859, 111895, 111896, 111897, 111898
})]
public class SpellWarlGrimoireOfServiceSpellScript : SpellScript, ISpellOnSummon
{
    public enum EServiceSpells
    {
        ImpSingeMagic = 89808,
        VoidwalkerSuffering = 17735,
        SuccubusSeduction = 6358,
        FelhunterLock = 19647,
        FelguardAxeToss = 89766
    }

    public void OnSummon(Creature creature)
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null ||
            creature == null ||
            target == null)
            return;

        switch (SpellInfo.Id)
        {
            case WarlockSpells.GRIMOIRE_IMP: // Imp
                creature.SpellFactory.CastSpell(caster, (uint)EServiceSpells.ImpSingeMagic, true);

                break;
            case WarlockSpells.GRIMOIRE_VOIDWALKER: // Voidwalker
                creature.SpellFactory.CastSpell(target, (uint)EServiceSpells.VoidwalkerSuffering, true);

                break;
            case WarlockSpells.GRIMOIRE_SUCCUBUS: // Succubus
                creature.SpellFactory.CastSpell(target, (uint)EServiceSpells.SuccubusSeduction, true);

                break;
            case WarlockSpells.GRIMOIRE_FELHUNTER: // Felhunter
                creature.SpellFactory.CastSpell(target, (uint)EServiceSpells.FelhunterLock, true);

                break;
            case WarlockSpells.GRIMOIRE_FELGUARD: // Felguard
                creature.SpellFactory.CastSpell(target, (uint)EServiceSpells.FelguardAxeToss, true);

                break;
        }
    }
}
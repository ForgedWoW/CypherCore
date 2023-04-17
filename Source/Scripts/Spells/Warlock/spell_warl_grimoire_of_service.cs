// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Grimoire of Service summons - 111859, 111895, 111896, 111897, 111898
[SpellScript(new uint[]
{
    111859, 111895, 111896, 111897, 111898
})]
public class SpellWarlGrimoireOfService : SpellScript, IHasSpellEffects, ISpellOnSummon
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void OnSummon(Creature creature)
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null || creature == null || target == null)
            return;

        switch (SpellInfo.Id)
        {
            case WarlockSpells.GRIMOIRE_IMP: // Imp
                creature.SpellFactory.CastSpell(caster, EServiceSpells.IMP_SINGE_MAGIC, true);

                break;
            case WarlockSpells.GRIMOIRE_VOIDWALKER: // Voidwalker
                creature.SpellFactory.CastSpell(target, EServiceSpells.VOIDWALKER_SUFFERING, true);

                break;
            case WarlockSpells.GRIMOIRE_SUCCUBUS: // Succubus
                creature.SpellFactory.CastSpell(target, EServiceSpells.SUCCUBUS_SEDUCTION, true);

                break;
            case WarlockSpells.GRIMOIRE_FELHUNTER: // Felhunter
                creature.SpellFactory.CastSpell(target, EServiceSpells.FELHUNTER_LOCK, true);

                break;
            case WarlockSpells.GRIMOIRE_FELGUARD: // Felguard
                creature.SpellFactory.CastSpell(target, EServiceSpells.FELGUARD_AXE_TOSS, true);

                break;
        }
    }

    private struct EServiceSpells
    {
        public const uint IMP_SINGE_MAGIC = 89808;
        public const uint VOIDWALKER_SUFFERING = 17735;
        public const uint SUCCUBUS_SEDUCTION = 6358;
        public const uint FELHUNTER_LOCK = 19647;
        public const uint FELGUARD_AXE_TOSS = 89766;
    }
}
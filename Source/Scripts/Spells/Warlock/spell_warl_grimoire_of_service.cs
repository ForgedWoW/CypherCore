// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Grimoire of Service summons - 111859, 111895, 111896, 111897, 111898
[SpellScript(new uint[]
{
    111859, 111895, 111896, 111897, 111898
})]
public class spell_warl_grimoire_of_service : SpellScript, IHasSpellEffects, ISpellOnSummon
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
                creature.CastSpell(caster, eServiceSpells.IMP_SINGE_MAGIC, true);

                break;
            case WarlockSpells.GRIMOIRE_VOIDWALKER: // Voidwalker
                creature.CastSpell(target, eServiceSpells.VOIDWALKER_SUFFERING, true);

                break;
            case WarlockSpells.GRIMOIRE_SUCCUBUS: // Succubus
                creature.CastSpell(target, eServiceSpells.SUCCUBUS_SEDUCTION, true);

                break;
            case WarlockSpells.GRIMOIRE_FELHUNTER: // Felhunter
                creature.CastSpell(target, eServiceSpells.FELHUNTER_LOCK, true);

                break;
            case WarlockSpells.GRIMOIRE_FELGUARD: // Felguard
                creature.CastSpell(target, eServiceSpells.FELGUARD_AXE_TOSS, true);

                break;
        }
    }

    private struct eServiceSpells
    {
        public const uint IMP_SINGE_MAGIC = 89808;
        public const uint VOIDWALKER_SUFFERING = 17735;
        public const uint SUCCUBUS_SEDUCTION = 6358;
        public const uint FELHUNTER_LOCK = 19647;
        public const uint FELGUARD_AXE_TOSS = 89766;
    }
}
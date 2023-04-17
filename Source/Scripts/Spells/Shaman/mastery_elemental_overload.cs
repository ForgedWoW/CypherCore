// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//168534
[Script]
public class MasteryElementalOverload : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
    public MasteryElementalOverload() : base("mastery_elemental_overload") { }
    public PlayerClass PlayerClass { get; } = PlayerClass.Shaman;

    public void OnSpellCast(Player player, Spell spell, bool unnamedParameter)
    {
        if (player.GetPrimarySpecialization() != TalentSpecialization.ShamanElemental)
            return;

        if (player.HasAura(ShamanSpells.MASTERY_ELEMENTAL_OVERLOAD) && RandomHelper.randChance(15))
        {
            var spellInfo = spell.SpellInfo;

            if (spellInfo != null)
                switch (spell.SpellInfo.Id)
                {
                    case ShamanSpells.LIGHTNING_BOLT_ELEM:
                        player.SpellFactory.CastSpell(player.SelectedUnit, ShamanSpells.LIGHTNING_BOLT_ELEM, true);

                        break;
                    case ShamanSpells.ELEMENTAL_BLAST:
                        player.SpellFactory.CastSpell(player.SelectedUnit, ShamanSpells.ELEMENTAL_BLAST, true);

                        break;
                    case ShamanSpells.LAVA_BURST:
                        player.SpellFactory.CastSpell(player.SelectedUnit, ShamanSpells.LAVA_BURST, true);

                        break;
                    case ShamanSpells.CHAIN_LIGHTNING:
                        player.SpellFactory.CastSpell(player.SelectedUnit, ShamanSpells.LAVA_BURST, true);

                        break;
                }
        }
    }
}
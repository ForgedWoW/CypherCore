// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Divine Purpose Proc
// Called by Seal of Light - 202273, Justicar's Vengeance - 215661, Word of Glory - 210191, Divine Storm - 53385, Templar's Verdict - 85256
// Called by Holy Shock - 20473, Light of Dawn - 85222
[SpellScript(new uint[]
{
    202273, 215661, 210191, 53385, 85256, 20473, 85222
})]
public class spell_pal_divine_purpose_proc : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasSpell(PaladinSpells.DIVINE_PURPOSE_RET) || player.HasSpell(PaladinSpells.DIVINE_PURPOSE_HOLY))
            {
                var spec = player.GetPrimarySpecialization();
                var activateSpell = SpellInfo.Id;

                switch (spec)
                {
                    case TalentSpecialization.PaladinRetribution:
                    {
                        if (RandomHelper.randChance(20))
                            if (activateSpell == (uint)PaladinSpells.JUSTICARS_VENGEANCE || activateSpell == (uint)PaladinSpells.WORD_OF_GLORY || activateSpell == (uint)PaladinSpells.DIVINE_STORM || activateSpell == (uint)PaladinSpells.TEMPLARS_VERDICT)
                                player.CastSpell(player, PaladinSpells.DivinePurposeTriggerred);

                        break;
                    }
                    case TalentSpecialization.PaladinHoly:
                    {
                        if (RandomHelper.randChance(15))
                        {
                            if (activateSpell == (uint)PaladinSpells.HolyShock)
                            {
                                player.CastSpell(player, PaladinSpells.DIVINE_PURPOSE_HOLY_AURA_1);

                                if (player.SpellHistory.HasCooldown(PaladinSpells.HolyShock))
                                    player.SpellHistory.ResetCooldown(PaladinSpells.HolyShock, true);
                            }

                            if (activateSpell == (uint)PaladinSpells.LIGHT_OF_DAWN)
                            {
                                player.CastSpell(player, PaladinSpells.DIVINE_PURPOSE_HOLY_AURA_2);

                                if (player.SpellHistory.HasCooldown(PaladinSpells.LIGHT_OF_DAWN))
                                    player.SpellHistory.ResetCooldown(PaladinSpells.LIGHT_OF_DAWN, true);
                            }
                        }

                        break;
                    }
                }
            }
    }
}
﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(121253)]
public class spell_monk_keg_smash : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var _player = caster.AsPlayer;

            if (_player != null)
            {
                var target = HitUnit;

                if (target != null)
                {
                    _player.CastSpell(target, MonkSpells.KEG_SMASH_VISUAL, true);
                    _player.CastSpell(target, MonkSpells.WEAKENED_BLOWS, true);
                    _player.CastSpell(_player, MonkSpells.KEG_SMASH_ENERGIZE, true);

                    // Prevent to receive 2 CHI more than once time per cast
                    _player. // Prevent to receive 2 CHI more than once time per cast
                        SpellHistory.AddCooldown(MonkSpells.KEG_SMASH_ENERGIZE, 0, TimeSpan.FromSeconds(1));

                    _player.CastSpell(target, MonkSpells.DIZZYING_HAZE, true);
                }
            }
        }
    }
}
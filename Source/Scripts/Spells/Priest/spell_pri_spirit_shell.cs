// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(109964)]
public class SpellPriSpiritShell : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
                if (player.HasAura(PriestSpells.SPIRIT_SHELL_AURA))
                {
                    var bp = HitHeal;

                    HitHeal = 0;

                    var shell = player.GetAuraEffect(114908, 0);

                    if (shell != null)
                    {
                        shell.SetAmount(Math.Min(shell.Amount + bp, (int)player.CountPctFromMaxHealth(60)));
                    }
                    else
                    {
                        var args = new CastSpellExtraArgs();
                        args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
                        args.SetTriggerFlags(TriggerCastFlags.FullMask);
                        player.SpellFactory.CastSpell(target, PriestSpells.SPIRIT_SHELL_ABSORPTION, args);
                    }
                }
        }
    }
}
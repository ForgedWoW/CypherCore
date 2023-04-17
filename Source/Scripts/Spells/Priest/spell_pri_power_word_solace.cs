// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 129250 - Power Word: Solace
internal class SpellPriPowerWordSolace : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(RestoreMana, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
    }

    private void RestoreMana(int effIndex)
    {
        Caster
            .SpellFactory.CastSpell(Caster,
                       PriestSpells.POWER_WORD_SOLACE_ENERGIZE,
                       new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(Spell)
                                                                                    .AddSpellMod(SpellValueMod.BasePoint0, EffectValue / 100));
    }
}
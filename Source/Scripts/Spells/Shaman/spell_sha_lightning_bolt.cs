﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 188196 - Lightning Bolt
[SpellScript(188196)]
internal class spell_sha_lightning_bolt : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
    }

    private void HandleScript(int effIndex)
    {
        var energizeAmount = Caster.GetAuraEffect(ShamanSpells.MaelstromController, 0);

        if (energizeAmount != null)
            Caster
                .CastSpell(Caster,
                           ShamanSpells.LightningBoltEnergize,
                           new CastSpellExtraArgs(energizeAmount)
                               .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.Amount));
    }
}
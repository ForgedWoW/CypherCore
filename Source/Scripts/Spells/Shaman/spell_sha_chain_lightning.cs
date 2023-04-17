// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 188443 - Chain Lightning
[SpellScript(188443)]
internal class SpellShaChainLightning : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
    }

    private void HandleScript(int effIndex)
    {
        var energizeAmount = Caster.GetAuraEffect(ShamanSpells.MAELSTROM_CONTROLLER, 4);

        if (energizeAmount != null)
            Caster
                .SpellFactory.CastSpell(Caster,
                                        ShamanSpells.CHAIN_LIGHTNING_ENERGIZE,
                                        new CastSpellExtraArgs(energizeAmount)
                                            .AddSpellMod(SpellValueMod.BasePoint0, (int)(energizeAmount.Amount * GetUnitTargetCountForEffect(0))));
    }
}
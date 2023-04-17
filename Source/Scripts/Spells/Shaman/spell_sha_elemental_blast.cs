// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 120588 - Elemental Blast Overload
[SpellScript(120588)]
internal class SpellShaElementalBlast : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    private readonly uint[] _buffSpells =
    {
        ShamanSpells.ElementalBlastCrit, ShamanSpells.ElementalBlastHaste, ShamanSpells.ElementalBlastMastery
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        var caster = Caster;
        var spellId = _buffSpells.SelectRandomElementByWeight(buffSpellId => { return !caster.HasAura(buffSpellId) ? 1.0f : 0.0f; });

        Caster.SpellFactory.CastSpell(Caster, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEnergize, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
    }

    private void HandleEnergize(int effIndex)
    {
        var energizeAmount = Caster.GetAuraEffect(ShamanSpells.MAELSTROM_CONTROLLER, SpellInfo.Id == ShamanSpells.ElementalBlast ? 9 : 10);

        if (energizeAmount != null)
            Caster
                .SpellFactory.CastSpell(Caster,
                           ShamanSpells.ELEMENTAL_BLAST_ENERGIZE,
                           new CastSpellExtraArgs(energizeAmount)
                               .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.Amount));
    }
}
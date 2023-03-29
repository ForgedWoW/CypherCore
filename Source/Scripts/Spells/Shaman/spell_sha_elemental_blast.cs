// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 120588 - Elemental Blast Overload
[SpellScript(120588)]
internal class spell_sha_elemental_blast : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    private readonly uint[] BuffSpells =
    {
        ShamanSpells.ElementalBlastCrit, ShamanSpells.ElementalBlastHaste, ShamanSpells.ElementalBlastMastery
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        var caster = Caster;
        var spellId = BuffSpells.SelectRandomElementByWeight(buffSpellId => { return !caster.HasAura(buffSpellId) ? 1.0f : 0.0f; });

        Caster.CastSpell(Caster, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEnergize, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
    }

    private void HandleEnergize(int effIndex)
    {
        var energizeAmount = Caster.GetAuraEffect(ShamanSpells.MaelstromController, SpellInfo.Id == ShamanSpells.ElementalBlast ? 9 : 10);

        if (energizeAmount != null)
            Caster
                .CastSpell(Caster,
                           ShamanSpells.ElementalBlastEnergize,
                           new CastSpellExtraArgs(energizeAmount)
                               .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.Amount));
    }
}
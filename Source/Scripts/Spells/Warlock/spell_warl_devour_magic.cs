// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(new uint[]
{
    67518, 19505
})] // 67518, 19505 - Devour Magic
internal class SpellWarlDevourMagic : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(OnSuccessfulDispel, 0, SpellEffectName.Dispel, SpellScriptHookType.EffectSuccessfulDispel));
    }

    private void OnSuccessfulDispel(int effIndex)
    {
        var caster = Caster;
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint0, GetEffectInfo(1).CalcValue(caster));

        caster.SpellFactory.CastSpell(caster, WarlockSpells.DEVOUR_MAGIC_HEAL, args);

        // Glyph of Felhunter
        var owner = caster.OwnerUnit;

        if (owner)
            if (owner.GetAura(WarlockSpells.GLYPH_OF_DEMON_TRAINING) != null)
                owner.SpellFactory.CastSpell(owner, WarlockSpells.DEVOUR_MAGIC_HEAL, args);
    }
}
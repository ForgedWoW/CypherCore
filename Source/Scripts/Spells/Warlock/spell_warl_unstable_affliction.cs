// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(new uint[]
{
    30108, 34438, 34439, 35183
})] // 30108, 34438, 34439, 35183 - Unstable Affliction
internal class SpellWarlUnstableAffliction : AuraScript, IAfterAuraDispel
{
    public void HandleDispel(DispelInfo dispelInfo)
    {
        var caster = Caster;

        if (caster)
        {
            var aurEff = GetEffect(1);

            if (aurEff != null)
            {
                // backfire Damage and silence
                CastSpellExtraArgs args = new(aurEff);
                args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.Amount * 9);
                caster.SpellFactory.CastSpell(dispelInfo.Dispeller, WarlockSpells.UNSTABLE_AFFLICTION_DISPEL, args);
            }
        }
    }
}
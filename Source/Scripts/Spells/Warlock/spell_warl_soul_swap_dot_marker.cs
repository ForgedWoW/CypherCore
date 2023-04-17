// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(92795)] //! Soul Swap Copy Spells - 92795 - Simply copies spell IDs.
internal class SpellWarlSoulSwapDotMarker : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var swapVictim = Caster;
        var warlock = HitUnit;

        if (!warlock ||
            !swapVictim)
            return;

        SpellWarlSoulSwapOverride swapSpellScript = null;
        var swapOverrideAura = warlock.GetAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

        if (swapOverrideAura != null)
            swapSpellScript = swapOverrideAura.GetScript<SpellWarlSoulSwapOverride>();

        if (swapSpellScript == null)
            return;

        var classMask = EffectInfo.SpellClassMask;

        var appliedAuras = swapVictim.GetAppliedAurasQuery();

        foreach (var itr in appliedAuras.HasCasterGuid(warlock.GUID).HasSpellFamily(SpellFamilyNames.Warlock).GetResults())
        {
            var spellProto = itr.Base.SpellInfo;

            if (spellProto.SpellFamilyFlags & classMask)
                swapSpellScript.AddDot(itr.Base.Id);
        }

        swapSpellScript.SetOriginalSwapSource(swapVictim);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Suffering - 119907
[SpellScript(119907)]
public class SpellWarlSuffering : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var pet = caster.GetGuardianPet();

        if (caster == null || pet == null)
            return SpellCastResult.DontReport;

        if (pet.SpellHistory.HasCooldown(WarlockSpells.VOIDWALKER_SUFFERING))
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;
        var pet = caster.GetGuardianPet();

        if (caster == null || pet == null || target == null)
            return;

        /*if (pet->GetEntry() != PET_ENTRY_VOIDWALKER)
            return;*/

        pet.SpellFactory.CastSpell(target, WarlockSpells.VOIDWALKER_SUFFERING, true);
        caster.AsPlayer.SpellHistory.ModifyCooldown(SpellInfo.Id, TimeSpan.FromSeconds(10));
    }
}
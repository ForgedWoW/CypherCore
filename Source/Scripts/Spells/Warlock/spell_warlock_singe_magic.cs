// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 212623 - Singe Magic
[SpellScript(212623)]
public class spell_warlock_singe_magic : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (caster == null || !caster.AsPlayer)
            return SpellCastResult.BadTargets;

        if (caster.AsPlayer.CurrentPet && caster.AsPlayer.CurrentPet.Entry == 416)
            return SpellCastResult.SpellCastOk;

        return SpellCastResult.CantDoThatRightNow;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        var pet = caster.AsPlayer.CurrentPet;

        if (pet != null)
            pet.CastSpell(target, WarlockSpells.SINGE_MAGIC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)GetEffectInfo(0).BasePoints));
    }
}
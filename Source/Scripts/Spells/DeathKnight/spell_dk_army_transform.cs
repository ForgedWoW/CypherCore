// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script] // 127517 - Army Transform
internal class SpellDkArmyTransform : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsGuardian;
    }

    public SpellCastResult CheckCast()
    {
        var owner = Caster.OwnerUnit;

        if (owner)
            if (owner.HasAura(DeathKnightSpells.GlyphOfFoulMenagerie))
                return SpellCastResult.SpellCastOk;

        return SpellCastResult.SpellUnavailable;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        Caster.SpellFactory.CastSpell(Caster, DeathKnightSpells.ArmyTransforms.SelectRandom(), true);
    }
}
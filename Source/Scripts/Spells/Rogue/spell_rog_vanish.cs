// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 1856 - Vanish - VANISH
internal class SpellRogVanish : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(OnLaunchTarget, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
    }

    private void OnLaunchTarget(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        var target = HitUnit;

        target.RemoveAurasByType(AuraType.ModStalked);

        if (!target.IsPlayer)
            return;

        if (target.HasAura(RogueSpells.VanishAura))
            return;

        target.SpellFactory.CastSpell(target, RogueSpells.VanishAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        target.SpellFactory.CastSpell(target, RogueSpells.StealthShapeshiftAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }
}
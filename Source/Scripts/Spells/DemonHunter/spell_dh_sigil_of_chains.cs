// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[Script] // 208673 - Sigil of Chains
internal class SpellDhSigilOfChains : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEffectHitTarget(int effIndex)
    {
        var loc = ExplTargetDest;

        if (loc != null)
        {
            Caster.SpellFactory.CastSpell(HitUnit, DemonHunterSpells.SigilOfChainsSlow, new CastSpellExtraArgs(true));
            HitUnit.SpellFactory.CastSpell(loc, DemonHunterSpells.SIGIL_OF_CHAINS_GRIP, new CastSpellExtraArgs(true));
        }
    }
}
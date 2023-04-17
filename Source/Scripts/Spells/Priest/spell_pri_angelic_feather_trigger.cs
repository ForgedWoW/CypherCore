// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 121536 - Angelic Feather talent
internal class SpellPriAngelicFeatherTrigger : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleEffectDummy(int effIndex)
    {
        var destPos = HitDest;
        var radius = EffectInfo.CalcRadius();

        // Caster is prioritary
        if (Caster.IsWithinDist2d(destPos, radius))
            Caster.SpellFactory.CastSpell(Caster, PriestSpells.ANGELIC_FEATHER_AURA, true);
        else
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.CastDifficulty = CastDifficulty;
            Caster.SpellFactory.CastSpell(destPos, PriestSpells.ANGELIC_FEATHER_AREATRIGGER, args);
        }
    }
}
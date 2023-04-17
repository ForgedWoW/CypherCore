// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(108196)]
public class SpellDkDeathSiphon : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }


    private void HandleScriptEffect(int effIndex)
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (HitUnit)
            {
                var bp = HitDamage;
                var args = new CastSpellExtraArgs();
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
                args.SetTriggerFlags(TriggerCastFlags.FullMask);
                player.SpellFactory.CastSpell(player, DeathKnightSpells.DEATH_SIPHON_HEAL, args);
            }
    }
}
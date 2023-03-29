// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.SOAR_RACIAL)]
public class spell_evoker_soar : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHit(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        // Increase flight speed by 830540%
        caster.SetSpeedRate(UnitMoveType.Flight, 83054.0f);

        var player = HitPlayer;
        // Add "Skyward Ascent" and "Surge Forward" to the caster's spellbook
        player.LearnSpell(EvokerSpells.SKYWARD_ASCENT, false);
        player.LearnSpell(EvokerSpells.SURGE_FORWARD, false);
    }
}
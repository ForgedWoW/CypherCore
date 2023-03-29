// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_whisper_to_controller : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
    }

    private void HandleScript(int effIndex)
    {
        var casterSummon = Caster.ToTempSummon();

        if (casterSummon != null)
        {
            var target = casterSummon.GetSummonerUnit().AsPlayer;

            if (target != null)
                casterSummon.Whisper((uint)EffectValue, target, false);
        }
    }
}
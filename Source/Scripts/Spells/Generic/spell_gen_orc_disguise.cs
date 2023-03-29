﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_orc_disguise : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;
        var target = HitPlayer;

        if (target)
        {
            var gender = target.NativeGender;

            if (gender == Gender.Male)
                caster.CastSpell(target, GenericSpellIds.OrcDisguiseMale, true);
            else
                caster.CastSpell(target, GenericSpellIds.OrcDisguiseFemale, true);
        }
    }
}
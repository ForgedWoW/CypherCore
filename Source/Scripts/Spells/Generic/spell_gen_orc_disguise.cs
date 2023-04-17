// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenOrcDisguise : SpellScript, IHasSpellEffects
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
                caster.SpellFactory.CastSpell(target, GenericSpellIds.ORC_DISGUISE_MALE, true);
            else
                caster.SpellFactory.CastSpell(target, GenericSpellIds.ORC_DISGUISE_FEMALE, true);
        }
    }
}
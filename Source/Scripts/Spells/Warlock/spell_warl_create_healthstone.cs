// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(6201)] // 6201 - Create Healthstone
internal class spell_warl_create_healthstone : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        Caster.CastSpell(Caster, WarlockSpells.CREATE_HEALTHSTONE, true);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 342247 - Alter Time Active
internal class SpellMageAlterTimeActive : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(RemoveAlterTimeAura, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void RemoveAlterTimeAura(int effIndex)
    {
        var unit = Caster;
        unit.RemoveAura(MageSpells.ALTER_TIME_AURA, AuraRemoveMode.Expire);
        unit.RemoveAura(MageSpells.ARCANE_ALTER_TIME_AURA, AuraRemoveMode.Expire);
    }
}
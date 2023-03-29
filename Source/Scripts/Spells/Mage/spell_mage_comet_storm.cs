// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 153595 - Comet Storm (launch)
internal class spell_mage_comet_storm : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(EffectHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void EffectHit(int effIndex)
    {
        Caster.Events.AddEventAtOffset(new CometStormEvent(Caster, Spell.CastId, HitDest), RandomHelper.RandTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(275)));
    }
}
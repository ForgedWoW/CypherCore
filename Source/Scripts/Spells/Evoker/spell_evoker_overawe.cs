﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.OPPRESSING_ROAR)]
public class spell_evoker_overawe : SpellScript, IHasSpellEffects, ISpellAfterCast, ISpellBeforeCast
{
	int _dispells = 0;

    public List<ISpellEffect> SpellEffects { get; } = new();

    public void BeforeCast()
    {
        if (!Caster.HasAura(EvokerSpells.OVERAWE))
            Spell.SetSpellValue(Framework.Constants.SpellValueMod.BasePoint0, 0f);
    }

    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.TryGetAura(EvokerSpells.OVERAWE, out var aura))
            player.SpellHistory.ModifyCooldown(EvokerSpells.OPPRESSING_ROAR, TimeSpan.FromSeconds(-(aura.SpellInfo.GetEffect(0).BasePoints * _dispells)));
    }

    private void OnSuccessfulDispel(int obj)
    {
        _dispells++;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(OnSuccessfulDispel, 0, SpellEffectName.Dispel, SpellScriptHookType.EffectSuccessfulDispel));
    }
}
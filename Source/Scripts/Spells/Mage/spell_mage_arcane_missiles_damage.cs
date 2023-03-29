﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(7268)]
public class spell_mage_arcane_missiles_damage : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectTargetSelectHandler(CheckTarget, 0, Targets.UnitChannelTarget));
    }

    private void CheckTarget(WorldObject target)
    {
        if (target == Caster)
            target = null;
    }
}
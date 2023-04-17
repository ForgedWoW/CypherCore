// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 45438 - Ice Block
internal class SpellMageIceBlock : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectTargetSelectHandler(PreventStunWithEverwarmSocks, 0, Targets.UnitCaster));
        SpellEffects.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 5, Targets.UnitCaster));
        SpellEffects.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 6, Targets.UnitCaster));
    }

    private void PreventStunWithEverwarmSocks(WorldObject target)
    {
        if (Caster.HasAura(MageSpells.EVERWARM_SOCKS))
            target = null;
    }

    private void PreventEverwarmSocks(WorldObject target)
    {
        if (!Caster.HasAura(MageSpells.EVERWARM_SOCKS))
            target = null;
    }
}
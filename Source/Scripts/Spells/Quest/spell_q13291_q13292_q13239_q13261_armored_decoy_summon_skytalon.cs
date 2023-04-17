// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 59303 - Summon Frost Wyrm
internal class SpellQ13291Q13292Q13239Q13261ArmoredDecoySummonSkytalon : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCasterBack));
    }

    private void SetDest(SpellDestination dest)
    {
        // Adjust effect summon position
        Position offset = new(0.0f, 0.0f, 20.0f, 0.0f);
        dest.RelocateOffset(offset);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Spells.Auras;

namespace Forged.MapServer.Spells;

public class SpellFlatModifierByLabel : SpellModifier
{
    public SpellFlatModByLabel Value = new();

    public SpellFlatModifierByLabel(Aura ownerAura) : base(ownerAura) { }
}
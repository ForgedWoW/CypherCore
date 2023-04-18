// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Spells.Auras;

namespace Forged.MapServer.Spells;

internal class SpellPctModifierByLabel : SpellModifier
{
    public SpellPctModifierByLabel(Aura ownerAura) : base(ownerAura) { }
    public SpellPctModByLabel Value { get; set; } = new();
}
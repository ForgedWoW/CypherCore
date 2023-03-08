// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Spells;

public class SpellFlatModifierByLabel : SpellModifier
{
	public SpellFlatModByLabel Value = new();

	public SpellFlatModifierByLabel(Aura ownerAura) : base(ownerAura) { }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Spells;

class SpellPctModifierByLabel : SpellModifier
{
	public SpellPctModByLabel Value = new();

	public SpellPctModifierByLabel(Aura ownerAura) : base(ownerAura) { }
}
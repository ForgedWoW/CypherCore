// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Spells;

public class SpellModifier
{
	public SpellModOp Op { get; set; }
	public SpellModType Type { get; set; }
	public uint SpellId { get; set; }
	public Aura OwnerAura { get; set; }

	public SpellModifier(Aura ownerAura)
	{
		Op = SpellModOp.HealingAndDamage;
		Type = SpellModType.Flat;
		SpellId = 0;
		OwnerAura = ownerAura;
	}
}
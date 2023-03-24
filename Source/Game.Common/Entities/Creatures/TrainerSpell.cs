// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Entities.Creatures;

public class TrainerSpell
{
	public uint SpellId { get; set; }
	public uint MoneyCost { get; set; }
	public uint ReqSkillLine { get; set; }
	public uint ReqSkillRank { get; set; }
	public Array<uint> ReqAbility { get; set; } = new(3);
	public byte ReqLevel { get; set; }

	public bool IsCastable()
	{
		return Global.SpellMgr.GetSpellInfo(SpellId, Difficulty.None).HasEffect(SpellEffectName.LearnSpell);
	}
}

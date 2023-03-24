// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Entities.Players;

public class SpecializationInfo
{
	public Dictionary<uint, PlayerSpellState>[] Talents { get; set; } = new Dictionary<uint, PlayerSpellState>[PlayerConst.MaxSpecializations];
	public uint[][] PvpTalents { get; set; } = new uint[PlayerConst.MaxSpecializations][];
	public List<uint>[] Glyphs { get; set; } = new List<uint>[PlayerConst.MaxSpecializations];
	public uint ResetTalentsCost { get; set; }
	public long ResetTalentsTime { get; set; }
	public byte ActiveGroup { get; set; }

	public SpecializationInfo()
	{
		for (byte i = 0; i < PlayerConst.MaxSpecializations; ++i)
		{
			Talents[i] = new Dictionary<uint, PlayerSpellState>();
			PvpTalents[i] = new uint[PlayerConst.MaxPvpTalentSlots];
			Glyphs[i] = new List<uint>();
		}
	}
}

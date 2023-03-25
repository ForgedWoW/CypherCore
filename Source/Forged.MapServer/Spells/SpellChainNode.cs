// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells;

public class SpellChainNode
{
	public SpellInfo Prev;
	public SpellInfo Next;
	public SpellInfo First;
	public SpellInfo Last;
	public byte Rank;
}
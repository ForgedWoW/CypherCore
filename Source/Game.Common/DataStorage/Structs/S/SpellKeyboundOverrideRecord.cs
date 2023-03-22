// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpellKeyboundOverrideRecord
{
	public uint Id;
	public string Function;
	public sbyte Type;
	public uint Data;
	public int Flags;
}
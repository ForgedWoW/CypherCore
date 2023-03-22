﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class CharacterLoadoutRecord
{
	public uint Id;
	public long RaceMask;
	public sbyte ChrClassID;
	public int Purpose;
	public sbyte ItemContext;

	public bool IsForNewCharacter()
	{
		return Purpose == 9;
	}
}
﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class ToyRecord
{
	public string SourceText;
	public uint Id;
	public uint ItemID;
	public byte Flags;
	public sbyte SourceTypeEnum;
}
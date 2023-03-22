﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpellReagentsCurrencyRecord
{
	public uint Id;
	public int SpellID;
	public ushort CurrencyTypesID;
	public ushort CurrencyCount;
}
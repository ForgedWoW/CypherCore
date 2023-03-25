// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public struct ColumnCompressionData_Pallet
{
	public int BitOffset;
	public int BitWidth;
	public int Cardinality;
}
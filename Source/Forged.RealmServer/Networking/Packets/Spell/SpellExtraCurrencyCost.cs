// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct SpellExtraCurrencyCost
{
	public int CurrencyID;
	public int Count;

	public void Read(WorldPacket data)
	{
		CurrencyID = data.ReadInt32();
		Count = data.ReadInt32();
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public class TaxiPathBySourceAndDestination
{
	public uint Id;
	public uint price;

	public TaxiPathBySourceAndDestination(uint _id, uint _price)
	{
		Id = _id;
		price = _price;
	}
}
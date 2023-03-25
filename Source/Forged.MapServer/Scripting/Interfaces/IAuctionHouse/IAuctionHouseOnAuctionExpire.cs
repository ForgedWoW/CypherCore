// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AuctionHouse;

namespace Forged.MapServer.Scripting.Interfaces.IAuctionHouse;

public interface IAuctionHouseOnAuctionExpire : IScriptObject
{
	void OnAuctionExpire(AuctionHouseObject ah, AuctionPosting auction);
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AuctionPostingServerFlag
{
	None = 0x0,
	GmLogBuyer = 0x1 // write transaction to gm log file for buyer (optimization flag - avoids querying database for offline player permissions)
}
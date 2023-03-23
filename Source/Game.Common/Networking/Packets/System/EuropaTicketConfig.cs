// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.System;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.System;

public struct EuropaTicketConfig
{
	public bool TicketsEnabled;
	public bool BugsEnabled;
	public bool ComplaintsEnabled;
	public bool SuggestionsEnabled;

	public SavedThrottleObjectState ThrottleState;

	public void Write(WorldPacket data)
	{
		data.WriteBit(TicketsEnabled);
		data.WriteBit(BugsEnabled);
		data.WriteBit(ComplaintsEnabled);
		data.WriteBit(SuggestionsEnabled);

		ThrottleState.Write(data);
	}
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Query;

public class QueryTimeResponse : ServerPacket
{
	public long CurrentTime;
	public QueryTimeResponse() : base(ServerOpcodes.QueryTimeResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt64(CurrentTime);
	}
}

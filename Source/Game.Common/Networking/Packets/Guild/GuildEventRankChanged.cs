// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Guild;

public class GuildEventRankChanged : ServerPacket
{
	public uint RankID;
	public GuildEventRankChanged() : base(ServerOpcodes.GuildEventRankChanged) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(RankID);
	}
}

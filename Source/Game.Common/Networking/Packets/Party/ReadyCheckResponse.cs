// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Party;

public class ReadyCheckResponse : ServerPacket
{
	public ObjectGuid PartyGUID;
	public ObjectGuid Player;
	public bool IsReady;
	public ReadyCheckResponse() : base(ServerOpcodes.ReadyCheckResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PartyGUID);
		_worldPacket.WritePackedGuid(Player);

		_worldPacket.WriteBit(IsReady);
		_worldPacket.FlushBits();
	}
}

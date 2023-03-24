// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Party;

public class ReadyCheckStarted : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid PartyGUID;
	public ObjectGuid InitiatorGUID;
	public uint Duration;
	public ReadyCheckStarted() : base(ServerOpcodes.ReadyCheckStarted) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(PartyGUID);
		_worldPacket.WritePackedGuid(InitiatorGUID);
		_worldPacket.WriteUInt32(Duration);
	}
}

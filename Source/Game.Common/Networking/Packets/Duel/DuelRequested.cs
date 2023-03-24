// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Duel;

public class DuelRequested : ServerPacket
{
	public ObjectGuid ArbiterGUID;
	public ObjectGuid RequestedByGUID;
	public ObjectGuid RequestedByWowAccount;
	public DuelRequested() : base(ServerOpcodes.DuelRequested, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ArbiterGUID);
		_worldPacket.WritePackedGuid(RequestedByGUID);
		_worldPacket.WritePackedGuid(RequestedByWowAccount);
	}
}

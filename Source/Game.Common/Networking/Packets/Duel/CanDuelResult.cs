// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Duel;

public class CanDuelResult : ServerPacket
{
	public ObjectGuid TargetGUID;
	public bool Result;
	public CanDuelResult() : base(ServerOpcodes.CanDuelResult) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TargetGUID);
		_worldPacket.WriteBit(Result);
		_worldPacket.FlushBits();
	}
}

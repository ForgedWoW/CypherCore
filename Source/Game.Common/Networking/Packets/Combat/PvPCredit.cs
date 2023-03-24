// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Combat;

public class PvPCredit : ServerPacket
{
	public int OriginalHonor;
	public int Honor;
	public ObjectGuid Target;
	public uint Rank;
	public PvPCredit() : base(ServerOpcodes.PvpCredit) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(OriginalHonor);
		_worldPacket.WriteInt32(Honor);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WriteUInt32(Rank);
	}
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.VoidStorage;

public class VoidTransferResult : ServerPacket
{
	public VoidTransferError Result;

	public VoidTransferResult(VoidTransferError result) : base(ServerOpcodes.VoidTransferResult, ConnectionType.Instance)
	{
		Result = result;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Result);
	}
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Instance;

public class PendingRaidLock : ServerPacket
{
	public int TimeUntilLock;
	public uint CompletedMask;
	public bool Extending;
	public bool WarningOnly;
	public PendingRaidLock() : base(ServerOpcodes.PendingRaidLock) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(TimeUntilLock);
		_worldPacket.WriteUInt32(CompletedMask);
		_worldPacket.WriteBit(Extending);
		_worldPacket.WriteBit(WarningOnly);
		_worldPacket.FlushBits();
	}
}

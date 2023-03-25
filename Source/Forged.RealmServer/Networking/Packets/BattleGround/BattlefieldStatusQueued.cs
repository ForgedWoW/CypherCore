// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class BattlefieldStatusQueued : ServerPacket
{
	public uint AverageWaitTime;
	public BattlefieldStatusHeader Hdr = new();
	public bool AsGroup;
	public bool SuspendedQueue;
	public bool EligibleForMatchmaking;
	public uint WaitTime;
	public int Unused920;
	public BattlefieldStatusQueued() : base(ServerOpcodes.BattlefieldStatusQueued) { }

	public override void Write()
	{
		Hdr.Write(_worldPacket);
		_worldPacket.WriteUInt32(AverageWaitTime);
		_worldPacket.WriteUInt32(WaitTime);
		_worldPacket.WriteInt32(Unused920);
		_worldPacket.WriteBit(AsGroup);
		_worldPacket.WriteBit(EligibleForMatchmaking);
		_worldPacket.WriteBit(SuspendedQueue);
		_worldPacket.FlushBits();
	}
}
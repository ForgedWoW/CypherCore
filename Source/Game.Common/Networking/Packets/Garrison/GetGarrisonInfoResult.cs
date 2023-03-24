// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Garrison;

public class GetGarrisonInfoResult : ServerPacket
{
	public uint FactionIndex;
	public List<GarrisonInfo> Garrisons = new();
	public List<FollowerSoftCapInfo> FollowerSoftCaps = new();
	public GetGarrisonInfoResult() : base(ServerOpcodes.GetGarrisonInfoResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(FactionIndex);
		_worldPacket.WriteInt32(Garrisons.Count);
		_worldPacket.WriteInt32(FollowerSoftCaps.Count);

		foreach (var followerSoftCapInfo in FollowerSoftCaps)
			followerSoftCapInfo.Write(_worldPacket);

		foreach (var garrison in Garrisons)
			garrison.Write(_worldPacket);
	}
}

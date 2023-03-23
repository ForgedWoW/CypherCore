// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Groups;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Party;

public class RaidMarkersChanged : ServerPacket
{
	public sbyte PartyIndex;
	public uint ActiveMarkers;

	public List<RaidMarker> RaidMarkers = new();
	public RaidMarkersChanged() : base(ServerOpcodes.RaidMarkersChanged) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WriteUInt32(ActiveMarkers);

		_worldPacket.WriteBits(RaidMarkers.Count, 4);
		_worldPacket.FlushBits();

		foreach (var raidMarker in RaidMarkers)
		{
			_worldPacket.WritePackedGuid(raidMarker.TransportGUID);
			_worldPacket.WriteUInt32(raidMarker.Location.MapId);
			_worldPacket.WriteXYZ(raidMarker.Location);
		}
	}
}

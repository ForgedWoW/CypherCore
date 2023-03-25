// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Taxi;

public class ShowTaxiNodes : ServerPacket
{
	public ShowTaxiNodesWindowInfo? WindowInfo;
	public byte[] CanLandNodes = null; // Nodes known by player
	public byte[] CanUseNodes = null;  // Nodes available for use - this can temporarily disable a known node
	public ShowTaxiNodes() : base(ServerOpcodes.ShowTaxiNodes) { }

	public override void Write()
	{
		_worldPacket.WriteBit(WindowInfo.HasValue);
		_worldPacket.FlushBits();

		_worldPacket.WriteInt32(CanLandNodes.Length);
		_worldPacket.WriteInt32(CanUseNodes.Length);

		if (WindowInfo.HasValue)
		{
			_worldPacket.WritePackedGuid(WindowInfo.Value.UnitGUID);
			_worldPacket.WriteInt32(WindowInfo.Value.CurrentNode);
		}

		foreach (var node in CanLandNodes)
			_worldPacket.WriteUInt8(node);

		foreach (var node in CanUseNodes)
			_worldPacket.WriteUInt8(node);
	}
}
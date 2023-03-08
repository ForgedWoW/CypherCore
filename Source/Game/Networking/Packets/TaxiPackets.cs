// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class TaxiNodeStatusQuery : ClientPacket
{
	public ObjectGuid UnitGUID;
	public TaxiNodeStatusQuery(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		UnitGUID = _worldPacket.ReadPackedGuid();
	}
}

class TaxiNodeStatusPkt : ServerPacket
{
	public TaxiNodeStatus Status; // replace with TaxiStatus enum
	public ObjectGuid Unit;
	public TaxiNodeStatusPkt() : base(ServerOpcodes.TaxiNodeStatus) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteBits(Status, 2);
		_worldPacket.FlushBits();
	}
}

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

class EnableTaxiNode : ClientPacket
{
	public ObjectGuid Unit;
	public EnableTaxiNode(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Unit = _worldPacket.ReadPackedGuid();
	}
}

class TaxiQueryAvailableNodes : ClientPacket
{
	public ObjectGuid Unit;
	public TaxiQueryAvailableNodes(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Unit = _worldPacket.ReadPackedGuid();
	}
}

class ActivateTaxi : ClientPacket
{
	public ObjectGuid Vendor;
	public uint Node;
	public uint GroundMountID;
	public uint FlyingMountID;
	public ActivateTaxi(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Vendor = _worldPacket.ReadPackedGuid();
		Node = _worldPacket.ReadUInt32();
		GroundMountID = _worldPacket.ReadUInt32();
		FlyingMountID = _worldPacket.ReadUInt32();
	}
}

class NewTaxiPath : ServerPacket
{
	public NewTaxiPath() : base(ServerOpcodes.NewTaxiPath) { }

	public override void Write() { }
}

class ActivateTaxiReplyPkt : ServerPacket
{
	public ActivateTaxiReply Reply;
	public ActivateTaxiReplyPkt() : base(ServerOpcodes.ActivateTaxiReply) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Reply, 4);
		_worldPacket.FlushBits();
	}
}

class TaxiRequestEarlyLanding : ClientPacket
{
	public TaxiRequestEarlyLanding(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public struct ShowTaxiNodesWindowInfo
{
	public ObjectGuid UnitGUID;
	public int CurrentNode;
}
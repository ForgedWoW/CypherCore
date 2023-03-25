// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.WorldState;

public class UpdateWorldState : ServerPacket
{
	public int Value;
	public bool Hidden; // @todo: research
	public uint VariableID;
	public UpdateWorldState() : base(ServerOpcodes.UpdateWorldState, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(VariableID);
		_worldPacket.WriteInt32(Value);
		_worldPacket.WriteBit(Hidden);
		_worldPacket.FlushBits();
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Update;

public class UpdateObject : ServerPacket
{
	public uint NumObjUpdates;
	public ushort MapID;
	public byte[] Data;
	public UpdateObject() : base(ServerOpcodes.UpdateObject, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(NumObjUpdates);
		_worldPacket.WriteUInt16(MapID);
		_worldPacket.WriteBytes(Data);
	}
}

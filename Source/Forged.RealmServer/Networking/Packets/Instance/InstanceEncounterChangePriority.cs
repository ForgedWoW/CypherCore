// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class InstanceEncounterChangePriority : ServerPacket
{
	public ObjectGuid Unit;
	public byte TargetFramePriority; // used to update the position of the unit's current frame
	public InstanceEncounterChangePriority() : base(ServerOpcodes.InstanceEncounterChangePriority, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt8(TargetFramePriority);
	}
}
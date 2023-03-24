// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Party;

public class RoleChangedInform : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid From;
	public ObjectGuid ChangedUnit;
	public int OldRole;
	public int NewRole;
	public RoleChangedInform() : base(ServerOpcodes.RoleChangedInform) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(From);
		_worldPacket.WritePackedGuid(ChangedUnit);
		_worldPacket.WriteInt32(OldRole);
		_worldPacket.WriteInt32(NewRole);
	}
}

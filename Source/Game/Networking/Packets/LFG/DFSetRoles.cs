// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class DFSetRoles : ClientPacket
{
	public LfgRoles RolesDesired;
	public byte PartyIndex;
	public DFSetRoles(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RolesDesired = (LfgRoles)_worldPacket.ReadUInt32();
		PartyIndex = _worldPacket.ReadUInt8();
	}
}
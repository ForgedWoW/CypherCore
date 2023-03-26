// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyInviteClient : ClientPacket
{
	public byte PartyIndex;
	public uint ProposedRoles;
	public string TargetName;
	public string TargetRealm;
	public ObjectGuid TargetGUID;
	public PartyInviteClient(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();

		var targetNameLen = _worldPacket.ReadBits<uint>(9);
		var targetRealmLen = _worldPacket.ReadBits<uint>(9);

		ProposedRoles = _worldPacket.ReadUInt32();
		TargetGUID = _worldPacket.ReadPackedGuid();

		TargetName = _worldPacket.ReadString(targetNameLen);
		TargetRealm = _worldPacket.ReadString(targetRealmLen);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyInviteClient : ClientPacket
{
    public byte PartyIndex;
    public uint ProposedRoles;
    public ObjectGuid TargetGUID;
    public string TargetName;
    public string TargetRealm;
    public PartyInviteClient(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = WorldPacket.ReadUInt8();

        var targetNameLen = WorldPacket.ReadBits<uint>(9);
        var targetRealmLen = WorldPacket.ReadBits<uint>(9);

        ProposedRoles = WorldPacket.ReadUInt32();
        TargetGUID = WorldPacket.ReadPackedGuid();

        TargetName = WorldPacket.ReadString(targetNameLen);
        TargetRealm = WorldPacket.ReadString(targetRealmLen);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildSetMemberNote : ClientPacket
{
    public bool IsPublic;
    // 0 == Officer, 1 == Public
    public string Note;

    public ObjectGuid NoteeGUID;
    public GuildSetMemberNote(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        NoteeGUID = WorldPacket.ReadPackedGuid();

        var noteLen = WorldPacket.ReadBits<uint>(8);
        IsPublic = WorldPacket.HasBit();

        Note = WorldPacket.ReadString(noteLen);
    }
}
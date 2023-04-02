// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Ticket;

public class SubmitUserFeedback : ClientPacket
{
    public SupportTicketHeader Header;
    public bool IsSuggestion;
    public string Note;
    public SubmitUserFeedback(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Header.Read(WorldPacket);
        var noteLength = WorldPacket.ReadBits<uint>(24);
        IsSuggestion = WorldPacket.HasBit();

        if (noteLength != 0)
            Note = WorldPacket.ReadString(noteLength - 1);
    }
}
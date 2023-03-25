// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class SubmitUserFeedback : ClientPacket
{
	public SupportTicketHeader Header;
	public string Note;
	public bool IsSuggestion;

	public SubmitUserFeedback(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Header.Read(_worldPacket);
		var noteLength = _worldPacket.ReadBits<uint>(24);
		IsSuggestion = _worldPacket.HasBit();

		if (noteLength != 0)
			Note = _worldPacket.ReadString(noteLength - 1);
	}
}
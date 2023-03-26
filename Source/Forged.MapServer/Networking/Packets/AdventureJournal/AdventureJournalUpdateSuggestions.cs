// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.AdventureJournal;

internal class AdventureJournalUpdateSuggestions : ClientPacket
{
	public bool OnLevelUp;
	public AdventureJournalUpdateSuggestions(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		OnLevelUp = _worldPacket.HasBit();
	}
}
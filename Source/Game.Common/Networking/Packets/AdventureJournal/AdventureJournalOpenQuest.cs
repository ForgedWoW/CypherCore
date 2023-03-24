﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.AdventureJournal;

public class AdventureJournalOpenQuest : ClientPacket
{
	public uint AdventureJournalID;
	public AdventureJournalOpenQuest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AdventureJournalID = _worldPacket.ReadUInt32();
	}
}

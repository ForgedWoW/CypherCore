// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class AdventureJournalDataResponse : ServerPacket
{
	public bool OnLevelUp;
	public List<AdventureJournalEntry> AdventureJournalDatas = new();
	public AdventureJournalDataResponse() : base(ServerOpcodes.AdventureJournalDataResponse) { }

	public override void Write()
	{
		_worldPacket.WriteBit(OnLevelUp);
		_worldPacket.FlushBits();
		_worldPacket.WriteInt32(AdventureJournalDatas.Count);

		foreach (var adventureJournal in AdventureJournalDatas)
		{
			_worldPacket.WriteInt32(adventureJournal.AdventureJournalID);
			_worldPacket.WriteInt32(adventureJournal.Priority);
		}
	}
}
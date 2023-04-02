// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AdventureJournal;

internal class AdventureJournalDataResponse : ServerPacket
{
    public List<AdventureJournalEntry> AdventureJournalDatas = new();
    public bool OnLevelUp;
    public AdventureJournalDataResponse() : base(ServerOpcodes.AdventureJournalDataResponse) { }

    public override void Write()
    {
        WorldPacket.WriteBit(OnLevelUp);
        WorldPacket.FlushBits();
        WorldPacket.WriteInt32(AdventureJournalDatas.Count);

        foreach (var adventureJournal in AdventureJournalDatas)
        {
            WorldPacket.WriteInt32(adventureJournal.AdventureJournalID);
            WorldPacket.WriteInt32(adventureJournal.Priority);
        }
    }
}
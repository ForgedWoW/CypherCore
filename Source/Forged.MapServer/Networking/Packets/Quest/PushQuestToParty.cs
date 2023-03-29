// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Quest;

internal class PushQuestToParty : ClientPacket
{
    public uint QuestID;
    public PushQuestToParty(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        QuestID = _worldPacket.ReadUInt32();
    }
}
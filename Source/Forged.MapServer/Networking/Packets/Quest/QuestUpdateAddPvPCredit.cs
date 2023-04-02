﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestUpdateAddPvPCredit : ServerPacket
{
    public ushort Count;
    public uint QuestID;
    public QuestUpdateAddPvPCredit() : base(ServerOpcodes.QuestUpdateAddPvpCredit, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(QuestID);
        _worldPacket.WriteUInt16(Count);
    }
}
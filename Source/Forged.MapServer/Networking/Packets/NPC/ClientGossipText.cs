﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Networking.Packets.NPC;

public class ClientGossipText
{
    public uint ContentTuningID;
    public uint QuestFlags;
    public uint QuestFlagsEx;
    public uint QuestID;
    public string QuestTitle;
    public int QuestType;
    public bool Repeatable;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(QuestID);
        data.WriteUInt32(ContentTuningID);
        data.WriteInt32(QuestType);
        data.WriteUInt32(QuestFlags);
        data.WriteUInt32(QuestFlagsEx);

        data.WriteBit(Repeatable);
        data.WriteBits(QuestTitle.GetByteCount(), 9);
        data.FlushBits();

        data.WriteString(QuestTitle);
    }
}
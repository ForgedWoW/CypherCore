// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverRequestItems : ServerPacket
{
    public bool AutoLaunched;
    public List<QuestObjectiveCollect> Collect = new();
    public uint CompEmoteDelay;
    public uint CompEmoteType;
    public string CompletionText = "";
    public List<ConditionalQuestText> ConditionalCompletionText = new();
    public List<QuestCurrency> Currency = new();
    public int MoneyToGet;
    public uint[] QuestFlags = new uint[3];
    public uint QuestGiverCreatureID;
    public ObjectGuid QuestGiverGUID;
    public uint QuestID;
    public string QuestTitle = "";
    public int StatusFlags;
    public uint SuggestPartyMembers;
    public QuestGiverRequestItems() : base(ServerOpcodes.QuestGiverRequestItems) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(QuestGiverGUID);
        WorldPacket.WriteUInt32(QuestGiverCreatureID);
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WriteUInt32(CompEmoteDelay);
        WorldPacket.WriteUInt32(CompEmoteType);
        WorldPacket.WriteUInt32(QuestFlags[0]);
        WorldPacket.WriteUInt32(QuestFlags[1]);
        WorldPacket.WriteUInt32(QuestFlags[2]);
        WorldPacket.WriteUInt32(SuggestPartyMembers);
        WorldPacket.WriteInt32(MoneyToGet);
        WorldPacket.WriteInt32(Collect.Count);
        WorldPacket.WriteInt32(Currency.Count);
        WorldPacket.WriteInt32(StatusFlags);

        foreach (var obj in Collect)
        {
            WorldPacket.WriteUInt32(obj.ObjectID);
            WorldPacket.WriteInt32(obj.Amount);
            WorldPacket.WriteUInt32(obj.Flags);
        }

        foreach (var cur in Currency)
        {
            WorldPacket.WriteUInt32(cur.CurrencyID);
            WorldPacket.WriteInt32(cur.Amount);
        }

        WorldPacket.WriteBit(AutoLaunched);
        WorldPacket.FlushBits();

        WorldPacket.WriteUInt32(QuestGiverCreatureID);
        WorldPacket.WriteInt32(ConditionalCompletionText.Count);

        WorldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
        WorldPacket.WriteBits(CompletionText.GetByteCount(), 12);
        WorldPacket.FlushBits();

        foreach (var conditionalQuestText in ConditionalCompletionText)
            conditionalQuestText.Write(WorldPacket);

        WorldPacket.WriteString(QuestTitle);
        WorldPacket.WriteString(CompletionText);
    }
}
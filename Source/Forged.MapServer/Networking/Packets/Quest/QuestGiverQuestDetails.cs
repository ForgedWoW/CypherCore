// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverQuestDetails : ServerPacket
{
    public bool AutoLaunched;
    public List<ConditionalQuestText> ConditionalDescriptionText = new();
    public List<QuestDescEmote> DescEmotes = new();
    public string DescriptionText = "";
    public bool DisplayPopup;
    public ObjectGuid InformUnit;
    public List<uint> LearnSpells = new();
    public string LogDescription = "";
    public List<QuestObjectiveSimple> Objectives = new();
    public uint PortraitGiver;
    public int PortraitGiverModelSceneID;
    public uint PortraitGiverMount;
    public string PortraitGiverName = "";
    public string PortraitGiverText = "";
    public uint PortraitTurnIn;
    public string PortraitTurnInName = "";
    public string PortraitTurnInText = "";
    public uint[] QuestFlags = new uint[3];
    public int QuestGiverCreatureID;
    public ObjectGuid QuestGiverGUID;
    public uint QuestID;
    public int QuestPackageID;
    public int QuestSessionBonus;
    public int QuestStartItemID;
    public string QuestTitle = "";
    public QuestRewards Rewards = new();
    public bool StartCheat;
    public uint SuggestedPartyMembers;
    public QuestGiverQuestDetails() : base(ServerOpcodes.QuestGiverQuestDetails) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(QuestGiverGUID);
        WorldPacket.WritePackedGuid(InformUnit);
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WriteInt32(QuestPackageID);
        WorldPacket.WriteUInt32(PortraitGiver);
        WorldPacket.WriteUInt32(PortraitGiverMount);
        WorldPacket.WriteInt32(PortraitGiverModelSceneID);
        WorldPacket.WriteUInt32(PortraitTurnIn);
        WorldPacket.WriteUInt32(QuestFlags[0]); // Flags
        WorldPacket.WriteUInt32(QuestFlags[1]); // FlagsEx
        WorldPacket.WriteUInt32(QuestFlags[2]); // FlagsEx
        WorldPacket.WriteUInt32(SuggestedPartyMembers);
        WorldPacket.WriteInt32(LearnSpells.Count);
        WorldPacket.WriteInt32(DescEmotes.Count);
        WorldPacket.WriteInt32(Objectives.Count);
        WorldPacket.WriteInt32(QuestStartItemID);
        WorldPacket.WriteInt32(QuestSessionBonus);
        WorldPacket.WriteInt32(QuestGiverCreatureID);
        WorldPacket.WriteInt32(ConditionalDescriptionText.Count);

        foreach (var spell in LearnSpells)
            WorldPacket.WriteUInt32(spell);

        foreach (var emote in DescEmotes)
        {
            WorldPacket.WriteInt32(emote.Type);
            WorldPacket.WriteUInt32(emote.Delay);
        }

        foreach (var obj in Objectives)
        {
            WorldPacket.WriteUInt32(obj.Id);
            WorldPacket.WriteInt32(obj.ObjectID);
            WorldPacket.WriteInt32(obj.Amount);
            WorldPacket.WriteUInt8(obj.Type);
        }

        WorldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
        WorldPacket.WriteBits(DescriptionText.GetByteCount(), 12);
        WorldPacket.WriteBits(LogDescription.GetByteCount(), 12);
        WorldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
        WorldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
        WorldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
        WorldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
        WorldPacket.WriteBit(AutoLaunched);
        WorldPacket.WriteBit(false); // unused in client
        WorldPacket.WriteBit(StartCheat);
        WorldPacket.WriteBit(DisplayPopup);
        WorldPacket.FlushBits();

        Rewards.Write(WorldPacket);

        WorldPacket.WriteString(QuestTitle);
        WorldPacket.WriteString(DescriptionText);
        WorldPacket.WriteString(LogDescription);
        WorldPacket.WriteString(PortraitGiverText);
        WorldPacket.WriteString(PortraitGiverName);
        WorldPacket.WriteString(PortraitTurnInText);
        WorldPacket.WriteString(PortraitTurnInName);

        foreach (var conditionalQuestText in ConditionalDescriptionText)
            conditionalQuestText.Write(WorldPacket);
    }
}
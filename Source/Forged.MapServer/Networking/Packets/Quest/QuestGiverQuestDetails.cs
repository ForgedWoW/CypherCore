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
        _worldPacket.WritePackedGuid(QuestGiverGUID);
        _worldPacket.WritePackedGuid(InformUnit);
        _worldPacket.WriteUInt32(QuestID);
        _worldPacket.WriteInt32(QuestPackageID);
        _worldPacket.WriteUInt32(PortraitGiver);
        _worldPacket.WriteUInt32(PortraitGiverMount);
        _worldPacket.WriteInt32(PortraitGiverModelSceneID);
        _worldPacket.WriteUInt32(PortraitTurnIn);
        _worldPacket.WriteUInt32(QuestFlags[0]); // Flags
        _worldPacket.WriteUInt32(QuestFlags[1]); // FlagsEx
        _worldPacket.WriteUInt32(QuestFlags[2]); // FlagsEx
        _worldPacket.WriteUInt32(SuggestedPartyMembers);
        _worldPacket.WriteInt32(LearnSpells.Count);
        _worldPacket.WriteInt32(DescEmotes.Count);
        _worldPacket.WriteInt32(Objectives.Count);
        _worldPacket.WriteInt32(QuestStartItemID);
        _worldPacket.WriteInt32(QuestSessionBonus);
        _worldPacket.WriteInt32(QuestGiverCreatureID);
        _worldPacket.WriteInt32(ConditionalDescriptionText.Count);

        foreach (var spell in LearnSpells)
            _worldPacket.WriteUInt32(spell);

        foreach (var emote in DescEmotes)
        {
            _worldPacket.WriteInt32(emote.Type);
            _worldPacket.WriteUInt32(emote.Delay);
        }

        foreach (var obj in Objectives)
        {
            _worldPacket.WriteUInt32(obj.Id);
            _worldPacket.WriteInt32(obj.ObjectID);
            _worldPacket.WriteInt32(obj.Amount);
            _worldPacket.WriteUInt8(obj.Type);
        }

        _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
        _worldPacket.WriteBits(DescriptionText.GetByteCount(), 12);
        _worldPacket.WriteBits(LogDescription.GetByteCount(), 12);
        _worldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
        _worldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
        _worldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
        _worldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
        _worldPacket.WriteBit(AutoLaunched);
        _worldPacket.WriteBit(false); // unused in client
        _worldPacket.WriteBit(StartCheat);
        _worldPacket.WriteBit(DisplayPopup);
        _worldPacket.FlushBits();

        Rewards.Write(_worldPacket);

        _worldPacket.WriteString(QuestTitle);
        _worldPacket.WriteString(DescriptionText);
        _worldPacket.WriteString(LogDescription);
        _worldPacket.WriteString(PortraitGiverText);
        _worldPacket.WriteString(PortraitGiverName);
        _worldPacket.WriteString(PortraitTurnInText);
        _worldPacket.WriteString(PortraitTurnInName);

        foreach (var conditionalQuestText in ConditionalDescriptionText)
            conditionalQuestText.Write(_worldPacket);
    }
}
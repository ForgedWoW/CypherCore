// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Collections;

namespace Forged.MapServer.Networking.Packets.Petition;

public class PetitionInfo
{
    public int AllowedClasses;
    public short AllowedGender;
    public int AllowedGuildID;
    public int AllowedMaxLevel;
    public int AllowedMinLevel;
    public int AllowedRaces;
    public string BodyText;
    public StringArray Choicetext = new(10);
    public int DeadLine;
    public int IssueDate;
    public uint MaxSignatures;
    public uint MinSignatures;
    public uint Muid = 0;
    public int NumChoices;
    public ObjectGuid Petitioner;
    public int PetitionID;
    public int StaticType;
    public string Title;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(PetitionID);
        data.WritePackedGuid(Petitioner);

        data.WriteUInt32(MinSignatures);
        data.WriteUInt32(MaxSignatures);
        data.WriteInt32(DeadLine);
        data.WriteInt32(IssueDate);
        data.WriteInt32(AllowedGuildID);
        data.WriteInt32(AllowedClasses);
        data.WriteInt32(AllowedRaces);
        data.WriteInt16(AllowedGender);
        data.WriteInt32(AllowedMinLevel);
        data.WriteInt32(AllowedMaxLevel);
        data.WriteInt32(NumChoices);
        data.WriteInt32(StaticType);
        data.WriteUInt32(Muid);

        data.WriteBits(Title.GetByteCount(), 7);
        data.WriteBits(BodyText.GetByteCount(), 12);

        for (byte i = 0; i < Choicetext.Length; i++)
            data.WriteBits(Choicetext[i].GetByteCount(), 6);

        data.FlushBits();

        for (byte i = 0; i < Choicetext.Length; i++)
            data.WriteString(Choicetext[i]);

        data.WriteString(Title);
        data.WriteString(BodyText);
    }
}
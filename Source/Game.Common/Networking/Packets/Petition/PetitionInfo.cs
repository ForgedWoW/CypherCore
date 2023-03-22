// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Collections;
using Game.Entities;

namespace Game.Networking.Packets;

public class PetitionInfo
{
	public int PetitionID;
	public ObjectGuid Petitioner;
	public string Title;
	public string BodyText;
	public uint MinSignatures;
	public uint MaxSignatures;
	public int DeadLine;
	public int IssueDate;
	public int AllowedGuildID;
	public int AllowedClasses;
	public int AllowedRaces;
	public short AllowedGender;
	public int AllowedMinLevel;
	public int AllowedMaxLevel;
	public int NumChoices;
	public int StaticType;
	public uint Muid = 0;
	public StringArray Choicetext = new(10);

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
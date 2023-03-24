// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class ConversationData : BaseUpdateData<Conversation>
{
	public UpdateField<bool> DontPlayBroadcastTextSounds = new(0, 1);
	public UpdateField<List<ConversationLine>> Lines = new(0, 2);
	public DynamicUpdateField<ConversationActorField> Actors = new(0, 3);
	public UpdateField<uint> LastLineEndTime = new(0, 4);
	public UpdateField<uint> Progress = new(0, 5);
	public UpdateField<uint> Flags = new(0, 6);

	public ConversationData() : base(0, TypeId.Conversation, 7) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
	{
		data.WriteInt32(Lines.GetValue().Count);
		data.WriteUInt32(GetViewerLastLineEndTime(this, owner, receiver));
		data.WriteUInt32(Progress);

		for (var i = 0; i < Lines.GetValue().Count; ++i)
			Lines.GetValue()[i].WriteCreate(data, owner, receiver);

		data.WriteBit(DontPlayBroadcastTextSounds);
		data.WriteInt32(Actors.Size());

		for (var i = 0; i < Actors.Size(); ++i)
			Actors[i].WriteCreate(data, owner, receiver);

		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Conversation owner, Player receiver)
	{
		data.WriteBits(ChangesMask.GetBlock(0), 6);

		if (ChangesMask[0])
		{
			if (ChangesMask[1])
				data.WriteBit(DontPlayBroadcastTextSounds);

			if (changesMask[2])
			{
				List<ConversationLine> list = Lines;
				data.WriteBits(list.Count, 32);

				for (var i = 0; i < list.Count; ++i)
					list[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
			}
		}

		data.FlushBits();

		if (ChangesMask[0])
			if (ChangesMask[3])
			{
				if (!ignoreNestedChangesMask)
					Actors.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Actors.Size(), data);
			}

		data.FlushBits();

		if (ChangesMask[0])
		{
			if (ChangesMask[3])
				for (var i = 0; i < Actors.Size(); ++i)
					if (Actors.HasChanged(i) || ignoreNestedChangesMask)
						Actors[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (ChangesMask[4])
				data.WriteUInt32(GetViewerLastLineEndTime(this, owner, receiver));

			if (ChangesMask[5])
				data.WriteUInt32(Progress);
		}

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(DontPlayBroadcastTextSounds);
		ClearChangesMask(Lines);
		ClearChangesMask(Actors);
		ClearChangesMask(LastLineEndTime);
		ClearChangesMask(Progress);
		ChangesMask.ResetAll();
	}

	public uint GetViewerLastLineEndTime(ConversationData conversationLineData, Conversation conversation, Player receiver)
	{
		var locale = receiver.Session.SessionDbLocaleIndex;

		return (uint)conversation.GetLastLineEndTime(locale).TotalMilliseconds;
	}
}

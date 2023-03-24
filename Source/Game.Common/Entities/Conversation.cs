// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;
using Game.Common.Networking;

namespace Game.Common.Entities;

public class Conversation : WorldObject
{
	readonly ConversationData m_conversationData;
	readonly Position _stationaryPosition = new();
	readonly Dictionary<(Locale locale, uint lineId), TimeSpan> _lineStartTimes = new();
	readonly TimeSpan[] _lastLineEndTimes = new TimeSpan[(int)Locale.Total];
	ObjectGuid _creatorGuid;
	TimeSpan _duration;
	uint _textureKitId;

	public override ObjectGuid OwnerGUID => GetCreatorGuid();

	public override uint Faction => 0;

	public override float StationaryX => _stationaryPosition.X;

	public override float StationaryY => _stationaryPosition.Y;

	public override float StationaryZ => _stationaryPosition.Z;

	public override float StationaryO => _stationaryPosition.Orientation;

	public Conversation() : base(false)
	{
		ObjectTypeMask |= TypeMask.Conversation;
		ObjectTypeId = TypeId.Conversation;

		_updateFlag.Stationary = true;
		_updateFlag.Conversation = true;

		m_conversationData = new ConversationData();
	}

	public override void AddToWorld()
	{
		//- Register the Conversation for guid lookup and for caster
		if (!IsInWorld)
		{
			Map.ObjectsStore.TryAdd(GUID, this);
			base.AddToWorld();
		}
	}

	public override void RemoveFromWorld()
	{
		//- Remove the Conversation from the accessor and from all lists of objects in world
		if (IsInWorld)
		{
			base.RemoveFromWorld();
			Map.ObjectsStore.TryRemove(GUID, out _);
		}
	}

	public override void Update(uint diff)
	{
		if (GetDuration() > TimeSpan.FromMilliseconds(diff))
		{
			_duration -= TimeSpan.FromMilliseconds(diff);

			DoWithSuppressingObjectUpdates(() =>
			{
				// Only sent in CreateObject
				ApplyModUpdateFieldValue(Values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Progress), diff, true);
				m_conversationData.ClearChanged(m_conversationData.Progress);
			});
		}
		else
		{
			Remove(); // expired

			return;
		}

		base.Update(diff);
	}

	public void Remove()
	{
		if (IsInWorld)
			AddObjectToRemoveList(); // calls RemoveFromWorld
	}

	public static Conversation CreateConversation(uint conversationEntry, Unit creator, Position pos, ObjectGuid privateObjectOwner, SpellInfo spellInfo = null, bool autoStart = true)
	{
		var conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);

		if (conversationTemplate == null)
			return null;

		var lowGuid = creator.Map.GenerateLowGuid(HighGuid.Conversation);

		Conversation conversation = new();
		conversation.Create(lowGuid, conversationEntry, creator.Map, creator, pos, privateObjectOwner, spellInfo);

		if (autoStart && !conversation.Start())
			return null;

		return conversation;
	}

	public void AddActor(int actorId, uint actorIdx, ObjectGuid actorGuid)
	{
		ConversationActorField actorField = Values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Actors, (int)actorIdx);
		SetUpdateFieldValue(ref actorField.CreatureID, 0u);
		SetUpdateFieldValue(ref actorField.CreatureDisplayInfoID, 0u);
		SetUpdateFieldValue(ref actorField.ActorGUID, actorGuid);
		SetUpdateFieldValue(ref actorField.Id, actorId);
		SetUpdateFieldValue(ref actorField.Type, ConversationActorType.WorldObject);
		SetUpdateFieldValue(ref actorField.NoActorObject, 0u);
	}

	public void AddActor(int actorId, uint actorIdx, ConversationActorType type, uint creatureId, uint creatureDisplayInfoId)
	{
		ConversationActorField actorField = Values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Actors, (int)actorIdx);
		SetUpdateFieldValue(ref actorField.CreatureID, creatureId);
		SetUpdateFieldValue(ref actorField.CreatureDisplayInfoID, creatureDisplayInfoId);
		SetUpdateFieldValue(ref actorField.ActorGUID, ObjectGuid.Empty);
		SetUpdateFieldValue(ref actorField.Id, actorId);
		SetUpdateFieldValue(ref actorField.Type, type);
		SetUpdateFieldValue(ref actorField.NoActorObject, type == ConversationActorType.WorldObject ? 1 : 0u);
	}

	public TimeSpan GetLineStartTime(Locale locale, int lineId)
	{
		return _lineStartTimes.LookupByKey((locale, lineId));
	}

	public TimeSpan GetLastLineEndTime(Locale locale)
	{
		return _lastLineEndTimes[(int)locale];
	}

	public uint GetScriptId()
	{
		return Global.ConversationDataStorage.GetConversationTemplate(Entry).ScriptId;
	}

	public override void BuildValuesCreate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		ObjectData.WriteCreate(buffer, flags, this, target);
		m_conversationData.WriteCreate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteUInt8((byte)flags);
		data.WriteBytes(buffer);
	}

	public override void BuildValuesUpdate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

		if (Values.HasChanged(TypeId.Object))
			ObjectData.WriteUpdate(buffer, flags, this, target);

		if (Values.HasChanged(TypeId.Conversation))
			m_conversationData.WriteUpdate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void ClearUpdateMask(bool remove)
	{
		Values.ClearChangesMask(m_conversationData);
		base.ClearUpdateMask(remove);
	}

	public uint GetTextureKitId()
	{
		return _textureKitId;
	}

	public ObjectGuid GetCreatorGuid()
	{
		return _creatorGuid;
	}

	void Create(ulong lowGuid, uint conversationEntry, Map map, Unit creator, Position pos, ObjectGuid privateObjectOwner, SpellInfo spellInfo = null)
	{
		var conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);
		//ASSERT(conversationTemplate);

		_creatorGuid = creator.GUID;
		PrivateObjectOwner = privateObjectOwner;

		Map = map;
		Location.Relocate(pos);
		RelocateStationaryPosition(pos);

		Create(ObjectGuid.Create(HighGuid.Conversation, Location.MapId, conversationEntry, lowGuid));
		PhasingHandler.InheritPhaseShift(this, creator);

		UpdatePositionData();
		SetZoneScript();

		Entry = conversationEntry;
		ObjectScale = 1.0f;

		_textureKitId = conversationTemplate.TextureKitId;

		foreach (var actor in conversationTemplate.Actors)
			new ConversationActorFillVisitor(this, creator, map, actor).Invoke(actor);

		Global.ScriptMgr.RunScript<IConversationOnConversationCreate>(script => script.OnConversationCreate(this, creator), GetScriptId());

		List<ConversationLine> lines = new();

		foreach (var line in conversationTemplate.Lines)
		{
			if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.ConversationLine, line.Id, creator))
				continue;

			ConversationLine lineField = new();
			lineField.ConversationLineID = line.Id;
			lineField.UiCameraID = line.UiCameraID;
			lineField.ActorIndex = line.ActorIdx;
			lineField.Flags = line.Flags;

			var convoLine = CliDB.ConversationLineStorage.LookupByKey(line.Id); // never null for conversationTemplate->Lines

			for (var locale = Locale.enUS; locale < Locale.Total; locale = locale + 1)
			{
				if (locale == Locale.None)
					continue;

				_lineStartTimes[(locale, line.Id)] = _lastLineEndTimes[(int)locale];

				if (locale == Locale.enUS)
					lineField.StartTime = (uint)_lastLineEndTimes[(int)locale].TotalMilliseconds;

				var broadcastTextDuration = Global.DB2Mgr.GetBroadcastTextDuration((int)convoLine.BroadcastTextID, locale);

				if (broadcastTextDuration != 0)
					_lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(broadcastTextDuration);

				_lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(convoLine.AdditionalDuration);
			}

			lines.Add(lineField);
		}

		_duration = _lastLineEndTimes.Max();
		SetUpdateFieldValue(Values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.LastLineEndTime), (uint)_duration.TotalMilliseconds);
		SetUpdateFieldValue(Values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Lines), lines);

		// conversations are despawned 5-20s after LastLineEndTime
		_duration += TimeSpan.FromSeconds(10);

		Global.ScriptMgr.RunScript<IConversationOnConversationCreate>(script => script.OnConversationCreate(this, creator), GetScriptId());
	}

	bool Start()
	{
		foreach (var line in m_conversationData.Lines.GetValue())
		{
			var actor = line.ActorIndex < m_conversationData.Actors.Size() ? m_conversationData.Actors[line.ActorIndex] : null;

			if (actor == null || (actor.CreatureID == 0 && actor.ActorGUID.IsEmpty && actor.NoActorObject == 0))
			{
				Log.outError(LogFilter.Conversation, $"Failed to create conversation (Id: {Entry}) due to missing actor (Idx: {line.ActorIndex}).");

				return false;
			}
		}

		if (!Map.AddToMap(this))
			return false;

		return true;
	}

	void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedConversationMask, Player target)
	{
		UpdateMask valuesMask = new((int)TypeId.Max);

		if (requestedObjectMask.IsAnySet())
			valuesMask.Set((int)TypeId.Object);

		if (requestedConversationMask.IsAnySet())
			valuesMask.Set((int)TypeId.Conversation);

		WorldPacket buffer = new();
		buffer.WriteUInt32(valuesMask.GetBlock(0));

		if (valuesMask[(int)TypeId.Object])
			ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

		if (valuesMask[(int)TypeId.Conversation])
			m_conversationData.WriteUpdate(buffer, requestedConversationMask, true, this, target);

		WorldPacket buffer1 = new();
		buffer1.WriteUInt8((byte)UpdateType.Values);
		buffer1.WritePackedGuid(GUID);
		buffer1.WriteUInt32(buffer.GetSize());
		buffer1.WriteBytes(buffer.GetData());

		data.AddUpdateBlock(buffer1);
	}

	TimeSpan GetDuration()
	{
		return _duration;
	}

	void RelocateStationaryPosition(Position pos)
	{
		_stationaryPosition.Relocate(pos);
	}

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly Conversation Owner;
		readonly ObjectFieldData ObjectMask = new();
		readonly ConversationData ConversationMask = new();

		public ValuesUpdateForPlayerWithMaskSender(Conversation owner)
		{
			Owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(Owner.Location.MapId);

			Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ConversationMask.GetUpdateMask(), player);

			udata.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}
}

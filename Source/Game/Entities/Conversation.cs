﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IConversation;
using Game.Spells;

namespace Game.Entities
{
	public class Conversation : WorldObject
	{
		private ConversationData _conversationData;
		private ObjectGuid _creatorGuid;
		private TimeSpan _duration;
		private TimeSpan[] _lastLineEndTimes = new TimeSpan[(int)Locale.Total];

		private Dictionary<(Locale locale, uint lineId), TimeSpan> _lineStartTimes = new();

		private Position _stationaryPosition = new();
		private uint _textureKitId;

		public Conversation() : base(false)
		{
			ObjectTypeMask |= TypeMask.Conversation;
			ObjectTypeId   =  TypeId.Conversation;

			_updateFlag.Stationary   = true;
			_updateFlag.Conversation = true;

			_conversationData = new ConversationData();
		}

		public override void AddToWorld()
		{
			//- Register the Conversation for guid lookup and for caster
			if (!IsInWorld)
			{
				GetMap().GetObjectsStore().Add(GetGUID(), this);
				base.AddToWorld();
			}
		}

		public override void RemoveFromWorld()
		{
			//- Remove the Conversation from the accessor and from all lists of objects in world
			if (IsInWorld)
			{
				base.RemoveFromWorld();
				GetMap().GetObjectsStore().Remove(GetGUID());
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
					                               ApplyModUpdateFieldValue(_values.ModifyValue(_conversationData).ModifyValue(_conversationData.Progress), diff, true);
					                               _conversationData.ClearChanged(_conversationData.Progress);
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
			ConversationTemplate conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);

			if (conversationTemplate == null)
				return null;

			ulong lowGuid = creator.GetMap().GenerateLowGuid(HighGuid.Conversation);

			Conversation conversation = new();
			conversation.Create(lowGuid, conversationEntry, creator.GetMap(), creator, pos, privateObjectOwner, spellInfo);

			if (autoStart && !conversation.Start())
				return null;

			return conversation;
		}

		private void Create(ulong lowGuid, uint conversationEntry, Map map, Unit creator, Position pos, ObjectGuid privateObjectOwner, SpellInfo spellInfo = null)
		{
			ConversationTemplate conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);
			//ASSERT(conversationTemplate);

			_creatorGuid = creator.GetGUID();
			SetPrivateObjectOwner(privateObjectOwner);

			SetMap(map);
			Relocate(pos);
			RelocateStationaryPosition(pos);

			_Create(ObjectGuid.Create(HighGuid.Conversation, GetMapId(), conversationEntry, lowGuid));
			PhasingHandler.InheritPhaseShift(this, creator);

			UpdatePositionData();
			SetZoneScript();

			SetEntry(conversationEntry);
			SetObjectScale(1.0f);

			_textureKitId = conversationTemplate.TextureKitId;

			foreach (var actor in conversationTemplate.Actors)
				new ConversationActorFillVisitor(this, creator, map, actor).Invoke(actor);

			Global.ScriptMgr.RunScript<IConversationOnConversationCreate>(script => script.OnConversationCreate(this, creator), GetScriptId());

			List<ConversationLine> lines = new();

			foreach (ConversationLineTemplate line in conversationTemplate.Lines)
			{
				if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.ConversationLine, line.Id, creator))
					continue;

				ConversationLine lineField = new();
				lineField.ConversationLineID = line.Id;
				lineField.UiCameraID         = line.UiCameraID;
				lineField.ActorIndex         = line.ActorIdx;
				lineField.Flags              = line.Flags;

				ConversationLineRecord convoLine = CliDB.ConversationLineStorage.LookupByKey(line.Id); // never null for conversationTemplate->Lines

				for (Locale locale = Locale.enUS; locale < Locale.Total; locale = locale + 1)
				{
					if (locale == Locale.None)
						continue;

					_lineStartTimes[(locale, line.Id)] = _lastLineEndTimes[(int)locale];

					if (locale == Locale.enUS)
						lineField.StartTime = (uint)_lastLineEndTimes[(int)locale].TotalMilliseconds;

					int broadcastTextDuration = Global.DB2Mgr.GetBroadcastTextDuration((int)convoLine.BroadcastTextID, locale);

					if (broadcastTextDuration != 0)
						_lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(broadcastTextDuration);

					_lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(convoLine.AdditionalDuration);
				}

				lines.Add(lineField);
			}

			_duration = _lastLineEndTimes.Max();
			SetUpdateFieldValue(_values.ModifyValue(_conversationData).ModifyValue(_conversationData.LastLineEndTime), (uint)_duration.TotalMilliseconds);
			SetUpdateFieldValue(_values.ModifyValue(_conversationData).ModifyValue(_conversationData.Lines), lines);

			// conversations are despawned 5-20s after LastLineEndTime
			_duration += TimeSpan.FromSeconds(10);

			Global.ScriptMgr.RunScript<IConversationOnConversationCreate>(script => script.OnConversationCreate(this, creator), GetScriptId());
		}

		private bool Start()
		{
			foreach (ConversationLine line in _conversationData.Lines.GetValue())
			{
				ConversationActorField actor = line.ActorIndex < _conversationData.Actors.Size() ? _conversationData.Actors[line.ActorIndex] : null;

				if (actor == null ||
				    (actor.CreatureID == 0 && actor.ActorGUID.IsEmpty() && actor.NoActorObject == 0))
				{
					Log.outError(LogFilter.Conversation, $"Failed to create conversation (Id: {GetEntry()}) due to missing actor (Idx: {line.ActorIndex}).");

					return false;
				}
			}

			if (!GetMap().AddToMap(this))
				return false;

			return true;
		}

		public void AddActor(int actorId, uint actorIdx, ObjectGuid actorGuid)
		{
			ConversationActorField actorField = _values.ModifyValue(_conversationData).ModifyValue(_conversationData.Actors, (int)actorIdx);
			SetUpdateFieldValue(ref actorField.CreatureID, 0u);
			SetUpdateFieldValue(ref actorField.CreatureDisplayInfoID, 0u);
			SetUpdateFieldValue(ref actorField.ActorGUID, actorGuid);
			SetUpdateFieldValue(ref actorField.Id, actorId);
			SetUpdateFieldValue(ref actorField.Type, ConversationActorType.WorldObject);
			SetUpdateFieldValue(ref actorField.NoActorObject, 0u);
		}

		public void AddActor(int actorId, uint actorIdx, ConversationActorType type, uint creatureId, uint creatureDisplayInfoId)
		{
			ConversationActorField actorField = _values.ModifyValue(_conversationData).ModifyValue(_conversationData.Actors, (int)actorIdx);
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
			return Global.ConversationDataStorage.GetConversationTemplate(GetEntry()).ScriptId;
		}

		public override void BuildValuesCreate(WorldPacket data, Player target)
		{
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			_objectData.WriteCreate(buffer, flags, this, target);
			_conversationData.WriteCreate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize());
			data.WriteUInt8((byte)flags);
			data.WriteBytes(buffer);
		}

		public override void BuildValuesUpdate(WorldPacket data, Player target)
		{
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			buffer.WriteUInt32(_values.GetChangedObjectTypeMask());

			if (_values.HasChanged(TypeId.Object))
				_objectData.WriteUpdate(buffer, flags, this, target);

			if (_values.HasChanged(TypeId.Conversation))
				_conversationData.WriteUpdate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize());
			data.WriteBytes(buffer);
		}

		private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedConversationMask, Player target)
		{
			UpdateMask valuesMask = new((int)TypeId.Max);

			if (requestedObjectMask.IsAnySet())
				valuesMask.Set((int)TypeId.Object);

			if (requestedConversationMask.IsAnySet())
				valuesMask.Set((int)TypeId.Conversation);

			WorldPacket buffer = new();
			buffer.WriteUInt32(valuesMask.GetBlock(0));

			if (valuesMask[(int)TypeId.Object])
				_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

			if (valuesMask[(int)TypeId.Conversation])
				_conversationData.WriteUpdate(buffer, requestedConversationMask, true, this, target);

			WorldPacket buffer1 = new();
			buffer1.WriteUInt8((byte)UpdateType.Values);
			buffer1.WritePackedGuid(GetGUID());
			buffer1.WriteUInt32(buffer.GetSize());
			buffer1.WriteBytes(buffer.GetData());

			data.AddUpdateBlock(buffer1);
		}

		public override void ClearUpdateMask(bool remove)
		{
			_values.ClearChangesMask(_conversationData);
			base.ClearUpdateMask(remove);
		}

		private TimeSpan GetDuration()
		{
			return _duration;
		}

		public uint GetTextureKitId()
		{
			return _textureKitId;
		}

		public ObjectGuid GetCreatorGuid()
		{
			return _creatorGuid;
		}

		public override ObjectGuid GetOwnerGUID()
		{
			return GetCreatorGuid();
		}

		public override uint GetFaction()
		{
			return 0;
		}

		public override float GetStationaryX()
		{
			return _stationaryPosition.GetPositionX();
		}

		public override float GetStationaryY()
		{
			return _stationaryPosition.GetPositionY();
		}

		public override float GetStationaryZ()
		{
			return _stationaryPosition.GetPositionZ();
		}

		public override float GetStationaryO()
		{
			return _stationaryPosition.GetOrientation();
		}

		private void RelocateStationaryPosition(Position pos)
		{
			_stationaryPosition.Relocate(pos);
		}

		private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
		{
			private ConversationData ConversationMask = new();
			private ObjectFieldData ObjectMask = new();
			private Conversation Owner;

			public ValuesUpdateForPlayerWithMaskSender(Conversation owner)
			{
				Owner = owner;
			}

			public void Invoke(Player player)
			{
				UpdateData udata = new(Owner.GetMapId());

				Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ConversationMask.GetUpdateMask(), player);

				udata.BuildPacket(out UpdateObject packet);
				player.SendPacket(packet);
			}
		}
	}

	internal class ConversationActorFillVisitor
	{
		private ConversationActorTemplate _actor;
		private Conversation _conversation;
		private Unit _creator;
		private Map _map;

		public ConversationActorFillVisitor(Conversation conversation, Unit creator, Map map, ConversationActorTemplate actor)
		{
			_conversation = conversation;
			_creator      = creator;
			_map          = map;
			_actor        = actor;
		}

		public void Invoke(ConversationActorTemplate template)
		{
			if (template.WorldObjectTemplate == null)
				Invoke(template.WorldObjectTemplate);

			if (template.NoObjectTemplate == null)
				Invoke(template.NoObjectTemplate);

			if (template.ActivePlayerTemplate == null)
				Invoke(template.ActivePlayerTemplate);

			if (template.TalkingHeadTemplate == null)
				Invoke(template.TalkingHeadTemplate);
		}

		public void Invoke(ConversationActorWorldObjectTemplate worldObject)
		{
			Creature bestFit = null;

			foreach (var creature in _map.GetCreatureBySpawnIdStore().LookupByKey(worldObject.SpawnId))
			{
				bestFit = creature;

				// If creature is in a personal phase then we pick that one specifically
				if (creature.GetPhaseShift().GetPersonalGuid() == _creator.GetGUID())
					break;
			}

			if (bestFit)
				_conversation.AddActor(_actor.Id, _actor.Index, bestFit.GetGUID());
		}

		public void Invoke(ConversationActorNoObjectTemplate noObject)
		{
			_conversation.AddActor(_actor.Id, _actor.Index, ConversationActorType.WorldObject, noObject.CreatureId, noObject.CreatureDisplayInfoId);
		}

		public void Invoke(ConversationActorActivePlayerTemplate activePlayer)
		{
			_conversation.AddActor(_actor.Id, _actor.Index, ObjectGuid.Create(HighGuid.Player, 0xFFFFFFFFFFFFFFFF));
		}

		public void Invoke(ConversationActorTalkingHeadTemplate talkingHead)
		{
			_conversation.AddActor(_actor.Id, _actor.Index, ConversationActorType.TalkingHead, talkingHead.CreatureId, talkingHead.CreatureDisplayInfoId);
		}
	}
}
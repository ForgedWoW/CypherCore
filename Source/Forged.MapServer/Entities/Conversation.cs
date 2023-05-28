// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Scripting.Interfaces.IConversation;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities;

public class Conversation : WorldObject
{
    private readonly ConditionManager _conditionManager;
    private readonly ConversationData _conversationData;
    private readonly ConversationDataStorage _conversationDataStorage;
    private readonly DB2Manager _db2Manager;
    private readonly TimeSpan[] _lastLineEndTimes = new TimeSpan[(int)Locale.Total];
    private readonly Dictionary<(Locale locale, uint lineId), TimeSpan> _lineStartTimes = new();
    private readonly Position _stationaryPosition = new();
    private TimeSpan _duration;

    public Conversation(ClassFactory classFactory, ConversationDataStorage conversationDataStorage, ConditionManager conditionManager, DB2Manager db2Manager) : base(false, classFactory)
    {
        _conversationDataStorage = conversationDataStorage;
        _conditionManager = conditionManager;
        _db2Manager = db2Manager;
        ObjectTypeMask |= TypeMask.Conversation;
        ObjectTypeId = TypeId.Conversation;

        UpdateFlag.Stationary = true;
        UpdateFlag.Conversation = true;

        _conversationData = new ConversationData();
    }

    public ObjectGuid CreatorGuid { get; private set; }
    public override uint Faction => 0;
    public override ObjectGuid OwnerGUID => CreatorGuid;
    public uint TextureKitId { get; private set; }

    public void AddActor(int actorId, uint actorIdx, ObjectGuid actorGuid)
    {
        ConversationActorField actorField = Values.ModifyValue(_conversationData).ModifyValue(_conversationData.Actors, (int)actorIdx);
        SetUpdateFieldValue(ref actorField.CreatureID, 0u);
        SetUpdateFieldValue(ref actorField.CreatureDisplayInfoID, 0u);
        SetUpdateFieldValue(ref actorField.ActorGUID, actorGuid);
        SetUpdateFieldValue(ref actorField.Id, actorId);
        SetUpdateFieldValue(ref actorField.Type, ConversationActorType.WorldObject);
        SetUpdateFieldValue(ref actorField.NoActorObject, 0u);
    }

    public void AddActor(int actorId, uint actorIdx, ConversationActorType type, uint creatureId, uint creatureDisplayInfoId)
    {
        ConversationActorField actorField = Values.ModifyValue(_conversationData).ModifyValue(_conversationData.Actors, (int)actorIdx);
        SetUpdateFieldValue(ref actorField.CreatureID, creatureId);
        SetUpdateFieldValue(ref actorField.CreatureDisplayInfoID, creatureDisplayInfoId);
        SetUpdateFieldValue(ref actorField.ActorGUID, ObjectGuid.Empty);
        SetUpdateFieldValue(ref actorField.Id, actorId);
        SetUpdateFieldValue(ref actorField.Type, type);
        SetUpdateFieldValue(ref actorField.NoActorObject, type == ConversationActorType.WorldObject ? 1 : 0u);
    }

    public override void AddToWorld()
    {
        //- Register the Conversation for guid lookup and for caster
        if (Location.IsInWorld)
            return;

        Location.Map.ObjectsStore.TryAdd(GUID, this);
        base.AddToWorld();
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        ObjectData.WriteCreate(buffer, flags, this, target);
        _conversationData.WriteCreate(buffer, flags, this, target);

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
            _conversationData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(_conversationData);
        base.ClearUpdateMask(remove);
    }

    public TimeSpan GetLastLineEndTime(Locale locale)
    {
        return _lastLineEndTimes[(int)locale];
    }

    public TimeSpan GetLineStartTime(Locale locale, int lineId)
    {
        return _lineStartTimes.LookupByKey((locale, lineId));
    }

    public uint GetScriptId()
    {
        return _conversationDataStorage.GetConversationTemplate(Entry).ScriptId;
    }

    public void Remove()
    {
        if (Location.IsInWorld)
            Location.AddObjectToRemoveList(); // calls RemoveFromWorld
    }

    public override void RemoveFromWorld()
    {
        //- Remove the Conversation from the accessor and from all lists of objects in world
        if (!Location.IsInWorld)
            return;

        base.RemoveFromWorld();
        Location.Map.ObjectsStore.TryRemove(GUID, out _);
    }

    public override void Update(uint diff)
    {
        if (_duration > TimeSpan.FromMilliseconds(diff))
        {
            _duration -= TimeSpan.FromMilliseconds(diff);

            DoWithSuppressingObjectUpdates(() =>
            {
                // Only sent in CreateObject
                ApplyModUpdateFieldValue(Values.ModifyValue(_conversationData).ModifyValue(_conversationData.Progress), diff, true);
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

    internal void Create(ulong lowGuid, uint conversationEntry, Map map, Unit creator, Position pos, ObjectGuid privateObjectOwner, SpellInfo spellInfo = null)
    {
        var conversationTemplate = _conversationDataStorage.GetConversationTemplate(conversationEntry);
        //ASSERT(conversationTemplate);

        CreatorGuid = creator.GUID;
        PrivateObjectOwner = privateObjectOwner;

        Location.WorldRelocate(map, pos);
        CheckAddToMap();
        _stationaryPosition.Relocate(pos);

        Create(ObjectGuid.Create(HighGuid.Conversation, Location.MapId, conversationEntry, lowGuid));
        PhasingHandler.InheritPhaseShift(this, creator);

        Location.UpdatePositionData();
        Location.SetZoneScript();

        Entry = conversationEntry;
        ObjectScale = 1.0f;

        TextureKitId = conversationTemplate.TextureKitId;

        foreach (var actor in conversationTemplate.Actors)
            new ConversationActorFillVisitor(this, creator, map, actor).Invoke(actor);

        ScriptManager.RunScript<IConversationOnConversationCreate>(script => script.OnConversationCreate(this, creator), GetScriptId());

        List<ConversationLine> lines = new();

        foreach (var line in conversationTemplate.Lines)
        {
            if (!_conditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.ConversationLine, line.Id, creator))
                continue;

            ConversationLine lineField = new()
            {
                ConversationLineID = line.Id,
                UiCameraID = line.UiCameraID,
                ActorIndex = line.ActorIdx,
                Flags = line.Flags
            };

            var convoLine = CliDB.ConversationLineStorage.LookupByKey(line.Id); // never null for conversationTemplate->Lines

            for (var locale = Locale.enUS; locale < Locale.Total; locale += 1)
            {
                if (locale == Locale.None)
                    continue;

                _lineStartTimes[(locale, line.Id)] = _lastLineEndTimes[(int)locale];

                if (locale == Locale.enUS)
                    lineField.StartTime = (uint)_lastLineEndTimes[(int)locale].TotalMilliseconds;

                var broadcastTextDuration = _db2Manager.GetBroadcastTextDuration((int)convoLine.BroadcastTextID, locale);

                if (broadcastTextDuration != 0)
                    _lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(broadcastTextDuration);

                _lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(convoLine.AdditionalDuration);
            }

            lines.Add(lineField);
        }

        _duration = _lastLineEndTimes.Max();
        SetUpdateFieldValue(Values.ModifyValue(_conversationData).ModifyValue(_conversationData.LastLineEndTime), (uint)_duration.TotalMilliseconds);
        SetUpdateFieldValue(Values.ModifyValue(_conversationData).ModifyValue(_conversationData.Lines), lines);

        // conversations are despawned 5-20s after LastLineEndTime
        _duration += TimeSpan.FromSeconds(10);

        ScriptManager.RunScript<IConversationOnConversationCreate>(script => script.OnConversationCreate(this, creator), GetScriptId());
    }

    internal bool Start()
    {
        foreach (var line in _conversationData.Lines.Value)
        {
            var actor = line.ActorIndex < _conversationData.Actors.Size() ? _conversationData.Actors[line.ActorIndex] : null;

            if (actor != null && (actor.CreatureID != 0 || !actor.ActorGUID.IsEmpty || actor.NoActorObject != 0))
                continue;

            Log.Logger.Error($"Failed to create conversation (Id: {Entry}) due to missing actor (Idx: {line.ActorIndex}).");

            return false;
        }

        return Location.Map.AddToMap(this);
    }
}
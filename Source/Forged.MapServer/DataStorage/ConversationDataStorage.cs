// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Globals;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.DataStorage;

public class ConversationDataStorage
{
    private readonly WorldDatabase _worldDatabase;
    private readonly GameObjectManager _objectManager;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<uint, ConversationTemplate> _conversationTemplateStorage = new();
    private readonly Dictionary<uint, ConversationLineTemplate> _conversationLineTemplateStorage = new();

    public ConversationDataStorage(WorldDatabase worldDatabase, GameObjectManager objectManager, CliDB cliDB, IConfiguration configuration)
    {
        _worldDatabase = worldDatabase;
        _objectManager = objectManager;
        _cliDB = cliDB;
        _configuration = configuration;
    }

    public void LoadConversationTemplates()
    {
        _conversationLineTemplateStorage.Clear();
        _conversationTemplateStorage.Clear();

        Dictionary<uint, List<ConversationActorTemplate>> actorsByConversation = new();

        var lineTemplates = _worldDatabase.Query("SELECT Id, UiCameraID, ActorIdx, Flags FROM conversation_line_template");

        if (!lineTemplates.IsEmpty())
        {
            var oldMSTime = Time.MSTime;

            do
            {
                var id = lineTemplates.Read<uint>(0);

                if (!_cliDB.ConversationLineStorage.ContainsKey(id))
                {
                    Log.Logger.Error("Table `conversation_line_template` has template for non existing ConversationLine (ID: {0}), skipped", id);

                    continue;
                }

                ConversationLineTemplate conversationLine = new()
                {
                    Id = id,
                    UiCameraID = lineTemplates.Read<uint>(1),
                    ActorIdx = lineTemplates.Read<byte>(2),
                    Flags = lineTemplates.Read<byte>(3)
                };

                _conversationLineTemplateStorage[id] = conversationLine;
            } while (lineTemplates.NextRow());

            Log.Logger.Information("Loaded {0} Conversation line templates in {1} ms", _conversationLineTemplateStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
        else
        {
            Log.Logger.Information("Loaded 0 Conversation line templates. DB table `conversation_line_template` is empty.");
        }

        var actorResult = _worldDatabase.Query("SELECT ConversationId, ConversationActorId, ConversationActorGuid, Idx, CreatureId, CreatureDisplayInfoId, NoActorObject, ActivePlayerObject FROM conversation_actors");

        if (!actorResult.IsEmpty())
        {
            var oldMSTime = Time.MSTime;
            uint count = 0;

            do
            {
                ConversationActorDbRow data = new(_worldDatabase, _objectManager, _cliDB, _configuration);
                ConversationActorTemplate actor = new();

                data.ConversationId = actorResult.Read<uint>(0);
                data.ConversationId = actorResult.Read<uint>(1);
                data.SpawnId = actorResult.Read<ulong>(2);
                data.ActorIndex = actor.Index = actorResult.Read<ushort>(3);
                data.CreatureId = actorResult.Read<uint>(4);
                data.CreatureDisplayInfoId = actorResult.Read<uint>(5);
                var noActorObject = actorResult.Read<byte>(6) == 1;
                var activePlayerObject = actorResult.Read<byte>(7) == 1;

                if (activePlayerObject)
                    actor.ActivePlayerTemplate = new ConversationActorActivePlayerTemplate();
                else if (noActorObject)
                    actor.NoObjectTemplate = new ConversationActorNoObjectTemplate();
                else if (data.SpawnId != 0)
                    actor.WorldObjectTemplate = new ConversationActorWorldObjectTemplate();
                else
                    actor.TalkingHeadTemplate = new ConversationActorTalkingHeadTemplate();

                var valid = data.Invoke(actor);

                if (!valid)
                    continue;

                if (!actorsByConversation.ContainsKey(data.ConversationId))
                    actorsByConversation[data.ConversationId] = new List<ConversationActorTemplate>();

                actorsByConversation[data.ConversationId].Add(actor);
                ++count;
            } while (actorResult.NextRow());

            Log.Logger.Information("Loaded {0} Conversation actors in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
        else
        {
            Log.Logger.Information("Loaded 0 Conversation actors. DB table `conversation_actors` is empty.");
        }

        // Validate FirstLineId
        Dictionary<uint, uint> prevConversationLineIds = new();

        foreach (var conversationLine in _cliDB.ConversationLineStorage.Values)
            if (conversationLine.NextConversationLineID != 0)
                prevConversationLineIds[conversationLine.NextConversationLineID] = conversationLine.Id;

        uint GetFirstLineIdFromAnyLineId(uint lineId)
        {
            uint prevLineId;

            while ((prevLineId = prevConversationLineIds.LookupByKey(lineId)) != 0)
                lineId = prevLineId;

            return lineId;
        }

        var templateResult = _worldDatabase.Query("SELECT Id, FirstLineId, TextureKitId, ScriptName FROM conversation_template");

        if (!templateResult.IsEmpty())
        {
            var oldMSTime = Time.MSTime;

            do
            {
                ConversationTemplate conversationTemplate = new()
                {
                    Id = templateResult.Read<uint>(0),
                    FirstLineId = templateResult.Read<uint>(1),
                    TextureKitId = templateResult.Read<uint>(2),
                    ScriptId = _objectManager.GetScriptId(templateResult.Read<string>(3))
                };

                conversationTemplate.Actors = actorsByConversation.TryGetValue(conversationTemplate.Id, out var actors) ? actors.ToList() : new List<ConversationActorTemplate>();

                var correctedFirstLineId = GetFirstLineIdFromAnyLineId(conversationTemplate.FirstLineId);

                if (conversationTemplate.FirstLineId != correctedFirstLineId)
                {
                    Log.Logger.Error($"Table `conversation_template` has incorrect FirstLineId {conversationTemplate.FirstLineId}, it should be {correctedFirstLineId} for Conversation {conversationTemplate.Id}, corrected");
                    conversationTemplate.FirstLineId = correctedFirstLineId;
                }

                var currentConversationLine = _cliDB.ConversationLineStorage.LookupByKey(conversationTemplate.FirstLineId);

                if (currentConversationLine == null)
                    Log.Logger.Error("Table `conversation_template` references an invalid line (ID: {0}) for Conversation {1}, skipped", conversationTemplate.FirstLineId, conversationTemplate.Id);

                while (currentConversationLine != null)
                {
                    var conversationLineTemplate = _conversationLineTemplateStorage.LookupByKey(currentConversationLine.Id);

                    if (conversationLineTemplate != null)
                        conversationTemplate.Lines.Add(conversationLineTemplate);
                    else
                        Log.Logger.Error("Table `conversation_line_template` has missing template for line (ID: {0}) in Conversation {1}, skipped", currentConversationLine.Id, conversationTemplate.Id);

                    if (currentConversationLine.NextConversationLineID == 0)
                        break;

                    currentConversationLine = _cliDB.ConversationLineStorage.LookupByKey(currentConversationLine.NextConversationLineID);
                }

                _conversationTemplateStorage[conversationTemplate.Id] = conversationTemplate;
            } while (templateResult.NextRow());

            Log.Logger.Information("Loaded {0} Conversation templates in {1} ms", _conversationTemplateStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
        else
        {
            Log.Logger.Information("Loaded 0 Conversation templates. DB table `conversation_template` is empty.");
        }
    }

    public ConversationTemplate GetConversationTemplate(uint conversationId)
    {
        return _conversationTemplateStorage.LookupByKey(conversationId);
    }

    public ConversationLineTemplate GetConversationLineTemplate(uint conversationLineId)
    {
        return _conversationLineTemplateStorage.LookupByKey(conversationLineId);
    }

    private struct ConversationActorDbRow
    {
        private readonly WorldDatabase _worldDatabase;
        private readonly GameObjectManager _objectManager;
        private readonly CliDB _cliDB;
        private readonly IConfiguration _configuration;
        public uint ConversationId;
        public uint ActorIndex;

        public ulong SpawnId;
        public uint CreatureId;
        public uint CreatureDisplayInfoId;

        public ConversationActorDbRow(WorldDatabase worldDatabase, GameObjectManager objectManager, CliDB cliDB, IConfiguration configuration)
        {
            _worldDatabase = worldDatabase;
            _objectManager = objectManager;
            _cliDB = cliDB;
            _configuration = configuration;
        }

        public bool Invoke(ConversationActorTemplate template)
        {
            if (template.WorldObjectTemplate != null)
                return Invoke(template.WorldObjectTemplate);

            if (template.NoObjectTemplate != null)
                return Invoke(template.NoObjectTemplate);

            if (template.ActivePlayerTemplate != null)
                return Invoke();

            if (template.TalkingHeadTemplate != null)
                return Invoke(template.TalkingHeadTemplate);

            return false;
        }

        public bool Invoke(ConversationActorWorldObjectTemplate worldObject)
        {
            if (_objectManager.GetCreatureData(SpawnId) == null)
            {
                Log.Logger.Error($"Table `conversation_actors` references an invalid creature guid (GUID: {SpawnId}) for Conversation {ConversationId} and Idx {ActorIndex}, skipped.");

                return false;
            }

            if (CreatureId != 0)
                Log.Logger.Error($"Table `conversation_actors` with ConversationActorGuid cannot have CreatureId ({CreatureId}). Conversation {ConversationId} and Idx {ActorIndex}.");

            if (CreatureDisplayInfoId != 0)
                Log.Logger.Error($"Table `conversation_actors` with ConversationActorGuid cannot have CreatureDisplayInfoId ({CreatureDisplayInfoId}). Conversation {ConversationId} and Idx {ActorIndex}.");

            if (worldObject == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM conversation_actors WHERE ConversationId = {ConversationId}");
                else
                    Log.Logger.Error($"Table `conversation_actors` references null world object (GUID: {SpawnId}) for Conversation {ConversationId} and Idx {ActorIndex}, skipped.");

                return false;
            }

            worldObject.SpawnId = SpawnId;

            return true;
        }

        public bool Invoke(ConversationActorNoObjectTemplate noObject)
        {
            if (_objectManager.GetCreatureTemplate(CreatureId) == null)
            {
                Log.Logger.Error($"Table `conversation_actors` references an invalid creature id ({CreatureId}) for Conversation {ConversationId} and Idx {ActorIndex}, skipped.");

                return false;
            }

            if (CreatureDisplayInfoId != 0 && !_cliDB.CreatureDisplayInfoStorage.ContainsKey(CreatureDisplayInfoId))
            {
                Log.Logger.Error($"Table `conversation_actors` references an invalid creature display id ({CreatureDisplayInfoId}) for Conversation {ConversationId} and Idx {ActorIndex}, skipped.");

                return false;
            }

            if (SpawnId != 0)
                Log.Logger.Error($"Table `conversation_actors` with NoActorObject cannot have ConversationActorGuid ({SpawnId}). Conversation {ConversationId} and Idx {ActorIndex}.");

            noObject.CreatureId = CreatureId;
            noObject.CreatureDisplayInfoId = CreatureDisplayInfoId;

            return true;
        }

        public bool Invoke()
        {
            if (SpawnId != 0)
                Log.Logger.Error($"Table `conversation_actors` with ActivePlayerObject cannot have ConversationActorGuid ({SpawnId}). Conversation {ConversationId} and Idx {ActorIndex}.");

            if (CreatureId != 0)
                Log.Logger.Error($"Table `conversation_actors` with ActivePlayerObject cannot have CreatureId ({CreatureId}). Conversation {ConversationId} and Idx {ActorIndex}.");

            if (CreatureDisplayInfoId != 0)
                Log.Logger.Error($"Table `conversation_actors` with ActivePlayerObject cannot have CreatureDisplayInfoId ({CreatureDisplayInfoId}). Conversation {ConversationId} and Idx {ActorIndex}.");

            return true;
        }

        public bool Invoke(ConversationActorTalkingHeadTemplate talkingHead)
        {
            if (_objectManager.GetCreatureTemplate(CreatureId) == null)
            {
                Log.Logger.Error($"Table `conversation_actors` references an invalid creature id ({CreatureId}) for Conversation {ConversationId} and Idx {ActorIndex}, skipped.");

                return false;
            }

            if (CreatureDisplayInfoId != 0 && !_cliDB.CreatureDisplayInfoStorage.ContainsKey(CreatureDisplayInfoId))
            {
                Log.Logger.Error($"Table `conversation_actors` references an invalid creature display id ({CreatureDisplayInfoId}) for Conversation {ConversationId} and Idx {ActorIndex}, skipped.");

                return false;
            }

            if (SpawnId != 0)
                Log.Logger.Error($"Table `conversation_actors` with TalkingHead cannot have ConversationActorGuid ({SpawnId}). Conversation {ConversationId} and Idx {ActorIndex}.");

            talkingHead.CreatureId = CreatureId;
            talkingHead.CreatureDisplayInfoId = CreatureDisplayInfoId;

            return true;
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Game.Common;

namespace Forged.MapServer.Entities;

public class ConversationFactory
{
    private readonly ConversationDataStorage _conversationDataStorage;
    private readonly ClassFactory _classFactory;

    public ConversationFactory(ConversationDataStorage conversationDataStorage, ClassFactory classFactory)
    {
        _conversationDataStorage = conversationDataStorage;
        _classFactory = classFactory;
    }

    public Conversation CreateConversation(uint conversationEntry, Unit creator, Position pos, ObjectGuid privateObjectOwner, SpellInfo spellInfo = null, bool autoStart = true)
    {
        var conversationTemplate = _conversationDataStorage.GetConversationTemplate(conversationEntry);

        if (conversationTemplate == null)
            return null;

        var lowGuid = creator.Location.Map.GenerateLowGuid(HighGuid.Conversation);

        var conversation = _classFactory.Resolve<Conversation>();
        conversation.Create(lowGuid, conversationEntry, creator.Location.Map, creator, pos, privateObjectOwner, spellInfo);

        if (autoStart && !conversation.Start())
            return null;

        return conversation;
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage;

public struct ConversationActorTemplate
{
    public ConversationActorActivePlayerTemplate ActivePlayerTemplate;
    public int Id;
    public uint Index;
    public ConversationActorNoObjectTemplate NoObjectTemplate;
    public ConversationActorTalkingHeadTemplate TalkingHeadTemplate;
    public ConversationActorWorldObjectTemplate WorldObjectTemplate;
}
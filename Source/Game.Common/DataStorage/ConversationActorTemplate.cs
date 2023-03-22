// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public struct ConversationActorTemplate
{
	public int Id;
	public uint Index;
	public ConversationActorWorldObjectTemplate WorldObjectTemplate;
	public ConversationActorNoObjectTemplate NoObjectTemplate;
	public ConversationActorActivePlayerTemplate ActivePlayerTemplate;
	public ConversationActorTalkingHeadTemplate TalkingHeadTemplate;
}
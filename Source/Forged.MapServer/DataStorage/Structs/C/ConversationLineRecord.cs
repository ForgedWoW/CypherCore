// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class ConversationLineRecord
{
	public uint Id;
	public uint BroadcastTextID;
	public uint SpellVisualKitID;
	public int AdditionalDuration;
	public ushort NextConversationLineID;
	public ushort AnimKitID;
	public byte SpeechType;
	public byte StartAnimation;
	public byte EndAnimation;
}
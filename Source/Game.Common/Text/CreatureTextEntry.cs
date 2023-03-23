// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game;
using Game.Common.Text;

namespace Game.Common.Text;

public class CreatureTextEntry
{
	public uint creatureId;
	public byte groupId;
	public byte id;
	public string text;
	public ChatMsg type;
	public Language lang;
	public float probability;
	public Emote emote;
	public uint duration;
	public uint sound;
	public SoundKitPlayType SoundPlayType;
	public uint BroadcastTextId;
	public CreatureTextRange TextRange;
}

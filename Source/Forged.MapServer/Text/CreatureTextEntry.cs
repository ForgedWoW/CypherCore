// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Text;

public class CreatureTextEntry
{
    public uint BroadcastTextId;
    public uint creatureId;
    public uint duration;
    public Emote emote;
    public byte groupId;
    public byte id;
    public Language lang;
    public float probability;
    public uint sound;
    public SoundKitPlayType SoundPlayType;
    public string text;
    public CreatureTextRange TextRange;
    public ChatMsg type;
}
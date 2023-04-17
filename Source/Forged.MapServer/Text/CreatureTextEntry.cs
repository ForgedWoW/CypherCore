// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Text;

public class CreatureTextEntry
{
    public uint BroadcastTextId { get; set; }
    public uint CreatureId { get; set; }
    public uint Duration { get; set; }
    public Emote Emote { get; set; }
    public byte GroupId { get; set; }
    public byte ID { get; set; }
    public Language Lang { get; set; }
    public float Probability { get; set; }
    public uint Sound { get; set; }
    public SoundKitPlayType SoundPlayType { get; set; }
    public string Text { get; set; }
    public CreatureTextRange TextRange { get; set; }
    public ChatMsg Type { get; set; }
}
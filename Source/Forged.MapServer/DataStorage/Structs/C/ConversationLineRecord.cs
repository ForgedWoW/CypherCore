// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ConversationLineRecord
{
    public int AdditionalDuration;
    public ushort AnimKitID;
    public uint BroadcastTextID;
    public byte EndAnimation;
    public uint Id;
    public ushort NextConversationLineID;
    public byte SpeechType;
    public uint SpellVisualKitID;
    public byte StartAnimation;
}
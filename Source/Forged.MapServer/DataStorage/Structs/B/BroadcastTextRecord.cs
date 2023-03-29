// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed class BroadcastTextRecord
{
    public LocalizedString Text;
    public LocalizedString Text1;
    public uint Id;
    public int LanguageID;
    public int ConditionID;
    public ushort EmotesID;
    public byte Flags;
    public uint ChatBubbleDurationMs;
    public int VoiceOverPriorityID;
    public uint[] SoundKitID = new uint[2];
    public ushort[] EmoteID = new ushort[3];
    public ushort[] EmoteDelay = new ushort[3];
}
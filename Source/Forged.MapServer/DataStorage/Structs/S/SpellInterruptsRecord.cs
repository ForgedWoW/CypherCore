// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellInterruptsRecord
{
    public int[] AuraInterruptFlags = new int[2];
    public int[] ChannelInterruptFlags = new int[2];
    public byte DifficultyID;
    public uint Id;
    public short InterruptFlags;
    public uint SpellID;
}
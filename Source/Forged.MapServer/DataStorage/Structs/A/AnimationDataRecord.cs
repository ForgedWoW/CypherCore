// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AnimationDataRecord
{
    public int BehaviorID;
    public byte BehaviorTier;
    public ushort Fallback;
    public int[] Flags = new int[2];
    public uint Id;
}
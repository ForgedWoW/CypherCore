// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SummonPropertiesRecord
{
    public SummonCategory Control;
    public uint Faction;
    public uint[] Flags = new uint[2];
    public uint Id;
    public int Slot;
    public SummonTitle Title;

    public SummonPropertiesFlags GetFlags()
    {
        return (SummonPropertiesFlags)Flags[0];
    }
}
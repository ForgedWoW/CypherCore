// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BattlePetBreedStateRecord
{
    public uint BattlePetBreedID;
    public int BattlePetStateID;
    public uint Id;
    public ushort Value;
}
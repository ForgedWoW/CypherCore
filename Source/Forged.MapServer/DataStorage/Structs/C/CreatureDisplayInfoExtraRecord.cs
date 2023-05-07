// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CreatureDisplayInfoExtraRecord
{
    public int BakeMaterialResourcesID;
    public sbyte DisplayClassID;
    public sbyte DisplayRaceID;
    public sbyte DisplaySexID;
    public sbyte Flags;
    public int HDBakeMaterialResourcesID;
    public uint Id;
}
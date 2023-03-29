// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CreatureDisplayInfoExtraRecord
{
    public uint Id;
    public sbyte DisplayRaceID;
    public sbyte DisplaySexID;
    public sbyte DisplayClassID;
    public sbyte Flags;
    public int BakeMaterialResourcesID;
    public int HDBakeMaterialResourcesID;
}
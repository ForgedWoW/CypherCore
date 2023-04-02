// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrAbilityRecord
{
    public string Description;
    public ushort FactionChangeGarrAbilityID;
    public GarrisonAbilityFlags Flags;
    public byte GarrAbilityCategoryID;
    public sbyte GarrFollowerTypeID;
    public int IconFileDataID;
    public uint Id;
    public string Name;
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrFollowerXAbilityRecord
{
    public byte FactionIndex;
    public ushort GarrAbilityID;
    public uint GarrFollowerID;
    public uint Id;
    public byte OrderIndex;
}
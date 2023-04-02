// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps;

public class TransportSpawn
{
    public uint PhaseGroup { get; set; }
    public uint PhaseId { get; set; }
    public PhaseUseFlagsValues PhaseUseFlags { get; set; }
    public ulong SpawnId { get; set; }
    public uint TransportGameObjectId { get; set; } // entry in respective _template table
}
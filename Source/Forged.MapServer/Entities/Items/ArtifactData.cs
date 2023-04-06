// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Entities.Items;

internal class ArtifactData
{
    public uint ArtifactAppearanceId { get; set; }
    public List<ArtifactPowerData> ArtifactPowers { get; set; } = new();
    public uint ArtifactTierId { get; set; }
    public ulong Xp { get; set; }
}
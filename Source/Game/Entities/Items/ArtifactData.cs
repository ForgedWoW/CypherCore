// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Entities;

class ArtifactData
{
	public ulong Xp;
	public uint ArtifactAppearanceId;
	public uint ArtifactTierId;
	public List<ArtifactPowerData> ArtifactPowers = new();
}
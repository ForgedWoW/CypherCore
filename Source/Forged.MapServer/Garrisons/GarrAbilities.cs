// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.G;

namespace Forged.MapServer.Garrisons;

internal class GarrAbilities
{
    public List<GarrAbilityRecord> Counters = new();
    public List<GarrAbilityRecord> Traits = new();
}
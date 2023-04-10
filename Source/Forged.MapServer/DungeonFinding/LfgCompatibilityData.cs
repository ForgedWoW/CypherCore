// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgCompatibilityData
{
    public LfgCompatibility Compatibility;
    public Dictionary<ObjectGuid, LfgRoles> Roles;

    public LfgCompatibilityData()
    {
        Compatibility = LfgCompatibility.Pending;
    }

    public LfgCompatibilityData(LfgCompatibility compatibility)
    {
        Compatibility = compatibility;
    }

    public LfgCompatibilityData(LfgCompatibility compatibility, Dictionary<ObjectGuid, LfgRoles> roles)
    {
        Compatibility = compatibility;
        Roles = roles;
    }
}
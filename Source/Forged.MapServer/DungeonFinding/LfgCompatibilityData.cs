// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgCompatibilityData
{
    public LfgCompatibility compatibility;
    public Dictionary<ObjectGuid, LfgRoles> roles;

    public LfgCompatibilityData()
    {
        compatibility = LfgCompatibility.Pending;
    }

    public LfgCompatibilityData(LfgCompatibility _compatibility)
    {
        compatibility = _compatibility;
    }

    public LfgCompatibilityData(LfgCompatibility _compatibility, Dictionary<ObjectGuid, LfgRoles> _roles)
    {
        compatibility = _compatibility;
        roles = _roles;
    }
}
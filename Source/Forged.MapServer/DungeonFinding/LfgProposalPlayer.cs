// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgProposalPlayer
{
    public LfgAnswer Accept { get; set; }
    public ObjectGuid Group { get; set; }
    public LfgRoles Role { get; set; }

    public LfgProposalPlayer()
    {
        Role = 0;
        Accept = LfgAnswer.Pending;
        Group = ObjectGuid.Empty;
    }
}
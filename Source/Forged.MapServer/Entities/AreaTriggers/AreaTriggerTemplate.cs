// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Entities.AreaTriggers;

/// <summary>
///     Scale array definition
///     0 - time offset from creation for starting of scaling
///     1+2,3+4 are values for curve points Vector2[2]
/// </summary>
//  5 is packed curve information (has_no_data & 1) | ((interpolation_mode & 0x7) << 1) | ((first_point_offset & 0x7FFFFF) << 4) | ((point_count & 0x1F) << 27)
public class AreaTriggerTemplate
{
    public List<AreaTriggerAction> Actions { get; set; } = new();
    public AreaTriggerFlags Flags { get; set; }
    public AreaTriggerId Id { get; set; }

    public bool HasFlag(AreaTriggerFlags flag)
    {
        return Flags.HasAnyFlag(flag);
    }
}
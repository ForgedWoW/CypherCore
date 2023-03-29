// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Battlepay;

public enum BpayDistributionStatus
{
    NONE = 0,
    AVAILABLE = 1,
    ADD_TO_PROCESS = 2,
    PROCESS_COMPLETE = 3,
    FINISHED = 4
}
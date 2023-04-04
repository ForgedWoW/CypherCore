// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Battlepay;

public enum BpayDistributionStatus
{
    None = 0,
    Available = 1,
    AddToProcess = 2,
    ProcessComplete = 3,
    Finished = 4
}
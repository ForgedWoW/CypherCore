// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum EmpowerState
{
	None = 0,
    CanceledStartup = 1,
    Prepared = 2,
    Empowering = 3,
    Canceled = 4,
    Finished = 5
}
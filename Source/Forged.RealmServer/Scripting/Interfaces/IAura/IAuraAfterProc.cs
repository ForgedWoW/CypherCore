// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Forged.RealmServer.Scripting.Interfaces.IAura;

public interface IAuraAfterProc : IAuraScript
{
	bool AfterProc(ProcEventInfo info);
}
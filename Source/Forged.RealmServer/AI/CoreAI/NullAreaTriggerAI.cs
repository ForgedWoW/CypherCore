// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.AreaTriggers;

namespace Forged.RealmServer.AI;

class NullAreaTriggerAI : AreaTriggerAI
{
	public NullAreaTriggerAI(AreaTrigger areaTrigger) : base(areaTrigger) { }
}
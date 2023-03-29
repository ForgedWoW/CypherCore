// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.AreaTriggers;

namespace Forged.MapServer.Scripting.Interfaces.IAreaTriggerEntity;

public interface IAreaTriggerEntityGetAI : IScriptObject
{
    AreaTriggerAI GetAI(AreaTrigger at);
}
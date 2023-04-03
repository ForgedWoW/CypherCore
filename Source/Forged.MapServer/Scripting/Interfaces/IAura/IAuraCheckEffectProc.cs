// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells.Auras;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraCheckEffectProc : IAuraEffectHandler
{
    bool CheckProc(AuraEffect aura, ProcEventInfo info);
}
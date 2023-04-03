// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraApplyHandler : IAuraEffectHandler
{
    AuraEffectHandleModes Modes { get; }
    void Apply(AuraEffect aura, AuraEffectHandleModes auraMode);
}
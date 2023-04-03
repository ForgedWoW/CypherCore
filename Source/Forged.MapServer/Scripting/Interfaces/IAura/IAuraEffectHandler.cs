// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraEffectHandler
{
    AuraType AuraType { get; }
    int EffectIndex { get; }
    AuraScriptHookType HookType { get; }
}
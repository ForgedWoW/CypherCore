// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public class AuraEffectHandler : IAuraEffectHandler
{
    public AuraType AuraType { get; private set; }
    public int EffectIndex { get; private set; }
    public AuraScriptHookType HookType { get; private set; }

    public AuraEffectHandler(int effectIndex, AuraType auraType, AuraScriptHookType hookType)
    {
        EffectIndex = effectIndex;
        AuraType = auraType;
        HookType = hookType;
    }
}
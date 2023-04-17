// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public class TargetHookHandler : SpellEffect, ITargetHookHandler
{
    public TargetHookHandler(int effectIndex, Targets targetType, bool area, SpellScriptHookType hookType, bool dest = false) : base(effectIndex, hookType)
    {
        TargetType = targetType;
        Area = area;
        Dest = dest;
    }

    public bool Area { get; }
    public bool Dest { get; }
    public Targets TargetType { get; }
}
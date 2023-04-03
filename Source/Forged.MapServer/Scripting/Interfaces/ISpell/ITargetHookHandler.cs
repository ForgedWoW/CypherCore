// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public interface ITargetHookHandler : ISpellEffect
{
    bool Area
    {
        get { return true; }
    }

    bool Dest
    {
        get { return false; }
    }

    Targets TargetType { get; }
}
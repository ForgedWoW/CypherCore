// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public interface ISpellObjectTargetSelectHandler : ITargetHookHandler
{
    void TargetSelect(WorldObject targets);
}
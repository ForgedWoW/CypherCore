// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script] // 206416 - First Blood
internal class SpellDhFirstBlood : AuraScript
{
    private ObjectGuid _firstTargetGUID;

    public ObjectGuid GetFirstTarget()
    {
        return _firstTargetGUID;
    }

    public void SetFirstTarget(ObjectGuid targetGuid)
    {
        _firstTargetGUID = targetGuid;
    }

    public override void Register() { }
}
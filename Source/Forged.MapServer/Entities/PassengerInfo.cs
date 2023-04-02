// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Entities;

public struct PassengerInfo
{
    public ObjectGuid Guid;
    public bool IsGravityDisabled;
    public bool IsUninteractible;
    public void Reset()
    {
        Guid = ObjectGuid.Empty;
        IsUninteractible = false;
        IsGravityDisabled = false;
    }
}
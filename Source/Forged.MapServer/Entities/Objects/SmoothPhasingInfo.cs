// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Objects;

public class SmoothPhasingInfo
{
    // Fields visible on client
    public ObjectGuid? ReplaceObject;

    public SmoothPhasingInfo(ObjectGuid replaceObject, bool replaceActive, bool stopAnimKits)
    {
        ReplaceObject = replaceObject;
        ReplaceActive = replaceActive;
        StopAnimKits = stopAnimKits;
    }

    // Serverside fields
    public bool Disabled { get; set; } = false;

    public bool ReplaceActive { get; set; }
    public bool StopAnimKits { get; set; }
}
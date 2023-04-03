// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Scripting.Interfaces;

public interface IScriptObject
{
    ClassFactory ClassFactory { get; set; }

    string GetName();

    bool IsDatabaseBound()
    {
        return false;
    }
}
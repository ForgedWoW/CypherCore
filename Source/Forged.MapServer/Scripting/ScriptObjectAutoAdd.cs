// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting.Interfaces;

namespace Forged.MapServer.Scripting;

public abstract class ScriptObjectAutoAdd : ScriptObject, IScriptAutoAdd
{
    protected ScriptObjectAutoAdd(string name) : base(name)
    {
    }
}
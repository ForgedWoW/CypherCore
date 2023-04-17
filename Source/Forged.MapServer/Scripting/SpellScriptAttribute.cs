// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Scripting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SpellScriptAttribute : ScriptAttribute
{
    public SpellScriptAttribute(params uint[] spellId) : base("", Array.Empty<object>())
    {
        SpellIds = spellId;
    }

    public SpellScriptAttribute(string name = "", params object[] args) : base(name, args) { }

    public SpellScriptAttribute(uint spellId, string name = "", bool allRanks = false, params object[] args) : base(name, args)
    {
        SpellIds = new[]
        {
            spellId
        };

        AllRanks = allRanks;
    }

    public SpellScriptAttribute(uint[] spellId, string name = "", bool allRanks = false, params object[] args) : base(name, args)
    {
        SpellIds = spellId;
        AllRanks = allRanks;
    }

    public bool AllRanks { get; private set; }
    public uint[] SpellIds { get; private set; }
}
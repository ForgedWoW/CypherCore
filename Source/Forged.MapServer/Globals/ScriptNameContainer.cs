// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;

namespace Forged.MapServer.Globals;

internal class ScriptNameContainer
{
    private readonly List<Entry> _indexToName = new();
    private readonly Dictionary<string, Entry> _nameToIndex = new();

    public ScriptNameContainer()
    {
        // We insert an empty placeholder here so we can use the
        // script id 0 as dummy for "no script found".
        Insert("", false);
    }

    public Entry Find(uint index)
    {
        return index < _indexToName.Count ? _indexToName[(int)index] : null;
    }

    public Entry Find(string name)
    {
        // assume "" is the first element
        return name.IsEmpty() ? null : _nameToIndex.LookupByKey(name);
    }

    public List<string> GetAllDBScriptNames()
    {
        List<string> scriptNames = new();

        foreach (var (name, entry) in _nameToIndex)
            if (entry.IsScriptDatabaseBound)
                scriptNames.Add(name);

        return scriptNames;
    }

    public int GetSize()
    {
        return _indexToName.Count;
    }

    public uint Insert(string scriptName, bool isScriptNameBound)
    {
        Entry entry = new((uint)_nameToIndex.Count, isScriptNameBound, scriptName);
        var result = _nameToIndex.TryAdd(scriptName, entry);

        if (result)
            _indexToName.Add(entry);

        return _nameToIndex[scriptName].Id;
    }

    public class Entry
    {
        public Entry(uint id, bool isScriptDatabaseBound, string name)
        {
            Id = id;
            IsScriptDatabaseBound = isScriptDatabaseBound;
            Name = name;
        }

        public uint Id { get; set; }
        public bool IsScriptDatabaseBound { get; set; }
        public string Name { get; set; }
    }
}
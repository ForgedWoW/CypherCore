// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;

namespace Forged.MapServer.Globals;

internal class ScriptNameContainer
{
    private readonly Dictionary<string, Entry> _nameToIndex = new();
    private readonly List<Entry> _indexToName = new();

    public ScriptNameContainer()
    {
        // We insert an empty placeholder here so we can use the
        // script id 0 as dummy for "no script found".
        var id = Insert("", false);
    }

    public uint Insert(string scriptName, bool isScriptNameBound)
    {
        Entry entry = new((uint)_nameToIndex.Count, isScriptNameBound, scriptName);
        var result = _nameToIndex.TryAdd(scriptName, entry);

        if (result)
            _indexToName.Add(entry);

        return _nameToIndex[scriptName].Id;
    }

    public int GetSize()
    {
        return _indexToName.Count;
    }

    public Entry Find(uint index)
    {
        return index < _indexToName.Count ? _indexToName[(int)index] : null;
    }

    public Entry Find(string name)
    {
        // assume "" is the first element
        if (name.IsEmpty())
            return null;

        return _nameToIndex.LookupByKey(name);
    }

    public List<string> GetAllDBScriptNames()
    {
        List<string> scriptNames = new();

        foreach (var (name, entry) in _nameToIndex)
            if (entry.IsScriptDatabaseBound)
                scriptNames.Add(name);

        return scriptNames;
    }

    public class Entry
    {
        public uint Id;
        public bool IsScriptDatabaseBound;
        public string Name;

        public Entry(uint id, bool isScriptDatabaseBound, string name)
        {
            Id = id;
            IsScriptDatabaseBound = isScriptDatabaseBound;
            Name = name;
        }
    }
}
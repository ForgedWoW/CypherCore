// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage.Structs.T;

namespace Forged.MapServer.Maps;

public class TransportAnimation
{
    private List<uint> _path;
    private List<uint> _rotation;
    public Dictionary<uint, TransportAnimationRecord> Path { get; } = new();
    public Dictionary<uint, TransportRotationRecord> Rotations { get; } = new();
    public uint TotalTime { get; set; }
    public TransportAnimationRecord GetNextAnimNode(uint time)
    {
        if (Path.Empty())
            return null;

        return Path.TryGetValue(time, out var record) ? record : Path.FirstOrDefault().Value;
    }

    public TransportRotationRecord GetNextAnimRotation(uint time)
    {
        if (Rotations.Empty())
            return null;

        return Rotations.TryGetValue(time, out var record) ? record : Rotations.FirstOrDefault().Value;
    }

    public TransportAnimationRecord GetPrevAnimNode(uint time)
    {
        if (Path.Empty())
            return null;

        _path ??= Path.Keys.ToList();

        var reqIndex = _path.IndexOf(time) - 1;

        if (reqIndex != -2 && reqIndex != -1)
            return Path[_path[reqIndex]];

        return Path.LastOrDefault().Value;
    }

    public TransportRotationRecord GetPrevAnimRotation(uint time)
    {
        if (Rotations.Empty())
            return null;

        _rotation ??= Rotations.Keys.ToList();

        var reqIndex = _rotation.IndexOf(time) - 1; // indexof returns -1 if index is not found, - 1 from that is -2

        if (reqIndex != -2 && reqIndex != -1)
            return Rotations[_rotation[reqIndex]];

        return Rotations.LastOrDefault().Value;
    }
}
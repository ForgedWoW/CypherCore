// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage.Structs.T;

namespace Forged.MapServer.Maps;

public class TransportAnimation
{
	List<uint> _path;
	List<uint> _rotation;
	public uint TotalTime { get; set; }
	public Dictionary<uint, TransportAnimationRecord> Path { get; } = new();
	public Dictionary<uint, TransportRotationRecord> Rotations { get; } = new();

	public TransportAnimationRecord GetPrevAnimNode(uint time)
	{
		if (Path.Empty())
			return null;

		if (_path == null)
			_path = Path.Keys.ToList();

		var reqIndex = _path.IndexOf(time) - 1;

		if (reqIndex != -2 && reqIndex != -1)
			return Path[_path[reqIndex]];

		return Path.LastOrDefault().Value;
	}

	public TransportRotationRecord GetPrevAnimRotation(uint time)
	{
		if (Rotations.Empty())
			return null;

		if (_rotation == null)
			_rotation = Rotations.Keys.ToList();

		var reqIndex = _rotation.IndexOf(time) - 1; // indexof returns -1 if index is not found, - 1 from that is -2

		if (reqIndex != -2 && reqIndex != -1)
			return Rotations[_rotation[reqIndex]];

		return Rotations.LastOrDefault().Value;
	}

	public TransportAnimationRecord GetNextAnimNode(uint time)
	{
		if (Path.Empty())
			return null;

		if (Path.TryGetValue(time, out var record))
			return record;

		return Path.FirstOrDefault().Value;
	}

	public TransportRotationRecord GetNextAnimRotation(uint time)
	{
		if (Rotations.Empty())
			return null;

		if (Rotations.TryGetValue(time, out var record))
			return record;

		return Rotations.FirstOrDefault().Value;
	}
}
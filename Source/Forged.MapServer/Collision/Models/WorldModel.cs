// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Forged.MapServer.Collision.Maps;
using Framework.Constants;
using Framework.GameMath;

namespace Forged.MapServer.Collision.Models;

public class WorldModel : IModel
{
	public uint Flags;
    private readonly List<GroupModel> _groupModels = new();
    private readonly BIH _groupTree = new();

    private uint _rootWmoid;

	public override bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit, ModelIgnoreFlags ignoreFlags)
	{
		// If the caller asked us to ignore certain objects we should check flags
		if ((ignoreFlags & ModelIgnoreFlags.M2) != ModelIgnoreFlags.Nothing)
			// M2 models are not taken into account for LoS calculation if caller requested their ignoring.
			if ((Flags & (uint)ModelFlags.M2) != 0)
				return false;

		// small M2 workaround, maybe better make separate class with virtual intersection funcs
		// in any case, there's no need to use a bound tree if we only have one submodel
		if (_groupModels.Count == 1)
			return _groupModels[0].IntersectRay(ray, ref distance, stopAtFirstHit);

		WModelRayCallBack isc = new(_groupModels);
		_groupTree.IntersectRay(ray, isc, ref distance, stopAtFirstHit);

		return isc.Hit;
	}

	public bool IntersectPoint(Vector3 p, Vector3 down, out float dist, AreaInfo info)
	{
		dist = 0f;

		if (_groupModels.Empty())
			return false;

		WModelAreaCallback callback = new(_groupModels, down);
		_groupTree.IntersectPoint(p, callback);

		if (callback.Hit != null)
		{
			info.RootId = (int)_rootWmoid;
			info.GroupId = (int)callback.Hit.GetWmoID();
			info.Flags = callback.Hit.GetMogpFlags();
			info.Result = true;
			dist = callback.ZDist;

			return true;
		}

		return false;
	}

	public bool GetLocationInfo(Vector3 p, Vector3 down, out float dist, GroupLocationInfo info)
	{
		dist = 0f;

		if (_groupModels.Empty())
			return false;

		WModelAreaCallback callback = new(_groupModels, down);
		_groupTree.IntersectPoint(p, callback);

		if (callback.Hit != null)
		{
			info.RootId = (int)_rootWmoid;
			info.HitModel = callback.Hit;
			dist = callback.ZDist;

			return true;
		}

		return false;
	}

	public bool ReadFile(string filename)
	{
		if (!File.Exists(filename))
		{
			filename += ".vmo";

			if (!File.Exists(filename))
				return false;
		}

		using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));

		if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
			return false;

		if (reader.ReadStringFromChars(4) != "WMOD")
			return false;

		reader.ReadUInt32(); //chunkSize notused
		_rootWmoid = reader.ReadUInt32();

		// read group models
		if (reader.ReadStringFromChars(4) != "GMOD")
			return false;

		var count = reader.ReadUInt32();

		for (var i = 0; i < count; ++i)
		{
			GroupModel group = new();
			group.ReadFromFile(reader);
			_groupModels.Add(group);
		}

		// read group BIH
		if (reader.ReadStringFromChars(4) != "GBIH")
			return false;

		return _groupTree.ReadFromFile(reader);
	}
}
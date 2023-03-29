// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Maps;

namespace Forged.RealmServer.Entities;

public class WorldLocation : Position
{
	Cell _currentCell;

	public ObjectCellMoveState MoveState { get; set; }

	public Position NewPosition { get; set; } = new();

	public uint MapId { get; set; }

	public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0, float z = 0, float o = 0)
	{
		MapId = mapId;
		Relocate(x, y, z, o);
	}

	public WorldLocation(uint mapId, Position pos)
	{
		MapId = mapId;
		Relocate(pos);
	}

	public WorldLocation(WorldLocation loc)
	{
		MapId = loc.MapId;
		Relocate(loc);
	}

	public WorldLocation(Position pos)
	{
		MapId = 0xFFFFFFFF;
		Relocate(pos);
	}

	public void WorldRelocate(uint mapId, Position pos)
	{
		MapId = mapId;
		Relocate(pos);
	}

	public void WorldRelocate(WorldLocation loc)
	{
		MapId = loc.MapId;
		Relocate(loc);
	}

	public void WorldRelocate(uint mapId = 0xFFFFFFFF, float x = 0.0f, float y = 0.0f, float z = 0.0f, float o = 0.0f)
	{
		MapId = mapId;
		Relocate(x, y, z, o);
	}

	public Cell GetCurrentCell()
	{
		return _currentCell;
	}

	public void SetCurrentCell(Cell cell)
	{
		_currentCell = cell;
	}

	public void SetNewCellPosition(float x, float y, float z, float o)
	{
		MoveState = ObjectCellMoveState.Active;
		NewPosition.Relocate(x, y, z, o);
	}

	public virtual string GetDebugInfo()
	{
		var mapEntry = _cliDb.MapStorage.LookupByKey(MapId);

		return $"MapID: {MapId} Map name: '{(mapEntry != null ? mapEntry.MapName[_worldManager.DefaultDbcLocale] : "<not found>")}' {base.ToString()}";
	}

	public override string ToString()
	{
		return $"X: {X} Y: {Y} Z: {Z} O: {Orientation} MapId: {MapId}";
	}
}
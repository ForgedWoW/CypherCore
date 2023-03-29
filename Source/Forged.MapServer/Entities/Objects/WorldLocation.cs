// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;

namespace Forged.MapServer.Entities.Objects;

public class WorldLocation : Position
{
    private Cell _currentCell;
	private uint _mapId;
    private uint _instanceId;
	public ObjectCellMoveState MoveState { get; set; }

	public Position NewPosition { get; set; } = new();

    public uint MapId => Map?.Id ?? _mapId;

    public Map Map { get; set; }

	public uint Zone { get; set; }
    public uint Area { get; set; }
    public bool IsInWorld { get; set; }

    public uint InstanceId
    {
        get => Map?.InstanceId ?? _instanceId;
        set => _instanceId = value;
    }

    public InstanceScript InstanceScript => Map is { IsDungeon: true } ? ((InstanceMap)Map).InstanceScript : null;

    public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0, float z = 0, float o = 0)
	{
        _mapId = mapId;
		Relocate(x, y, z, o);
	}

	public WorldLocation(uint mapId, Position pos)
	{
        _mapId = mapId;
		Relocate(pos);
	}

	public WorldLocation(WorldLocation loc)
	{
		_mapId = loc.MapId;
        Map = loc.Map;
		Relocate(loc);
	}

	public WorldLocation(Position pos)
	{
		_mapId = 0xFFFFFFFF;
		Relocate(pos);
	}

	public void WorldRelocate(uint mapId, Position pos)
	{
		_mapId = mapId;
		Relocate(pos);
	}

    public void WorldRelocate(Map map, Position pos)
    {
        Map = map;
        Relocate(pos);
    }

    public void WorldRelocate(WorldLocation loc)
	{
		_mapId = loc.MapId;
        Map = loc.Map;
        Relocate(loc);
	}

	public void WorldRelocate(uint mapId = 0xFFFFFFFF, float x = 0.0f, float y = 0.0f, float z = 0.0f, float o = 0.0f)
	{
		_mapId = mapId;
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
        return $"MapID: {MapId} {base.ToString()}";
	}

	public override string ToString()
	{
		return $"X: {X} Y: {Y} Z: {Z} O: {Orientation} MapId: {MapId}";
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Maps;

namespace Game.Entities
{
    public class WorldLocation : Position
    {
        uint _mapId;
        Cell _currentCell;
        public ObjectCellMoveState MoveState { get; set; }

        public Position NewPosition { get; set; } = new();

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
            _mapId = loc._mapId;
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
        
        public void WorldRelocate(WorldLocation loc)
        {
            _mapId = loc._mapId;
            Relocate(loc);
        }

        public void WorldRelocate(uint mapId = 0xFFFFFFFF, float x = 0.0f, float y = 0.0f, float z = 0.0f, float o = 0.0f)
        {
            _mapId = mapId;
            Relocate(x, y, z, o);
        }

        public uint GetMapId() { return _mapId; }
        public void SetMapId(uint mapId) { _mapId = mapId; }

        public Cell GetCurrentCell()
        {
            if (_currentCell == null)
                Log.outError(LogFilter.Server, "Calling currentCell  but its null");

            return _currentCell;
        }
        public void SetCurrentCell(Cell cell) { _currentCell = cell; }
        public void SetNewCellPosition(float x, float y, float z, float o)
        {
            MoveState = ObjectCellMoveState.Active;
            NewPosition.Relocate(x, y, z, o);
        }

        public virtual string GetDebugInfo()
        {
            var mapEntry = CliDB.MapStorage.LookupByKey(_mapId);
            return $"MapID: {_mapId} Map name: '{(mapEntry != null ? mapEntry.MapName[Global.WorldMgr.GetDefaultDbcLocale()] : "<not found>")}' {base.ToString()}";
        }
        
        public override string ToString()
        {
            return $"X: {X} Y: {Y} Z: {Z} O: {Orientation} MapId: {_mapId}";
        }
    }
}

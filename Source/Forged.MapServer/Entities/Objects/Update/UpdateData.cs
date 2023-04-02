// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Update;
using Framework.IO;

namespace Forged.MapServer.Entities.Objects.Update;

public class UpdateData
{
    private readonly ByteBuffer _data = new();
    private readonly List<ObjectGuid> _destroyGUIDs = new();
    private readonly List<ObjectGuid> _outOfRangeGUIDs = new();
    private uint _blockCount;
    private uint _mapId;
    public UpdateData(uint mapId)
    {
        _mapId = mapId;
    }

    public void AddDestroyObject(ObjectGuid guid)
    {
        _destroyGUIDs.Add(guid);
    }

    public void AddOutOfRangeGUID(List<ObjectGuid> guids)
    {
        _outOfRangeGUIDs.AddRange(guids);
    }

    public void AddOutOfRangeGUID(ObjectGuid guid)
    {
        _outOfRangeGUIDs.Add(guid);
    }

    public void AddUpdateBlock(ByteBuffer block)
    {
        _data.WriteBytes(block.GetData());
        ++_blockCount;
    }

    public bool BuildPacket(out UpdateObject packet)
    {
        packet = new UpdateObject
        {
            NumObjUpdates = _blockCount,
            MapID = (ushort)_mapId
        };

        WorldPacket buffer = new();

        if (buffer.WriteBit(!_outOfRangeGUIDs.Empty() || !_destroyGUIDs.Empty()))
        {
            buffer.WriteUInt16((ushort)_destroyGUIDs.Count);
            buffer.WriteInt32(_destroyGUIDs.Count + _outOfRangeGUIDs.Count);

            foreach (var destroyGuid in _destroyGUIDs)
                buffer.WritePackedGuid(destroyGuid);

            foreach (var outOfRangeGuid in _outOfRangeGUIDs)
                buffer.WritePackedGuid(outOfRangeGuid);
        }

        var bytes = _data.GetData();
        buffer.WriteInt32(bytes.Length);
        buffer.WriteBytes(bytes);

        packet.Data = buffer.GetData();

        return true;
    }

    public void Clear()
    {
        _data.Clear();
        _destroyGUIDs.Clear();
        _outOfRangeGUIDs.Clear();
        _blockCount = 0;
        _mapId = 0;
    }

    public List<ObjectGuid> GetOutOfRangeGUIDs()
    {
        return _outOfRangeGUIDs;
    }

    public bool HasData()
    {
        return _blockCount > 0 || !_outOfRangeGUIDs.Empty() || !_destroyGUIDs.Empty();
    }
    public void SetMapId(ushort mapId)
    {
        _mapId = mapId;
    }
}
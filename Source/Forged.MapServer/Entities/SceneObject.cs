// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Framework.Constants;

namespace Forged.MapServer.Entities;

public class SceneObject : WorldObject
{
    private readonly SceneObjectData _sceneObjectData;
    private readonly Position _stationaryPosition;
    private ObjectGuid _createdBySpellCast;

    public SceneObject() : base(false)
    {
        ObjectTypeMask |= TypeMask.SceneObject;
        ObjectTypeId = TypeId.SceneObject;

        UpdateFlag.Stationary = true;
        UpdateFlag.SceneObject = true;

        _sceneObjectData = new SceneObjectData();
        _stationaryPosition = new Position();
    }

    public override uint Faction => 0;
    public override ObjectGuid OwnerGUID => _sceneObjectData.CreatedBy;

    public static SceneObject CreateSceneObject(uint sceneId, Unit creator, Position pos, ObjectGuid privateObjectOwner)
    {
        var sceneTemplate = GameObjectManager.GetSceneTemplate(sceneId);

        if (sceneTemplate == null)
            return null;

        var lowGuid = creator.Location.Map.GenerateLowGuid(HighGuid.SceneObject);

        SceneObject sceneObject = new();

        if (!sceneObject.Create(lowGuid, SceneType.Normal, sceneId, sceneTemplate?.ScenePackageId ?? 0, creator.Location.Map, creator, pos, privateObjectOwner))
        {
            sceneObject.Dispose();

            return null;
        }

        return sceneObject;
    }

    public override void AddToWorld()
    {
        if (!Location.IsInWorld)
        {
            Location.Map.ObjectsStore.TryAdd(GUID, this);
            base.AddToWorld();
        }
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        ObjectData.WriteCreate(buffer, flags, this, target);
        _sceneObjectData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteUInt8((byte)flags);
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.SceneObject))
            _sceneObjectData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(_sceneObjectData);
        base.ClearUpdateMask(remove);
    }

    public override void RemoveFromWorld()
    {
        if (!Location.IsInWorld)
            return;

        base.RemoveFromWorld();
        Location.Map.ObjectsStore.TryRemove(GUID, out _);
    }

    public void SetCreatedBySpellCast(ObjectGuid castId)
    {
        _createdBySpellCast = castId;
    }

    public override void Update(uint diff)
    {
        base.Update(diff);

        if (ShouldBeRemoved())
            Remove();
    }

    private bool Create(ulong lowGuid, SceneType type, uint sceneId, uint scriptPackageId, Map map, Unit creator, Position pos, ObjectGuid privateObjectOwner)
    {
        Location.WorldRelocate(map, pos);
        CheckAddToMap();
        RelocateStationaryPosition(pos);

        PrivateObjectOwner = privateObjectOwner;

        Create(ObjectGuid.Create(HighGuid.SceneObject, Location.MapId, sceneId, lowGuid));
        PhasingHandler.InheritPhaseShift(this, creator);

        Entry = scriptPackageId;
        ObjectScale = 1.0f;

        SetUpdateFieldValue(Values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.ScriptPackageID), (int)scriptPackageId);
        SetUpdateFieldValue(Values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.RndSeedVal), GameTime.CurrentTimeMS);
        SetUpdateFieldValue(Values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.CreatedBy), creator.GUID);
        SetUpdateFieldValue(Values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.SceneType), (uint)type);

        return Location.Map.AddToMap(this);
    }

    private void RelocateStationaryPosition(Position pos)
    {
        _stationaryPosition.Relocate(pos);
    }

    private void Remove()
    {
        if (Location.IsInWorld)
            Location.AddObjectToRemoveList();
    }

    private bool ShouldBeRemoved()
    {
        var creator = ObjectAccessor.GetUnit(this, OwnerGUID);

        if (creator == null)
            return true;

        if (_createdBySpellCast.IsEmpty)
            return false;
        // search for a dummy aura on creator

        var linkedAura = creator.GetAuraQuery().HasSpellId(_createdBySpellCast.Entry).HasCastId(_createdBySpellCast).GetResults().FirstOrDefault();

        return linkedAura == null;
    }
}
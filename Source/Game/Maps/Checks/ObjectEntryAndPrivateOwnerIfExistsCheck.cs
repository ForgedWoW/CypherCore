using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class ObjectEntryAndPrivateOwnerIfExistsCheck : ICheck<WorldObject>
{
    ObjectGuid _ownerGUID;
    uint _entry;

    public ObjectEntryAndPrivateOwnerIfExistsCheck(ObjectGuid ownerGUID, uint entry)
    {
        _ownerGUID = ownerGUID;
        _entry = entry;
    }

    public bool Invoke(WorldObject obj)
    {
        return obj.GetEntry() == _entry && (!obj.IsPrivateObject() || obj.GetPrivateObjectOwner() == _ownerGUID);
    }
}
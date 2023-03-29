// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Objects.Update;

public class UpdateFieldHolder
{
    private readonly UpdateMask _changesMask = new((int)TypeId.Max);

    public UpdateFieldHolder(WorldObject owner) { }

    public BaseUpdateData<T> ModifyValue<T>(BaseUpdateData<T> updateData)
    {
        _changesMask.Set(updateData.Bit);

        return updateData;
    }

    public void ClearChangesMask<T>(BaseUpdateData<T> updateData)
    {
        _changesMask.Reset(updateData.Bit);
        updateData.ClearChangesMask();
    }

    public void ClearChangesMask<T, U>(BaseUpdateData<T> updateData, ref UpdateField<U> updateField) where T : new() where U : new()
    {
        _changesMask.Reset(updateData.Bit);

        var hasChangesMask = (IHasChangesMask)updateField.Value;

        if (hasChangesMask != null)
            hasChangesMask.ClearChangesMask();
    }

    public uint GetChangedObjectTypeMask()
    {
        return _changesMask.GetBlock(0);
    }

    public bool HasChanged(TypeId index)
    {
        return _changesMask[(int)index];
    }
}
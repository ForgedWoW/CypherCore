// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using System;

namespace Forged.MapServer.Entities.Objects.Update;

public class StableInfo : BaseUpdateData<StableInfo>
{
    public DynamicUpdateField<StablePetInfo> Pets = new(0, 1);
    public UpdateField<ObjectGuid> StableMaster = new(0, 2);

    public StableInfo()  : base(3)
    {

    }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteUInt32((uint)Pets.Size());
        data.WritePackedGuid(StableMaster);

        foreach (var pet in Pets)
        {
            pet.WriteCreate(data, owner, receiver);
        }
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 3);
        
        if (changesMask[0] && changesMask[1])
        {
            if (!ignoreChangesMask)
                Pets.WriteUpdateMask(data);
            else
                WriteCompleteDynamicFieldUpdateMask(Pets.Size(), data);
        }

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
            {
                foreach (var pet in Pets)
                {
                    if (Pets.HasChanged(Pets.FindIndex(pet)) || ignoreChangesMask)
                    {
                        pet.WriteUpdate(data, ignoreChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[2])
            {
                data.WritePackedGuid(StableMaster);
            }
        }
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(Pets);
        ClearChangesMask(StableMaster);
        ChangesMask.ResetAll();
    }
}
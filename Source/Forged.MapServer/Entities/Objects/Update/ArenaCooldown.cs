// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class ArenaCooldown : BaseUpdateData<Player>
{
    public UpdateField<int> Charges = new(0, 2);
    public UpdateField<uint> EndTime = new(0, 5);
    public UpdateField<uint> Flags = new(0, 3);
    public UpdateField<byte> MaxCharges = new(0, 7);
    public UpdateField<uint> NextChargeTime = new(0, 6);
    public UpdateField<int> SpellID = new(0, 1);
    public UpdateField<uint> StartTime = new(0, 4);
    public ArenaCooldown() : base(8) { }

    public override void ClearChangesMask()
    {
        ClearChangesMask(SpellID);
        ClearChangesMask(Charges);
        ClearChangesMask(Flags);
        ClearChangesMask(StartTime);
        ClearChangesMask(EndTime);
        ClearChangesMask(NextChargeTime);
        ClearChangesMask(MaxCharges);
        ChangesMask.ResetAll();
    }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteInt32(SpellID);
        data.WriteInt32(Charges);
        data.WriteUInt32(Flags);
        data.WriteUInt32(StartTime);
        data.WriteUInt32(EndTime);
        data.WriteUInt32(NextChargeTime);
        data.WriteUInt8(MaxCharges);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 8);

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
                data.WriteInt32(SpellID);

            if (changesMask[2])
                data.WriteInt32(Charges);

            if (changesMask[3])
                data.WriteUInt32(Flags);

            if (changesMask[4])
                data.WriteUInt32(StartTime);

            if (changesMask[5])
                data.WriteUInt32(EndTime);

            if (changesMask[6])
                data.WriteUInt32(NextChargeTime);

            if (changesMask[7])
                data.WriteUInt8(MaxCharges);
        }
    }
}
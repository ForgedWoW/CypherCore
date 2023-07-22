// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class SetupCurrency : ServerPacket
{
    public List<Record> Data = new();
    public SetupCurrency() : base(ServerOpcodes.SetupCurrency, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Data.Count);

        foreach (var data in Data)
        {
            WorldPacket.WriteUInt32(data.Type);
            WorldPacket.WriteUInt32(data.Quantity);

            WorldPacket.WriteBit(data.WeeklyQuantity.HasValue);
            WorldPacket.WriteBit(data.MaxWeeklyQuantity.HasValue);
            WorldPacket.WriteBit(data.TrackedQuantity.HasValue);
            WorldPacket.WriteBit(data.MaxQuantity.HasValue);
            WorldPacket.WriteBit(data.TotalEarned.HasValue);
            WorldPacket.WriteBit(data.NextRechargeTime.HasValue);
            WorldPacket.WriteBit(data.RechargeCycleStartTime.HasValue);
            WorldPacket.WriteBits(data.Flags, 5);
            WorldPacket.FlushBits();

            if (data.WeeklyQuantity.HasValue)
                WorldPacket.WriteUInt32(data.WeeklyQuantity.Value);

            if (data.MaxWeeklyQuantity.HasValue)
                WorldPacket.WriteUInt32(data.MaxWeeklyQuantity.Value);

            if (data.TrackedQuantity.HasValue)
                WorldPacket.WriteUInt32(data.TrackedQuantity.Value);

            if (data.MaxQuantity.HasValue)
                WorldPacket.WriteInt32(data.MaxQuantity.Value);

            if (data.TotalEarned.HasValue)
                WorldPacket.WriteInt32(data.TotalEarned.Value);

            if (data.NextRechargeTime.HasValue)
                WorldPacket.WriteInt64(data.NextRechargeTime.Value);
            if (data.RechargeCycleStartTime.HasValue)
                WorldPacket.WriteInt64(data.RechargeCycleStartTime.Value);
        }
    }

    public struct Record
    {
        public byte Flags;
        public long? NextRechargeTime;
        public long? RechargeCycleStartTime;
        public int? MaxQuantity;
        public uint? MaxWeeklyQuantity;
        public uint Quantity;

        public int? TotalEarned;

        // Weekly Currency cap.
        public uint? TrackedQuantity;

        public uint Type;
        public uint? WeeklyQuantity; // Currency count obtained this Week.  
    }
}
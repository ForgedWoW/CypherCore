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
        _worldPacket.WriteInt32(Data.Count);

        foreach (var data in Data)
        {
            _worldPacket.WriteUInt32(data.Type);
            _worldPacket.WriteUInt32(data.Quantity);

            _worldPacket.WriteBit(data.WeeklyQuantity.HasValue);
            _worldPacket.WriteBit(data.MaxWeeklyQuantity.HasValue);
            _worldPacket.WriteBit(data.TrackedQuantity.HasValue);
            _worldPacket.WriteBit(data.MaxQuantity.HasValue);
            _worldPacket.WriteBit(data.TotalEarned.HasValue);
            _worldPacket.WriteBit(data.LastSpendTime.HasValue);
            _worldPacket.WriteBits(data.Flags, 5);
            _worldPacket.FlushBits();

            if (data.WeeklyQuantity.HasValue)
                _worldPacket.WriteUInt32(data.WeeklyQuantity.Value);

            if (data.MaxWeeklyQuantity.HasValue)
                _worldPacket.WriteUInt32(data.MaxWeeklyQuantity.Value);

            if (data.TrackedQuantity.HasValue)
                _worldPacket.WriteUInt32(data.TrackedQuantity.Value);

            if (data.MaxQuantity.HasValue)
                _worldPacket.WriteInt32(data.MaxQuantity.Value);

            if (data.TotalEarned.HasValue)
                _worldPacket.WriteInt32(data.TotalEarned.Value);

            if (data.LastSpendTime.HasValue)
                _worldPacket.WriteInt64(data.LastSpendTime.Value);
        }
    }

    public struct Record
    {
        public byte Flags;
        public long? LastSpendTime;
        public int? MaxQuantity;
        public uint? MaxWeeklyQuantity;
        public uint Quantity;
        public int? TotalEarned;
        // Weekly Currency cap.
        public uint? TrackedQuantity;

        public uint Type;
        public uint? WeeklyQuantity;    // Currency count obtained this Week.  
    }
}
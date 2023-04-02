// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class SetCurrency : ServerPacket
{
    public uint? FirstCraftOperationID;
    public CurrencyGainFlags Flags;
    public long? LastSpendTime;
    public int? MaxQuantity;
    public int Quantity;
    public int? QuantityChange;
    public CurrencyGainSource? QuantityGainSource;
    public CurrencyDestroyReason? QuantityLostSource;
    public bool SuppressChatLog;
    public List<UiEventToast> Toasts = new();
    public int? TotalEarned;
    public int? TrackedQuantity;
    public uint Type;
    public int? WeeklyQuantity;
    public SetCurrency() : base(ServerOpcodes.SetCurrency, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(Type);
        WorldPacket.WriteInt32(Quantity);
        WorldPacket.WriteUInt32((uint)Flags);
        WorldPacket.WriteInt32(Toasts.Count);

        foreach (var toast in Toasts)
            toast.Write(WorldPacket);

        WorldPacket.WriteBit(WeeklyQuantity.HasValue);
        WorldPacket.WriteBit(TrackedQuantity.HasValue);
        WorldPacket.WriteBit(MaxQuantity.HasValue);
        WorldPacket.WriteBit(TotalEarned.HasValue);
        WorldPacket.WriteBit(SuppressChatLog);
        WorldPacket.WriteBit(QuantityChange.HasValue);
        WorldPacket.WriteBit(QuantityGainSource.HasValue);
        WorldPacket.WriteBit(QuantityLostSource.HasValue);
        WorldPacket.WriteBit(FirstCraftOperationID.HasValue);
        WorldPacket.WriteBit(LastSpendTime.HasValue);
        WorldPacket.FlushBits();

        if (WeeklyQuantity.HasValue)
            WorldPacket.WriteInt32(WeeklyQuantity.Value);

        if (TrackedQuantity.HasValue)
            WorldPacket.WriteInt32(TrackedQuantity.Value);

        if (MaxQuantity.HasValue)
            WorldPacket.WriteInt32(MaxQuantity.Value);

        if (TotalEarned.HasValue)
            WorldPacket.WriteInt32(TotalEarned.Value);

        if (QuantityChange.HasValue)
            WorldPacket.WriteInt32(QuantityChange.Value);

        if (QuantityGainSource.HasValue)
            WorldPacket.WriteInt32((int)QuantityGainSource.Value);

        if (QuantityLostSource.HasValue)
            WorldPacket.WriteInt32((int)QuantityLostSource.Value);

        if (FirstCraftOperationID.HasValue)
            WorldPacket.WriteUInt32(FirstCraftOperationID.Value);

        if (LastSpendTime.HasValue)
            WorldPacket.WriteInt64(LastSpendTime.Value);
    }
}
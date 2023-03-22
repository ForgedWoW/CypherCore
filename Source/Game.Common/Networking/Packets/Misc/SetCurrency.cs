// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class SetCurrency : ServerPacket
{
	public uint Type;
	public int Quantity;
	public CurrencyGainFlags Flags;
	public List<UiEventToast> Toasts = new();
	public int? WeeklyQuantity;
	public int? TrackedQuantity;
	public int? MaxQuantity;
	public int? TotalEarned;
	public int? QuantityChange;
	public CurrencyGainSource? QuantityGainSource;
	public CurrencyDestroyReason? QuantityLostSource;
	public uint? FirstCraftOperationID;
	public long? LastSpendTime;
	public bool SuppressChatLog;
	public SetCurrency() : base(ServerOpcodes.SetCurrency, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Type);
		_worldPacket.WriteInt32(Quantity);
		_worldPacket.WriteUInt32((uint)Flags);
		_worldPacket.WriteInt32(Toasts.Count);

		foreach (var toast in Toasts)
			toast.Write(_worldPacket);

		_worldPacket.WriteBit(WeeklyQuantity.HasValue);
		_worldPacket.WriteBit(TrackedQuantity.HasValue);
		_worldPacket.WriteBit(MaxQuantity.HasValue);
		_worldPacket.WriteBit(TotalEarned.HasValue);
		_worldPacket.WriteBit(SuppressChatLog);
		_worldPacket.WriteBit(QuantityChange.HasValue);
		_worldPacket.WriteBit(QuantityGainSource.HasValue);
		_worldPacket.WriteBit(QuantityLostSource.HasValue);
		_worldPacket.WriteBit(FirstCraftOperationID.HasValue);
		_worldPacket.WriteBit(LastSpendTime.HasValue);
		_worldPacket.FlushBits();

		if (WeeklyQuantity.HasValue)
			_worldPacket.WriteInt32(WeeklyQuantity.Value);

		if (TrackedQuantity.HasValue)
			_worldPacket.WriteInt32(TrackedQuantity.Value);

		if (MaxQuantity.HasValue)
			_worldPacket.WriteInt32(MaxQuantity.Value);

		if (TotalEarned.HasValue)
			_worldPacket.WriteInt32(TotalEarned.Value);

		if (QuantityChange.HasValue)
			_worldPacket.WriteInt32(QuantityChange.Value);

		if (QuantityGainSource.HasValue)
			_worldPacket.WriteInt32((int)QuantityGainSource.Value);

		if (QuantityLostSource.HasValue)
			_worldPacket.WriteInt32((int)QuantityLostSource.Value);

		if (FirstCraftOperationID.HasValue)
			_worldPacket.WriteUInt32(FirstCraftOperationID.Value);

		if (LastSpendTime.HasValue)
			_worldPacket.WriteInt64(LastSpendTime.Value);
	}
}
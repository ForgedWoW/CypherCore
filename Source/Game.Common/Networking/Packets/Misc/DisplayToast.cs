// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.Misc;

public class DisplayToast : ServerPacket
{
	public ulong Quantity;
	public DisplayToastMethod DisplayToastMethod;
	public bool Mailed;
	public DisplayToastType Type = DisplayToastType.Money;
	public uint QuestID;
	public bool IsSecondaryResult;
	public ItemInstance Item;
	public bool BonusRoll;
	public int LootSpec;
	public Gender Gender = Gender.None;
	public uint CurrencyID;

	public DisplayToast() : base(ServerOpcodes.DisplayToast, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(Quantity);
		_worldPacket.WriteUInt8((byte)DisplayToastMethod);
		_worldPacket.WriteUInt32(QuestID);

		_worldPacket.WriteBit(Mailed);
		_worldPacket.WriteBits((byte)Type, 2);
		_worldPacket.WriteBit(IsSecondaryResult);

		switch (Type)
		{
			case DisplayToastType.NewItem:
				_worldPacket.WriteBit(BonusRoll);
				Item.Write(_worldPacket);
				_worldPacket.WriteInt32(LootSpec);
				_worldPacket.WriteInt32((int)Gender);

				break;
			case DisplayToastType.NewCurrency:
				_worldPacket.WriteUInt32(CurrencyID);

				break;
			default:
				break;
		}

		_worldPacket.FlushBits();
	}
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Bpay;

namespace Game.Common.Networking.Packets.Bpay;

public class DistributionUpdate : ServerPacket
{
	public BpayDistributionObject DistributionObject { get; set; } = new();

	public DistributionUpdate() : base(ServerOpcodes.BattlePayDistributionUpdate) { }

	public override void Write()
	{
		DistributionObject.Write(_worldPacket);
	}
}

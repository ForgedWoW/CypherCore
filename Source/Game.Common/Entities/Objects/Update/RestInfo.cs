// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class RestInfo : BaseUpdateData<Player>
{
	public UpdateField<uint> Threshold = new(0, 1);
	public UpdateField<byte> StateID = new(0, 2);

	public RestInfo() : base(3) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteUInt32(Threshold);
		data.WriteUInt8(StateID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 3);

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[1])
				data.WriteUInt32(Threshold);

			if (changesMask[2])
				data.WriteUInt8(StateID);
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Threshold);
		ClearChangesMask(StateID);
		ChangesMask.ResetAll();
	}
}

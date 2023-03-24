// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Items;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class SelectedAzeriteEssences : BaseUpdateData<AzeriteItem>
{
	public UpdateField<bool> Enabled = new(0, 1);
	public UpdateField<uint> SpecializationID = new(0, 2);
	public UpdateFieldArray<uint> AzeriteEssenceID = new(4, 3, 4);

	public SelectedAzeriteEssences() : base(8) { }

	public void WriteCreate(WorldPacket data, AzeriteItem owner, Player receiver)
	{
		for (var i = 0; i < 4; ++i)
			data.WriteUInt32(AzeriteEssenceID[i]);

		data.WriteUInt32(SpecializationID);
		data.WriteBit(Enabled);
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AzeriteItem owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlocksMask(0), 1);

		if (changesMask.GetBlock(0) != 0)
			data.WriteBits(changesMask.GetBlock(0), 32);

		if (changesMask[0])
			if (changesMask[1])
				data.WriteBit(Enabled);

		data.FlushBits();

		if (changesMask[0])
			if (changesMask[2])
				data.WriteUInt32(SpecializationID);

		if (changesMask[3])
			for (var i = 0; i < 4; ++i)
				if (changesMask[4 + i])
					data.WriteUInt32(AzeriteEssenceID[i]);

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Enabled);
		ClearChangesMask(SpecializationID);
		ClearChangesMask(AzeriteEssenceID);
		ChangesMask.ResetAll();
	}
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public struct SpellTargetedHealPrediction
{
	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(TargetGUID);
		Predict.Write(data);
	}

	public ObjectGuid TargetGUID;
	public SpellHealPrediction Predict;
}
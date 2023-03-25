// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public class DungeonScoreBestRunForAffix
{
	public int KeystoneAffixID;
	public MythicPlusRun Run = new();
	public float Score;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(KeystoneAffixID);
		data.WriteFloat(Score);
		Run.Write(data);
	}
}
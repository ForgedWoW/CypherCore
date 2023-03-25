// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class GameObjectStats
{
	public string[] Name = new string[4];
	public string IconName;
	public string CastBarCaption;
	public string UnkString;
	public uint Type;
	public uint DisplayID;
	public int[] Data = new int[SharedConst.MaxGOData];
	public float Size;
	public List<uint> QuestItems = new();
	public uint ContentTuningId;
}
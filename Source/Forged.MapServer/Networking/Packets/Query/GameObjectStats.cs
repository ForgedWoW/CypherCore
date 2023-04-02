// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class GameObjectStats
{
    public string CastBarCaption;
    public uint ContentTuningId;
    public int[] Data = new int[SharedConst.MaxGOData];
    public uint DisplayID;
    public string IconName;
    public string[] Name = new string[4];
    public List<uint> QuestItems = new();
    public float Size;
    public uint Type;
    public string UnkString;
}
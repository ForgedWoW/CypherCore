// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GameObjectsRecord
{
    public uint DisplayID;
    public uint Id;
    public LocalizedString Name;
    public uint OwnerID;
    public int PhaseGroupID;
    public int PhaseID;
    public int PhaseUseFlags;
    public Vector3 Pos;
    public int[] PropValue = new int[8];
    public float[] Rot = new float[4];
    public float Scale;
    public GameObjectTypes TypeID;
}
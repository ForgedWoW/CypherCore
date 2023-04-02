// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TaxiNodesRecord
{
    public ushort CharacterBitNumber;
    public uint ConditionID;
    public ushort ContinentID;
    public float Facing;
    public TaxiNodeFlags Flags;
    public Vector2 FlightMapOffset;
    public uint Id;
    public Vector2 MapOffset;
    public int MinimapAtlasMemberID;
    public uint[] MountCreatureID = new uint[2];
    public LocalizedString Name;
    public Vector3 Pos;
    public uint SpecialIconConditionID;
    public int UiTextureKitID;
    public uint VisibilityConditionID;
}
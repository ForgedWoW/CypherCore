// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TaxiNodesRecord
{
	public LocalizedString Name;
	public Vector3 Pos;
	public Vector2 MapOffset;
	public Vector2 FlightMapOffset;
	public uint Id;
	public ushort ContinentID;
	public uint ConditionID;
	public ushort CharacterBitNumber;
	public TaxiNodeFlags Flags;
	public int UiTextureKitID;
	public int MinimapAtlasMemberID;
	public float Facing;
	public uint SpecialIconConditionID;
	public uint VisibilityConditionID;
	public uint[] MountCreatureID = new uint[2];
}
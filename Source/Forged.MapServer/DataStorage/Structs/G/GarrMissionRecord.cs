// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrMissionRecord
{
    public uint Id;
    public LocalizedString Name;
    public LocalizedString Location;
    public LocalizedString Description;
    public Vector2 MapPos;
    public Vector2 WorldPos;
    public byte GarrTypeID;
    public byte GarrMissionTypeID;
    public sbyte GarrFollowerTypeID;
    public byte MaxFollowers;
    public uint MissionCost;
    public ushort MissionCostCurrencyTypesID;
    public byte OfferedGarrMissionTextureID;
    public ushort UiTextureKitID;
    public uint EnvGarrMechanicID;
    public int EnvGarrMechanicTypeID;
    public uint PlayerConditionID;
    public int GarrMissionSetID;
    public sbyte TargetLevel;
    public ushort TargetItemLevel;
    public int MissionDuration;
    public int TravelDuration;
    public uint OfferDuration;
    public byte BaseCompletionChance;
    public uint BaseFollowerXP;
    public uint OvermaxRewardPackID;
    public byte FollowerDeathChance;
    public uint AreaID;
    public int Flags;
    public float AutoMissionScalar;
    public int AutoMissionScalarCurveID;
    public int AutoCombatantEnvCasterID;
}
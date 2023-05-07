// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrMissionRecord
{
    public uint AreaID;
    public int AutoCombatantEnvCasterID;
    public float AutoMissionScalar;
    public int AutoMissionScalarCurveID;
    public byte BaseCompletionChance;
    public uint BaseFollowerXP;
    public LocalizedString Description;
    public uint EnvGarrMechanicID;
    public int EnvGarrMechanicTypeID;
    public int Flags;
    public byte FollowerDeathChance;
    public sbyte GarrFollowerTypeID;
    public int GarrMissionSetID;
    public byte GarrMissionTypeID;
    public byte GarrTypeID;
    public uint Id;
    public LocalizedString Location;
    public Vector2 MapPos;
    public byte MaxFollowers;
    public uint MissionCost;
    public ushort MissionCostCurrencyTypesID;
    public int MissionDuration;
    public LocalizedString Name;
    public uint OfferDuration;
    public byte OfferedGarrMissionTextureID;
    public uint OvermaxRewardPackID;
    public uint PlayerConditionID;
    public ushort TargetItemLevel;
    public sbyte TargetLevel;
    public int TravelDuration;
    public ushort UiTextureKitID;
    public Vector2 WorldPos;
}
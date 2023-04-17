// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Game.BattleFields;

internal static class WgConst
{
    public const uint MAP_ID = 571; // Northrend

    public const byte MAX_OUTSIDE_NPCS = 14;
    public const byte OUTSIDE_ALLIANCE_NPC = 7;
    public const byte MAX_WORKSHOPS = 6;

    #region Data

    public static BfWgCoordGY[] WgGraveYard =
    {
        new(5104.750f, 2300.940f, 368.579f, 0.733038f, 1329, WgGossipText.GYNE, TeamIds.Neutral), new(5099.120f, 3466.036f, 368.484f, 5.317802f, 1330, WgGossipText.GYNW, TeamIds.Neutral), new(4314.648f, 2408.522f, 392.642f, 6.268125f, 1333, WgGossipText.GYSE, TeamIds.Neutral), new(4331.716f, 3235.695f, 390.251f, 0.008500f, 1334, WgGossipText.GYSW, TeamIds.Neutral), new(5537.986f, 2897.493f, 517.057f, 4.819249f, 1285, WgGossipText.GY_KEEP, TeamIds.Neutral), new(5032.454f, 3711.382f, 372.468f, 3.971623f, 1331, WgGossipText.GY_HORDE, TeamIds.Horde), new(5140.790f, 2179.120f, 390.950f, 1.972220f, 1332, WgGossipText.GY_ALLIANCE, TeamIds.Alliance)
    };

    public static WorldStates[] ClockWorldState =
    {
        WorldStates.BattlefieldWgTimeBattleEnd, WorldStates.BattlefieldWgTimeNextBattle
    };

    public static uint[] WintergraspFaction =
    {
        1732, 1735, 35
    };

    public static Position WintergraspStalkerPos = new(4948.985f, 2937.789f, 550.5172f, 1.815142f);

    public static Position RelicPos = new(5440.379f, 2840.493f, 430.2816f, -1.832595f);
    public static Quaternion RelicRot = new(0.0f, 0.0f, -0.7933531f, 0.6087617f);

    //Destructible (Wall, Tower..)
    public static WintergraspBuildingSpawnData[] WgGameObjectBuilding =
    {
        // Wall (Not spawned in db)
        // Entry  WS      X          Y          Z           O                rX   rY   rZ             rW             Type
        new(190219, 3749, 5371.457f, 3047.472f, 407.5710f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.00000000f, WgGameObjectBuildingType.Wall), new(190220, 3750, 5331.264f, 3047.105f, 407.9228f, 0.05235888f, 0.0f, 0.0f, 0.026176450f, 0.99965730f, WgGameObjectBuildingType.Wall), new(191795, 3764, 5385.841f, 2909.490f, 409.7127f, 0.00872424f, 0.0f, 0.0f, 0.004362106f, 0.99999050f, WgGameObjectBuildingType.Wall), new(191796, 3772, 5384.452f, 2771.835f, 410.2704f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.00000000f, WgGameObjectBuildingType.Wall), new(191799, 3762, 5371.436f, 2630.610f, 408.8163f, 3.13285800f, 0.0f, 0.0f, 0.999990500f, 0.00436732f, WgGameObjectBuildingType.Wall), new(191800, 3766, 5301.838f, 2909.089f, 409.8661f, 0.00872424f, 0.0f, 0.0f, 0.004362106f, 0.99999050f, WgGameObjectBuildingType.Wall), new(191801, 3770, 5301.063f, 2771.411f, 409.9014f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.00000000f, WgGameObjectBuildingType.Wall), new(191802, 3751, 5280.197f, 2995.583f, 408.8249f, 1.61442800f, 0.0f, 0.0f, 0.722363500f, 0.69151360f, WgGameObjectBuildingType.Wall), new(191803, 3752, 5279.136f, 2956.023f, 408.6041f, 1.57079600f, 0.0f, 0.0f, 0.707106600f, 0.70710690f, WgGameObjectBuildingType.Wall), new(191804, 3767, 5278.685f, 2882.513f, 409.5388f, 1.57079600f, 0.0f, 0.0f, 0.707106600f, 0.70710690f, WgGameObjectBuildingType.Wall), new(191806, 3769, 5279.502f, 2798.945f, 409.9983f, 1.57079600f, 0.0f, 0.0f, 0.707106600f, 0.70710690f, WgGameObjectBuildingType.Wall), new(191807, 3759, 5279.937f, 2724.766f, 409.9452f, 1.56207000f, 0.0f, 0.0f, 0.704014800f, 0.71018530f, WgGameObjectBuildingType.Wall), new(191808, 3760, 5279.601f, 2683.786f, 409.8488f, 1.55334100f, 0.0f, 0.0f, 0.700908700f, 0.71325110f, WgGameObjectBuildingType.Wall), new(191809, 3761, 5330.955f, 2630.777f, 409.2826f, 3.13285800f, 0.0f, 0.0f, 0.999990500f, 0.00436732f, WgGameObjectBuildingType.Wall), new(190369, 3753, 5256.085f, 2933.963f, 409.3571f, 3.13285800f, 0.0f, 0.0f, 0.999990500f, 0.00436732f, WgGameObjectBuildingType.Wall), new(190370, 3758, 5257.463f, 2747.327f, 409.7427f, -3.13285800f, 0.0f, 0.0f, -0.999990500f, 0.00436732f, WgGameObjectBuildingType.Wall), new(190371, 3754, 5214.960f, 2934.089f, 409.1905f, -0.00872424f, 0.0f, 0.0f, -0.004362106f, 0.99999050f, WgGameObjectBuildingType.Wall), new(190372, 3757, 5215.821f, 2747.566f, 409.1884f, -3.13285800f, 0.0f, 0.0f, -0.999990500f, 0.00436732f, WgGameObjectBuildingType.Wall), new(190374, 3755, 5162.273f, 2883.043f, 410.2556f, 1.57952200f, 0.0f, 0.0f, 0.710185100f, 0.70401500f, WgGameObjectBuildingType.Wall), new(190376, 3756, 5163.724f, 2799.838f, 409.2270f, 1.57952200f, 0.0f, 0.0f, 0.710185100f, 0.70401500f, WgGameObjectBuildingType.Wall),

        // Tower of keep (Not spawned in db)
        new(190221, 3711, 5281.154f, 3044.588f, 407.8434f, 3.115388f, 0.0f, 0.0f, 0.9999142f, 0.013101960f, WgGameObjectBuildingType.KeepTower),   // NW
        new(190373, 3713, 5163.757f, 2932.228f, 409.1904f, 3.124123f, 0.0f, 0.0f, 0.9999619f, 0.008734641f, WgGameObjectBuildingType.KeepTower),   // SW
        new(190377, 3714, 5166.397f, 2748.368f, 409.1884f, -1.570796f, 0.0f, 0.0f, -0.7071066f, 0.707106900f, WgGameObjectBuildingType.KeepTower), // SE
        new(190378, 3712, 5281.192f, 2632.479f, 409.0985f, -1.588246f, 0.0f, 0.0f, -0.7132492f, 0.700910500f, WgGameObjectBuildingType.KeepTower), // NE

        // Wall (with passage) (Not spawned in db)
        new(191797, 3765, 5343.290f, 2908.860f, 409.5757f, 0.00872424f, 0.0f, 0.0f, 0.004362106f, 0.9999905f, WgGameObjectBuildingType.Wall), new(191798, 3771, 5342.719f, 2771.386f, 409.6249f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.0000000f, WgGameObjectBuildingType.Wall), new(191805, 3768, 5279.126f, 2840.797f, 409.7826f, 1.57952200f, 0.0f, 0.0f, 0.710185100f, 0.7040150f, WgGameObjectBuildingType.Wall),

        // South tower (Not spawned in db)
        new(190356, 3704, 4557.173f, 3623.943f, 395.8828f, 1.675516f, 0.0f, 0.0f, 0.7431450f, 0.669130400f, WgGameObjectBuildingType.Tower),   // W
        new(190357, 3705, 4398.172f, 2822.497f, 405.6270f, -3.124123f, 0.0f, 0.0f, -0.9999619f, 0.008734641f, WgGameObjectBuildingType.Tower), // S
        new(190358, 3706, 4459.105f, 1944.326f, 434.9912f, -2.002762f, 0.0f, 0.0f, -0.8422165f, 0.539139500f, WgGameObjectBuildingType.Tower), // E

        // Door of forteress (Not spawned in db)
        new(WgGameObjects.FORTRESS_GATE, 3763, 5162.991f, 2841.232f, 410.1892f, -3.132858f, 0.0f, 0.0f, -0.9999905f, 0.00436732f, WgGameObjectBuildingType.Door),

        // Last door (Not spawned in db)
        new(WgGameObjects.VAULT_GATE, 3773, 5397.108f, 2841.54f, 425.9014f, 3.141593f, 0.0f, 0.0f, -1.0f, 0.0f, WgGameObjectBuildingType.DoorLast)
    };

    public static StaticWintergraspTowerInfo[] TowerData =
    {
        new(WintergraspTowerIds.FORTRESS_NW, WintergraspText.NW_KEEPTOWER_DAMAGE, WintergraspText.NW_KEEPTOWER_DESTROY), new(WintergraspTowerIds.FORTRESS_SW, WintergraspText.SW_KEEPTOWER_DAMAGE, WintergraspText.SW_KEEPTOWER_DESTROY), new(WintergraspTowerIds.FORTRESS_SE, WintergraspText.SE_KEEPTOWER_DAMAGE, WintergraspText.SE_KEEPTOWER_DESTROY), new(WintergraspTowerIds.FORTRESS_NE, WintergraspText.NE_KEEPTOWER_DAMAGE, WintergraspText.NE_KEEPTOWER_DESTROY), new(WintergraspTowerIds.SHADOWSIGHT, WintergraspText.WESTERN_TOWER_DAMAGE, WintergraspText.WESTERN_TOWER_DESTROY), new(WintergraspTowerIds.WINTERS_EDGE, WintergraspText.SOUTHERN_TOWER_DAMAGE, WintergraspText.SOUTHERN_TOWER_DESTROY), new(WintergraspTowerIds.FLAMEWATCH, WintergraspText.EASTERN_TOWER_DAMAGE, WintergraspText.EASTERN_TOWER_DESTROY)
    };

    public static Position[] WgTurret =
    {
        new(5391.19f, 3060.8f, 419.616f, 1.69557f), new(5266.75f, 2976.5f, 421.067f, 3.20354f), new(5234.86f, 2948.8f, 420.88f, 1.61311f), new(5323.05f, 2923.7f, 421.645f, 1.5817f), new(5363.82f, 2923.87f, 421.709f, 1.60527f), new(5264.04f, 2861.34f, 421.587f, 3.21142f), new(5264.68f, 2819.78f, 421.656f, 3.15645f), new(5322.16f, 2756.69f, 421.646f, 4.69978f), new(5363.78f, 2756.77f, 421.629f, 4.78226f), new(5236.2f, 2732.68f, 421.649f, 4.72336f), new(5265.02f, 2704.63f, 421.7f, 3.12507f), new(5350.87f, 2616.03f, 421.243f, 4.72729f), new(5390.95f, 2615.5f, 421.126f, 4.6409f), new(5148.8f, 2820.24f, 421.621f, 3.16043f), new(5147.98f, 2861.93f, 421.63f, 3.18792f)
    };

    public static WintergraspGameObjectData[] WgPortalDefenderData =
    {
        // Player teleporter
        new(5153.408f, 2901.349f, 409.1913f, -0.06981169f, 0.0f, 0.0f, -0.03489876f, 0.9993908f, 190763, 191575), new(5268.698f, 2666.421f, 409.0985f, -0.71558490f, 0.0f, 0.0f, -0.35020730f, 0.9366722f, 190763, 191575), new(5197.050f, 2944.814f, 409.1913f, 2.33874000f, 0.0f, 0.0f, 0.92050460f, 0.3907318f, 190763, 191575), new(5196.671f, 2737.345f, 409.1892f, -2.93213900f, 0.0f, 0.0f, -0.99452110f, 0.1045355f, 190763, 191575), new(5314.580f, 3055.852f, 408.8620f, 0.54105060f, 0.0f, 0.0f, 0.26723770f, 0.9636307f, 190763, 191575), new(5391.277f, 2828.094f, 418.6752f, -2.16420600f, 0.0f, 0.0f, -0.88294700f, 0.4694727f, 190763, 191575), new(5153.931f, 2781.671f, 409.2455f, 1.65806200f, 0.0f, 0.0f, 0.73727700f, 0.6755905f, 190763, 191575), new(5311.445f, 2618.931f, 409.0916f, -2.37364400f, 0.0f, 0.0f, -0.92718320f, 0.3746083f, 190763, 191575), new(5269.208f, 3013.838f, 408.8276f, -1.76278200f, 0.0f, 0.0f, -0.77162460f, 0.6360782f, 190763, 191575), new(5401.634f, 2853.667f, 418.6748f, 2.63544400f, 0.0f, 0.0f, 0.96814730f, 0.2503814f, 192819, 192819), // return portal inside fortress, neutral

        // Vehicle teleporter
        new(5314.515f, 2703.687f, 408.5502f, -0.89011660f, 0.0f, 0.0f, -0.43051050f, 0.9025856f, 192951, 192951), new(5316.252f, 2977.042f, 408.5385f, -0.82030330f, 0.0f, 0.0f, -0.39874840f, 0.9170604f, 192951, 192951)
    };

    public static WintergraspTowerData[] AttackTowers =
    {
        //West Tower
        new()
        {
            TowerEntry = 190356,
            GameObject = new WintergraspGameObjectData[]
            {
                new(4559.113f, 3606.216f, 419.9992f, 4.799657f, 0.0f, 0.0f, -0.67558960f, 0.73727790f, 192488, 192501), // Flag on tower
                new(4539.420f, 3622.490f, 420.0342f, 3.211419f, 0.0f, 0.0f, -0.99939060f, 0.03490613f, 192488, 192501), // Flag on tower
                new(4555.258f, 3641.648f, 419.9740f, 1.675514f, 0.0f, 0.0f, 0.74314400f, 0.66913150f, 192488, 192501),  // Flag on tower
                new(4574.872f, 3625.911f, 420.0792f, 0.087266f, 0.0f, 0.0f, 0.04361916f, 0.99904820f, 192488, 192501),  // Flag on tower
                new(4433.899f, 3534.142f, 360.2750f, 4.433136f, 0.0f, 0.0f, -0.79863550f, 0.60181500f, 192269, 192278), // Flag near workshop
                new(4572.933f, 3475.519f, 363.0090f, 1.422443f, 0.0f, 0.0f, 0.65275960f, 0.75756520f, 192269, 192277)   // Flag near bridge
            },
            CreatureBottom = new WintergraspObjectPositionData[]
            {
                new(4418.688477f, 3506.251709f, 358.975494f, 4.293305f, WgNpcs.GUARD_H, WgNpcs.GUARD_A) // Roaming Guard
            }
        },

        //South Tower
        new()
        {
            TowerEntry = 190357,
            GameObject = new WintergraspGameObjectData[]
            {
                new(4416.004f, 2822.666f, 429.8512f, 6.2657330f, 0.0f, 0.0f, -0.00872612f, 0.99996190f, 192488, 192501), // Flag on tower
                new(4398.819f, 2804.698f, 429.7920f, 4.6949370f, 0.0f, 0.0f, -0.71325020f, 0.70090960f, 192488, 192501), // Flag on tower
                new(4387.622f, 2719.566f, 389.9351f, 4.7385700f, 0.0f, 0.0f, -0.69779010f, 0.71630230f, 192366, 192414), // Flag near tower
                new(4464.124f, 2855.453f, 406.1106f, 0.8290324f, 0.0f, 0.0f, 0.40274720f, 0.91531130f, 192366, 192429),  // Flag near tower
                new(4526.457f, 2810.181f, 391.1997f, 3.2899610f, 0.0f, 0.0f, -0.99724960f, 0.07411628f, 192269, 192278)  // Flag near bridge
            },
            CreatureBottom = new WintergraspObjectPositionData[]
            {
                new(4452.859863f, 2808.870117f, 402.604004f, 6.056290f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4455.899902f, 2835.958008f, 401.122559f, 0.034907f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4412.649414f, 2953.792236f, 374.799957f, 0.980838f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Roaming Guard
                new(4362.089844f, 2811.510010f, 407.337006f, 3.193950f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4412.290039f, 2753.790039f, 401.015015f, 5.829400f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4421.939941f, 2773.189941f, 400.894989f, 5.707230f, WgNpcs.GUARD_H, WgNpcs.GUARD_A)  // Standing Guard
            }
        },

        //East Tower
        new()
        {
            TowerEntry = 190358,
            GameObject = new WintergraspGameObjectData[]
            {
                new(4466.793f, 1960.418f, 459.1437f, 1.151916f, 0.0f, 0.0f, 0.5446386f, 0.8386708f, 192488, 192501),  // Flag on tower
                new(4475.351f, 1937.031f, 459.0702f, 5.846854f, 0.0f, 0.0f, -0.2164392f, 0.9762961f, 192488, 192501), // Flag on tower
                new(4451.758f, 1928.104f, 459.0759f, 4.276057f, 0.0f, 0.0f, -0.8433914f, 0.5372996f, 192488, 192501), // Flag on tower
                new(4442.987f, 1951.898f, 459.0930f, 2.740162f, 0.0f, 0.0f, 0.9799242f, 0.1993704f, 192488, 192501)   // Flag on tower
            },
            CreatureBottom = new WintergraspObjectPositionData[]
            {
                new(4501.060059f, 1990.280029f, 431.157013f, 1.029740f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4463.830078f, 2015.180054f, 430.299988f, 1.431170f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4494.580078f, 1943.760010f, 435.627014f, 6.195920f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4450.149902f, 1897.579956f, 435.045013f, 4.398230f, WgNpcs.GUARD_H, WgNpcs.GUARD_A), // Standing Guard
                new(4428.870117f, 1906.869995f, 432.648010f, 3.996800f, WgNpcs.GUARD_H, WgNpcs.GUARD_A)  // Standing Guard
            }
        }
    };

    public static WintergraspTowerCannonData[] TowerCannon =
    {
        new()
        {
            TowerEntry = 190221,
            TurretTop = new Position[]
            {
                new(5255.88f, 3047.63f, 438.499f, 3.13677f), new(5280.9f, 3071.32f, 438.499f, 1.62879f)
            }
        },
        new()
        {
            TowerEntry = 190373,
            TurretTop = new Position[]
            {
                new(5138.59f, 2935.16f, 439.845f, 3.11723f), new(5163.06f, 2959.52f, 439.846f, 1.47258f)
            }
        },
        new()
        {
            TowerEntry = 190377,
            TurretTop = new Position[]
            {
                new(5163.84f, 2723.74f, 439.844f, 1.3994f), new(5139.69f, 2747.4f, 439.844f, 3.17221f)
            }
        },
        new()
        {
            TowerEntry = 190378,
            TurretTop = new Position[]
            {
                new(5278.21f, 2607.23f, 439.755f, 4.71944f), new(5255.01f, 2631.98f, 439.755f, 3.15257f)
            }
        },
        new()
        {
            TowerEntry = 190356,
            TowerCannonBottom = new Position[]
            {
                new(4537.380371f, 3599.531738f, 402.886993f, 3.998462f), new(4581.497559f, 3604.087158f, 402.886963f, 5.651723f)
            },
            TurretTop = new Position[]
            {
                new(4469.448242f, 1966.623779f, 465.647217f, 1.153573f), new(4581.895996f, 3626.438477f, 426.539062f, 0.117806f)
            }
        },
        new()
        {
            TowerEntry = 190357,
            TowerCannonBottom = new Position[]
            {
                new(4421.640137f, 2799.935791f, 412.630920f, 5.459298f), new(4420.263184f, 2845.340332f, 412.630951f, 0.742197f)
            },
            TurretTop = new Position[]
            {
                new(4423.430664f, 2822.762939f, 436.283142f, 6.223487f), new(4397.825684f, 2847.629639f, 436.283325f, 1.579430f), new(4398.814941f, 2797.266357f, 436.283051f, 4.703747f)
            }
        },
        new()
        {
            TowerEntry = 190358,
            TowerCannonBottom = new Position[]
            {
                new(4448.138184f, 1974.998779f, 441.995911f, 1.967238f), new(4448.713379f, 1955.148682f, 441.995178f, 0.380733f)
            },
            TurretTop = new Position[]
            {
                new(4469.448242f, 1966.623779f, 465.647217f, 1.153573f), new(4481.996582f, 1933.658325f, 465.647186f, 5.873029f)
            }
        }
    };

    public static StaticWintergraspWorkshopInfo[] WorkshopData =
    {
        new()
        {
            WorkshopId = WgWorkshopIds.NE,
            WorldStateId = WorldStates.BattlefieldWgWorkshopNe,
            AllianceCaptureTextId = WintergraspText.SUNKEN_RING_CAPTURE_ALLIANCE,
            AllianceAttackTextId = WintergraspText.SUNKEN_RING_ATTACK_ALLIANCE,
            HordeCaptureTextId = WintergraspText.SUNKEN_RING_CAPTURE_HORDE,
            HordeAttackTextId = WintergraspText.SUNKEN_RING_ATTACK_HORDE
        },
        new()
        {
            WorkshopId = WgWorkshopIds.NW,
            WorldStateId = WorldStates.BattlefieldWgWorkshopNw,
            AllianceCaptureTextId = WintergraspText.BROKEN_TEMPLE_CAPTURE_ALLIANCE,
            AllianceAttackTextId = WintergraspText.BROKEN_TEMPLE_ATTACK_ALLIANCE,
            HordeCaptureTextId = WintergraspText.BROKEN_TEMPLE_CAPTURE_HORDE,
            HordeAttackTextId = WintergraspText.BROKEN_TEMPLE_ATTACK_HORDE
        },
        new()
        {
            WorkshopId = WgWorkshopIds.SE,
            WorldStateId = WorldStates.BattlefieldWgWorkshopSe,
            AllianceCaptureTextId = WintergraspText.EASTSPARK_CAPTURE_ALLIANCE,
            AllianceAttackTextId = WintergraspText.EASTSPARK_ATTACK_ALLIANCE,
            HordeCaptureTextId = WintergraspText.EASTSPARK_CAPTURE_HORDE,
            HordeAttackTextId = WintergraspText.EASTSPARK_ATTACK_HORDE
        },
        new()
        {
            WorkshopId = WgWorkshopIds.SW,
            WorldStateId = WorldStates.BattlefieldWgWorkshopSw,
            AllianceCaptureTextId = WintergraspText.WESTSPARK_CAPTURE_ALLIANCE,
            AllianceAttackTextId = WintergraspText.WESTSPARK_ATTACK_ALLIANCE,
            HordeCaptureTextId = WintergraspText.WESTSPARK_CAPTURE_HORDE,
            HordeAttackTextId = WintergraspText.WESTSPARK_ATTACK_HORDE
        },

        // KEEP WORKSHOPS - It can't be taken, so it doesn't have a textids
        new()
        {
            WorkshopId = WgWorkshopIds.KEEP_WEST,
            WorldStateId = WorldStates.BattlefieldWgWorkshopKW
        },
        new()
        {
            WorkshopId = WgWorkshopIds.KEEP_EAST,
            WorldStateId = WorldStates.BattlefieldWgWorkshopKE
        }
    };

    #endregion
}

internal struct WgData
{
    public const int DAMAGED_TOWER_DEF = 0;
    public const int BROKEN_TOWER_DEF = 1;
    public const int DAMAGED_TOWER_ATT = 2;
    public const int BROKEN_TOWER_ATT = 3;
    public const int MAX_VEHICLE_A = 4;
    public const int MAX_VEHICLE_H = 5;
    public const int VEHICLE_A = 6;
    public const int VEHICLE_H = 7;
    public const int MAX = 8;
}

internal struct WgAchievements
{
    public const uint WIN_WG = 1717;
    public const uint WIN_WG100 = 1718;         // @Todo: Has To Be Implemented
    public const uint WG_GNOMESLAUGHTER = 1723; // @Todo: Has To Be Implemented
    public const uint WG_TOWER_DESTROY = 1727;
    public const uint DESTRUCTION_DERBY_A = 1737; // @Todo: Has To Be Implemented
    public const uint WG_TOWER_CANNON_KILL = 1751; // @Todo: Has To Be Implemented
    public const uint WG_MASTER_A = 1752;         // @Todo: Has To Be Implemented
    public const uint WIN_WG_TIMER10 = 1755;
    public const uint STONE_KEEPER50 = 2085;     // @Todo: Has To Be Implemented
    public const uint STONE_KEEPER100 = 2086;    // @Todo: Has To Be Implemented
    public const uint STONE_KEEPER250 = 2087;    // @Todo: Has To Be Implemented
    public const uint STONE_KEEPER500 = 2088;    // @Todo: Has To Be Implemented
    public const uint STONE_KEEPER1000 = 2089;   // @Todo: Has To Be Implemented
    public const uint WG_RANGER = 2199;          // @Todo: Has To Be Implemented
    public const uint DESTRUCTION_DERBY_H = 2476; // @Todo: Has To Be Implemented
    public const uint WG_MASTER_H = 2776;         // @Todo: Has To Be Implemented
}

internal struct WgSpells
{
    // Wartime Auras
    public const uint RECRUIT = 37795;
    public const uint CORPORAL = 33280;
    public const uint LIEUTENANT = 55629;
    public const uint TENACITY = 58549;
    public const uint TENACITY_VEHICLE = 59911;
    public const uint TOWER_CONTROL = 62064;
    public const uint SPIRITUAL_IMMUNITY = 58729;
    public const uint GREAT_HONOR = 58555;
    public const uint GREATER_HONOR = 58556;
    public const uint GREATEST_HONOR = 58557;
    public const uint ALLIANCE_FLAG = 14268;
    public const uint HORDE_FLAG = 14267;
    public const uint GRAB_PASSENGER = 61178;

    // Reward Spells
    public const uint VICTORY_REWARD = 56902;
    public const uint DEFEAT_REWARD = 58494;
    public const uint DAMAGED_TOWER = 59135;
    public const uint DESTROYED_TOWER = 59136;
    public const uint DAMAGED_BUILDING = 59201;
    public const uint INTACT_BUILDING = 59203;

    public const uint TELEPORT_BRIDGE = 59096;
    public const uint TELEPORT_FORTRESS = 60035;

    public const uint TELEPORT_DALARAN = 53360;
    public const uint VICTORY_AURA = 60044;

    // Other Spells
    public const uint WINTERGRASP_WATER = 36444;
    public const uint ESSENCE_OF_WINTERGRASP = 58045;
    public const uint WINTERGRASP_RESTRICTED_FLIGHT_AREA = 91604;

    // Phasing Spells
    public const uint HORDE_CONTROLS_FACTORY_PHASE_SHIFT = 56618;    // Adds Phase 16
    public const uint ALLIANCE_CONTROLS_FACTORY_PHASE_SHIFT = 56617; // Adds Phase 32

    public const uint HORDE_CONTROL_PHASE_SHIFT = 55773;    // Adds Phase 64
    public const uint ALLIANCE_CONTROL_PHASE_SHIFT = 55774; // Adds Phase 128
}

internal struct WgNpcs
{
    public const uint GUARD_H = 30739;
    public const uint GUARD_A = 30740;
    public const uint STALKER = 15214;

    public const uint TAUNKA_SPIRIT_GUIDE = 31841;  // Horde Spirit Guide For Wintergrasp
    public const uint DWARVEN_SPIRIT_GUIDE = 31842; // Alliance Spirit Guide For Wintergrasp

    public const uint SIEGE_ENGINE_ALLIANCE = 28312;
    public const uint SIEGE_ENGINE_HORDE = 32627;
    public const uint CATAPULT = 27881;
    public const uint DEMOLISHER = 28094;
    public const uint TOWER_CANNON = 28366;
}

internal struct WgGameObjects
{
    public const uint FACTORY_BANNER_NE = 190475;
    public const uint FACTORY_BANNER_NW = 190487;
    public const uint FACTORY_BANNER_SE = 194959;
    public const uint FACTORY_BANNER_SW = 194962;

    public const uint TITAN_S_RELIC = 192829;

    public const uint FORTRESS_TOWER1 = 190221;
    public const uint FORTRESS_TOWER2 = 190373;
    public const uint FORTRESS_TOWER3 = 190377;
    public const uint FORTRESS_TOWER4 = 190378;

    public const uint SHADOWSIGHT_TOWER = 190356;
    public const uint WINTER_S_EDGE_TOWER = 190357;
    public const uint FLAMEWATCH_TOWER = 190358;

    public const uint FORTRESS_GATE = 190375;
    public const uint VAULT_GATE = 191810;

    public const uint KEEP_COLLISION_WALL = 194323;
}

internal struct WintergraspTowerIds
{
    public const byte FORTRESS_NW = 0;
    public const byte FORTRESS_SW = 1;
    public const byte FORTRESS_SE = 2;
    public const byte FORTRESS_NE = 3;
    public const byte SHADOWSIGHT = 4;
    public const byte WINTERS_EDGE = 5;
    public const byte FLAMEWATCH = 6;
}

internal struct WgWorkshopIds
{
    public const byte SE = 0;
    public const byte SW = 1;
    public const byte NE = 2;
    public const byte NW = 3;
    public const byte KEEP_WEST = 4;
    public const byte KEEP_EAST = 5;
}

internal struct WgGossipText
{
    public const int GYNE = 20071;
    public const int GYNW = 20072;
    public const int GYSE = 20074;
    public const int GYSW = 20073;
    public const int GY_KEEP = 20070;
    public const int GY_HORDE = 20075;
    public const int GY_ALLIANCE = 20076;
}

internal struct WgGraveyardId
{
    public const uint WORKSHOP_NE = 0;
    public const uint WORKSHOP_NW = 1;
    public const uint WORKSHOP_SE = 2;
    public const uint WORKSHOP_SW = 3;
    public const uint KEEP = 4;
    public const uint HORDE = 5;
    public const uint ALLIANCE = 6;
    public const uint MAX = 7;
}

internal struct WintergraspQuests
{
    public const uint VICTORY_ALLIANCE = 13181;
    public const uint VICTORY_HORDE = 13183;
    public const uint CREDIT_TOWERS_DESTROYED = 35074;
    public const uint CREDIT_DEFEND_SIEGE = 31284;
}

internal struct WintergraspText
{
    // Invisible Stalker
    public const byte SOUTHERN_TOWER_DAMAGE = 1;
    public const byte SOUTHERN_TOWER_DESTROY = 2;
    public const byte EASTERN_TOWER_DAMAGE = 3;
    public const byte EASTERN_TOWER_DESTROY = 4;
    public const byte WESTERN_TOWER_DAMAGE = 5;
    public const byte WESTERN_TOWER_DESTROY = 6;
    public const byte NW_KEEPTOWER_DAMAGE = 7;
    public const byte NW_KEEPTOWER_DESTROY = 8;
    public const byte SE_KEEPTOWER_DAMAGE = 9;
    public const byte SE_KEEPTOWER_DESTROY = 10;
    public const byte BROKEN_TEMPLE_ATTACK_ALLIANCE = 11;
    public const byte BROKEN_TEMPLE_CAPTURE_ALLIANCE = 12;
    public const byte BROKEN_TEMPLE_ATTACK_HORDE = 13;
    public const byte BROKEN_TEMPLE_CAPTURE_HORDE = 14;
    public const byte EASTSPARK_ATTACK_ALLIANCE = 15;
    public const byte EASTSPARK_CAPTURE_ALLIANCE = 16;
    public const byte EASTSPARK_ATTACK_HORDE = 17;
    public const byte EASTSPARK_CAPTURE_HORDE = 18;
    public const byte SUNKEN_RING_ATTACK_ALLIANCE = 19;
    public const byte SUNKEN_RING_CAPTURE_ALLIANCE = 20;
    public const byte SUNKEN_RING_ATTACK_HORDE = 21;
    public const byte SUNKEN_RING_CAPTURE_HORDE = 22;
    public const byte WESTSPARK_ATTACK_ALLIANCE = 23;
    public const byte WESTSPARK_CAPTURE_ALLIANCE = 24;
    public const byte WESTSPARK_ATTACK_HORDE = 25;
    public const byte WESTSPARK_CAPTURE_HORDE = 26;

    public const byte START_GROUPING = 27;
    public const byte START_BATTLE = 28;
    public const byte FORTRESS_DEFEND_ALLIANCE = 29;
    public const byte FORTRESS_CAPTURE_ALLIANCE = 30;
    public const byte FORTRESS_DEFEND_HORDE = 31;
    public const byte FORTRESS_CAPTURE_HORDE = 32;

    public const byte NE_KEEPTOWER_DAMAGE = 33;
    public const byte NE_KEEPTOWER_DESTROY = 34;
    public const byte SW_KEEPTOWER_DAMAGE = 35;
    public const byte SW_KEEPTOWER_DESTROY = 36;

    public const byte RANK_CORPORAL = 37;
    public const byte RANK_FIRST_LIEUTENANT = 38;
}

internal enum WgGameObjectState
{
    None,
    NeutralIntact,
    NeutralDamage,
    NeutralDestroy,
    HordeIntact,
    HordeDamage,
    HordeDestroy,
    AllianceIntact,
    AllianceDamage,
    AllianceDestroy
}

internal enum WgGameObjectBuildingType
{
    Door,
    Titanrelic,
    Wall,
    DoorLast,
    KeepTower,
    Tower
}

//Data Structs
internal struct BfWgCoordGY
{
    public BfWgCoordGY(float x, float y, float z, float o, uint graveyardId, int textId, uint startControl)
    {
        Pos = new Position(x, y, z, o);
        GraveyardID = graveyardId;
        TextId = textId;
        StartControl = startControl;
    }

    public Position Pos;
    public uint GraveyardID;
    public int TextId; // for gossip menu
    public uint StartControl;
}

internal struct WintergraspBuildingSpawnData
{
    public WintergraspBuildingSpawnData(uint entry, uint worldstate, float x, float y, float z, float o, float rX, float rY, float rZ, float rW, WgGameObjectBuildingType type)
    {
        Entry = entry;
        WorldState = worldstate;
        Pos = new Position(x, y, z, o);
        Rot = new Quaternion(rX, rY, rZ, rW);
        BuildingType = type;
    }

    public uint Entry;
    public uint WorldState;
    public Position Pos;
    public Quaternion Rot;
    public WgGameObjectBuildingType BuildingType;
}

internal struct WintergraspGameObjectData
{
    public WintergraspGameObjectData(float x, float y, float z, float o, float rX, float rY, float rZ, float rW, uint hordeEntry, uint allianceEntry)
    {
        Pos = new Position(x, y, z, o);
        Rot = new Quaternion(rX, rY, rZ, rW);
        HordeEntry = hordeEntry;
        AllianceEntry = allianceEntry;
    }

    public Position Pos;
    public Quaternion Rot;
    public uint HordeEntry;
    public uint AllianceEntry;
}

internal class WintergraspTowerData
{
    // Creature: Turrets and Guard // @todo: Killed on Tower destruction ? Tower Damage ? Requires confirming
    public WintergraspObjectPositionData[] CreatureBottom = new WintergraspObjectPositionData[9];
    public WintergraspGameObjectData[] GameObject = new WintergraspGameObjectData[6]; // Gameobject position and entry (Horde/Alliance)
    public uint TowerEntry;                                                           // Gameobject Id of tower
}

internal struct WintergraspObjectPositionData
{
    public WintergraspObjectPositionData(float x, float y, float z, float o, uint hordeEntry, uint allianceEntry)
    {
        Pos = new Position(x, y, z, o);
        HordeEntry = hordeEntry;
        AllianceEntry = allianceEntry;
    }

    public Position Pos;
    public uint HordeEntry;
    public uint AllianceEntry;
}

internal class WintergraspTowerCannonData
{
    public Position[] TowerCannonBottom;

    public uint TowerEntry;
    public Position[] TurretTop;

    public WintergraspTowerCannonData()
    {
        TowerCannonBottom = Array.Empty<Position>();
        TurretTop = Array.Empty<Position>();
    }
}

internal class StaticWintergraspWorkshopInfo
{
    public byte AllianceAttackTextId;
    public byte AllianceCaptureTextId;
    public byte HordeAttackTextId;
    public byte HordeCaptureTextId;
    public byte WorkshopId;
    public WorldStates WorldStateId;
}

internal class StaticWintergraspTowerInfo
{
    public byte DamagedTextId;
    public byte DestroyedTextId;

    public byte TowerId;

    public StaticWintergraspTowerInfo(byte towerId, byte damagedTextId, byte destroyedTextId)
    {
        TowerId = towerId;
        DamagedTextId = damagedTextId;
        DestroyedTextId = destroyedTextId;
    }
}
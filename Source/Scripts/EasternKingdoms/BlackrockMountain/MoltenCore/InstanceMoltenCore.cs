// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore;

internal struct DataTypes
{
    public const uint LUCIFRON = 0;
    public const uint MAGMADAR = 1;
    public const uint GEHENNAS = 2;
    public const uint GARR = 3;
    public const uint SHAZZRAH = 4;
    public const uint BARON_GEDDON = 5;
    public const uint SULFURON_HARBINGER = 6;
    public const uint GOLEMAGG_THE_INCINERATOR = 7;
    public const uint MAJORDOMO_EXECUTUS = 8;
    public const uint RAGNAROS = 9;

    public const uint MAX_ENCOUNTER = 10;
}

internal struct ActionIds
{
    public const int START_RAGNAROS = 0;
    public const int START_RAGNAROS_ALT = 1;
}

internal struct McCreatureIds
{
    public const uint LUCIFRON = 12118;
    public const uint MAGMADAR = 11982;
    public const uint GEHENNAS = 12259;
    public const uint GARR = 12057;
    public const uint SHAZZRAH = 12264;
    public const uint BARON_GEDDON = 12056;
    public const uint SULFURON_HARBINGER = 12098;
    public const uint GOLEMAGG_THE_INCINERATOR = 11988;
    public const uint MAJORDOMO_EXECUTUS = 12018;
    public const uint RAGNAROS = 11502;
    public const uint FLAMEWAKER_HEALER = 11663;
    public const uint FLAMEWAKER_ELITE = 11664;
}

internal struct McGameObjectIds
{
    public const uint CACHE_OF_THE_FIRELORD = 179703;
}

internal struct McMiscConst
{
    public const uint DATA_RAGNAROS_ADDS = 0;

    public static Position[] SummonPositions =
    {
        new(737.850f, -1145.35f, -120.288f, 4.71368f), new(744.162f, -1151.63f, -119.726f, 4.58204f), new(751.247f, -1152.82f, -119.744f, 4.49673f), new(759.206f, -1155.09f, -120.051f, 4.30104f), new(755.973f, -1152.33f, -120.029f, 4.25588f), new(731.712f, -1147.56f, -120.195f, 4.95955f), new(726.499f, -1149.80f, -120.156f, 5.24055f), new(722.408f, -1152.41f, -120.029f, 5.33087f), new(718.994f, -1156.36f, -119.805f, 5.75738f), new(838.510f, -829.840f, -232.000f, 2.00000f)
    };

    public static Position RagnarosTelePos = new(829.159f, -815.773f, -228.972f, 5.30500f);
    public static Position RagnarosSummonPos = new(838.510f, -829.840f, -232.000f, 2.00000f);
}

[Script]
internal class InstanceMoltenCore : InstanceMapScript, IInstanceMapGetInstanceScript
{
    private static readonly DungeonEncounterData[] Encounters =
    {
        new(DataTypes.LUCIFRON, 663), new(DataTypes.MAGMADAR, 664), new(DataTypes.GEHENNAS, 665), new(DataTypes.GARR, 666), new(DataTypes.SHAZZRAH, 667), new(DataTypes.BARON_GEDDON, 668), new(DataTypes.SULFURON_HARBINGER, 669), new(DataTypes.GOLEMAGG_THE_INCINERATOR, 670), new(DataTypes.MAJORDOMO_EXECUTUS, 671), new(DataTypes.RAGNAROS, 672)
    };

    public InstanceMoltenCore() : base(nameof(InstanceMoltenCore), 409) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceMoltenCoreInstanceMapScript(map);
    }

    private class InstanceMoltenCoreInstanceMapScript : InstanceScript
    {
        private ObjectGuid _cacheOfTheFirelordGUID;
        private bool _executusSchedule;
        private ObjectGuid _golemaggTheIncineratorGUID;
        private ObjectGuid _majordomoExecutusGUID;
        private byte _ragnarosAddDeaths;

        public InstanceMoltenCoreInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("MC");
            SetBossNumber(DataTypes.MAX_ENCOUNTER);
            LoadDungeonEncounterData(Encounters);
            _executusSchedule = false;
            _ragnarosAddDeaths = 0;
        }

        public override void OnPlayerEnter(Player player)
        {
            if (_executusSchedule)
                SummonMajordomoExecutus();
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch (creature.Entry)
            {
                case McCreatureIds.GOLEMAGG_THE_INCINERATOR:
                    _golemaggTheIncineratorGUID = creature.GUID;

                    break;
                case McCreatureIds.MAJORDOMO_EXECUTUS:
                    _majordomoExecutusGUID = creature.GUID;

                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            switch (go.Entry)
            {
                case McGameObjectIds.CACHE_OF_THE_FIRELORD:
                    _cacheOfTheFirelordGUID = go.GUID;

                    break;
            }
        }

        public override void SetData(uint type, uint data)
        {
            if (type == McMiscConst.DATA_RAGNAROS_ADDS)
            {
                if (data == 1)
                    ++_ragnarosAddDeaths;
                else if (data == 0)
                    _ragnarosAddDeaths = 0;
            }
        }

        public override uint GetData(uint type)
        {
            switch (type)
            {
                case McMiscConst.DATA_RAGNAROS_ADDS:
                    return _ragnarosAddDeaths;
            }

            return 0;
        }

        public override ObjectGuid GetGuidData(uint type)
        {
            switch (type)
            {
                case DataTypes.GOLEMAGG_THE_INCINERATOR:
                    return _golemaggTheIncineratorGUID;
                case DataTypes.MAJORDOMO_EXECUTUS:
                    return _majordomoExecutusGUID;
            }

            return ObjectGuid.Empty;
        }

        public override bool SetBossState(uint bossId, EncounterState state)
        {
            if (!base.SetBossState(bossId, state))
                return false;

            if (state == EncounterState.Done &&
                bossId < DataTypes.MAJORDOMO_EXECUTUS)
                if (CheckMajordomoExecutus())
                    SummonMajordomoExecutus();

            if (bossId == DataTypes.MAJORDOMO_EXECUTUS &&
                state == EncounterState.Done)
                DoRespawnGameObject(_cacheOfTheFirelordGUID, TimeSpan.FromDays(7));

            return true;
        }

        public override void AfterDataLoad()
        {
            if (CheckMajordomoExecutus())
                _executusSchedule = true;
        }

        private void SummonMajordomoExecutus()
        {
            _executusSchedule = false;

            if (!_majordomoExecutusGUID.IsEmpty)
                return;

            if (GetBossState(DataTypes.MAJORDOMO_EXECUTUS) != EncounterState.Done)
            {
                Instance.SummonCreature(McCreatureIds.MAJORDOMO_EXECUTUS, McMiscConst.SummonPositions[0]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_HEALER, McMiscConst.SummonPositions[1]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_HEALER, McMiscConst.SummonPositions[2]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_HEALER, McMiscConst.SummonPositions[3]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_HEALER, McMiscConst.SummonPositions[4]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_ELITE, McMiscConst.SummonPositions[5]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_ELITE, McMiscConst.SummonPositions[6]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_ELITE, McMiscConst.SummonPositions[7]);
                Instance.SummonCreature(McCreatureIds.FLAMEWAKER_ELITE, McMiscConst.SummonPositions[8]);
            }
            else
            {
                var summon = Instance.SummonCreature(McCreatureIds.MAJORDOMO_EXECUTUS, McMiscConst.RagnarosTelePos);

                if (summon)
                    summon.AI.DoAction(ActionIds.START_RAGNAROS_ALT);
            }
        }

        private bool CheckMajordomoExecutus()
        {
            if (GetBossState(DataTypes.RAGNAROS) == EncounterState.Done)
                return false;

            for (byte i = 0; i < DataTypes.MAJORDOMO_EXECUTUS; ++i)
                if (GetBossState(i) != EncounterState.Done)
                    return false;

            return true;
        }
    }
}
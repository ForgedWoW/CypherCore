// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects.Update;

public class UnitData : BaseUpdateData<Unit>
{
    public UpdateField<List<uint>> StateWorldEffectIDs = new(0, 1);
    public DynamicUpdateField<PassiveSpellHistory> PassiveSpells = new(0, 2);
    public DynamicUpdateField<int> WorldEffects = new(0, 3);
    public DynamicUpdateField<ObjectGuid> ChannelObjects = new(0, 4);
    public UpdateField<uint> DisplayID = new(0, 5);
    public UpdateField<uint> StateSpellVisualID = new(0, 6);
    public UpdateField<uint> StateAnimID = new(0, 7);
    public UpdateField<uint> StateAnimKitID = new(0, 8);
    public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new(0, 9);
    public UpdateField<int> SpellOverrideNameID = new(0, 10);
    public UpdateField<ObjectGuid> Charm = new(0, 11);
    public UpdateField<ObjectGuid> Summon = new(0, 12);
    public UpdateField<ObjectGuid> Critter = new(0, 13);
    public UpdateField<ObjectGuid> CharmedBy = new(0, 14);
    public UpdateField<ObjectGuid> SummonedBy = new(0, 15);
    public UpdateField<ObjectGuid> CreatedBy = new(0, 16);
    public UpdateField<ObjectGuid> DemonCreator = new(0, 17);
    public UpdateField<ObjectGuid> LookAtControllerTarget = new(0, 18);
    public UpdateField<ObjectGuid> Target = new(0, 19);
    public UpdateField<ObjectGuid> BattlePetCompanionGUID = new(0, 20);
    public UpdateField<ulong> BattlePetDBID = new(0, 21);
    public UpdateField<UnitChannel> ChannelData = new(0, 22);
    public UpdateField<sbyte> SpellEmpowerStage = new(0, 23);
    public UpdateField<uint> SummonedByHomeRealm = new(0, 24);
    public UpdateField<byte> Race = new(0, 25);
    public UpdateField<byte> ClassId = new(0, 26);
    public UpdateField<byte> PlayerClassId = new(0, 27);
    public UpdateField<byte> Sex = new(0, 28);
    public UpdateField<byte> DisplayPower = new(0, 29);
    public UpdateField<uint> OverrideDisplayPowerID = new(0, 30);
    public UpdateField<long> Health = new(0, 31);
    public UpdateField<long> MaxHealth = new(32, 33);
    public UpdateField<uint> Level = new(32, 34);
    public UpdateField<int> EffectiveLevel = new(32, 35);
    public UpdateField<uint> ContentTuningID = new(32, 36);
    public UpdateField<int> ScalingLevelMin = new(32, 37);
    public UpdateField<int> ScalingLevelMax = new(32, 38);
    public UpdateField<int> ScalingLevelDelta = new(32, 39);
    public UpdateField<int> ScalingFactionGroup = new(32, 40);
    public UpdateField<int> ScalingHealthItemLevelCurveID = new(32, 41);
    public UpdateField<int> ScalingDamageItemLevelCurveID = new(32, 42);
    public UpdateField<uint> FactionTemplate = new(32, 43);
    public UpdateField<uint> Flags = new(32, 44);
    public UpdateField<uint> Flags2 = new(32, 45);
    public UpdateField<uint> Flags3 = new(32, 46);
    public UpdateField<uint> AuraState = new(32, 47);
    public UpdateField<uint> RangedAttackRoundBaseTime = new(32, 48);
    public UpdateField<float> BoundingRadius = new(32, 49);
    public UpdateField<float> CombatReach = new(32, 50);
    public UpdateField<float> DisplayScale = new(32, 51);
    public UpdateField<int> CreatureFamily = new(32, 52);
    public UpdateField<int> CreatureType = new(32, 53);
    public UpdateField<uint> NativeDisplayID = new(32, 54);
    public UpdateField<float> NativeXDisplayScale = new(32, 55);
    public UpdateField<uint> MountDisplayID = new(32, 56);
    public UpdateField<uint> CosmeticMountDisplayID = new(32, 57);
    public UpdateField<float> MinDamage = new(32, 58);
    public UpdateField<float> MaxDamage = new(32, 59);
    public UpdateField<float> MinOffHandDamage = new(32, 60);
    public UpdateField<float> MaxOffHandDamage = new(32, 61);
    public UpdateField<byte> StandState = new(32, 62);
    public UpdateField<byte> PetTalentPoints = new(32, 63);
    public UpdateField<byte> VisFlags = new(64, 65);
    public UpdateField<byte> AnimTier = new(64, 66);
    public UpdateField<uint> PetNumber = new(64, 67);
    public UpdateField<uint> PetNameTimestamp = new(64, 68);
    public UpdateField<uint> PetExperience = new(64, 69);
    public UpdateField<uint> PetNextLevelExperience = new(64, 70);
    public UpdateField<float> ModCastingSpeed = new(64, 71);
    public UpdateField<float> ModCastingSpeedNeg = new(64, 72);
    public UpdateField<float> ModSpellHaste = new(64, 73);
    public UpdateField<float> ModHaste = new(64, 74);
    public UpdateField<float> ModRangedHaste = new(64, 75);
    public UpdateField<float> ModHasteRegen = new(64, 76);
    public UpdateField<float> ModTimeRate = new(64, 77);
    public UpdateField<uint> CreatedBySpell = new(64, 78);
    public UpdateField<int> EmoteState = new(64, 79);
    public UpdateField<uint> BaseMana = new(64, 80);
    public UpdateField<uint> BaseHealth = new(64, 81);
    public UpdateField<byte> SheatheState = new(64, 82);
    public UpdateField<byte> PvpFlags = new(64, 83);
    public UpdateField<byte> PetFlags = new(64, 84);
    public UpdateField<byte> ShapeshiftForm = new(64, 85);
    public UpdateField<int> AttackPower = new(64, 86);
    public UpdateField<int> AttackPowerModPos = new(64, 87);
    public UpdateField<int> AttackPowerModNeg = new(64, 88);
    public UpdateField<float> AttackPowerMultiplier = new(64, 89);
    public UpdateField<int> RangedAttackPower = new(64, 90);
    public UpdateField<int> RangedAttackPowerModPos = new(64, 91);
    public UpdateField<int> RangedAttackPowerModNeg = new(64, 92);
    public UpdateField<float> RangedAttackPowerMultiplier = new(64, 93);
    public UpdateField<int> MainHandWeaponAttackPower = new(64, 94);
    public UpdateField<int> OffHandWeaponAttackPower = new(64, 95);
    public UpdateField<int> RangedWeaponAttackPower = new(96, 97);
    public UpdateField<int> SetAttackSpeedAura = new(96, 98);
    public UpdateField<float> Lifesteal = new(96, 99);
    public UpdateField<float> MinRangedDamage = new(96, 100);
    public UpdateField<float> MaxRangedDamage = new(96, 101);
    public UpdateField<float> ManaCostMultiplier = new(96, 102);
    public UpdateField<float> MaxHealthModifier = new(96, 103);
    public UpdateField<float> HoverHeight = new(96, 104);
    public UpdateField<uint> MinItemLevelCutoff = new(96, 105);
    public UpdateField<uint> MinItemLevel = new(96, 106);
    public UpdateField<uint> MaxItemLevel = new(96, 107);
    public UpdateField<int> AzeriteItemLevel = new(96, 108);
    public UpdateField<uint> WildBattlePetLevel = new(96, 109);
    public UpdateField<uint> BattlePetCompanionExperience = new(96, 110);
    public UpdateField<uint> BattlePetCompanionNameTimestamp = new(96, 111);
    public UpdateField<int> InteractSpellID = new(96, 112);
    public UpdateField<int> ScaleDuration = new(96, 113);
    public UpdateField<int> LooksLikeMountID = new(96, 114);
    public UpdateField<int> LooksLikeCreatureID = new(96, 115);
    public UpdateField<int> LookAtControllerID = new(96, 116);
    public UpdateField<int> PerksVendorItemID = new(96, 117);
    public UpdateField<int> TaxiNodesID = new(96, 118);
    public UpdateField<ObjectGuid> GuildGUID = new(96, 119);
    public UpdateField<int> FlightCapabilityID = new(96, 120);
    public UpdateField<uint> SilencedSchoolMask = new(96, 121);
    public UpdateField<ObjectGuid> NameplateAttachToGUID = new(96, 122); // When set, nameplate of this unit will instead appear on that object
    public UpdateFieldArray<uint> NpcFlags = new(2, 123, 124);
    public UpdateFieldArray<int> Power = new(7, 126, 127);
    public UpdateFieldArray<uint> MaxPower = new(7, 126, 134);
    public UpdateFieldArray<float> PowerRegenFlatModifier = new(7, 126, 141);
    public UpdateFieldArray<float> PowerRegenInterruptedFlatModifier = new(7, 126, 148);
    public UpdateFieldArray<VisibleItem> VirtualItems = new(3, 155, 156);
    public UpdateFieldArray<uint> AttackRoundBaseTime = new(2, 159, 160);
    public UpdateFieldArray<int> Stats = new(4, 162, 163);
    public UpdateFieldArray<int> StatPosBuff = new(4, 162, 167);
    public UpdateFieldArray<int> StatNegBuff = new(4, 162, 171);
    public UpdateFieldArray<int> Resistances = new(7, 175, 176);
    public UpdateFieldArray<int> BonusResistanceMods = new(7, 175, 183);
    public UpdateFieldArray<int> ManaCostModifier = new(7, 175, 190);

    public UnitData() : base(0, TypeId.Unit, 197) { }

    public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
    {
        data.WriteUInt32(GetViewerDependentDisplayId(this, owner, receiver));

        for (var i = 0; i < 2; ++i)
            data.WriteUInt32(GetViewerDependentNpcFlags(this, i, owner, receiver));

        data.WriteUInt32(StateSpellVisualID);
        data.WriteUInt32(StateAnimID);
        data.WriteUInt32(StateAnimKitID);
        data.WriteInt32(StateWorldEffectIDs.GetValue().Count);
        data.WriteUInt32(StateWorldEffectsQuestObjectiveID);
        data.WriteInt32(SpellOverrideNameID);

        for (var i = 0; i < StateWorldEffectIDs.GetValue().Count; ++i)
            data.WriteUInt32(StateWorldEffectIDs.GetValue()[i]);

        data.WritePackedGuid(Charm);
        data.WritePackedGuid(Summon);

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            data.WritePackedGuid(Critter);

        data.WritePackedGuid(CharmedBy);
        data.WritePackedGuid(SummonedBy);
        data.WritePackedGuid(CreatedBy);
        data.WritePackedGuid(DemonCreator);
        data.WritePackedGuid(LookAtControllerTarget);
        data.WritePackedGuid(Target);
        data.WritePackedGuid(BattlePetCompanionGUID);
        data.WriteUInt64(BattlePetDBID);
        ChannelData.GetValue().WriteCreate(data, owner, receiver);
        data.WriteInt8(SpellEmpowerStage);
        data.WriteUInt32(SummonedByHomeRealm);
        data.WriteUInt8(Race);
        data.WriteUInt8(ClassId);
        data.WriteUInt8(PlayerClassId);
        data.WriteUInt8(Sex);
        data.WriteUInt8(DisplayPower);
        data.WriteUInt32(OverrideDisplayPowerID);
        data.Write(Health);

        for (var i = 0; i < 7; ++i)
        {
            data.WriteInt32(Power[i]);
            data.WriteUInt32(MaxPower[i]);
        }

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
            for (var i = 0; i < 7; ++i)
            {
                data.WriteFloat(PowerRegenFlatModifier[i]);
                data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
            }

        data.Write(MaxHealth);
        data.WriteUInt32(Level);
        data.WriteInt32(EffectiveLevel);
        data.WriteUInt32(ContentTuningID);
        data.WriteInt32(ScalingLevelMin);
        data.WriteInt32(ScalingLevelMax);
        data.WriteInt32(ScalingLevelDelta);
        data.WriteInt32(ScalingFactionGroup);
        data.WriteInt32(ScalingHealthItemLevelCurveID);
        data.WriteInt32(ScalingDamageItemLevelCurveID);
        data.WriteUInt32(GetViewerDependentFactionTemplate(this, owner, receiver));

        for (var i = 0; i < 3; ++i)
            VirtualItems[i].WriteCreate(data, owner, receiver);

        data.WriteUInt32(GetViewerDependentFlags(this, owner, receiver));
        data.WriteUInt32(Flags2);
        data.WriteUInt32(GetViewerDependentFlags3(this, owner, receiver));
        data.WriteUInt32(GetViewerDependentAuraState(this, owner, receiver));

        for (var i = 0; i < 2; ++i)
            data.WriteUInt32(AttackRoundBaseTime[i]);

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            data.WriteUInt32(RangedAttackRoundBaseTime);

        data.WriteFloat(BoundingRadius);
        data.WriteFloat(CombatReach);
        data.WriteFloat(DisplayScale);
        data.WriteInt32(CreatureFamily);
        data.WriteInt32(CreatureType);
        data.WriteUInt32(NativeDisplayID);
        data.WriteFloat(NativeXDisplayScale);
        data.WriteUInt32(MountDisplayID);
        data.WriteUInt32(CosmeticMountDisplayID);

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
        {
            data.WriteFloat(MinDamage);
            data.WriteFloat(MaxDamage);
            data.WriteFloat(MinOffHandDamage);
            data.WriteFloat(MaxOffHandDamage);
        }

        data.WriteUInt8(StandState);
        data.WriteUInt8(PetTalentPoints);
        data.WriteUInt8(VisFlags);
        data.WriteUInt8(AnimTier);
        data.WriteUInt32(PetNumber);
        data.WriteUInt32(PetNameTimestamp);
        data.WriteUInt32(PetExperience);
        data.WriteUInt32(PetNextLevelExperience);
        data.WriteFloat(ModCastingSpeed);
        data.WriteFloat(ModCastingSpeedNeg);
        data.WriteFloat(ModSpellHaste);
        data.WriteFloat(ModHaste);
        data.WriteFloat(ModRangedHaste);
        data.WriteFloat(ModHasteRegen);
        data.WriteFloat(ModTimeRate);
        data.WriteUInt32(CreatedBySpell);
        data.WriteInt32(EmoteState);

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            for (var i = 0; i < 4; ++i)
            {
                data.WriteInt32(Stats[i]);
                data.WriteInt32(StatPosBuff[i]);
                data.WriteInt32(StatNegBuff[i]);
            }

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
            for (var i = 0; i < 7; ++i)
                data.WriteInt32(Resistances[i]);

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            for (var i = 0; i < 7; ++i)
            {
                data.WriteInt32(BonusResistanceMods[i]);
                data.WriteInt32(ManaCostModifier[i]);
            }

        data.WriteUInt32(BaseMana);

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            data.WriteUInt32(BaseHealth);

        data.WriteUInt8(SheatheState);
        data.WriteUInt8((byte)GetViewerDependentPvpFlags(this, owner, receiver));
        data.WriteUInt8(PetFlags);
        data.WriteUInt8(ShapeshiftForm);

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
        {
            data.WriteInt32(AttackPower);
            data.WriteInt32(AttackPowerModPos);
            data.WriteInt32(AttackPowerModNeg);
            data.WriteFloat(AttackPowerMultiplier);
            data.WriteInt32(RangedAttackPower);
            data.WriteInt32(RangedAttackPowerModPos);
            data.WriteInt32(RangedAttackPowerModNeg);
            data.WriteFloat(RangedAttackPowerMultiplier);
            data.WriteInt32(MainHandWeaponAttackPower);
            data.WriteInt32(OffHandWeaponAttackPower);
            data.WriteInt32(RangedWeaponAttackPower);
            data.WriteInt32(SetAttackSpeedAura);
            data.WriteFloat(Lifesteal);
            data.WriteFloat(MinRangedDamage);
            data.WriteFloat(MaxRangedDamage);
            data.WriteFloat(ManaCostMultiplier);
            data.WriteFloat(MaxHealthModifier);
        }

        data.WriteFloat(HoverHeight);
        data.WriteUInt32(MinItemLevelCutoff);
        data.WriteUInt32(MinItemLevel);
        data.WriteUInt32(MaxItemLevel);
        data.WriteInt32(AzeriteItemLevel);
        data.WriteUInt32(WildBattlePetLevel);
        data.WriteUInt32(BattlePetCompanionExperience);
        data.WriteUInt32(BattlePetCompanionNameTimestamp);
        data.WriteInt32(InteractSpellID);
        data.WriteInt32(ScaleDuration);
        data.WriteInt32(LooksLikeMountID);
        data.WriteInt32(LooksLikeCreatureID);
        data.WriteInt32(LookAtControllerID);
        data.WriteInt32(PerksVendorItemID);
        data.WriteInt32(TaxiNodesID);
        data.WritePackedGuid(GuildGUID);
        data.WriteInt32(PassiveSpells.Size());
        data.WriteInt32(WorldEffects.Size());
        data.WriteInt32(ChannelObjects.Size());
        data.WriteInt32(FlightCapabilityID);
        data.WriteUInt32(SilencedSchoolMask);
        data.WritePackedGuid(NameplateAttachToGUID);

        for (var i = 0; i < PassiveSpells.Size(); ++i)
            PassiveSpells[i].WriteCreate(data, owner, receiver);

        for (var i = 0; i < WorldEffects.Size(); ++i)
            data.WriteInt32(WorldEffects[i]);

        for (var i = 0; i < ChannelObjects.Size(); ++i)
            data.WritePackedGuid(ChannelObjects[i]);
    }

    public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
    {
        UpdateMask allowedMaskForTarget = new(195,
                                              new uint[]
                                              {
                                                  0xFFFFDFFFu, 0xC3FEFFFFu, 0x003DFFFFu, 0xFFFFFF01u, 0xF8001FFFu, 0x00000003u, 0x00000000u
                                              });

        AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        WriteUpdate(data, ChangesMask & allowedMaskForTarget, false, owner, receiver);
    }

    public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
    {
        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            allowedMaskForTarget.OR(new UpdateMask(195,
                                                   new uint[]
                                                   {
                                                       0x00002000u, 0x3C010000u, 0xFFC20000u, 0x400000FEu, 0x03FFF000u, 0xFFFFFFFEu, 0x0000000Fu
                                                   }));

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
            allowedMaskForTarget.OR(new UpdateMask(195,
                                                   new uint[]
                                                   {
                                                       0x00000000u, 0x00000000u, 0x00000000u, 0x40000000u, 0x07FFE000u, 0x00000000u, 0x00000000u
                                                   }));

        if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
            allowedMaskForTarget.OR(new UpdateMask(195,
                                                   new uint[]
                                                   {
                                                       0x00000000u, 0x3C000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x007F8000u, 0x00000000u
                                                   }));
    }

    public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
    {
        UpdateMask allowedMaskForTarget = new(195,
                                              new[]
                                              {
                                                  0xFFFFDFFFu, 0xC3FEFFFFu, 0x003DFFFFu, 0xFFFFFF01u, 0xF8001FFFu, 0x00000003u, 0x00000000u
                                              });

        AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        changesMask.AND(allowedMaskForTarget);
    }

    public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Unit owner, Player receiver)
    {
        data.WriteBits(changesMask.GetBlocksMask(0), 7);

        for (uint i = 0; i < 7; ++i)
            if (changesMask.GetBlock(i) != 0)
                data.WriteBits(changesMask.GetBlock(i), 32);

        if (changesMask[0])
            if (changesMask[1])
            {
                data.WriteBits(StateWorldEffectIDs.GetValue().Count, 32);

                for (var i = 0; i < StateWorldEffectIDs.GetValue().Count; ++i)
                    data.WriteUInt32(StateWorldEffectIDs.GetValue()[i]);
            }

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[2])
            {
                if (!ignoreNestedChangesMask)
                    PassiveSpells.WriteUpdateMask(data);
                else
                    WriteCompleteDynamicFieldUpdateMask(PassiveSpells.Size(), data);
            }

            if (changesMask[3])
            {
                if (!ignoreNestedChangesMask)
                    WorldEffects.WriteUpdateMask(data);
                else
                    WriteCompleteDynamicFieldUpdateMask(WorldEffects.Size(), data);
            }

            if (changesMask[4])
            {
                if (!ignoreNestedChangesMask)
                    ChannelObjects.WriteUpdateMask(data);
                else
                    WriteCompleteDynamicFieldUpdateMask(ChannelObjects.Size(), data);
            }
        }

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[2])
                for (var i = 0; i < PassiveSpells.Size(); ++i)
                    if (PassiveSpells.HasChanged(i) || ignoreNestedChangesMask)
                        PassiveSpells[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

            if (changesMask[3])
                for (var i = 0; i < WorldEffects.Size(); ++i)
                    if (WorldEffects.HasChanged(i) || ignoreNestedChangesMask)
                        data.WriteInt32(WorldEffects[i]);

            if (changesMask[4])
                for (var i = 0; i < ChannelObjects.Size(); ++i)
                    if (ChannelObjects.HasChanged(i) || ignoreNestedChangesMask)
                        data.WritePackedGuid(ChannelObjects[i]);

            if (changesMask[5])
                data.WriteUInt32(GetViewerDependentDisplayId(this, owner, receiver));

            if (changesMask[6])
                data.WriteUInt32(StateSpellVisualID);

            if (changesMask[7])
                data.WriteUInt32(StateAnimID);

            if (changesMask[8])
                data.WriteUInt32(StateAnimKitID);

            if (changesMask[9])
                data.WriteUInt32(StateWorldEffectsQuestObjectiveID);

            if (changesMask[10])
                data.WriteInt32(SpellOverrideNameID);

            if (changesMask[11])
                data.WritePackedGuid(Charm);

            if (changesMask[12])
                data.WritePackedGuid(Summon);

            if (changesMask[13])
                data.WritePackedGuid(Critter);

            if (changesMask[14])
                data.WritePackedGuid(CharmedBy);

            if (changesMask[15])
                data.WritePackedGuid(SummonedBy);

            if (changesMask[16])
                data.WritePackedGuid(CreatedBy);

            if (changesMask[17])
                data.WritePackedGuid(DemonCreator);

            if (changesMask[18])
                data.WritePackedGuid(LookAtControllerTarget);

            if (changesMask[19])
                data.WritePackedGuid(Target);

            if (changesMask[20])
                data.WritePackedGuid(BattlePetCompanionGUID);

            if (changesMask[21])
                data.WriteUInt64(BattlePetDBID);

            if (changesMask[22])
                ChannelData.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

            if (changesMask[23])
                data.WriteInt8(SpellEmpowerStage);

            if (changesMask[24])
                data.WriteUInt32(SummonedByHomeRealm);

            if (changesMask[25])
                data.WriteUInt8(Race);

            if (changesMask[26])
                data.WriteUInt8(ClassId);

            if (changesMask[27])
                data.WriteUInt8(PlayerClassId);

            if (changesMask[28])
                data.WriteUInt8(Sex);

            if (changesMask[29])
                data.WriteUInt8(DisplayPower);

            if (changesMask[30])
                data.WriteUInt32(OverrideDisplayPowerID);

            if (changesMask[31])
                data.Write(Health);
        }

        if (changesMask[32])
        {
            if (changesMask[33])
                data.Write(MaxHealth);

            if (changesMask[34])
                data.WriteUInt32(Level);

            if (changesMask[35])
                data.WriteInt32(EffectiveLevel);

            if (changesMask[36])
                data.WriteUInt32(ContentTuningID);

            if (changesMask[37])
                data.WriteInt32(ScalingLevelMin);

            if (changesMask[38])
                data.WriteInt32(ScalingLevelMax);

            if (changesMask[39])
                data.WriteInt32(ScalingLevelDelta);

            if (changesMask[40])
                data.WriteInt32(ScalingFactionGroup);

            if (changesMask[41])
                data.WriteInt32(ScalingHealthItemLevelCurveID);

            if (changesMask[42])
                data.WriteInt32(ScalingDamageItemLevelCurveID);

            if (changesMask[43])
                data.WriteUInt32(GetViewerDependentFactionTemplate(this, owner, receiver));

            if (changesMask[44])
                data.WriteUInt32(GetViewerDependentFlags(this, owner, receiver));

            if (changesMask[45])
                data.WriteUInt32(Flags2);

            if (changesMask[46])
                data.WriteUInt32(GetViewerDependentFlags3(this, owner, receiver));

            if (changesMask[47])
                data.WriteUInt32(GetViewerDependentAuraState(this, owner, receiver));

            if (changesMask[48])
                data.WriteUInt32(RangedAttackRoundBaseTime);

            if (changesMask[49])
                data.WriteFloat(BoundingRadius);

            if (changesMask[50])
                data.WriteFloat(CombatReach);

            if (changesMask[51])
                data.WriteFloat(DisplayScale);

            if (changesMask[52])
                data.WriteInt32(CreatureFamily);

            if (changesMask[53])
                data.WriteInt32(CreatureType);

            if (changesMask[54])
                data.WriteUInt32(NativeDisplayID);

            if (changesMask[55])
                data.WriteFloat(NativeXDisplayScale);

            if (changesMask[56])
                data.WriteUInt32(MountDisplayID);

            if (changesMask[57])
                data.WriteUInt32(CosmeticMountDisplayID);

            if (changesMask[58])
                data.WriteFloat(MinDamage);

            if (changesMask[59])
                data.WriteFloat(MaxDamage);

            if (changesMask[60])
                data.WriteFloat(MinOffHandDamage);

            if (changesMask[61])
                data.WriteFloat(MaxOffHandDamage);

            if (changesMask[62])
                data.WriteUInt8(StandState);

            if (changesMask[63])
                data.WriteUInt8(PetTalentPoints);
        }

        if (changesMask[64])
        {
            if (changesMask[65])
                data.WriteUInt8(VisFlags);

            if (changesMask[66])
                data.WriteUInt8(AnimTier);

            if (changesMask[67])
                data.WriteUInt32(PetNumber);

            if (changesMask[68])
                data.WriteUInt32(PetNameTimestamp);

            if (changesMask[69])
                data.WriteUInt32(PetExperience);

            if (changesMask[70])
                data.WriteUInt32(PetNextLevelExperience);

            if (changesMask[71])
                data.WriteFloat(ModCastingSpeed);

            if (changesMask[72])
                data.WriteFloat(ModCastingSpeedNeg);

            if (changesMask[73])
                data.WriteFloat(ModSpellHaste);

            if (changesMask[74])
                data.WriteFloat(ModHaste);

            if (changesMask[75])
                data.WriteFloat(ModRangedHaste);

            if (changesMask[76])
                data.WriteFloat(ModHasteRegen);

            if (changesMask[77])
                data.WriteFloat(ModTimeRate);

            if (changesMask[78])
                data.WriteUInt32(CreatedBySpell);

            if (changesMask[79])
                data.WriteInt32(EmoteState);

            if (changesMask[80])
                data.WriteUInt32(BaseMana);

            if (changesMask[81])
                data.WriteUInt32(BaseHealth);

            if (changesMask[82])
                data.WriteUInt8(SheatheState);

            if (changesMask[83])
                data.WriteUInt8(GetViewerDependentPvpFlags(this, owner, receiver));

            if (changesMask[84])
                data.WriteUInt8(PetFlags);

            if (changesMask[85])
                data.WriteUInt8(ShapeshiftForm);

            if (changesMask[86])
                data.WriteInt32(AttackPower);

            if (changesMask[87])
                data.WriteInt32(AttackPowerModPos);

            if (changesMask[88])
                data.WriteInt32(AttackPowerModNeg);

            if (changesMask[89])
                data.WriteFloat(AttackPowerMultiplier);

            if (changesMask[90])
                data.WriteInt32(RangedAttackPower);

            if (changesMask[91])
                data.WriteInt32(RangedAttackPowerModPos);

            if (changesMask[92])
                data.WriteInt32(RangedAttackPowerModNeg);

            if (changesMask[93])
                data.WriteFloat(RangedAttackPowerMultiplier);

            if (changesMask[94])
                data.WriteInt32(MainHandWeaponAttackPower);

            if (changesMask[95])
                data.WriteInt32(OffHandWeaponAttackPower);
        }

        if (changesMask[96])
        {
            if (changesMask[97])
                data.WriteInt32(RangedWeaponAttackPower);

            if (changesMask[98])
                data.WriteInt32(SetAttackSpeedAura);

            if (changesMask[99])
                data.WriteFloat(Lifesteal);

            if (changesMask[100])
                data.WriteFloat(MinRangedDamage);

            if (changesMask[101])
                data.WriteFloat(MaxRangedDamage);

            if (changesMask[102])
                data.WriteFloat(ManaCostMultiplier);

            if (changesMask[103])
                data.WriteFloat(MaxHealthModifier);

            if (changesMask[104])
                data.WriteFloat(HoverHeight);

            if (changesMask[105])
                data.WriteUInt32(MinItemLevelCutoff);

            if (changesMask[106])
                data.WriteUInt32(MinItemLevel);

            if (changesMask[107])
                data.WriteUInt32(MaxItemLevel);

            if (changesMask[108])
                data.WriteInt32(AzeriteItemLevel);

            if (changesMask[109])
                data.WriteUInt32(WildBattlePetLevel);

            if (changesMask[110])
                data.WriteUInt32(BattlePetCompanionExperience);

            if (changesMask[111])
                data.WriteUInt32(BattlePetCompanionNameTimestamp);

            if (changesMask[112])
                data.WriteInt32(InteractSpellID);

            if (changesMask[113])
                data.WriteInt32(ScaleDuration);

            if (changesMask[114])
                data.WriteInt32(LooksLikeMountID);

            if (changesMask[115])
                data.WriteInt32(LooksLikeCreatureID);

            if (changesMask[116])
                data.WriteInt32(LookAtControllerID);

            if (changesMask[117])
                data.WriteInt32(PerksVendorItemID);

            if (changesMask[118])
                data.WriteInt32(TaxiNodesID);

            if (changesMask[119])
                data.WritePackedGuid(GuildGUID);

            if (changesMask[120])
                data.WriteInt32(FlightCapabilityID);

            if (changesMask[121])
                data.WriteUInt32(SilencedSchoolMask);

            if (changesMask[122])
                data.WritePackedGuid(NameplateAttachToGUID);
        }

        if (changesMask[123])
            for (var i = 0; i < 2; ++i)
                if (changesMask[124 + i])
                    data.WriteUInt32(GetViewerDependentNpcFlags(this, i, owner, receiver));

        if (changesMask[126])
            for (var i = 0; i < 7; ++i)
            {
                if (changesMask[127 + i])
                    data.WriteInt32(Power[i]);

                if (changesMask[134 + i])
                    data.WriteUInt32(MaxPower[i]);

                if (changesMask[141 + i])
                    data.WriteFloat(PowerRegenFlatModifier[i]);

                if (changesMask[148 + i])
                    data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
            }

        if (changesMask[155])
            for (var i = 0; i < 3; ++i)
                if (changesMask[156 + i])
                    VirtualItems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

        if (changesMask[159])
            for (var i = 0; i < 2; ++i)
                if (changesMask[160 + i])
                    data.WriteUInt32(AttackRoundBaseTime[i]);

        if (changesMask[162])
            for (var i = 0; i < 4; ++i)
            {
                if (changesMask[163 + i])
                    data.WriteInt32(Stats[i]);

                if (changesMask[167 + i])
                    data.WriteInt32(StatPosBuff[i]);

                if (changesMask[171 + i])
                    data.WriteInt32(StatNegBuff[i]);
            }

        if (changesMask[175])
            for (var i = 0; i < 7; ++i)
            {
                if (changesMask[176 + i])
                    data.WriteInt32(Resistances[i]);

                if (changesMask[183 + i])
                    data.WriteInt32(BonusResistanceMods[i]);

                if (changesMask[190 + i])
                    data.WriteInt32(ManaCostModifier[i]);
            }
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(StateWorldEffectIDs);
        ClearChangesMask(PassiveSpells);
        ClearChangesMask(WorldEffects);
        ClearChangesMask(ChannelObjects);
        ClearChangesMask(DisplayID);
        ClearChangesMask(StateSpellVisualID);
        ClearChangesMask(StateAnimID);
        ClearChangesMask(StateAnimKitID);
        ClearChangesMask(StateWorldEffectsQuestObjectiveID);
        ClearChangesMask(SpellOverrideNameID);
        ClearChangesMask(Charm);
        ClearChangesMask(Summon);
        ClearChangesMask(Critter);
        ClearChangesMask(CharmedBy);
        ClearChangesMask(SummonedBy);
        ClearChangesMask(CreatedBy);
        ClearChangesMask(DemonCreator);
        ClearChangesMask(LookAtControllerTarget);
        ClearChangesMask(Target);
        ClearChangesMask(BattlePetCompanionGUID);
        ClearChangesMask(BattlePetDBID);
        ClearChangesMask(ChannelData);
        ClearChangesMask(SpellEmpowerStage);
        ClearChangesMask(SummonedByHomeRealm);
        ClearChangesMask(Race);
        ClearChangesMask(ClassId);
        ClearChangesMask(PlayerClassId);
        ClearChangesMask(Sex);
        ClearChangesMask(DisplayPower);
        ClearChangesMask(OverrideDisplayPowerID);
        ClearChangesMask(Health);
        ClearChangesMask(MaxHealth);
        ClearChangesMask(Level);
        ClearChangesMask(EffectiveLevel);
        ClearChangesMask(ContentTuningID);
        ClearChangesMask(ScalingLevelMin);
        ClearChangesMask(ScalingLevelMax);
        ClearChangesMask(ScalingLevelDelta);
        ClearChangesMask(ScalingFactionGroup);
        ClearChangesMask(ScalingHealthItemLevelCurveID);
        ClearChangesMask(ScalingDamageItemLevelCurveID);
        ClearChangesMask(FactionTemplate);
        ClearChangesMask(Flags);
        ClearChangesMask(Flags2);
        ClearChangesMask(Flags3);
        ClearChangesMask(AuraState);
        ClearChangesMask(RangedAttackRoundBaseTime);
        ClearChangesMask(BoundingRadius);
        ClearChangesMask(CombatReach);
        ClearChangesMask(DisplayScale);
        ClearChangesMask(CreatureFamily);
        ClearChangesMask(CreatureType);
        ClearChangesMask(NativeDisplayID);
        ClearChangesMask(NativeXDisplayScale);
        ClearChangesMask(MountDisplayID);
        ClearChangesMask(CosmeticMountDisplayID);
        ClearChangesMask(MinDamage);
        ClearChangesMask(MaxDamage);
        ClearChangesMask(MinOffHandDamage);
        ClearChangesMask(MaxOffHandDamage);
        ClearChangesMask(StandState);
        ClearChangesMask(PetTalentPoints);
        ClearChangesMask(VisFlags);
        ClearChangesMask(AnimTier);
        ClearChangesMask(PetNumber);
        ClearChangesMask(PetNameTimestamp);
        ClearChangesMask(PetExperience);
        ClearChangesMask(PetNextLevelExperience);
        ClearChangesMask(ModCastingSpeed);
        ClearChangesMask(ModCastingSpeedNeg);
        ClearChangesMask(ModSpellHaste);
        ClearChangesMask(ModHaste);
        ClearChangesMask(ModRangedHaste);
        ClearChangesMask(ModHasteRegen);
        ClearChangesMask(ModTimeRate);
        ClearChangesMask(CreatedBySpell);
        ClearChangesMask(EmoteState);
        ClearChangesMask(BaseMana);
        ClearChangesMask(BaseHealth);
        ClearChangesMask(SheatheState);
        ClearChangesMask(PvpFlags);
        ClearChangesMask(PetFlags);
        ClearChangesMask(ShapeshiftForm);
        ClearChangesMask(AttackPower);
        ClearChangesMask(AttackPowerModPos);
        ClearChangesMask(AttackPowerModNeg);
        ClearChangesMask(AttackPowerMultiplier);
        ClearChangesMask(RangedAttackPower);
        ClearChangesMask(RangedAttackPowerModPos);
        ClearChangesMask(RangedAttackPowerModNeg);
        ClearChangesMask(RangedAttackPowerMultiplier);
        ClearChangesMask(MainHandWeaponAttackPower);
        ClearChangesMask(OffHandWeaponAttackPower);
        ClearChangesMask(RangedWeaponAttackPower);
        ClearChangesMask(SetAttackSpeedAura);
        ClearChangesMask(Lifesteal);
        ClearChangesMask(MinRangedDamage);
        ClearChangesMask(MaxRangedDamage);
        ClearChangesMask(ManaCostMultiplier);
        ClearChangesMask(MaxHealthModifier);
        ClearChangesMask(HoverHeight);
        ClearChangesMask(MinItemLevelCutoff);
        ClearChangesMask(MinItemLevel);
        ClearChangesMask(MaxItemLevel);
        ClearChangesMask(AzeriteItemLevel);
        ClearChangesMask(WildBattlePetLevel);
        ClearChangesMask(BattlePetCompanionExperience);
        ClearChangesMask(BattlePetCompanionNameTimestamp);
        ClearChangesMask(InteractSpellID);
        ClearChangesMask(ScaleDuration);
        ClearChangesMask(LooksLikeMountID);
        ClearChangesMask(LooksLikeCreatureID);
        ClearChangesMask(LookAtControllerID);
        ClearChangesMask(PerksVendorItemID);
        ClearChangesMask(TaxiNodesID);
        ClearChangesMask(GuildGUID);
        ClearChangesMask(FlightCapabilityID);
        ClearChangesMask(SilencedSchoolMask);
        ClearChangesMask(NameplateAttachToGUID);
        ClearChangesMask(NpcFlags);
        ClearChangesMask(Power);
        ClearChangesMask(MaxPower);
        ClearChangesMask(PowerRegenFlatModifier);
        ClearChangesMask(PowerRegenInterruptedFlatModifier);
        ClearChangesMask(VirtualItems);
        ClearChangesMask(AttackRoundBaseTime);
        ClearChangesMask(Stats);
        ClearChangesMask(StatPosBuff);
        ClearChangesMask(StatNegBuff);
        ClearChangesMask(Resistances);
        ClearChangesMask(BonusResistanceMods);
        ClearChangesMask(ManaCostModifier);
        ChangesMask.ResetAll();
    }

    private uint GetViewerDependentDisplayId(UnitData unitData, Unit unit, Player receiver)
    {
        uint displayId = unitData.DisplayID;

        if (unit.IsCreature)
        {
            var cinfo = unit.AsCreature.Template;
            var summon = unit.ToTempSummon();

            if (summon != null)
                if (summon.GetSummonerGUID() == receiver.GUID)
                {
                    if (summon.GetCreatureIdVisibleToSummoner().HasValue)
                        cinfo = Global.ObjectMgr.GetCreatureTemplate(summon.GetCreatureIdVisibleToSummoner().Value);

                    if (summon.GetDisplayIdVisibleToSummoner().HasValue)
                        displayId = summon.GetDisplayIdVisibleToSummoner().Value;
                }

            // this also applies for transform auras
            var transform = Global.SpellMgr.GetSpellInfo(unit.TransformSpell, unit.Map.DifficultyID);

            if (transform != null)
                foreach (var spellEffectInfo in transform.Effects)
                    if (spellEffectInfo.IsAura(AuraType.Transform))
                    {
                        var transformInfo = Global.ObjectMgr.GetCreatureTemplate((uint)spellEffectInfo.MiscValue);

                        if (transformInfo != null)
                        {
                            cinfo = transformInfo;

                            break;
                        }
                    }

            if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger))
                if (receiver.IsGameMaster)
                    displayId = cinfo.GetFirstVisibleModel().CreatureDisplayId;
        }

        return displayId;
    }

    private uint GetViewerDependentNpcFlags(UnitData unitData, int i, Unit unit, Player receiver)
    {
        var npcFlag = unitData.NpcFlags[i];

        if (i == 0 && unit.IsCreature && !receiver.CanSeeSpellClickOn(unit.AsCreature))
            npcFlag &= ~(uint)NPCFlags.SpellClick;

        return npcFlag;
    }

    private uint GetViewerDependentFactionTemplate(UnitData unitData, Unit unit, Player receiver)
    {
        uint factionTemplate = unitData.FactionTemplate;

        if (unit.IsControlledByPlayer && receiver != unit && GetDefaultValue("AllowTwoSide.Interaction.Group", false) && unit.IsInRaidWith(receiver))
        {
            var ft1 = unit.GetFactionTemplateEntry();
            var ft2 = receiver.GetFactionTemplateEntry();

            if (ft1 != null && ft2 != null && !ft1.IsFriendlyTo(ft2))
                // pretend that all other HOSTILE players have own faction, to allow follow, heal, rezz (trade wont work)
                factionTemplate = receiver.Faction;
        }

        return factionTemplate;
    }

    private uint GetViewerDependentFlags(UnitData unitData, Unit unit, Player receiver)
    {
        uint flags = unitData.Flags;

        // Update fields of triggers, transformed units or uninteractible units (values dependent on GM state)
        if (receiver.IsGameMaster)
            flags &= ~(uint)UnitFlags.Uninteractible;

        return flags;
    }

    private uint GetViewerDependentFlags3(UnitData unitData, Unit unit, Player receiver)
    {
        uint flags = unitData.Flags3;

        if ((flags & (uint)UnitFlags3.AlreadySkinned) != 0 && unit.IsCreature && !unit.AsCreature.IsSkinnedBy(receiver))
            flags &= ~(uint)UnitFlags3.AlreadySkinned;

        return flags;
    }

    private uint GetViewerDependentAuraState(UnitData unitData, Unit unit, Player receiver)
    {
        // Check per caster aura states to not enable using a spell in client if specified aura is not by target
        return unit.BuildAuraStateUpdateForTarget(receiver);
    }

    private byte GetViewerDependentPvpFlags(UnitData unitData, Unit unit, Player receiver)
    {
        byte pvpFlags = unitData.PvpFlags;

        if (unit.IsControlledByPlayer && receiver != unit && GetDefaultValue("AllowTwoSide.Interaction.Group", false) && unit.IsInRaidWith(receiver))
        {
            var ft1 = unit.GetFactionTemplateEntry();
            var ft2 = receiver.GetFactionTemplateEntry();

            if (ft1 != null && ft2 != null && !ft1.IsFriendlyTo(ft2))
                // Allow targeting opposite faction in party when enabled in config
                pvpFlags &= (byte)UnitPVPStateFlags.Sanctuary;
        }

        return pvpFlags;
    }
}
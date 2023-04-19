// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Totem;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities;

public class Totem : Minion
{
    public Totem(SummonPropertiesRecord propertiesRecord, Unit owner, ClassFactory classFactory) : base(propertiesRecord, owner, false, classFactory)
    {
        UnitTypeMask |= UnitTypeMask.Totem;
        TotemType = TotemType.Passive;
    }

    public uint GetSpell(byte slot = 0)
    {
        return Spells[slot];
    }

    public uint TotemDuration { get; private set; }

    public TotemType TotemType { get; private set; }

    public override void InitStats(uint duration)
    {
        // client requires SMSG_TOTEM_CREATED to be sent before adding to world and before removing old totem
        var owner = OwnerUnit.AsPlayer;

        if (owner != null)
        {
            if (SummonPropertiesRecord.Slot is >= (int)Framework.Constants.SummonSlot.Totem and < SharedConst.MaxTotemSlot)
            {
                TotemCreated packet = new()
                {
                    Totem = GUID,
                    Slot = (byte)(SummonPropertiesRecord.Slot - (int)Framework.Constants.SummonSlot.Totem),
                    Duration = duration,
                    SpellID = UnitData.CreatedBySpell
                };

                owner.AsPlayer.SendPacket(packet);
            }

            // set display id depending on caster's race
            var totemDisplayId = SpellManager.GetModelForTotem(UnitData.CreatedBySpell, owner.Race);

            if (totemDisplayId != 0)
                SetDisplayId(totemDisplayId);
            else
                Log.Logger.Debug($"Totem with entry {Entry}, does not have a specialized model for spell {UnitData.CreatedBySpell} and race {owner.Race}. Set to default.");
        }

        base.InitStats(duration);

        // Get spell cast by totem
        var totemSpell = SpellManager.GetSpellInfo(GetSpell(), Location.Map.DifficultyID);

        if (totemSpell != null)
            if (totemSpell.CalcCastTime() != 0) // If spell has cast time -> its an active totem
                TotemType = TotemType.Active;

        TotemDuration = duration;
    }

    public override void InitSummon()
    {
        if (TotemType == TotemType.Passive && GetSpell() != 0)
            SpellFactory.CastSpell(this, GetSpell(), true);

        // Some totems can have both instant effect and passive spell
        if (GetSpell(1) != 0)
            SpellFactory.CastSpell(this, GetSpell(1), true);
    }

    public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
    {
        // immune to all positive spells, except of stoneclaw totem absorb and sentry totem bind sight
        // totems positive spells have unit_caster target
        if (spellEffectInfo.Effect != SpellEffectName.Dummy &&
            spellEffectInfo.Effect != SpellEffectName.ScriptEffect &&
            spellInfo.IsPositive &&
            spellEffectInfo.TargetA.Target != Targets.UnitCaster &&
            spellEffectInfo.TargetA.CheckType != SpellTargetCheckTypes.Entry)
            return true;

        return spellEffectInfo.ApplyAuraName switch
        {
            AuraType.PeriodicDamage => true,
            AuraType.PeriodicLeech  => true,
            AuraType.ModFear        => true,
            AuraType.Transform      => true,
            _                       => base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute)
        };
    }

    public void SetTotemDuration(uint duration)
    {
        TotemDuration = duration;
    }

    public override void UnSummon()
    {
        UnSummon(TimeSpan.Zero);
    }

    public override void UnSummon(TimeSpan msTime)
    {
        if (msTime != TimeSpan.Zero)
        {
            Events.AddEvent(new ForcedUnsummonDelayEvent(this), Events.CalculateTime(msTime));

            return;
        }

        CombatStop();
        RemoveAurasDueToSpell(GetSpell(), GUID);

        // clear owner's totem slot
        for (byte i = (int)Framework.Constants.SummonSlot.Totem; i < SharedConst.MaxTotemSlot; ++i)
            if (OwnerUnit.SummonSlot[i] == GUID)
            {
                OwnerUnit.SummonSlot[i].Clear();

                break;
            }

        OwnerUnit.RemoveAurasDueToSpell(GetSpell(), GUID);

        // remove aura all party members too
        var owner = OwnerUnit.AsPlayer;

        if (owner != null)
        {
            owner.SendAutoRepeatCancel(this);

            var spell = SpellManager.GetSpellInfo(UnitData.CreatedBySpell, Location.Map.DifficultyID);

            if (spell != null)
                SpellHistory.SendCooldownEvent(spell, 0, null, false);

            var group = owner.Group;

            if (group != null)
                for (var refe = group.FirstMember; refe != null; refe = refe.Next())
                {
                    var target = refe.Source;

                    if (target != null && target.Location.IsInMap(owner) && group.SameSubGroup(owner, target))
                        target.RemoveAurasDueToSpell(GetSpell(), GUID);
                }
        }

        Location.AddObjectToRemoveList();
    }

    public override void Update(uint diff)
    {
        if (!OwnerUnit.IsAlive || !IsAlive)
        {
            UnSummon(); // remove self

            return;
        }

        if (TotemDuration <= diff)
        {
            UnSummon(); // remove self

            return;
        }

        TotemDuration -= diff;

        base.Update(diff);
    }

    public override bool UpdateAllStats()
    {
        return true;
    }

    public override void UpdateArmor() { }

    public override void UpdateAttackPowerAndDamage(bool ranged = false) { }

    public override void UpdateDamagePhysical(WeaponAttackType attType) { }

    public override void UpdateMaxHealth() { }

    public override void UpdateMaxPower(PowerType power) { }

    public override void UpdateResistances(SpellSchools school) { }

    public override bool UpdateStats(Stats stat)
    {
        return true;
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities;

public class Guardian : Minion
{
    private const int ENTRY_BLOODWORM = 28017;
    private const int ENTRY_FELGUARD = 17252;
    private const int ENTRY_FELHUNTER = 417;
    private const int ENTRY_FIRE_ELEMENTAL = 15438;
    private const int ENTRY_GHOUL = 26125;
    private const int ENTRY_IMP = 416;
    private const int ENTRY_SUCCUBUS = 1863;
    private const int ENTRY_TREANT = 1964;
    private const int ENTRY_VOIDWALKER = 1860;
    private const int ENTRY_WATER_ELEMENTAL = 510;
    private readonly float[] _statFromOwner = new float[(int)Stats.Max];

    private float _bonusSpellDamage;

    public Guardian(SummonPropertiesRecord propertiesRecord, Unit owner, bool isWorldObject)
        : base(propertiesRecord, owner, isWorldObject)
    {
        _bonusSpellDamage = 0;

        UnitTypeMask |= UnitTypeMask.Guardian;

        if (propertiesRecord != null && (propertiesRecord.Title == SummonTitle.Pet || propertiesRecord.Control == SummonCategory.Pet))
        {
            UnitTypeMask |= UnitTypeMask.ControlableGuardian;
            InitCharmInfo();
        }
    }

    public float GetBonusDamage()
    {
        return _bonusSpellDamage;
    }

    public float GetBonusStatFromOwner(Stats stat)
    {
        return _statFromOwner[(int)stat];
    }

    public override void InitStats(uint duration)
    {
        base.InitStats(duration);

        InitStatsForLevel(OwnerUnit.Level);

        if (OwnerUnit.IsTypeId(TypeId.Player) && HasUnitTypeMask(UnitTypeMask.ControlableGuardian))
            GetCharmInfo().InitCharmCreateSpells();

        ReactState = ReactStates.Aggressive;
    }

    // @todo Move stat mods code to pet passive auras
    public bool InitStatsForLevel(uint petlevel)
    {
        var cinfo = Template;

        SetLevel(petlevel);

        //Determine pet type
        var petType = PetType.Max;

        if (IsPet && OwnerUnit.IsTypeId(TypeId.Player))
        {
            if (OwnerUnit.Class is PlayerClass.Warlock or PlayerClass.Shaman or PlayerClass.Deathknight
               ) // Risen Ghoul
                petType = PetType.Summon;
            else if (OwnerUnit.Class == PlayerClass.Hunter)
            {
                petType = PetType.Hunter;
                UnitTypeMask |= UnitTypeMask.HunterPet;
            }
            else
                Log.Logger.Error("Unknown type pet {0} is summoned by player class {1}", Entry, OwnerUnit.Class);
        }

        var creature_ID = petType == PetType.Hunter ? 1 : cinfo.Entry;

        SetMeleeDamageSchool((SpellSchools)cinfo.DmgSchool);

        SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, (float)petlevel * 50);

        SetBaseAttackTime(WeaponAttackType.BaseAttack, SharedConst.BaseAttackTime);
        SetBaseAttackTime(WeaponAttackType.OffAttack, SharedConst.BaseAttackTime);
        SetBaseAttackTime(WeaponAttackType.RangedAttack, SharedConst.BaseAttackTime);

        //scale
        ObjectScale = NativeObjectScale;

        // Resistance
        // Hunters pet should not inherit resistances from creature_template, they have separate auras for that
        if (!IsHunterPet)
            for (var i = (int)SpellSchools.Holy; i < (int)SpellSchools.Max; ++i)
                SetStatFlatModifier(UnitMods.ResistanceStart + i, UnitModifierFlatType.Base, cinfo.Resistance[i]);

        // Health, Mana or Power, Armor
        var pInfo = Global.ObjectMgr.GetPetLevelInfo(creature_ID, petlevel);

        if (pInfo != null) // exist in DB
        {
            SetCreateHealth(pInfo.health);
            SetCreateMana(pInfo.mana);

            if (pInfo.armor > 0)
                SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, pInfo.armor);

            for (byte stat = 0; stat < (int)Stats.Max; ++stat)
                SetCreateStat((Stats)stat, pInfo.stats[stat]);
        }
        else // not exist in DB, use some default fake data
        {
            // remove elite bonuses included in DB values
            var stats = Global.ObjectMgr.GetCreatureBaseStats(petlevel, cinfo.UnitClass);
            ApplyLevelScaling();

            SetCreateHealth((uint)(Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureHealth, petlevel, cinfo.GetHealthScalingExpansion(), UnitData.ContentTuningID, (PlayerClass)cinfo.UnitClass) * cinfo.ModHealth * cinfo.ModHealthExtra * GetHealthMod(cinfo.Rank)));
            SetCreateMana(stats.GenerateMana(cinfo));

            SetCreateStat(Stats.Strength, 22);
            SetCreateStat(Stats.Agility, 22);
            SetCreateStat(Stats.Stamina, 25);
            SetCreateStat(Stats.Intellect, 28);
        }

        // Power
        if (petType == PetType.Hunter) // Hunter pets have focus
            SetPowerType(PowerType.Focus);
        else if (IsPetGhoul() || IsPetAbomination()) // DK pets have energy
        {
            SetPowerType(PowerType.Energy);
            SetFullPower(PowerType.Energy);
        }
        else if (IsPetImp() || IsPetFelhunter() || IsPetVoidwalker() || IsPetSuccubus() || IsPetDoomguard() || IsPetFelguard()) // Warlock pets have energy (since 5.x)
            SetPowerType(PowerType.Energy);
        else
            SetPowerType(PowerType.Mana);

        // Damage
        SetBonusDamage(0);

        switch (petType)
        {
            case PetType.Summon:
            {
                // the damage bonus used for pets is either fire or shadow damage, whatever is higher
                var fire = OwnerUnit.AsPlayer.ActivePlayerData.ModDamageDonePos[(int)SpellSchools.Fire];
                var shadow = OwnerUnit.AsPlayer.ActivePlayerData.ModDamageDonePos[(int)SpellSchools.Shadow];
                var val = fire > shadow ? fire : shadow;

                if (val < 0)
                    val = 0;

                SetBonusDamage((int)(val * 0.15f));

                SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - petlevel / 4);
                SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + petlevel / 4);

                break;
            }
            case PetType.Hunter:
            {
                AsPet.SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel(petlevel) * 0.05f));
                //these formula may not be correct; however, it is designed to be close to what it should be
                //this makes dps 0.5 of pets level
                SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - petlevel / 4);
                //damage range is then petlevel / 2
                SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + petlevel / 4);

                //damage is increased afterwards as strength and pet scaling modify attack power
                break;
            }
            default:
            {
                switch (Entry)
                {
                    case 510: // mage Water Elemental
                    {
                        SetBonusDamage((int)(OwnerUnit.SpellBaseDamageBonusDone(SpellSchoolMask.Frost) * 0.33f));

                        break;
                    }
                    case 1964: //force of nature
                    {
                        if (pInfo == null)
                            SetCreateHealth(30 + 30 * petlevel);

                        var bonusDmg = OwnerUnit.SpellBaseDamageBonusDone(SpellSchoolMask.Nature) * 0.15f;
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel * 2.5f - (float)petlevel / 2 + bonusDmg);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel * 2.5f + (float)petlevel / 2 + bonusDmg);

                        break;
                    }
                    case 15352: //earth elemental 36213
                    {
                        if (pInfo == null)
                            SetCreateHealth(100 + 120 * petlevel);

                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - petlevel / 4);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + petlevel / 4);

                        break;
                    }
                    case 15438: //fire elemental
                    {
                        if (pInfo == null)
                        {
                            SetCreateHealth(40 * petlevel);
                            SetCreateMana(28 + 10 * petlevel);
                        }

                        SetBonusDamage((int)(OwnerUnit.SpellBaseDamageBonusDone(SpellSchoolMask.Fire) * 0.5f));
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel * 4 - petlevel);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel * 4 + petlevel);

                        break;
                    }
                    case 19668: // Shadowfiend
                    {
                        if (pInfo == null)
                        {
                            SetCreateMana(28 + 10 * petlevel);
                            SetCreateHealth(28 + 30 * petlevel);
                        }

                        var bonus_dmg = (int)(OwnerUnit.SpellBaseDamageBonusDone(SpellSchoolMask.Shadow) * 0.3f);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel * 4 - petlevel + bonus_dmg);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel * 4 + petlevel + bonus_dmg);

                        break;
                    }
                    case 19833: //Snake Trap - Venomous Snake
                    {
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel / 2 - 25);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel / 2 - 18);

                        break;
                    }
                    case 19921: //Snake Trap - Viper
                    {
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel / 2 - 10);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel / 2);

                        break;
                    }
                    case 29264: // Feral Spirit
                    {
                        if (pInfo == null)
                            SetCreateHealth(30 * petlevel);

                        // wolf attack speed is 1.5s
                        SetBaseAttackTime(WeaponAttackType.BaseAttack, cinfo.BaseAttackTime);

                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel * 4 - petlevel);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel * 4 + petlevel);

                        SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, OwnerUnit.GetArmor() * 0.35f);                  // Bonus Armor (35% of player armor)
                        SetStatFlatModifier(UnitMods.StatStamina, UnitModifierFlatType.Base, OwnerUnit.GetStat(Stats.Stamina) * 0.3f); // Bonus Stamina (30% of player stamina)

                        if (!HasAura(58877))      //prevent apply twice for the 2 wolves
                            AddAura(58877, this); //Spirit Hunt, passive, Spirit Wolves' attacks heal them and their master for 150% of damage done.

                        break;
                    }
                    case 31216: // Mirror Image
                    {
                        SetBonusDamage((int)(OwnerUnit.SpellBaseDamageBonusDone(SpellSchoolMask.Frost) * 0.33f));
                        SetDisplayId(OwnerUnit.DisplayId);

                        if (pInfo == null)
                        {
                            SetCreateMana(28 + 30 * petlevel);
                            SetCreateHealth(28 + 10 * petlevel);
                        }

                        break;
                    }
                    case 27829: // Ebon Gargoyle
                    {
                        if (pInfo == null)
                        {
                            SetCreateMana(28 + 10 * petlevel);
                            SetCreateHealth(28 + 30 * petlevel);
                        }

                        SetBonusDamage((int)(OwnerUnit.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.5f));
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - petlevel / 4);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + petlevel / 4);

                        break;
                    }
                    case 28017: // Bloodworms
                    {
                        SetCreateHealth(4 * petlevel);
                        SetBonusDamage((int)(OwnerUnit.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.006f));
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - 30 - petlevel / 4);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel - 30 + petlevel / 4);

                        break;
                    }
                    default:
                    {
                        /* ToDo: Check what 5f5d2028 broke/fixed and how much of Creature::UpdateLevelDependantStats()
                         * should be copied here (or moved to another method or if that function should be called here
                         * or not just for this default case)
                         */
                        var basedamage = GetBaseDamageForLevel(petlevel);

                        var weaponBaseMinDamage = basedamage;
                        var weaponBaseMaxDamage = basedamage * 1.5f;

                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
                        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

                        break;
                    }
                }

                break;
            }
        }

        UpdateAllStats();

        SetFullHealth();
        SetFullPower(PowerType.Mana);

        return true;
    }

    public override void InitSummon()
    {
        base.InitSummon();

        if (OwnerUnit.IsTypeId(TypeId.Player) && OwnerUnit.MinionGUID == GUID && OwnerUnit.CharmedGUID.IsEmpty)
            OwnerUnit.AsPlayer.CharmSpellInitialize();
    }

    public override bool UpdateAllStats()
    {
        UpdateMaxHealth();

        for (var i = Stats.Strength; i < Stats.Max; ++i)
            UpdateStats(i);

        for (var i = PowerType.Mana; i < PowerType.Max; ++i)
            UpdateMaxPower(i);

        UpdateAllResistances();

        return true;
    }

    public override void UpdateArmor()
    {
        var bonus_armor = 0.0f;
        var unitMod = UnitMods.Armor;

        // hunter pets gain 35% of owner's armor value, warlock pets gain 100% of owner's armor
        if (IsHunterPet)
            bonus_armor = MathFunctions.CalculatePct(OwnerUnit.GetArmor(), 70);
        else if (IsPet)
            bonus_armor = OwnerUnit.GetArmor();

        var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base);
        var baseValue = value;
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
        value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total) + bonus_armor;
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

        SetArmor((int)baseValue, (int)(value - baseValue));
    }

    public override void UpdateAttackPowerAndDamage(bool ranged = false)
    {
        if (ranged)
            return;

        float val;
        double bonusAP = 0.0f;
        var unitMod = UnitMods.AttackPower;

        if (Entry == ENTRY_IMP) // imp's attack power
            val = GetStat(Stats.Strength) - 10.0f;
        else
            val = 2 * GetStat(Stats.Strength) - 20.0f;

        var owner = OwnerUnit ? OwnerUnit.AsPlayer : null;

        if (owner != null)
        {
            if (IsHunterPet) //hunter pets benefit from owner's attack power
            {
                var mod = 1.0f; //Hunter contribution modifier
                bonusAP = owner.GetTotalAttackPowerValue(WeaponAttackType.RangedAttack) * 0.22f * mod;
                SetBonusDamage((int)(owner.GetTotalAttackPowerValue(WeaponAttackType.RangedAttack) * 0.1287f * mod));
            }
            else if (IsPetGhoul()) //ghouls benefit from deathknight's attack power (may be summon pet or not)
            {
                bonusAP = owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.22f;
                SetBonusDamage((int)(owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.1287f));
            }
            else if (IsSpiritWolf()) //wolf benefit from shaman's attack power
            {
                var dmg_multiplier = 0.31f;
                bonusAP = owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * dmg_multiplier;
                SetBonusDamage((int)(owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * dmg_multiplier));
            }
            //demons benefit from warlocks shadow or fire damage
            else if (IsPet)
            {
                var fire = owner.ActivePlayerData.ModDamageDonePos[(int)SpellSchools.Fire] - owner.ActivePlayerData.ModDamageDoneNeg[(int)SpellSchools.Fire];
                var shadow = owner.ActivePlayerData.ModDamageDonePos[(int)SpellSchools.Shadow] - owner.ActivePlayerData.ModDamageDoneNeg[(int)SpellSchools.Shadow];
                var maximum = fire > shadow ? fire : shadow;

                if (maximum < 0)
                    maximum = 0;

                SetBonusDamage((int)(maximum * 0.15f));
                bonusAP = maximum * 0.57f;
            }
            //water elementals benefit from mage's frost damage
            else if (Entry == ENTRY_WATER_ELEMENTAL)
            {
                var frost = owner.ActivePlayerData.ModDamageDonePos[(int)SpellSchools.Frost] - owner.ActivePlayerData.ModDamageDoneNeg[(int)SpellSchools.Frost];

                if (frost < 0)
                    frost = 0;

                SetBonusDamage((int)(frost * 0.4f));
            }
        }

        SetStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Base, val + bonusAP);

        //in BASE_VALUE of UNIT_MOD_ATTACK_POWER for creatures we store data of meleeattackpower field in DB
        var base_attPower = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) * GetPctModifierValue(unitMod, UnitModifierPctType.Base);
        var attPowerMultiplier = GetPctModifierValue(unitMod, UnitModifierPctType.Total) - 1.0f;

        SetAttackPower((int)base_attPower);
        SetAttackPowerMultiplier((float)attPowerMultiplier);

        //automatically update weapon damage after attack power modification
        UpdateDamagePhysical(WeaponAttackType.BaseAttack);
    }

    public override void UpdateDamagePhysical(WeaponAttackType attType)
    {
        if (attType > WeaponAttackType.BaseAttack)
            return;

        var bonusDamage = 0.0f;
        var playerOwner = Owner.AsPlayer;

        if (playerOwner != null)
        {
            //force of nature
            if (Entry == ENTRY_TREANT)
            {
                var spellDmg = playerOwner.ActivePlayerData.ModDamageDonePos[(int)SpellSchools.Nature] - playerOwner.ActivePlayerData.ModDamageDoneNeg[(int)SpellSchools.Nature];

                if (spellDmg > 0)
                    bonusDamage = spellDmg * 0.09f;
            }
            //greater fire elemental
            else if (Entry == ENTRY_FIRE_ELEMENTAL)
            {
                var spellDmg = playerOwner.ActivePlayerData.ModDamageDonePos[(int)SpellSchools.Fire] - playerOwner.ActivePlayerData.ModDamageDoneNeg[(int)SpellSchools.Fire];

                if (spellDmg > 0)
                    bonusDamage = spellDmg * 0.4f;
            }
        }

        var unitMod = UnitMods.DamageMainHand;

        double att_speed = GetBaseAttackTime(WeaponAttackType.BaseAttack) / 1000.0f;

        var base_value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetTotalAttackPowerValue(attType, false) / 3.5f * att_speed + bonusDamage;
        var base_pct = GetPctModifierValue(unitMod, UnitModifierPctType.Base);
        var total_value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
        var total_pct = GetPctModifierValue(unitMod, UnitModifierPctType.Total);

        var weapon_mindamage = GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage);
        var weapon_maxdamage = GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage);

        var mindamage = ((base_value + weapon_mindamage) * base_pct + total_value) * total_pct;
        var maxdamage = ((base_value + weapon_maxdamage) * base_pct + total_value) * total_pct;

        SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinDamage), (float)mindamage);
        SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxDamage), (float)maxdamage);
    }

    public override void UpdateMaxHealth()
    {
        var unitMod = UnitMods.Health;
        var stamina = GetStat(Stats.Stamina) - GetCreateStat(Stats.Stamina);

        var multiplicator = Entry switch
        {
            ENTRY_IMP        => 8.4f,
            ENTRY_VOIDWALKER => 11.0f,
            ENTRY_SUCCUBUS   => 9.1f,
            ENTRY_FELHUNTER  => 9.5f,
            ENTRY_FELGUARD   => 11.0f,
            ENTRY_BLOODWORM  => 1.0f,
            _                => 10.0f
        };

        var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetCreateHealth();
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
        value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total) + stamina * multiplicator;
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

        SetMaxHealth((uint)value);
    }

    public override void UpdateMaxPower(PowerType power)
    {
        if (GetPowerIndex(power) == (uint)PowerType.Max)
            return;

        var unitMod = UnitMods.PowerStart + (int)power;

        var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetCreatePowerValue(power);
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
        value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

        SetMaxPower(power, (int)value);
    }

    public override void UpdateResistances(SpellSchools school)
    {
        if (school > SpellSchools.Normal)
        {
            var baseValue = GetFlatModifierValue(UnitMods.ResistanceStart + (int)school, UnitModifierFlatType.Base);
            var bonusValue = GetTotalAuraModValue(UnitMods.ResistanceStart + (int)school) - baseValue;

            // hunter and warlock pets gain 40% of owner's resistance
            if (IsPet)
            {
                baseValue += MathFunctions.CalculatePct(Owner.GetResistance(school), 40);
                bonusValue += MathFunctions.CalculatePct(Owner.GetBonusResistanceMod(school), 40);
            }

            SetResistance(school, (int)baseValue);
            SetBonusResistanceMod(school, (int)bonusValue);
        }
        else
            UpdateArmor();
    }

    public override bool UpdateStats(Stats stat)
    {
        var value = GetTotalStatValue(stat);
        UpdateStatBuffMod(stat);
        var ownersBonus = 0.0f;

        var owner = OwnerUnit;
        // Handle Death Knight Glyphs and Talents
        var mod = 0.75f;

        if (IsPetGhoul() && stat is Stats.Stamina or Stats.Strength)
        {
            switch (stat)
            {
                case Stats.Stamina:
                    mod = 0.3f;

                    break; // Default Owner's Stamina scale
                case Stats.Strength:
                    mod = 0.7f;

                    break; // Default Owner's Strength scale
                default: break;
            }

            ownersBonus = owner.GetStat(stat) * mod;
            value += ownersBonus;
        }
        else if (stat == Stats.Stamina)
        {
            ownersBonus = MathFunctions.CalculatePct(owner.GetStat(Stats.Stamina), 30);
            value += ownersBonus;
        }
        //warlock's and mage's pets gain 30% of owner's intellect
        else if (stat == Stats.Intellect)
            if (owner.Class is PlayerClass.Warlock or PlayerClass.Mage)
            {
                ownersBonus = MathFunctions.CalculatePct(owner.GetStat(stat), 30);
                value += ownersBonus;
            }

        SetStat(stat, (int)value);
        _statFromOwner[(int)stat] = ownersBonus;
        UpdateStatBuffMod(stat);

        switch (stat)
        {
            case Stats.Strength:
                UpdateAttackPowerAndDamage();

                break;
            case Stats.Agility:
                UpdateArmor();

                break;
            case Stats.Stamina:
                UpdateMaxHealth();

                break;
            case Stats.Intellect:
                UpdateMaxPower(PowerType.Mana);

                break;
        }

        return true;
    }

    private void SetBonusDamage(float damage)
    {
        _bonusSpellDamage = damage;
        var playerOwner = OwnerUnit.AsPlayer;

        playerOwner?.SetPetSpellPower((uint)damage);
    }
}
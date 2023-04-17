// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.Moroes;

internal struct SpellIds
{
    public const uint VANISH = 29448;
    public const uint GARROTE = 37066;
    public const uint BLIND = 34694;
    public const uint GOUGE = 29425;
    public const uint FRENZY = 37023;

    // Adds
    public const uint MANABURN = 29405;
    public const uint MINDFLY = 29570;
    public const uint SWPAIN = 34441;
    public const uint SHADOWFORM = 29406;

    public const uint HAMMEROFJUSTICE = 13005;
    public const uint JUDGEMENTOFCOMMAND = 29386;
    public const uint SEALOFCOMMAND = 29385;

    public const uint DISPELMAGIC = 15090;
    public const uint GREATERHEAL = 29564;
    public const uint HOLYFIRE = 29563;
    public const uint PWSHIELD = 29408;

    public const uint CLEANSE = 29380;
    public const uint GREATERBLESSOFMIGHT = 29381;
    public const uint HOLYLIGHT = 29562;
    public const uint DIVINESHIELD = 41367;

    public const uint HAMSTRING = 9080;
    public const uint MORTALSTRIKE = 29572;
    public const uint WHIRLWIND = 29573;

    public const uint DISARM = 8379;
    public const uint HEROICSTRIKE = 29567;
    public const uint SHIELDBASH = 11972;
    public const uint SHIELDWALL = 29390;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_SPECIAL = 1;
    public const uint SAY_KILL = 2;
    public const uint SAY_DEATH = 3;
}

internal struct MiscConst
{
    public const uint GROUP_NON_ENRAGE = 1;

    public static Position[] Locations =
    {
        new(-10991.0f, -1884.33f, 81.73f, 0.614315f), new(-10989.4f, -1885.88f, 81.73f, 0.904913f), new(-10978.1f, -1887.07f, 81.73f, 2.035550f), new(-10975.9f, -1885.81f, 81.73f, 2.253890f)
    };

    public static uint[] Adds =
    {
        17007, 19872, 19873, 19874, 19875, 19876
    };
}

[Script]
internal class BossMoroes : BossAI
{
    public ObjectGuid[] AddGUID = new ObjectGuid[4];
    private readonly uint[] _addId = new uint[4];
    private bool _enrage;

    private bool _inVanish;

    public BossMoroes(Creature creature) : base(creature, DataTypes.MOROES)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        if (Me.IsAlive)
            SpawnAdds();

        Instance.SetBossState(DataTypes.MOROES, EncounterState.NotStarted);
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           MiscConst.GROUP_NON_ENRAGE,
                           task =>
                           {
                               for (byte i = 0; i < 4; ++i)
                                   if (!AddGUID[i].IsEmpty)
                                   {
                                       var temp = ObjectAccessor.GetCreature(Me, AddGUID[i]);

                                       if (temp && temp.IsAlive)
                                           if (!temp.Victim)
                                               temp.AI.AttackStart(Me.Victim);
                                   }

                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(23),
                           MiscConst.GROUP_NON_ENRAGE,
                           task =>
                           {
                               DoCastVictim(SpellIds.GOUGE);
                               task.Repeat(TimeSpan.FromSeconds(40));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           MiscConst.GROUP_NON_ENRAGE,
                           task =>
                           {
                               DoCast(Me, SpellIds.VANISH);
                               _inVanish = true;

                               task.Schedule(TimeSpan.FromSeconds(5),
                                             garroteTask =>
                                             {
                                                 Talk(TextIds.SAY_SPECIAL);

                                                 var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                                                 if (target)
                                                     target.SpellFactory.CastSpell(target, SpellIds.GARROTE, true);

                                                 _inVanish = false;
                                             });

                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(35),
                           MiscConst.GROUP_NON_ENRAGE,
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.MinDistance, 0, 0.0f, true, false);

                               if (target)
                                   DoCast(target, SpellIds.BLIND);

                               task.Repeat(TimeSpan.FromSeconds(40));
                           });

        Talk(TextIds.SAY_AGGRO);
        AddsAttack();
    }

    public override void KilledUnit(Unit victim)
    {
        Talk(TextIds.SAY_KILL);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DEATH);

        base.JustDied(killer);

        DeSpawnAdds();

        //remove aura from spell Garrote when Moroes dies
        Instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.GARROTE);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (!_enrage &&
            HealthBelowPct(30))
        {
            DoCast(Me, SpellIds.FRENZY);
            _enrage = true;
            Scheduler.CancelGroup(MiscConst.GROUP_NON_ENRAGE);
        }

        Scheduler.Update(diff,
                         () =>
                         {
                             if (!_inVanish)
                                 DoMeleeAttackIfReady();
                         });
    }

    private void Initialize()
    {
        _enrage = false;
        _inVanish = false;
    }

    private void SpawnAdds()
    {
        DeSpawnAdds();

        if (IsAddlistEmpty())
        {
            var addList = MiscConst.Adds.ToList();
            addList.RandomResize(4);

            for (var i = 0; i < 4; ++i)
            {
                Creature creature = Me.SummonCreature(addList[i], MiscConst.Locations[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromSeconds(10));

                if (creature)
                {
                    AddGUID[i] = creature.GUID;
                    _addId[i] = addList[i];
                }
            }
        }
        else
            for (byte i = 0; i < 4; ++i)
            {
                Creature creature = Me.SummonCreature(_addId[i], MiscConst.Locations[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromSeconds(10));

                if (creature)
                    AddGUID[i] = creature.GUID;
            }
    }

    private bool IsAddlistEmpty()
    {
        for (byte i = 0; i < 4; ++i)
            if (_addId[i] == 0)
                return true;

        return false;
    }

    private void DeSpawnAdds()
    {
        for (byte i = 0; i < 4; ++i)
            if (!AddGUID[i].IsEmpty)
            {
                var temp = ObjectAccessor.GetCreature(Me, AddGUID[i]);

                if (temp)
                    temp.DespawnOrUnsummon();
            }
    }

    private void AddsAttack()
    {
        for (byte i = 0; i < 4; ++i)
            if (!AddGUID[i].IsEmpty)
            {
                var temp = ObjectAccessor.GetCreature((Me), AddGUID[i]);

                if (temp && temp.IsAlive)
                {
                    temp.AI.AttackStart(Me.Victim);
                    DoZoneInCombat(temp);
                }
                else
                    EnterEvadeMode();
            }
    }
}

internal class BossMoroesGuest : ScriptedAI
{
    private readonly ObjectGuid[] _guestGUID = new ObjectGuid[4];
    private readonly InstanceScript _instance;

    public BossMoroesGuest(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        _instance.SetBossState(DataTypes.MOROES, EncounterState.NotStarted);
    }

    public void AcquireGUID()
    {
        var moroes = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.MOROES));

        if (moroes)
            for (byte i = 0; i < 4; ++i)
            {
                var guid = moroes.GetAI<BossMoroes>().AddGUID[i];

                if (!guid.IsEmpty)
                    _guestGUID[i] = guid;
            }
    }

    public Unit SelectGuestTarget()
    {
        var tempGUID = _guestGUID[RandomHelper.Rand32() % 4];

        if (!tempGUID.IsEmpty)
        {
            var unit = Global.ObjAccessor.GetUnit(Me, tempGUID);

            if (unit && unit.IsAlive)
                return unit;
        }

        return Me;
    }

    public override void UpdateAI(uint diff)
    {
        if (_instance.GetBossState(DataTypes.MOROES) != EncounterState.InProgress)
            EnterEvadeMode();

        DoMeleeAttackIfReady();
    }
}

[Script]
internal class BossBaronessDorotheaMillstipe : BossMoroesGuest
{
    private uint _manaBurnTimer;
    private uint _mindFlayTimer;

    private uint _shadowWordPainTimer;

    //Shadow Priest
    public BossBaronessDorotheaMillstipe(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        DoCast(Me, SpellIds.SHADOWFORM, new CastSpellExtraArgs(true));

        base.Reset();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        base.UpdateAI(diff);

        if (_mindFlayTimer <= diff)
        {
            DoCastVictim(SpellIds.MINDFLY);
            _mindFlayTimer = 12000; // 3 sec channeled
        }
        else
            _mindFlayTimer -= diff;

        if (_manaBurnTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target)
                if (target.DisplayPowerType == PowerType.Mana)
                    DoCast(target, SpellIds.MANABURN);

            _manaBurnTimer = 5000; // 3 sec cast
        }
        else
            _manaBurnTimer -= diff;

        if (_shadowWordPainTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target)
            {
                DoCast(target, SpellIds.SWPAIN);
                _shadowWordPainTimer = 7000;
            }
        }
        else
            _shadowWordPainTimer -= diff;
    }

    private void Initialize()
    {
        _manaBurnTimer = 7000;
        _mindFlayTimer = 1000;
        _shadowWordPainTimer = 6000;
    }
}

[Script]
internal class BossBaronRafeDreuger : BossMoroesGuest
{
    private uint _hammerOfJusticeTimer;
    private uint _judgementOfCommandTimer;

    private uint _sealOfCommandTimer;

    //Retr Pally
    public BossBaronRafeDreuger(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        base.Reset();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        base.UpdateAI(diff);

        if (_sealOfCommandTimer <= diff)
        {
            DoCast(Me, SpellIds.SEALOFCOMMAND);
            _sealOfCommandTimer = 32000;
            _judgementOfCommandTimer = 29000;
        }
        else
            _sealOfCommandTimer -= diff;

        if (_judgementOfCommandTimer <= diff)
        {
            DoCastVictim(SpellIds.JUDGEMENTOFCOMMAND);
            _judgementOfCommandTimer = _sealOfCommandTimer + 29000;
        }
        else
            _judgementOfCommandTimer -= diff;

        if (_hammerOfJusticeTimer <= diff)
        {
            DoCastVictim(SpellIds.HAMMEROFJUSTICE);
            _hammerOfJusticeTimer = 12000;
        }
        else
            _hammerOfJusticeTimer -= diff;
    }

    private void Initialize()
    {
        _hammerOfJusticeTimer = 1000;
        _sealOfCommandTimer = 7000;
        _judgementOfCommandTimer = _sealOfCommandTimer + 29000;
    }
}

[Script]
internal class BossLadyCatrionaVonIndi : BossMoroesGuest
{
    private uint _dispelMagicTimer;
    private uint _greaterHealTimer;
    private uint _holyFireTimer;

    private uint _powerWordShieldTimer;

    //Holy Priest
    public BossLadyCatrionaVonIndi(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        AcquireGUID();

        base.Reset();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        base.UpdateAI(diff);

        if (_powerWordShieldTimer <= diff)
        {
            DoCast(Me, SpellIds.PWSHIELD);
            _powerWordShieldTimer = 15000;
        }
        else
            _powerWordShieldTimer -= diff;

        if (_greaterHealTimer <= diff)
        {
            var target = SelectGuestTarget();

            DoCast(target, SpellIds.GREATERHEAL);
            _greaterHealTimer = 17000;
        }
        else
            _greaterHealTimer -= diff;

        if (_holyFireTimer <= diff)
        {
            DoCastVictim(SpellIds.HOLYFIRE);
            _holyFireTimer = 22000;
        }
        else
            _holyFireTimer -= diff;

        if (_dispelMagicTimer <= diff)
        {
            var target = RandomHelper.RAND(SelectGuestTarget(), SelectTarget(SelectTargetMethod.Random, 0, 100, true));

            if (target)
                DoCast(target, SpellIds.DISPELMAGIC);

            _dispelMagicTimer = 25000;
        }
        else
            _dispelMagicTimer -= diff;
    }

    private void Initialize()
    {
        _dispelMagicTimer = 11000;
        _greaterHealTimer = 1500;
        _holyFireTimer = 5000;
        _powerWordShieldTimer = 1000;
    }
}

[Script]
internal class BossLadyKeiraBerrybuck : BossMoroesGuest
{
    private uint _cleanseTimer;
    private uint _divineShieldTimer;
    private uint _greaterBlessTimer;

    private uint _holyLightTimer;

    //Holy Pally
    public BossLadyKeiraBerrybuck(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        AcquireGUID();

        base.Reset();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        base.UpdateAI(diff);

        if (_divineShieldTimer <= diff)
        {
            DoCast(Me, SpellIds.DIVINESHIELD);
            _divineShieldTimer = 31000;
        }
        else
            _divineShieldTimer -= diff;

        if (_holyLightTimer <= diff)
        {
            var target = SelectGuestTarget();

            DoCast(target, SpellIds.HOLYLIGHT);
            _holyLightTimer = 10000;
        }
        else
            _holyLightTimer -= diff;

        if (_greaterBlessTimer <= diff)
        {
            var target = SelectGuestTarget();

            DoCast(target, SpellIds.GREATERBLESSOFMIGHT);

            _greaterBlessTimer = 50000;
        }
        else
            _greaterBlessTimer -= diff;

        if (_cleanseTimer <= diff)
        {
            var target = SelectGuestTarget();

            DoCast(target, SpellIds.CLEANSE);

            _cleanseTimer = 10000;
        }
        else
            _cleanseTimer -= diff;
    }

    private void Initialize()
    {
        _cleanseTimer = 13000;
        _greaterBlessTimer = 1000;
        _holyLightTimer = 7000;
        _divineShieldTimer = 31000;
    }
}

[Script]
internal class BossLordRobinDaris : BossMoroesGuest
{
    private uint _hamstringTimer;
    private uint _mortalStrikeTimer;

    private uint _whirlWindTimer;

    //Arms Warr
    public BossLordRobinDaris(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        base.Reset();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        base.UpdateAI(diff);

        if (_hamstringTimer <= diff)
        {
            DoCastVictim(SpellIds.HAMSTRING);
            _hamstringTimer = 12000;
        }
        else
            _hamstringTimer -= diff;

        if (_mortalStrikeTimer <= diff)
        {
            DoCastVictim(SpellIds.MORTALSTRIKE);
            _mortalStrikeTimer = 18000;
        }
        else
            _mortalStrikeTimer -= diff;

        if (_whirlWindTimer <= diff)
        {
            DoCast(Me, SpellIds.WHIRLWIND);
            _whirlWindTimer = 21000;
        }
        else
            _whirlWindTimer -= diff;
    }

    private void Initialize()
    {
        _hamstringTimer = 7000;
        _mortalStrikeTimer = 10000;
        _whirlWindTimer = 21000;
    }
}

[Script]
internal class BossLordCrispinFerence : BossMoroesGuest
{
    private uint _disarmTimer;
    private uint _heroicStrikeTimer;
    private uint _shieldBashTimer;

    private uint _shieldWallTimer;

    //Arms Warr
    public BossLordCrispinFerence(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        base.Reset();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        base.UpdateAI(diff);

        if (_disarmTimer <= diff)
        {
            DoCastVictim(SpellIds.DISARM);
            _disarmTimer = 12000;
        }
        else
            _disarmTimer -= diff;

        if (_heroicStrikeTimer <= diff)
        {
            DoCastVictim(SpellIds.HEROICSTRIKE);
            _heroicStrikeTimer = 10000;
        }
        else
            _heroicStrikeTimer -= diff;

        if (_shieldBashTimer <= diff)
        {
            DoCastVictim(SpellIds.SHIELDBASH);
            _shieldBashTimer = 13000;
        }
        else
            _shieldBashTimer -= diff;

        if (_shieldWallTimer <= diff)
        {
            DoCast(Me, SpellIds.SHIELDWALL);
            _shieldWallTimer = 21000;
        }
        else
            _shieldWallTimer -= diff;
    }

    private void Initialize()
    {
        _disarmTimer = 6000;
        _heroicStrikeTimer = 10000;
        _shieldBashTimer = 8000;
        _shieldWallTimer = 4000;
    }
}
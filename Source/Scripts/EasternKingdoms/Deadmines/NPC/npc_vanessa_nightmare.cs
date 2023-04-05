// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(DMCreatures.NPC_VANESSA_NIGHTMARE)]
public class npc_vanessa_nightmare : BossAI
{
    public static readonly Position[] EnragedWorgen_1 =
    {
        new(-97.79166f, -717.8542f, 8.668088f, 4.520403f), new(-94.40278f, -719.7274f, 8.598646f, 3.560472f), new(-101.9167f, -718.7552f, 8.726379f, 5.51524f)
    };

    public static readonly Position[] EnragedWorgen_2 =
    {
        new(3.137153f, -760.0313f, 9.725998f, 5.393067f), new(8.798013f, -762.2252f, 9.625132f, 3.379143f), new(4.232807f, -766.6125f, 9.804724f, 1.292649f)
    };


    public static readonly Position[] ElectricSpark =
    {
        new(-101.4959f, -648.5552f, 8.121676f, 0.04567058f), new(-120.96f, -638.3806f, 13.38522f, 6.237791f), new(-135.365f, -623.0541f, 15.48179f, 6.237976f), new(-120.1277f, -617.6179f, 15.28394f, 0.04498905f), new(-136.7082f, -604.6687f, 16.56965f, 6.2384f), new(-130.45f, -586.5038f, 19.61726f, 6.238641f), new(-142.9731f, -574.9221f, 20.18317f, 6.238891f)
    };

    public static readonly Position[] FieryBlaze =
    {
        new(-178.2031f, -594.9965f, 40.6501f, 4.415683f), new(-220.625f, -577.9618f, 21.06016f, 2.513274f), new(-205.3056f, -563.6285f, 21.06016f, 5.25344f), new(-214.8958f, -546.7136f, 19.3898f, 4.712389f), new(-207.8004f, -570.6441f, 21.06016f, 1.762783f), new(-221.4653f, -549.9358f, 19.3898f, 3.211406f), new(-229.9635f, -559.2552f, 19.3898f, 0), new(-216.8438f, -568.9011f, 21.06016f, 3.909538f), new(-235.9045f, -563.3906f, 19.3898f, 0), new(-226.6736f, -580.8316f, 20.43056f, 2.775074f), new(-227.5226f, -595.1979f, 20.42358f, 4.206244f), new(-215.0399f, -576.3941f, 21.06016f, 3.735005f), new(-210.592f, -583.4739f, 21.06016f, 0), new(-216.5399f, -602.6528f, 24.88029f, 2.687807f), new(-220.4879f, -596.382f, 21.95116f, 0), new(-190.4774f, -552.2778f, 51.31293f, 5.305801f), new(-195.6267f, -550.4393f, 51.31293f, 3.752458f), new(-209.7257f, -557.1042f, 51.31293f, 3.525565f), new(-187.9531f, -567.0469f, 51.31293f, 5.305801f), new(-192.2031f, -595.9636f, 36.37407f, 2.80998f), new(-183.4236f, -577.2674f, 46.87183f, 3.944444f), new(-184.6528f, -572.3663f, 49.27317f, 3.159046f), new(-187.3333f, -550.8143f, 19.3898f, 3.385939f), new(-185.2083f, -562.4844f, 19.3898f, 0.9599311f), new(-228.592f, -553.1684f, 19.3898f, 5.550147f), new(-210.7431f, -603.2813f, 27.17259f, 4.904375f), new(-194.1302f, -548.3055f, 19.3898f, 4.153883f), new(-181.2379f, -555.3177f, 19.3898f, 0.3141593f), new(-191.2205f, -581.4965f, 21.06015f, 2.007129f), new(-198.4653f, -580.757f, 21.06015f, 0.8901179f), new(-196.5504f, -587.7031f, 21.06015f, 1.27409f), new(-241.5938f, -578.6858f, 19.3898f, 2.775074f), new(-226.1615f, -573.8021f, 20.40991f, 5.218534f), new(-186.9792f, -556.8472f, 19.3898f, 4.153883f), new(-201.224f, -570.6788f, 21.06016f, 3.577925f), new(-196.8767f, -574.9688f, 21.06016f, 4.29351f), new(-225.6962f, -601.3871f, 21.82762f, 4.555309f), new(-215.7205f, -608.4722f, 25.87703f, 2.530727f), new(-197.1007f, -609.7257f, 32.38494f, 0), new(-221.8629f, -607.2205f, 23.7542f, 4.939282f), new(-201.9757f, -611.8663f, 30.62297f, 2.897247f)
    };

    public static readonly Position[] FamilySpawn =
    {
        new(-98.63194f, -721.6268f, 8.547067f, 1.53589f), new(5.239583f, -763.0868f, 9.800426f, 2.007129f), new(-83.86406f, -775.2837f, 28.37906f, 1.710423f), new(-83.16319f, -774.9636f, 26.90351f, 1.710423f)
    };

    public bool Nightmare;
    public bool ShiftToTwo;
    public bool ShiftToThree;
    public bool ShiftToFour;

    public byte Phase;
    public byte NightmareCount;
    public byte WorgenCount;
    public uint NightmareTimer;


    public npc_vanessa_nightmare(Creature creature) : base(creature, DMData.DATA_VANESSA_NIGHTMARE) { }


    public override void Reset()
    {
        Me.Say(boss_vanessa_vancleef.VANESSA_GLUB_1, Language.Universal);
        Nightmare = true;
        ShiftToTwo = false;
        ShiftToThree = false;
        ShiftToFour = false;
        NightmareCount = 0;
        WorgenCount = 0;
        Phase = 0;
        NightmareTimer = 3500;
        Summons.DespawnAll();
        Me.SetSpeed(UnitMoveType.Run, 5.0f);
    }

    public void NightmarePass()
    {
        NightmareCount++;

        if (NightmareCount == 1)
        {
            Summons.DespawnAll();
            ShiftToTwo = true;
            Phase = 4;
            NightmareTimer = 3500;
        }

        if (NightmareCount == 2)
        {
            Summons.DespawnAll();
            ShiftToThree = true;
            Phase = 9;
            NightmareTimer = 3500;
        }

        if (NightmareCount == 3)
        {
            Summons.DespawnAll();
            ShiftToFour = true;
            Phase = 13;
            NightmareTimer = 3500;
        }
    }

    public void WorgenKilled()
    {
        WorgenCount++;

        if (WorgenCount == 3)
            Phase = 18;

        if (WorgenCount == 6)
            Phase = 20;

        if (WorgenCount == 7)
            Phase = 23;
    }

    public override void JustSummoned(Creature summoned)
    {
        Summons.Summon(summoned);
    }

    public override void SummonedCreatureDespawn(Creature summon)
    {
        Summons.Despawn(summon);
    }

    public void SummonAllFires()
    {
        //for (byte i = 0; i < 4; ++i)
        //{
        //    Creature saFires = me.SummonCreature(DMCreatures.NPC_FIRE_BUNNY, FieryBlaze[i], TempSummonType.ManualDespawn);
        //    if (saFires != null)
        //    {
        //        saFires.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Pacified | UnitFlags.Uninteractible);
        //    }
        //}
    }

    public void SummonSparks()
    {
        for (byte i = 0; i < 7; ++i)
        {
            Creature sSp = Me.SummonCreature(DMCreatures.NPC_SPARK, ElectricSpark[i], TempSummonType.ManualDespawn);

            if (sSp != null)
                sSp.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Pacified | UnitFlags.Uninteractible);
        }
    }

    public void SummonWorgen_1()
    {
        for (byte i = 0; i < 3; ++i)
            Me.SummonCreature(DMCreatures.NPC_ENRAGED_WORGEN, EnragedWorgen_1[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
    }

    public void SummonWorgen_2()
    {
        for (byte i = 0; i < 3; ++i)
            Me.SummonCreature(DMCreatures.NPC_ENRAGED_WORGEN, EnragedWorgen_2[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
    }

    public override void UpdateAI(uint diff)
    {
        if (Nightmare)
        {
            if (NightmareTimer <= diff)
                switch (Phase)
                {
                    case 0:
                        SummonAllFires();
                        Me.Say(boss_vanessa_vancleef.VANESSA_GLUB_2, Language.Universal);
                        NightmareTimer = 3000;
                        Phase++;

                        break;
                    case 1:
                    {
                        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_3, null, true);
                        NightmareTimer = 4000;
                        Phase++;
                    }

                        break;
                    case 2:
                    {
                        var Glubtok = Me.FindNearestCreature(DMCreatures.NPC_GLUBTOK_NIGHTMARE, 200.0f, true);

                        if (Glubtok != null)
                        {
                            Glubtok.SetVisible(false);
                            Glubtok.MotionMaster.MoveCharge(-174.85f, -579.76f, 19.31f, 10.0f);
                        }

                        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_4, null, true);
                        NightmareTimer = 2000;
                        Phase++;
                    }

                        break;
                    case 3:
                        Nightmare = false;
                        Me.SetVisible(false);
                        Me.MotionMaster.MovePoint(0, -178.85f, -585.76f, 19.31f);

                        break;
                }
            else
                NightmareTimer -= diff;
        }

        if (ShiftToTwo)
        {
            if (NightmareTimer <= diff)
                switch (Phase)
                {
                    case 4:
                        Me.SetVisible(true);
                        Me.Say(boss_vanessa_vancleef.VANESSA_HELIX_1, Language.Universal);
                        Me.SummonCreature(DMCreatures.NPC_HELIX_NIGHTMARE, -174.85f, -579.76f, 19.31f, 3.14f, TempSummonType.ManualDespawn);
                        NightmareTimer = 3000;
                        Phase++;

                        break;
                    case 5:
                        Me.Say(boss_vanessa_vancleef.VANESSA_HELIX_2, Language.Universal);
                        NightmareTimer = 10000;
                        Phase++;

                        break;
                    case 6:
                        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_5, null, true);
                        NightmareTimer = 1000;
                        Phase++;

                        break;
                    case 7:
                        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_7, null, true);
                        Me.SetVisible(false);
                        NightmareTimer = 2000;
                        Phase++;

                        break;
                    case 8:
                        ShiftToTwo = false;
                        Me.MotionMaster.MovePoint(1, -150.96f, -579.99f, 19.31f);

                        break;
                    
                }
            else
                NightmareTimer -= diff;
        }

        if (ShiftToThree)
        {
            if (NightmareTimer <= diff)
                switch (Phase)
                {
                    case 9:
                    {
                        SummonSparks();
                        Me.SetVisible(true);
                        Instance.SetData(DMData.DATA_NIGHTMARE_HELIX, (uint)EncounterState.Done);
                        Me.Say(boss_vanessa_vancleef.VANESSA_MECHANICAL_1, Language.Universal);
                        Me.SummonCreature(DMCreatures.NPC_MECHANICAL_NIGHTMARE, -101.4549f, -663.6493f, 7.505813f, 1.85f, TempSummonType.ManualDespawn);
                        NightmareTimer = 4000;
                        Phase++;
                    }

                        break;
                    case 10:
                        Me.Say(boss_vanessa_vancleef.VANESSA_MECHANICAL_2, Language.Universal);
                        NightmareTimer = 3000;
                        Phase++;

                        break;
                    case 11:
                        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_8, null, true);
                        NightmareTimer = 3000;
                        Phase++;

                        break;
                    case 12:
                        ShiftToThree = false;
                        Me.SetVisible(false);
                        Me.MotionMaster.MovePoint(2, -96.46f, -660.42f, 7.41f);

                        break;
                }
            else
                NightmareTimer -= diff;
        }

        if (ShiftToFour)
        {
            if (NightmareTimer <= diff)
                switch (Phase)
                {
                    case 13:
                        Me.SetVisible(true);
                        Me.Say(boss_vanessa_vancleef.VANESSA_RIPSNARL_1, Language.Universal);
                        NightmareTimer = 4000;
                        Phase++;

                        break;
                    case 14:
                        Me.Say(boss_vanessa_vancleef.VANESSA_RIPSNARL_2, Language.Universal);
                        NightmareTimer = 6000;
                        Phase++;

                        break;
                    case 15:
                        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_9, null, true);
                        Instance.SetData(DMData.DATA_NIGHTMARE_MECHANICAL, (uint)EncounterState.Done);
                        NightmareTimer = 2000;
                        Phase++;

                        break;
                    case 16:
                    {
                        var players = new List<Unit>();

                        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                        var searcher = new PlayerListSearcher(Me, players, checker);
                        Cell.VisitGrid(Me, searcher, 150f);

                        foreach (var item in players)
                            Me.CastSpell(item, boss_vanessa_vancleef.Spells.SPRINT, true);

                        Me.SummonCreature(DMCreatures.NPC_EMME_HARRINGTON, FamilySpawn[0], TempSummonType.ManualDespawn);
                        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_10, null, true);
                        SummonWorgen_1();

                        Me.SetVisible(false);
                        Me.MotionMaster.MovePoint(3, -103.72f, -724.06f, 8.47f);
                        Phase++;
                        NightmareTimer = 1000;
                    }

                        break;
                    case 17:
                        break;
                    case 18:
                    {
                        var players = new List<Unit>();

                        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                        var searcher = new PlayerListSearcher(Me, players, checker);
                        Cell.VisitGrid(Me, searcher, 150f);

                        foreach (var item in players)
                        {
                            item.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_11, null, true);
                            Me.CastSpell(item, boss_vanessa_vancleef.Spells.SPRINT, true);
                        }

                        Me.SummonCreature(DMCreatures.NPC_ERIK_HARRINGTON, FamilySpawn[1], TempSummonType.ManualDespawn);
                        SummonWorgen_2();

                        Me.MotionMaster.MovePoint(4, 2.56f, -776.13f, 9.52f);
                        Phase++;
                        NightmareTimer = 3000;
                    }

                        break;
                    case 19:
                        break;
                    case 20:
                    {
                        var players = new List<Unit>();

                        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                        var searcher = new PlayerListSearcher(Me, players, checker);
                        Cell.VisitGrid(Me, searcher, 150f);

                        foreach (var item in players)
                        {
                            Me.CastSpell(item, boss_vanessa_vancleef.Spells.SPRINT, true);
                            item.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_12, null, true);
                        }

                        Me.MotionMaster.MovePoint(5, -83.16319f, -774.9636f, 26.90351f);
                        Me.SummonCreature(DMCreatures.NPC_JAMES_HARRINGTON, FamilySpawn[3], TempSummonType.ManualDespawn);
                        NightmareTimer = 5000;
                        Phase++;
                    }

                        break;
                    case 21:
                        NightmareTimer = 1000;
                        Phase++;

                        break;
                    case 22:
                        break;
                    case 23:
                    {
                        var players = new List<Unit>();
                        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                        var searcher = new PlayerListSearcher(Me, players, checker);
                        Cell.VisitGrid(Me, searcher, 150f);

                        foreach (var item in players)
                        {
                            item.RemoveAura(DMSharedSpells.NIGHTMARE_ELIXIR);
                            item.RemoveAura(boss_vanessa_vancleef.Spells.EFFECT_1);
                            item.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_13, null, true);
                        }

                        Me.SummonCreature(DMCreatures.NPC_VANESSA_BOSS, -79.44965f, -819.8351f, 39.89838f, 0.01745329f, TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(120000));
                        var note = Me.FindNearestCreature(DMCreatures.NPC_VANESSA_NOTE, 300.0f);

                        if (note != null)
                            note.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));

                        NightmareTimer = 1000;
                        Phase++;
                    }

                        break;
                    case 24:
                        break;
                    
                }
            else
                NightmareTimer -= diff;
        }
    }
}
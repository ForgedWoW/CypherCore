// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Karazhan.Netherspite;

internal struct SpellIds
{
    public const uint NetherburnAura = 30522;
    public const uint Voidzone = 37063;
    public const uint NetherInfusion = 38688;
    public const uint Netherbreath = 38523;
    public const uint BanishVisual = 39833;
    public const uint BanishRoot = 42716;
    public const uint Empowerment = 38549;
    public const uint NetherspiteRoar = 38684;
}

internal struct TextIds
{
    public const uint EmotePhasePortal = 0;
    public const uint EmotePhaseBanish = 1;
}

internal enum Portals
{
    Red = 0,   // Perseverence
    Green = 1, // Serenity
    Blue = 2   // Dominance
}

internal struct MiscConst
{
    public static Vector3[] PortalCoord =
    {
        new(-11195.353516f, -1613.237183f, 278.237258f), // Left side
        new(-11137.846680f, -1685.607422f, 278.239258f), // Right side
        new(-11094.493164f, -1591.969238f, 279.949188f)  // Back side
    };

    public static uint[] PortalID =
    {
        17369, 17367, 17368
    };

    public static uint[] PortalVisual =
    {
        30487, 30490, 30491
    };

    public static uint[] PortalBeam =
    {
        30465, 30464, 30463
    };

    public static uint[] PlayerBuff =
    {
        30421, 30422, 30423
    };

    public static uint[] NetherBuff =
    {
        30466, 30467, 30468
    };

    public static uint[] PlayerDebuff =
    {
        38637, 38638, 38639
    };
}

[Script]
internal class boss_netherspite : ScriptedAI
{
    private readonly ObjectGuid[] BeamerGUID = new ObjectGuid[3]; // Guid's of auxiliary beaming portals
    private readonly ObjectGuid[] BeamTarget = new ObjectGuid[3]; // Guid's of portals' current targets
    private readonly InstanceScript instance;
    private readonly ObjectGuid[] PortalGUID = new ObjectGuid[3]; // Guid's of portals
    private bool Berserk;
    private uint EmpowermentTimer;
    private uint NetherbreathTimer;
    private uint NetherInfusionTimer; // berserking timer
    private uint PhaseTimer;          // timer for phase switching

    private bool PortalPhase;
    private uint PortalTimer; // timer for beam checking
    private uint VoidZoneTimer;

    public boss_netherspite(Creature creature) : base(creature)
    {
        Initialize();
        instance = creature.InstanceScript;

        PortalPhase = false;
        PhaseTimer = 0;
        EmpowermentTimer = 0;
        PortalTimer = 0;
    }

    public override void Reset()
    {
        Initialize();

        HandleDoors(true);
        DestroyPortals();
    }

    public override void JustEngagedWith(Unit who)
    {
        HandleDoors(false);
        SwitchToPortalPhase();
    }

    public override void JustDied(Unit killer)
    {
        HandleDoors(true);
        DestroyPortals();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        // Void Zone
        if (VoidZoneTimer <= diff)
        {
            DoCast(SelectTarget(SelectTargetMethod.Random, 1, 45, true), SpellIds.Voidzone, new CastSpellExtraArgs(true));
            VoidZoneTimer = 15000;
        }
        else
        {
            VoidZoneTimer -= diff;
        }

        // NetherInfusion Berserk
        if (!Berserk &&
            NetherInfusionTimer <= diff)
        {
            Me.AddAura(SpellIds.NetherInfusion, Me);
            DoCast(Me, SpellIds.NetherspiteRoar);
            Berserk = true;
        }
        else
        {
            NetherInfusionTimer -= diff;
        }

        if (PortalPhase) // Portal Phase
        {
            // Distribute beams and buffs
            if (PortalTimer <= diff)
            {
                UpdatePortals();
                PortalTimer = 1000;
            }
            else
            {
                PortalTimer -= diff;
            }

            // Empowerment & Nether Burn
            if (EmpowermentTimer <= diff)
            {
                DoCast(Me, SpellIds.Empowerment);
                Me.AddAura(SpellIds.NetherburnAura, Me);
                EmpowermentTimer = 90000;
            }
            else
            {
                EmpowermentTimer -= diff;
            }

            if (PhaseTimer <= diff)
            {
                if (!Me.IsNonMeleeSpellCast(false))
                {
                    SwitchToBanishPhase();

                    return;
                }
            }
            else
            {
                PhaseTimer -= diff;
            }
        }
        else // Banish Phase
        {
            // Netherbreath
            if (NetherbreathTimer <= diff)
            {
                var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

                if (target)
                    DoCast(target, SpellIds.Netherbreath);

                NetherbreathTimer = RandomHelper.URand(5000, 7000);
            }
            else
            {
                NetherbreathTimer -= diff;
            }

            if (PhaseTimer <= diff)
            {
                if (!Me.IsNonMeleeSpellCast(false))
                {
                    SwitchToPortalPhase();

                    return;
                }
            }
            else
            {
                PhaseTimer -= diff;
            }
        }

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        Berserk = false;
        NetherInfusionTimer = 540000;
        VoidZoneTimer = 15000;
        NetherbreathTimer = 3000;
    }

    private bool IsBetween(WorldObject u1, WorldObject target, WorldObject u2) // the in-line checker
    {
        if (!u1 ||
            !u2 ||
            !target)
            return false;

        float xn, yn, xp, yp, xh, yh;
        xn = u1.Location.X;
        yn = u1.Location.Y;
        xp = u2.Location.X;
        yp = u2.Location.Y;
        xh = target.Location.X;
        yh = target.Location.Y;

        // check if Target is between (not checking distance from the beam yet)
        if (dist(xn, yn, xh, yh) >= dist(xn, yn, xp, yp) ||
            dist(xp, yp, xh, yh) >= dist(xn, yn, xp, yp))
            return false;

        // check  distance from the beam
        return (Math.Abs((xn - xp) * yh + (yp - yn) * xh - xn * yp + xp * yn) / dist(xn, yn, xp, yp) < 1.5f);
    }

    private double dist(float xa, float ya, float xb, float yb) // auxiliary method for distance
    {
        return MathF.Sqrt((xa - xb) * (xa - xb) + (ya - yb) * (ya - yb));
    }

    private void SummonPortals()
    {
        var r = RandomHelper.Rand32() % 4;
        var pos = new int[3];
        pos[(int)Portals.Red] = ((r % 2) != 0 ? (r > 1 ? 2 : 1) : 0);
        pos[(int)Portals.Green] = ((r % 2) != 0 ? 0 : (r > 1 ? 2 : 1));
        pos[(int)Portals.Blue] = (r > 1 ? 1 : 2); // Blue Portal not on the left side (0)

        for (var i = 0; i < 3; ++i)
        {
            Creature portal = Me.SummonCreature(MiscConst.PortalID[i], MiscConst.PortalCoord[pos[i]].X, MiscConst.PortalCoord[pos[i]].Y, MiscConst.PortalCoord[pos[i]].Z, 0, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

            if (portal)
            {
                PortalGUID[i] = portal.GUID;
                portal.AddAura(MiscConst.PortalVisual[i], portal);
            }
        }
    }

    private void DestroyPortals()
    {
        for (var i = 0; i < 3; ++i)
        {
            var portal = ObjectAccessor.GetCreature(Me, PortalGUID[i]);

            if (portal)
                portal.DisappearAndDie();

            var portal1 = ObjectAccessor.GetCreature(Me, BeamerGUID[i]);

            if (portal1)
                portal1.DisappearAndDie();

            PortalGUID[i].Clear();
            BeamTarget[i].Clear();
        }
    }

    private void UpdatePortals() // Here we handle the beams' behavior
    {
        for (var j = 0; j < 3; ++j) // j = color
        {
            var portal = ObjectAccessor.GetCreature(Me, PortalGUID[j]);

            if (portal)
            {
                // the one who's been cast upon before
                var current = Global.ObjAccessor.GetUnit(portal, BeamTarget[j]);
                // temporary store for the best suitable beam reciever
                Unit target = Me;

                var players = Me.Map.Players;

                // get the best suitable Target
                foreach (var player in players)
                    if (player &&
                        player.IsAlive // alive
                        &&
                        (!target || target.GetDistance2d(portal) > player.GetDistance2d(portal)) // closer than current best
                        &&
                        !player.HasAura(MiscConst.PlayerDebuff[j]) // not exhausted
                        &&
                        !player.HasAura(MiscConst.PlayerBuff[(j + 1) % 3]) // not on another beam
                        &&
                        !player.HasAura(MiscConst.PlayerBuff[(j + 2) % 3]) &&
                        IsBetween(Me, player, portal)) // on the beam
                        target = player;

                // buff the Target
                if (target.IsPlayer)
                    target.AddAura(MiscConst.PlayerBuff[j], target);
                else
                    target.AddAura(MiscConst.NetherBuff[j], target);

                // cast visual beam on the chosen Target if switched
                // simple Target switching isn't working . using BeamerGUID to cast (workaround)
                if (!current ||
                    target != current)
                {
                    BeamTarget[j] = target.GUID;
                    // remove currently beaming portal
                    var beamer = ObjectAccessor.GetCreature(portal, BeamerGUID[j]);

                    if (beamer)
                    {
                        beamer.CastSpell(target, MiscConst.PortalBeam[j], false);
                        beamer.DisappearAndDie();
                        BeamerGUID[j].Clear();
                    }

                    // create new one and start beaming on the Target
                    Creature beamer1 = portal.SummonCreature(MiscConst.PortalID[j], portal.Location.X, portal.Location.Y, portal.Location.Z, portal.Location.Orientation, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

                    if (beamer1)
                    {
                        beamer1.CastSpell(target, MiscConst.PortalBeam[j], false);
                        BeamerGUID[j] = beamer1.GUID;
                    }
                }

                // aggro Target if Red Beam
                if (j == (int)Portals.Red &&
                    Me.Victim != target &&
                    target.IsPlayer)
                    AddThreat(target, 100000.0f);
            }
        }
    }

    private void SwitchToPortalPhase()
    {
        Me.RemoveAura(SpellIds.BanishRoot);
        Me.RemoveAura(SpellIds.BanishVisual);
        SummonPortals();
        PhaseTimer = 60000;
        PortalPhase = true;
        PortalTimer = 10000;
        EmpowermentTimer = 10000;
        Talk(TextIds.EmotePhasePortal);
    }

    private void SwitchToBanishPhase()
    {
        Me.RemoveAura(SpellIds.Empowerment);
        Me.RemoveAura(SpellIds.NetherburnAura);
        DoCast(Me, SpellIds.BanishVisual, new CastSpellExtraArgs(true));
        DoCast(Me, SpellIds.BanishRoot, new CastSpellExtraArgs(true));
        DestroyPortals();
        PhaseTimer = 30000;
        PortalPhase = false;
        Talk(TextIds.EmotePhaseBanish);

        for (byte i = 0; i < 3; ++i)
            Me.RemoveAura(MiscConst.NetherBuff[i]);
    }

    private void HandleDoors(bool open) // Massive Door switcher
    {
        var Door = ObjectAccessor.GetGameObject(Me, instance.GetGuidData(DataTypes.GoMassiveDoor));

        if (Door)
            Door.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
    }
}
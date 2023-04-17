// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.Netherspite;

internal struct SpellIds
{
    public const uint NETHERBURN_AURA = 30522;
    public const uint VOIDZONE = 37063;
    public const uint NETHER_INFUSION = 38688;
    public const uint NETHERBREATH = 38523;
    public const uint BANISH_VISUAL = 39833;
    public const uint BANISH_ROOT = 42716;
    public const uint EMPOWERMENT = 38549;
    public const uint NETHERSPITE_ROAR = 38684;
}

internal struct TextIds
{
    public const uint EMOTE_PHASE_PORTAL = 0;
    public const uint EMOTE_PHASE_BANISH = 1;
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
internal class BossNetherspite : ScriptedAI
{
    private readonly ObjectGuid[] _beamerGUID = new ObjectGuid[3]; // Guid's of auxiliary beaming portals
    private readonly ObjectGuid[] _beamTarget = new ObjectGuid[3]; // Guid's of portals' current targets
    private readonly InstanceScript _instance;
    private readonly ObjectGuid[] _portalGUID = new ObjectGuid[3]; // Guid's of portals
    private bool _berserk;
    private uint _empowermentTimer;
    private uint _netherbreathTimer;
    private uint _netherInfusionTimer; // berserking timer
    private uint _phaseTimer;          // timer for phase switching

    private bool _portalPhase;
    private uint _portalTimer; // timer for beam checking
    private uint _voidZoneTimer;

    public BossNetherspite(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;

        _portalPhase = false;
        _phaseTimer = 0;
        _empowermentTimer = 0;
        _portalTimer = 0;
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
        if (_voidZoneTimer <= diff)
        {
            DoCast(SelectTarget(SelectTargetMethod.Random, 1, 45, true), SpellIds.VOIDZONE, new CastSpellExtraArgs(true));
            _voidZoneTimer = 15000;
        }
        else
        {
            _voidZoneTimer -= diff;
        }

        // NetherInfusion Berserk
        if (!_berserk &&
            _netherInfusionTimer <= diff)
        {
            Me.AddAura(SpellIds.NETHER_INFUSION, Me);
            DoCast(Me, SpellIds.NETHERSPITE_ROAR);
            _berserk = true;
        }
        else
        {
            _netherInfusionTimer -= diff;
        }

        if (_portalPhase) // Portal Phase
        {
            // Distribute beams and buffs
            if (_portalTimer <= diff)
            {
                UpdatePortals();
                _portalTimer = 1000;
            }
            else
            {
                _portalTimer -= diff;
            }

            // Empowerment & Nether Burn
            if (_empowermentTimer <= diff)
            {
                DoCast(Me, SpellIds.EMPOWERMENT);
                Me.AddAura(SpellIds.NETHERBURN_AURA, Me);
                _empowermentTimer = 90000;
            }
            else
            {
                _empowermentTimer -= diff;
            }

            if (_phaseTimer <= diff)
            {
                if (!Me.IsNonMeleeSpellCast(false))
                {
                    SwitchToBanishPhase();

                    return;
                }
            }
            else
            {
                _phaseTimer -= diff;
            }
        }
        else // Banish Phase
        {
            // Netherbreath
            if (_netherbreathTimer <= diff)
            {
                var target = SelectTarget(SelectTargetMethod.Random, 0, 40, true);

                if (target)
                    DoCast(target, SpellIds.NETHERBREATH);

                _netherbreathTimer = RandomHelper.URand(5000, 7000);
            }
            else
            {
                _netherbreathTimer -= diff;
            }

            if (_phaseTimer <= diff)
            {
                if (!Me.IsNonMeleeSpellCast(false))
                {
                    SwitchToPortalPhase();

                    return;
                }
            }
            else
            {
                _phaseTimer -= diff;
            }
        }

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _berserk = false;
        _netherInfusionTimer = 540000;
        _voidZoneTimer = 15000;
        _netherbreathTimer = 3000;
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
        if (Dist(xn, yn, xh, yh) >= Dist(xn, yn, xp, yp) ||
            Dist(xp, yp, xh, yh) >= Dist(xn, yn, xp, yp))
            return false;

        // check  distance from the beam
        return (Math.Abs((xn - xp) * yh + (yp - yn) * xh - xn * yp + xp * yn) / Dist(xn, yn, xp, yp) < 1.5f);
    }

    private double Dist(float xa, float ya, float xb, float yb) // auxiliary method for distance
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
                _portalGUID[i] = portal.GUID;
                portal.AddAura(MiscConst.PortalVisual[i], portal);
            }
        }
    }

    private void DestroyPortals()
    {
        for (var i = 0; i < 3; ++i)
        {
            var portal = ObjectAccessor.GetCreature(Me, _portalGUID[i]);

            if (portal)
                portal.DisappearAndDie();

            var portal1 = ObjectAccessor.GetCreature(Me, _beamerGUID[i]);

            if (portal1)
                portal1.DisappearAndDie();

            _portalGUID[i].Clear();
            _beamTarget[i].Clear();
        }
    }

    private void UpdatePortals() // Here we handle the beams' behavior
    {
        for (var j = 0; j < 3; ++j) // j = color
        {
            var portal = ObjectAccessor.GetCreature(Me, _portalGUID[j]);

            if (portal)
            {
                // the one who's been cast upon before
                var current = Global.ObjAccessor.GetUnit(portal, _beamTarget[j]);
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
                    _beamTarget[j] = target.GUID;
                    // remove currently beaming portal
                    var beamer = ObjectAccessor.GetCreature(portal, _beamerGUID[j]);

                    if (beamer)
                    {
                        beamer.SpellFactory.CastSpell(target, MiscConst.PortalBeam[j], false);
                        beamer.DisappearAndDie();
                        _beamerGUID[j].Clear();
                    }

                    // create new one and start beaming on the Target
                    Creature beamer1 = portal.SummonCreature(MiscConst.PortalID[j], portal.Location.X, portal.Location.Y, portal.Location.Z, portal.Location.Orientation, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

                    if (beamer1)
                    {
                        beamer1.SpellFactory.CastSpell(target, MiscConst.PortalBeam[j], false);
                        _beamerGUID[j] = beamer1.GUID;
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
        Me.RemoveAura(SpellIds.BANISH_ROOT);
        Me.RemoveAura(SpellIds.BANISH_VISUAL);
        SummonPortals();
        _phaseTimer = 60000;
        _portalPhase = true;
        _portalTimer = 10000;
        _empowermentTimer = 10000;
        Talk(TextIds.EMOTE_PHASE_PORTAL);
    }

    private void SwitchToBanishPhase()
    {
        Me.RemoveAura(SpellIds.EMPOWERMENT);
        Me.RemoveAura(SpellIds.NETHERBURN_AURA);
        DoCast(Me, SpellIds.BANISH_VISUAL, new CastSpellExtraArgs(true));
        DoCast(Me, SpellIds.BANISH_ROOT, new CastSpellExtraArgs(true));
        DestroyPortals();
        _phaseTimer = 30000;
        _portalPhase = false;
        Talk(TextIds.EMOTE_PHASE_BANISH);

        for (byte i = 0; i < 3; ++i)
            Me.RemoveAura(MiscConst.NetherBuff[i]);
    }

    private void HandleDoors(bool open) // Massive Door switcher
    {
        var door = ObjectAccessor.GetGameObject(Me, _instance.GetGuidData(DataTypes.GO_MASSIVE_DOOR));

        if (door)
            door.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
    }
}
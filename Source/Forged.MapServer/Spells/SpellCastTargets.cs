// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellCastTargets
{
    private SpellDestination _dst;

    // objects (can be used at spell creating and after Update at casting)
    private Item _itemTarget;

    // object GUID/etc, can be used always
    private ObjectGuid _objectTargetGuid;

    public SpellCastTargets()
    {
        TargetString = "";

        Src = new SpellDestination();
        _dst = new SpellDestination();
    }

    public SpellCastTargets(Unit caster, SpellCastRequest spellCastRequest)
    {
        TargetMask = spellCastRequest.Target.Flags;
        _objectTargetGuid = spellCastRequest.Target.Unit;
        ItemTargetGuid = spellCastRequest.Target.Item;
        TargetString = spellCastRequest.Target.Name;

        Src = new SpellDestination();
        _dst = new SpellDestination();

        if (spellCastRequest.Target.SrcLocation != null)
        {
            Src.TransportGuid = spellCastRequest.Target.SrcLocation.Transport;

            var pos = !Src.TransportGuid.IsEmpty ? Src.TransportOffset : Src.Position;

            pos.Relocate(spellCastRequest.Target.SrcLocation.Location);

            if (spellCastRequest.Target.Orientation.HasValue)
                pos.Orientation = spellCastRequest.Target.Orientation.Value;
        }

        if (spellCastRequest.Target.DstLocation != null)
        {
            _dst.TransportGuid = spellCastRequest.Target.DstLocation.Transport;

            var pos = !_dst.TransportGuid.IsEmpty ? _dst.TransportOffset : _dst.Position;

            pos.Relocate(spellCastRequest.Target.DstLocation.Location);

            if (spellCastRequest.Target.Orientation.HasValue)
                pos.Orientation = spellCastRequest.Target.Orientation.Value;
        }

        Pitch = spellCastRequest.MissileTrajectory.Pitch;
        Speed = spellCastRequest.MissileTrajectory.Speed;

        Update(caster);
    }

    public Corpse CorpseTarget => ObjectTarget?.AsCorpse;

    public ObjectGuid CorpseTargetGUID
    {
        get
        {
            if (_objectTargetGuid.IsCorpse)
                return _objectTargetGuid;

            return ObjectGuid.Empty;
        }
    }

    public float Dist2d => Src.Position.GetExactDist2d(_dst.Position);

    public SpellDestination Dst
    {
        get => _dst;
        set
        {
            _dst = value;
            TargetMask |= SpellCastTargetFlags.DestLocation;
        }
    }

    public WorldLocation DstPos => _dst.Position;

    public GameObject GOTarget
    {
        get => ObjectTarget?.AsGameObject;

        set
        {
            if (value == null)
                return;

            ObjectTarget = value;
            _objectTargetGuid = value.GUID;
            TargetMask |= SpellCastTargetFlags.Gameobject;
        }
    }

    public bool HasDst => Convert.ToBoolean(TargetMask & SpellCastTargetFlags.DestLocation);
    public bool HasSrc => Convert.ToBoolean(TargetMask & SpellCastTargetFlags.SourceLocation);
    public bool HasTraj => Speed != 0;

    public Item ItemTarget
    {
        get => _itemTarget;
        set
        {
            if (value == null)
                return;

            _itemTarget = value;
            ItemTargetGuid = value.GUID;
            ItemTargetEntry = value.Entry;
            TargetMask |= SpellCastTargetFlags.Item;
        }
    }

    public uint ItemTargetEntry { get; private set; }
    public ObjectGuid ItemTargetGuid { get; private set; }
    public WorldObject ObjectTarget { get; private set; }
    public ObjectGuid ObjectTargetGUID => _objectTargetGuid;
    public float Pitch { get; set; }
    public float Speed { get; set; }
    public float SpeedXY => (float)(Speed * Math.Cos(Pitch));
    public float SpeedZ => (float)(Speed * Math.Sin(Pitch));
    public SpellDestination Src { get; private set; }
    public Position SrcPos => Src.Position;
    public SpellCastTargetFlags TargetMask { get; set; }
    public string TargetString { get; }

    public Unit UnitTarget
    {
        get => ObjectTarget?.AsUnit;

        set
        {
            if (value == null)
                return;

            ObjectTarget = value;
            _objectTargetGuid = value.GUID;
            TargetMask |= SpellCastTargetFlags.Unit;
        }
    }

    public ObjectGuid UnitTargetGUID => _objectTargetGuid.IsUnit ? _objectTargetGuid : ObjectGuid.Empty;

    public void ModDst(Position pos)
    {
        _dst.Relocate(pos);
    }

    public void ModDst(SpellDestination spellDest)
    {
        _dst = spellDest;
    }

    public void ModSrc(Position pos)
    {
        Src.Relocate(pos);
    }

    public void RemoveDst()
    {
        TargetMask &= ~SpellCastTargetFlags.DestLocation;
    }

    public void RemoveObjectTarget()
    {
        ObjectTarget = null;
        _objectTargetGuid.Clear();
        TargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask | SpellCastTargetFlags.GameobjectMask);
    }

    public void RemoveSrc()
    {
        TargetMask &= ~SpellCastTargetFlags.SourceLocation;
    }

    public void SetDst(float x, float y, float z, float orientation, uint mapId = 0xFFFFFFFF)
    {
        _dst = new SpellDestination(x, y, z, orientation, mapId);
        TargetMask |= SpellCastTargetFlags.DestLocation;
    }

    public void SetDst(Position pos)
    {
        _dst = new SpellDestination(pos);
        TargetMask |= SpellCastTargetFlags.DestLocation;
    }

    public void SetDst(WorldObject wObj)
    {
        _dst = new SpellDestination(wObj);
        TargetMask |= SpellCastTargetFlags.DestLocation;
    }

    public void SetDst(SpellCastTargets spellTargets)
    {
        _dst = spellTargets._dst;
        TargetMask |= SpellCastTargetFlags.DestLocation;
    }

    public void SetSrc(WorldObject wObj)
    {
        Src = new SpellDestination(wObj);
        TargetMask |= SpellCastTargetFlags.SourceLocation;
    }

    public void SetTargetFlag(SpellCastTargetFlags flag)
    {
        TargetMask |= flag;
    }

    public void SetTradeItemTarget(Player caster)
    {
        ItemTargetGuid = ObjectGuid.TradeItem;
        ItemTargetEntry = 0;
        TargetMask |= SpellCastTargetFlags.TradeItem;

        Update(caster);
    }

    public void Update(WorldObject caster)
    {
        ObjectTarget = _objectTargetGuid == caster.GUID ? caster : caster.ObjectAccessor.GetWorldObject(caster, _objectTargetGuid);

        _itemTarget = null;

        if (caster is Player)
        {
            var player = caster.AsPlayer;

            if (TargetMask.HasAnyFlag(SpellCastTargetFlags.Item))
                _itemTarget = player.GetItemByGuid(ItemTargetGuid);
            else if (TargetMask.HasAnyFlag(SpellCastTargetFlags.TradeItem))
                if (ItemTargetGuid == ObjectGuid.TradeItem) // here it is not guid but slot. Also prevents hacking slots
                {
                    var pTrade = player.TradeData;

                    if (pTrade != null)
                        _itemTarget = pTrade.TraderData.GetItem(TradeSlots.NonTraded);
                }

            if (_itemTarget != null)
                ItemTargetEntry = _itemTarget.Entry;
        }

        // update positions by transport move
        if (HasSrc && !Src.TransportGuid.IsEmpty)
        {
            var transport = caster.ObjectAccessor.GetWorldObject(caster, Src.TransportGuid);

            if (transport != null)
            {
                Src.Position.Relocate(transport.Location);
                Src.Position.RelocateOffset(Src.TransportOffset);
            }
        }

        if (!HasDst || _dst.TransportGuid.IsEmpty)
            return;

        var destTransport = caster.ObjectAccessor.GetWorldObject(caster, _dst.TransportGuid);

        if (destTransport == null)
            return;

        _dst.Position.Relocate(destTransport.Location);
        _dst.Position.RelocateOffset(_dst.TransportOffset);
    }

    public void UpdateTradeSlotItem()
    {
        if (_itemTarget == null || !Convert.ToBoolean(TargetMask & SpellCastTargetFlags.TradeItem))
            return;

        ItemTargetGuid = _itemTarget.GUID;
        ItemTargetEntry = _itemTarget.Entry;
    }

    public void Write(SpellTargetData data)
    {
        data.Flags = TargetMask;

        if (TargetMask.HasAnyFlag(SpellCastTargetFlags.Unit | SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.CorpseEnemy | SpellCastTargetFlags.UnitMinipet))
            data.Unit = _objectTargetGuid;

        if (TargetMask.HasAnyFlag(SpellCastTargetFlags.Item | SpellCastTargetFlags.TradeItem) && _itemTarget != null)
            data.Item = _itemTarget.GUID;

        if (TargetMask.HasAnyFlag(SpellCastTargetFlags.SourceLocation))
        {
            TargetLocation target = new()
            {
                Transport = Src.TransportGuid,
                Location = !Src.TransportGuid.IsEmpty ? Src.TransportOffset : Src.Position
                // relative position guid here - transport for example
            };

            data.SrcLocation = target;
        }

        if (Convert.ToBoolean(TargetMask & SpellCastTargetFlags.DestLocation))
        {
            TargetLocation target = new()
            {
                Transport = _dst.TransportGuid,
                Location = !_dst.TransportGuid.IsEmpty ? _dst.TransportOffset : _dst.Position
                // relative position guid here - transport for example
            };

            data.DstLocation = target;
        }

        if (Convert.ToBoolean(TargetMask & SpellCastTargetFlags.String))
            data.Name = TargetString;
    }
}
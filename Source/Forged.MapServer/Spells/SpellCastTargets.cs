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
	readonly string _strTarget;

	// objects (can be used at spell creating and after Update at casting)
	WorldObject _objectTarget;
	Item _itemTarget;

	// object GUID/etc, can be used always
	ObjectGuid _objectTargetGuid;
	ObjectGuid _itemTargetGuid;
	uint _itemTargetEntry;

	SpellDestination _src;
	SpellDestination _dst;


	public SpellCastTargetFlags TargetMask { get; set; }

	public ObjectGuid ItemTargetGuid => _itemTargetGuid;

	public Item ItemTarget
	{
		get => _itemTarget;
		set
		{
			if (value == null)
				return;

			_itemTarget = value;
			_itemTargetGuid = value.GUID;
			_itemTargetEntry = value.Entry;
			TargetMask |= SpellCastTargetFlags.Item;
		}
	}

	public uint ItemTargetEntry => _itemTargetEntry;
	public bool HasSrc => Convert.ToBoolean(TargetMask & SpellCastTargetFlags.SourceLocation);

	public bool HasDst => Convert.ToBoolean(TargetMask & SpellCastTargetFlags.DestLocation);

	public bool HasTraj => Speed != 0;

	public float Pitch { get; set; }

	public float Speed { get; set; }

	public float Dist2d => _src.Position.GetExactDist2d(_dst.Position);
	public float SpeedXY => (float)(Speed * Math.Cos(Pitch));

	public float SpeedZ => (float)(Speed * Math.Sin(Pitch));
	public string TargetString => _strTarget;

	public ObjectGuid UnitTargetGUID
	{
		get
		{
			if (_objectTargetGuid.IsUnit)
				return _objectTargetGuid;

			return ObjectGuid.Empty;
		}
	}

	public Unit UnitTarget
	{
		get
		{
			if (_objectTarget)
				return _objectTarget.AsUnit;

			return null;
		}

		set
		{
			if (value == null)
				return;

			_objectTarget = value;
			_objectTargetGuid = value.GUID;
			TargetMask |= SpellCastTargetFlags.Unit;
		}
	}

	public GameObject GOTarget
	{
		get
		{
			if (_objectTarget != null)
				return _objectTarget.AsGameObject;

			return null;
		}

		set
		{
			if (value == null)
				return;

			_objectTarget = value;
			_objectTargetGuid = value.GUID;
			TargetMask |= SpellCastTargetFlags.Gameobject;
		}
	}

	public ObjectGuid CorpseTargetGUID
	{
		get
		{
			if (_objectTargetGuid.IsCorpse)
				return _objectTargetGuid;

			return ObjectGuid.Empty;
		}
	}

	public Corpse CorpseTarget
	{
		get
		{
			if (_objectTarget != null)
				return _objectTarget.AsCorpse;

			return null;
		}
	}

	public WorldObject ObjectTarget => _objectTarget;

	public ObjectGuid ObjectTargetGUID => _objectTargetGuid;

	public SpellDestination Src => _src;

	public Position SrcPos => _src.Position;

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

	ObjectGuid GOTargetGUID
	{
		get
		{
			if (_objectTargetGuid.IsAnyTypeGameObject)
				return _objectTargetGuid;

			return ObjectGuid.Empty;
		}
	}

	public SpellCastTargets()
	{
		_strTarget = "";

		_src = new SpellDestination();
		_dst = new SpellDestination();
	}

	public SpellCastTargets(Unit caster, SpellCastRequest spellCastRequest)
	{
		TargetMask = spellCastRequest.Target.Flags;
		_objectTargetGuid = spellCastRequest.Target.Unit;
		_itemTargetGuid = spellCastRequest.Target.Item;
		_strTarget = spellCastRequest.Target.Name;

		_src = new SpellDestination();
		_dst = new SpellDestination();

		if (spellCastRequest.Target.SrcLocation != null)
		{
			_src.TransportGuid = spellCastRequest.Target.SrcLocation.Transport;
			Position pos;

			if (!_src.TransportGuid.IsEmpty)
				pos = _src.TransportOffset;
			else
				pos = _src.Position;

			pos.Relocate(spellCastRequest.Target.SrcLocation.Location);

			if (spellCastRequest.Target.Orientation.HasValue)
				pos.Orientation = spellCastRequest.Target.Orientation.Value;
		}

		if (spellCastRequest.Target.DstLocation != null)
		{
			_dst.TransportGuid = spellCastRequest.Target.DstLocation.Transport;
			Position pos;

			if (!_dst.TransportGuid.IsEmpty)
				pos = _dst.TransportOffset;
			else
				pos = _dst.Position;

			pos.Relocate(spellCastRequest.Target.DstLocation.Location);

			if (spellCastRequest.Target.Orientation.HasValue)
				pos.Orientation = spellCastRequest.Target.Orientation.Value;
		}

		Pitch = spellCastRequest.MissileTrajectory.Pitch;
		Speed = spellCastRequest.MissileTrajectory.Speed;

		Update(caster);
	}

	public void Write(SpellTargetData data)
	{
		data.Flags = TargetMask;

		if (TargetMask.HasAnyFlag(SpellCastTargetFlags.Unit | SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.CorpseEnemy | SpellCastTargetFlags.UnitMinipet))
			data.Unit = _objectTargetGuid;

		if (TargetMask.HasAnyFlag(SpellCastTargetFlags.Item | SpellCastTargetFlags.TradeItem) && _itemTarget)
			data.Item = _itemTarget.GUID;

		if (TargetMask.HasAnyFlag(SpellCastTargetFlags.SourceLocation))
		{
			TargetLocation target = new()
			{
				Transport = _src.TransportGuid // relative position guid here - transport for example
			};

			if (!_src.TransportGuid.IsEmpty)
				target.Location = _src.TransportOffset;
			else
				target.Location = _src.Position;

			data.SrcLocation = target;
		}

		if (Convert.ToBoolean(TargetMask & SpellCastTargetFlags.DestLocation))
		{
			TargetLocation target = new()
			{
				Transport = _dst.TransportGuid // relative position guid here - transport for example
			};

			if (!_dst.TransportGuid.IsEmpty)
				target.Location = _dst.TransportOffset;
			else
				target.Location = _dst.Position;

			data.DstLocation = target;
		}

		if (Convert.ToBoolean(TargetMask & SpellCastTargetFlags.String))
			data.Name = _strTarget;
	}

	public void RemoveObjectTarget()
	{
		_objectTarget = null;
		_objectTargetGuid.Clear();
		TargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask | SpellCastTargetFlags.GameobjectMask);
	}

	public void SetTradeItemTarget(Player caster)
	{
		_itemTargetGuid = ObjectGuid.TradeItem;
		_itemTargetEntry = 0;
		TargetMask |= SpellCastTargetFlags.TradeItem;

		Update(caster);
	}

	public void UpdateTradeSlotItem()
	{
		if (_itemTarget != null && Convert.ToBoolean(TargetMask & SpellCastTargetFlags.TradeItem))
		{
			_itemTargetGuid = _itemTarget.GUID;
			_itemTargetEntry = _itemTarget.Entry;
		}
	}

	public void SetSrc(WorldObject wObj)
	{
		_src = new SpellDestination(wObj);
		TargetMask |= SpellCastTargetFlags.SourceLocation;
	}

	public void ModSrc(Position pos)
	{
		_src.Relocate(pos);
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

	public void ModDst(Position pos)
	{
		_dst.Relocate(pos);
	}

	public void ModDst(SpellDestination spellDest)
	{
		_dst = spellDest;
	}

	public void RemoveDst()
	{
		TargetMask &= ~SpellCastTargetFlags.DestLocation;
	}

	public void Update(WorldObject caster)
	{
		_objectTarget = (_objectTargetGuid == caster.GUID) ? caster : Global.ObjAccessor.GetWorldObject(caster, _objectTargetGuid);

		_itemTarget = null;

		if (caster is Player)
		{
			var player = caster.AsPlayer;

			if (TargetMask.HasAnyFlag(SpellCastTargetFlags.Item))
				_itemTarget = player.GetItemByGuid(_itemTargetGuid);
			else if (TargetMask.HasAnyFlag(SpellCastTargetFlags.TradeItem))
				if (_itemTargetGuid == ObjectGuid.TradeItem) // here it is not guid but slot. Also prevents hacking slots
				{
					var pTrade = player.GetTradeData();

					if (pTrade != null)
						_itemTarget = pTrade.GetTraderData().GetItem(TradeSlots.NonTraded);
				}

			if (_itemTarget != null)
				_itemTargetEntry = _itemTarget.Entry;
		}

		// update positions by transport move
		if (HasSrc && !_src.TransportGuid.IsEmpty)
		{
			var transport = Global.ObjAccessor.GetWorldObject(caster, _src.TransportGuid);

			if (transport != null)
			{
				_src.Position.Relocate(transport.Location);
				_src.Position.RelocateOffset(_src.TransportOffset);
			}
		}

		if (HasDst && !_dst.TransportGuid.IsEmpty)
		{
			var transport = Global.ObjAccessor.GetWorldObject(caster, _dst.TransportGuid);

			if (transport != null)
			{
				_dst.Position.Relocate(transport.Location);
				_dst.Position.RelocateOffset(_dst.TransportOffset);
			}
		}
	}

	public void SetTargetFlag(SpellCastTargetFlags flag)
	{
		TargetMask |= flag;
	}

	void SetSrc(float x, float y, float z)
	{
		_src = new SpellDestination(x, y, z);
		TargetMask |= SpellCastTargetFlags.SourceLocation;
	}

	void SetSrc(Position pos)
	{
		_src = new SpellDestination(pos);
		TargetMask |= SpellCastTargetFlags.SourceLocation;
	}
}
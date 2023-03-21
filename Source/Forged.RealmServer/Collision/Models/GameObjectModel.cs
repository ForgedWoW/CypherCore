// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Framework.Constants;
using Framework.GameMath;

namespace Forged.RealmServer.Collision;

public class GameObjectModel : IModel
{
	bool _collisionEnabled;
	AxisAlignedBox _iBound;
	Matrix4x4 _iInvRot;
	Vector3 _iPos;
	float _iInvScale;
	float _iScale;
	WorldModel _iModel;
	GameObjectModelOwnerBase _owner;
	bool _isWmo;

	public static GameObjectModel Create(GameObjectModelOwnerBase modelOwner)
	{
		GameObjectModel mdl = new();

		if (!mdl.Initialize(modelOwner))
			return null;

		return mdl;
	}

	public override bool IntersectRay(Ray ray, ref float maxDist, bool stopAtFirstHit, PhaseShift phaseShift, ModelIgnoreFlags ignoreFlags)
	{
		if (!IsCollisionEnabled() || !_owner.IsSpawned())
			return false;

		if (!_owner.IsInPhase(phaseShift))
			return false;

		var time = ray.intersectionTime(_iBound);

		if (time == float.PositiveInfinity)
			return false;

		// child bounds are defined in object space:
		var p = _iInvRot.Multiply(ray.Origin - _iPos) * _iInvScale;
		var modRay = new Ray(p, _iInvRot.Multiply(ray.Direction));
		var distance = maxDist * _iInvScale;
		var hit = _iModel.IntersectRay(modRay, ref distance, stopAtFirstHit, ignoreFlags);

		if (hit)
		{
			distance *= _iScale;
			maxDist = distance;
		}

		return hit;
	}

	public override void IntersectPoint(Vector3 point, AreaInfo info, PhaseShift phaseShift)
	{
		if (!IsCollisionEnabled() || !_owner.IsSpawned() || !IsMapObject())
			return;

		if (!_owner.IsInPhase(phaseShift))
			return;

		if (!_iBound.contains(point))
			return;

		// child bounds are defined in object space:
		var pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;
		var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

		if (_iModel.IntersectPoint(pModel, zDirModel, out var zDist, info))
		{
			var modelGround = pModel + zDist * zDirModel;
			var world_Z = (_iInvRot.Multiply(modelGround) * _iScale + _iPos).Z;

			if (info.GroundZ < world_Z)
			{
				info.GroundZ = world_Z;
				info.AdtId = _owner.GetNameSetId();
			}
		}
	}

	public bool GetLocationInfo(Vector3 point, LocationInfo info, PhaseShift phaseShift)
	{
		if (!IsCollisionEnabled() || !_owner.IsSpawned() || !IsMapObject())
			return false;

		if (!_owner.IsInPhase(phaseShift))
			return false;

		if (!_iBound.contains(point))
			return false;

		// child bounds are defined in object space:
		var pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;
		var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

		GroupLocationInfo groupInfo = new();

		if (_iModel.GetLocationInfo(pModel, zDirModel, out var zDist, groupInfo))
		{
			var modelGround = pModel + zDist * zDirModel;
			var world_Z = (_iInvRot.Multiply(modelGround) * _iScale + _iPos).Z;

			if (info.GroundZ < world_Z)
			{
				info.GroundZ = world_Z;

				return true;
			}
		}

		return false;
	}

	public bool GetLiquidLevel(Vector3 point, LocationInfo info, ref float liqHeight)
	{
		// child bounds are defined in object space:
		var pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;

		//Vector3 zDirModel = iInvRot * Vector3(0.f, 0.f, -1.f);
		if (info.HitModel.GetLiquidLevel(pModel, out var zDist))
		{
			// calculate world height (zDist in model coords):
			// assume WMO not tilted (wouldn't make much sense anyway)
			liqHeight = zDist * _iScale + _iPos.Z;

			return true;
		}

		return false;
	}

	public bool UpdatePosition()
	{
		if (_iModel == null)
			return false;

		var it = StaticModelList.Models.LookupByKey(_owner.GetDisplayId());

		if (it == null)
			return false;

		AxisAlignedBox mdl_box = new(it.Bound);

		// ignore models with no bounds
		if (mdl_box == AxisAlignedBox.Zero())
		{
			Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", it.Name);

			return false;
		}

		_iPos = _owner.GetPosition();

		var iRotation = _owner.GetRotation().ToMatrix();
		iRotation.Inverse(out _iInvRot);
		// transform bounding box:
		mdl_box = new AxisAlignedBox(mdl_box.Lo * _iScale, mdl_box.Hi * _iScale);
		AxisAlignedBox rotated_bounds = new();

		for (var i = 0; i < 8; ++i)
			rotated_bounds.merge(iRotation.Multiply(mdl_box.corner(i)));

		_iBound = rotated_bounds + _iPos;

		return true;
	}

	public virtual Vector3 GetPosition()
	{
		return _iPos;
	}

	public override AxisAlignedBox GetBounds()
	{
		return _iBound;
	}

	public void EnableCollision(bool enable)
	{
		_collisionEnabled = enable;
	}

	public bool IsMapObject()
	{
		return _isWmo;
	}

	public byte GetNameSetId()
	{
		return _owner.GetNameSetId();
	}

	public static bool LoadGameObjectModelList()
	{
		var oldMSTime = Time.MSTime;
		var filename = Global.WorldMgr.DataPath + "/vmaps/GameObjectModels.dtree";

		if (!File.Exists(filename))
		{
			Log.outWarn(LogFilter.Server, "Unable to open '{0}' file.", filename);

			return false;
		}

		try
		{
			using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
			var magic = reader.ReadStringFromChars(8);

			if (magic != MapConst.VMapMagic)
			{
				Log.outError(LogFilter.Misc, $"File '{filename}' has wrong header, expected {MapConst.VMapMagic}.");

				return false;
			}

			var length = reader.BaseStream.Length;

			while (true)
			{
				if (reader.BaseStream.Position >= length)
					break;

				var displayId = reader.ReadUInt32();
				var isWmo = reader.ReadBoolean();
				var nameLength = reader.ReadInt32();
				var name = reader.ReadString(nameLength);
				var v1 = reader.Read<Vector3>();
				var v2 = reader.Read<Vector3>();

				StaticModelList.Models.Add(displayId, new GameobjectModelData(name, v1, v2, isWmo));
			}
		}
		catch (EndOfStreamException ex)
		{
			Log.outException(ex);
		}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} GameObject models in {1} ms", StaticModelList.Models.Count, Time.GetMSTimeDiffToNow(oldMSTime));

		return true;
	}

	bool Initialize(GameObjectModelOwnerBase modelOwner)
	{
		var modelData = StaticModelList.Models.LookupByKey(modelOwner.GetDisplayId());

		if (modelData == null)
			return false;

		AxisAlignedBox mdl_box = new(modelData.Bound);

		// ignore models with no bounds
		if (mdl_box == AxisAlignedBox.Zero())
		{
			Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", modelData.Name);

			return false;
		}

		_iModel = Global.VMapMgr.AcquireModelInstance(modelData.Name);

		if (_iModel == null)
			return false;

		_iPos = modelOwner.GetPosition();
		_iScale = modelOwner.GetScale();
		_iInvScale = 1.0f / _iScale;

		var iRotation = modelOwner.GetRotation().ToMatrix();
		iRotation.Inverse(out _iInvRot);
		// transform bounding box:
		mdl_box = new AxisAlignedBox(mdl_box.Lo * _iScale, mdl_box.Hi * _iScale);
		AxisAlignedBox rotated_bounds = new();

		for (var i = 0; i < 8; ++i)
			rotated_bounds.merge(iRotation.Multiply(mdl_box.corner(i)));

		_iBound = rotated_bounds + _iPos;
		_owner = modelOwner;
		_isWmo = modelData.IsWmo;

		return true;
	}

	bool IsCollisionEnabled()
	{
		return _collisionEnabled;
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Framework.Constants;
using Framework.GameMath;

namespace Game.Collision;

public class ModelInstance : ModelMinimalData
{
	readonly Matrix4x4 _iInvRot;
	readonly float _iInvScale;
	WorldModel _iModel;

	public ModelInstance()
	{
		_iInvScale = 0.0f;
		_iModel = null;
	}

	public ModelInstance(ModelSpawn spawn, WorldModel model)
	{
		Flags = spawn.Flags;
		AdtId = spawn.AdtId;
		Id = spawn.Id;
		IPos = spawn.IPos;
		IScale = spawn.IScale;
		IBound = spawn.IBound;
		Name = spawn.Name;

		_iModel = model;

		Extensions.fromEulerAnglesZYX(MathFunctions.PI * spawn.IRot.Y / 180.0f, MathFunctions.PI * spawn.IRot.X / 180.0f, MathFunctions.PI * spawn.IRot.Z / 180.0f).Inverse(out _iInvRot);

		_iInvScale = 1.0f / IScale;
	}

	public bool IntersectRay(Ray pRay, ref float pMaxDist, bool pStopAtFirstHit, ModelIgnoreFlags ignoreFlags)
	{
		if (_iModel == null)
			return false;

		var time = pRay.intersectionTime(IBound);

		if (float.IsInfinity(time))
			return false;

		// child bounds are defined in object space:
		var p = _iInvRot.Multiply(pRay.Origin - IPos) * _iInvScale;
		var modRay = new Ray(p, _iInvRot.Multiply(pRay.Direction));
		var distance = pMaxDist * _iInvScale;
		var hit = _iModel.IntersectRay(modRay, ref distance, pStopAtFirstHit, ignoreFlags);

		if (hit)
		{
			distance *= IScale;
			pMaxDist = distance;
		}

		return hit;
	}

	public void IntersectPoint(Vector3 p, AreaInfo info)
	{
		if (_iModel == null)
			return;

		// M2 files don't contain area info, only WMO files
		if (Convert.ToBoolean(Flags & (uint)ModelFlags.M2))
			return;

		if (!IBound.contains(p))
			return;

		// child bounds are defined in object space:
		var pModel = _iInvRot.Multiply(p - IPos) * _iInvScale;
		var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

		if (_iModel.IntersectPoint(pModel, zDirModel, out var zDist, info))
		{
			var modelGround = pModel + zDist * zDirModel;
			// Transform back to world space. Note that:
			// Mat * vec == vec * Mat.transpose()
			// and for rotation matrices: Mat.inverse() == Mat.transpose()
			var world_Z = (_iInvRot.Multiply(modelGround) * IScale + IPos).Z;

			if (info.GroundZ < world_Z)
			{
				info.GroundZ = world_Z;
				info.AdtId = AdtId;
			}
		}
	}

	public bool GetLiquidLevel(Vector3 p, LocationInfo info, ref float liqHeight)
	{
		// child bounds are defined in object space:
		var pModel = _iInvRot.Multiply(p - IPos) * _iInvScale;

		//Vector3 zDirModel = iInvRot * Vector3(0.f, 0.f, -1.f);
		if (info.HitModel.GetLiquidLevel(pModel, out var zDist))
		{
			// calculate world height (zDist in model coords):
			// assume WMO not tilted (wouldn't make much sense anyway)
			liqHeight = zDist * IScale + IPos.Z;

			return true;
		}

		return false;
	}

	public bool GetLocationInfo(Vector3 p, LocationInfo info)
	{
		if (_iModel == null)
			return false;

		// M2 files don't contain area info, only WMO files
		if (Convert.ToBoolean(Flags & (uint)ModelFlags.M2))
			return false;

		if (!IBound.contains(p))
			return false;

		// child bounds are defined in object space:
		var pModel = _iInvRot.Multiply(p - IPos) * _iInvScale;
		var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

		GroupLocationInfo groupInfo = new();

		if (_iModel.GetLocationInfo(pModel, zDirModel, out var zDist, groupInfo))
		{
			var modelGround = pModel + zDist * zDirModel;
			// Transform back to world space. Note that:
			// Mat * vec == vec * Mat.transpose()
			// and for rotation matrices: Mat.inverse() == Mat.transpose()
			var worldZ = (_iInvRot.Multiply(modelGround * IScale) + IPos).Z;

			if (info.GroundZ < worldZ) // hm...could it be handled automatically with zDist at intersection?
			{
				info.RootId = groupInfo.RootId;
				info.HitModel = groupInfo.HitModel;
				info.GroundZ = worldZ;
				info.HitInstance = this;

				return true;
			}
		}

		return false;
	}

	public void SetUnloaded()
	{
		_iModel = null;
	}
}
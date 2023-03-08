// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Entities;

public class MovementInfo
{
	public struct TransportInfo
	{
		public void Reset()
		{
			Guid      = ObjectGuid.Empty;
			Pos       = new Position();
			Seat      = -1;
			Time      = 0;
			PrevTime  = 0;
			VehicleId = 0;
		}

		public ObjectGuid Guid;
		public Position Pos;
		public sbyte Seat;
		public uint Time;
		public uint PrevTime;
		public uint VehicleId;
	}

	public struct MovementInertia
	{
		public int Id;
		public Position Force;
		public uint Lifetime;
	}

	public struct JumpInfo
	{
		public void Reset()
		{
			FallTime = 0;
			Zspeed   = SinAngle = CosAngle = Xyspeed = 0.0f;
		}

		public uint FallTime;
		public float Zspeed;
		public float SinAngle;
		public float CosAngle;
		public float Xyspeed;
	}

	// advflying
	public struct AdvFlyingMovement
	{
		public float ForwardVelocity;
		public float UpVelocity;
	}

	public TransportInfo Transport;
	public MovementInertia? Inertia;
	public JumpInfo Jump;
	public AdvFlyingMovement? AdvFlying;
	MovementFlag _flags;
	MovementFlag2 _flags2;
	MovementFlags3 _flags3;

	public MovementInfo()
	{
		Guid    = ObjectGuid.Empty;
		_flags  = MovementFlag.None;
		_flags2 = MovementFlag2.None;
		Time    = 0;
		Pitch   = 0.0f;

		Pos = new Position();
		Transport.Reset();
		Jump.Reset();
	}

	public ObjectGuid Guid { get; set; }
	public Position Pos { get; set; }
	public uint Time { get; set; }
	public float Pitch { get; set; }
	public float StepUpStartElevation { get; set; }

	public MovementFlag GetMovementFlags()
	{
		return _flags;
	}

	public void SetMovementFlags(MovementFlag f)
	{
		_flags = f;
	}

	public void AddMovementFlag(MovementFlag f)
	{
		_flags |= f;
	}

	public void RemoveMovementFlag(MovementFlag f)
	{
		_flags &= ~f;
	}

	public bool HasMovementFlag(MovementFlag f)
	{
		return (_flags & f) != 0;
	}

	public MovementFlag2 GetMovementFlags2()
	{
		return _flags2;
	}

	public void SetMovementFlags2(MovementFlag2 f)
	{
		_flags2 = f;
	}

	public void AddMovementFlag2(MovementFlag2 f)
	{
		_flags2 |= f;
	}

	public void RemoveMovementFlag2(MovementFlag2 f)
	{
		_flags2 &= ~f;
	}

	public bool HasMovementFlag2(MovementFlag2 f)
	{
		return (_flags2 & f) != 0;
	}

	public MovementFlags3 GetExtraMovementFlags2()
	{
		return _flags3;
	}

	public void SetExtraMovementFlags2(MovementFlags3 flag)
	{
		_flags3 = flag;
	}

	public void AddExtraMovementFlag2(MovementFlags3 flag)
	{
		_flags3 |= flag;
	}

	public void RemoveExtraMovementFlag2(MovementFlags3 flag)
	{
		_flags3 &= ~flag;
	}

	public bool HasExtraMovementFlag2(MovementFlags3 flag)
	{
		return (_flags3 & flag) != 0;
	}

	public void SetFallTime(uint time)
	{
		Jump.FallTime = time;
	}

	public void ResetTransport()
	{
		Transport.Reset();
	}

	public void ResetJump()
	{
		Jump.Reset();
	}
}
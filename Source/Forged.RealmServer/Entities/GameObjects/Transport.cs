// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Forged.RealmServer.Maps;

namespace Forged.RealmServer.Entities.GameObjectType;

class Transport : GameObjectTypeBase, ITransport
{
	static readonly TimeSpan PositionUpdateInterval = TimeSpan.FromMilliseconds(50);
	readonly TransportAnimation _animationInfo;
	readonly List<uint> _stopFrames = new();
	readonly TimeTracker _positionUpdateTimer = new();
	readonly List<WorldObject> _passengers = new();
	uint _pathProgress;
	uint _stateChangeTime;
	uint _stateChangeProgress;
	bool _autoCycleBetweenStopFrames;

	public Transport(GameObject owner) : base(owner)
	{
		_animationInfo = Global.TransportMgr.GetTransportAnimInfo(owner.Template.entry);
		_pathProgress = _gameTime.GetGameTimeMS % GetTransportPeriod();
		_stateChangeTime = _gameTime.GetGameTimeMS;
		_stateChangeProgress = _pathProgress;

		var goInfo = Owner.Template;

		if (goInfo.Transport.Timeto2ndfloor > 0)
		{
			_stopFrames.Add(goInfo.Transport.Timeto2ndfloor);

			if (goInfo.Transport.Timeto3rdfloor > 0)
			{
				_stopFrames.Add(goInfo.Transport.Timeto3rdfloor);

				if (goInfo.Transport.Timeto4thfloor > 0)
				{
					_stopFrames.Add(goInfo.Transport.Timeto4thfloor);

					if (goInfo.Transport.Timeto5thfloor > 0)
					{
						_stopFrames.Add(goInfo.Transport.Timeto5thfloor);

						if (goInfo.Transport.Timeto6thfloor > 0)
						{
							_stopFrames.Add(goInfo.Transport.Timeto6thfloor);

							if (goInfo.Transport.Timeto7thfloor > 0)
							{
								_stopFrames.Add(goInfo.Transport.Timeto7thfloor);

								if (goInfo.Transport.Timeto8thfloor > 0)
								{
									_stopFrames.Add(goInfo.Transport.Timeto8thfloor);

									if (goInfo.Transport.Timeto9thfloor > 0)
									{
										_stopFrames.Add(goInfo.Transport.Timeto9thfloor);

										if (goInfo.Transport.Timeto10thfloor > 0)
											_stopFrames.Add(goInfo.Transport.Timeto10thfloor);
									}
								}
							}
						}
					}
				}
			}
		}

		if (!_stopFrames.Empty())
		{
			_pathProgress = 0;
			_stateChangeProgress = 0;
		}

		_positionUpdateTimer.Reset(PositionUpdateInterval);
	}

	public ObjectGuid GetTransportGUID()
	{
		return Owner.GUID;
	}

	public float GetTransportOrientation()
	{
		return Owner.Location.Orientation;
	}

	public void AddPassenger(WorldObject passenger)
	{
		if (!Owner.IsInWorld)
			return;

		if (!_passengers.Contains(passenger))
		{
			_passengers.Add(passenger);
			passenger.SetTransport(this);
			passenger.MovementInfo.Transport.Guid = GetTransportGUID();
			Log.outDebug(LogFilter.Transport, $"Object {passenger.GetName()} boarded transport {Owner.GetName()}.");
		}
	}

	public ITransport RemovePassenger(WorldObject passenger)
	{
		if (_passengers.Remove(passenger))
		{
			passenger.SetTransport(null);
			passenger.MovementInfo.Transport.Reset();
			Log.outDebug(LogFilter.Transport, $"Object {passenger.GetName()} removed from transport {Owner.GetName()}.");

			var plr = passenger.AsPlayer;

			if (plr != null)
				plr.SetFallInformation(0, plr.Location.Z);
		}

		return this;
	}

	public void CalculatePassengerPosition(Position pos)
	{
		ITransport.CalculatePassengerPosition(pos, Owner.Location.X, Owner.Location.Y, Owner.Location.Z, Owner.Location.Orientation);
	}

	public void CalculatePassengerOffset(Position pos)
	{
		ITransport.CalculatePassengerOffset(pos, Owner.Location.X, Owner.Location.Y, Owner.Location.Z, Owner.Location.Orientation);
	}

	public int GetMapIdForSpawning()
	{
		return Owner.Template.Transport.SpawnMap;
	}

	public override void Update(uint diff)
	{
		if (_animationInfo == null)
			return;

		_positionUpdateTimer.Update(diff);

		if (!_positionUpdateTimer.Passed)
			return;

		_positionUpdateTimer.Reset(PositionUpdateInterval);

		var now = _gameTime.GetGameTimeMS;
		var period = GetTransportPeriod();
		uint newProgress = 0;

		if (_stopFrames.Empty())
		{
			newProgress = now % period;
		}
		else
		{
			var stopTargetTime = 0;

			if (Owner.GoState == GameObjectState.TransportActive)
				stopTargetTime = 0;
			else
				stopTargetTime = (int)(_stopFrames[Owner.GoState - GameObjectState.TransportStopped]);

			if (now < Owner.GameObjectFieldData.Level)
			{
				var timeToStop = (int)(Owner.GameObjectFieldData.Level - _stateChangeTime);
				var stopSourcePathPct = (float)_stateChangeProgress / (float)period;
				var stopTargetPathPct = (float)stopTargetTime / (float)period;
				var timeSinceStopProgressPct = (float)(now - _stateChangeTime) / (float)timeToStop;

				float progressPct;

				if (!Owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
				{
					if (Owner.GoState == GameObjectState.TransportActive)
						stopTargetPathPct = 1.0f;

					var pathPctBetweenStops = stopTargetPathPct - stopSourcePathPct;

					if (pathPctBetweenStops < 0.0f)
						pathPctBetweenStops += 1.0f;

					progressPct = pathPctBetweenStops * timeSinceStopProgressPct + stopSourcePathPct;

					if (progressPct > 1.0f)
						progressPct = progressPct - 1.0f;
				}
				else
				{
					var pathPctBetweenStops = stopSourcePathPct - stopTargetPathPct;

					if (pathPctBetweenStops < 0.0f)
						pathPctBetweenStops += 1.0f;

					progressPct = stopSourcePathPct - pathPctBetweenStops * timeSinceStopProgressPct;

					if (progressPct < 0.0f)
						progressPct += 1.0f;
				}

				newProgress = (uint)((float)period * progressPct) % period;
			}
			else
			{
				newProgress = (uint)stopTargetTime;
			}

			if (newProgress == stopTargetTime && newProgress != _pathProgress)
			{
				uint eventId;

				switch (Owner.GoState - GameObjectState.TransportActive)
				{
					case 0:
						eventId = Owner.Template.Transport.Reached1stfloor;

						break;
					case 1:
						eventId = Owner.Template.Transport.Reached2ndfloor;

						break;
					case 2:
						eventId = Owner.Template.Transport.Reached3rdfloor;

						break;
					case 3:
						eventId = Owner.Template.Transport.Reached4thfloor;

						break;
					case 4:
						eventId = Owner.Template.Transport.Reached5thfloor;

						break;
					case 5:
						eventId = Owner.Template.Transport.Reached6thfloor;

						break;
					case 6:
						eventId = Owner.Template.Transport.Reached7thfloor;

						break;
					case 7:
						eventId = Owner.Template.Transport.Reached8thfloor;

						break;
					case 8:
						eventId = Owner.Template.Transport.Reached9thfloor;

						break;
					case 9:
						eventId = Owner.Template.Transport.Reached10thfloor;

						break;
					default:
						eventId = 0u;

						break;
				}

				if (eventId != 0)
					GameEvents.Trigger(eventId, Owner, null);

				if (_autoCycleBetweenStopFrames)
				{
					var currentState = Owner.GoState;
					GameObjectState newState;

					if (currentState == GameObjectState.TransportActive)
						newState = GameObjectState.TransportStopped;
					else if (currentState - GameObjectState.TransportActive == _stopFrames.Count)
						newState = currentState - 1;
					else if (Owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
						newState = currentState - 1;
					else
						newState = currentState + 1;

					Owner.SetGoState(newState);
				}
			}
		}

		if (_pathProgress == newProgress)
			return;

		_pathProgress = newProgress;

		var oldAnimation = _animationInfo.GetPrevAnimNode(newProgress);
		var newAnimation = _animationInfo.GetNextAnimNode(newProgress);

		if (oldAnimation != null && newAnimation != null)
		{
			var pathRotation = new Quaternion(Owner.GameObjectFieldData.ParentRotation.GetValue().X,
											Owner.GameObjectFieldData.ParentRotation.GetValue().Y,
											Owner.GameObjectFieldData.ParentRotation.GetValue().Z,
											Owner.GameObjectFieldData.ParentRotation.GetValue().W).ToMatrix();

			Vector3 prev = new(oldAnimation.Pos.X, oldAnimation.Pos.Y, oldAnimation.Pos.Z);
			Vector3 next = new(newAnimation.Pos.X, newAnimation.Pos.Y, newAnimation.Pos.Z);

			var dst = next;

			if (prev != next)
			{
				var animProgress = (float)(newProgress - oldAnimation.TimeIndex) / (float)(newAnimation.TimeIndex - oldAnimation.TimeIndex);

				dst = pathRotation.Multiply(Vector3.Lerp(prev, next, animProgress));
			}

			dst = pathRotation.Multiply(dst);
			dst += Owner.StationaryPosition1;

			Owner.Map.GameObjectRelocation(Owner, dst.X, dst.Y, dst.Z, Owner.Location.Orientation);
		}

		var oldRotation = _animationInfo.GetPrevAnimRotation(newProgress);
		var newRotation = _animationInfo.GetNextAnimRotation(newProgress);

		if (oldRotation != null && newRotation != null)
		{
			Quaternion prev = new(oldRotation.Rot[0], oldRotation.Rot[1], oldRotation.Rot[2], oldRotation.Rot[3]);
			Quaternion next = new(newRotation.Rot[0], newRotation.Rot[1], newRotation.Rot[2], newRotation.Rot[3]);

			var rotation = next;

			if (prev != next)
			{
				var animProgress = (float)(newProgress - oldRotation.TimeIndex) / (float)(newRotation.TimeIndex - oldRotation.TimeIndex);

				rotation = Quaternion.Lerp(prev, next, animProgress);
			}

			Owner.SetLocalRotation(rotation.X, rotation.Y, rotation.Z, rotation.W);
			Owner.UpdateModelPosition();
		}

		// update progress marker for client
		Owner.SetPathProgressForClient((float)_pathProgress / (float)period);
	}

	public override void OnStateChanged(GameObjectState oldState, GameObjectState newState)
	{
		if (_stopFrames.Empty())
		{
			if (newState != GameObjectState.TransportActive)
				Owner.SetGoState(GameObjectState.TransportActive);

			return;
		}

		uint stopPathProgress = 0;

		if (newState != GameObjectState.TransportActive)
		{
			var stopFrame = (int)(newState - GameObjectState.TransportStopped);
			stopPathProgress = _stopFrames[stopFrame];
		}

		_stateChangeTime = _gameTime.GetGameTimeMS;
		_stateChangeProgress = _pathProgress;
		var timeToStop = (uint)Math.Abs(_pathProgress - stopPathProgress);
		Owner.SetLevel(_gameTime.GetGameTimeMS + timeToStop);
		Owner.SetPathProgressForClient((float)_pathProgress / (float)GetTransportPeriod());

		if (oldState == GameObjectState.Active || oldState == newState)
		{
			// initialization
			if (_pathProgress > stopPathProgress)
				Owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
			else
				Owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);

			return;
		}

		var pauseTimesCount = _stopFrames.Count;
		var newToOldStateDelta = newState - oldState;

		if (newToOldStateDelta < 0)
			newToOldStateDelta += pauseTimesCount + 1;

		var oldToNewStateDelta = oldState - newState;

		if (oldToNewStateDelta < 0)
			oldToNewStateDelta += pauseTimesCount + 1;

		// this additional check is neccessary because client doesn't check dynamic flags on progress update
		// instead it multiplies progress from dynamicflags field by -1 and then compares that against 0
		// when calculating path progress while we simply check the flag if (!_owner.HasDynamicFlag(GO_DYNFLAG_LO_INVERTED_MOVEMENT))
		var isAtStartOfPath = _stateChangeProgress == 0;

		if (oldToNewStateDelta < newToOldStateDelta && !isAtStartOfPath)
			Owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
		else
			Owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
	}

	public override void OnRelocated()
	{
		UpdatePassengerPositions();
	}

	public void UpdatePassengerPositions()
	{
		foreach (var passenger in _passengers)
		{
			var pos = passenger.MovementInfo.Transport.Pos.Copy();
			CalculatePassengerPosition(pos);
			ITransport.UpdatePassengerPosition(this, Owner.Map, passenger, pos, true);
		}
	}

	public uint GetTransportPeriod()
	{
		if (_animationInfo != null)
			return _animationInfo.TotalTime;

		return 1;
	}

	public List<uint> GetPauseTimes()
	{
		return _stopFrames;
	}

	public void SetAutoCycleBetweenStopFrames(bool on)
	{
		_autoCycleBetweenStopFrames = on;
	}
}
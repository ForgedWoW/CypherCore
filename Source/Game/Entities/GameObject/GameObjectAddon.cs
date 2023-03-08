using System.Numerics;
using Framework.Constants;

namespace Game.Entities;

public class GameObjectAddon
{
	public Quaternion ParentRotation;
	public InvisibilityType invisibilityType;
	public uint invisibilityValue;
	public uint WorldEffectID;
	public uint AIAnimKitID;
}
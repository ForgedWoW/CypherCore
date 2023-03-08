using Framework.Constants;

namespace Game.Entities;

class GameObjectTypeBase
{
	protected readonly GameObject Owner;

	public GameObjectTypeBase(GameObject owner)
	{
		Owner = owner;
	}

	public virtual void Update(uint diff) { }
	public virtual void OnStateChanged(GameObjectState oldState, GameObjectState newState) { }
	public virtual void OnRelocated() { }

	public class CustomCommand
	{
		public virtual void Execute(GameObjectTypeBase type) { }
	}
}
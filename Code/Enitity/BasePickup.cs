using Sandbox;

public class BasePickup : Component, IInteractable 
{
	[Property] public string ItemId { get; set; } = "test_item";
	[Property] public int Amount { get; set; } = 1;

	public virtual void OnUse( GameObject player )
	{
		Log.Info( $"Игрок {player.Name} подобрал предмет {ItemId}!" );

		DestroyItemRpc();
	}

	[Rpc.Broadcast]
	protected void DestroyItemRpc()
	{
		if ( GameObject.IsValid() )
		{
			GameObject.Destroy();
		}
	}
}
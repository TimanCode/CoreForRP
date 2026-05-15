using Sandbox;
using System;

public class BasePickup : Component, IInteractable 
{
	[Property, Group("Основное")] public PrefabScene ItemPrefab { get; set; }
	[Property, Group("Основное")] public string DisplayName { get; set; } = "Предмет";
	
	[Property, Group("Инвентарь")] public int GridWidth { get; set; } = 1;
	[Property, Group("Инвентарь")] public int GridHeight { get; set; } = 1;
	[Property, Group("Инвентарь")] public string IconPath { get; set; } = "models/editor/axis_helper.vmdl_c"; 

	public void OnUse( GameObject player )
	{
		PickupRequestRpc( player );
	}

	[Rpc.Host]
	private void PickupRequestRpc( GameObject player )
	{
		if ( !GameObject.IsValid || player == null ) return;

		var inventory = player.Components.Get<PlayerInventory>();
		if ( inventory == null ) return;

		var itemToAdd = InventoryItemState.Create( ItemPrefab, IconPath, GridWidth, GridHeight );

		if ( inventory.TryAddItem( itemToAdd ) )
		{
			OnPickup( player );
			GameObject.Destroy(); 
		}
	}

	// Вызывается при подборе предмета с земли (сейчас пустой, чтобы броня не давалась сразу)
	protected virtual void OnPickup( GameObject player ) { }

	// НОВЫЙ МЕТОД: Вызывается при нажатии "Использовать" в инвентаре
	public virtual void OnConsume( GameObject player ) { }
}
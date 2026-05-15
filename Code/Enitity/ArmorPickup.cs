using Sandbox;

public sealed class ArmorPickup : BasePickup
{
	[Property] public int ArmorAmount { get; set; } = 100;
	[Property] public Clothing ArmorClothingItem { get; set; }

	public override void OnConsume( GameObject player )
	{
		var stats = player.Components.Get<PlayerStats>();
		if ( stats != null )
		{
			stats.AddArmor( ArmorAmount );

			if ( ArmorClothingItem != null )
			{
				// Передаем уникальный числовой ID ресурса через сеть
				stats.EquipArmorVisualRpc( ArmorClothingItem.ResourceId );
			}
		}
	}
}
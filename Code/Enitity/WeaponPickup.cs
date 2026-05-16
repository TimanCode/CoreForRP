using Sandbox;

public sealed class WeaponPickup : BasePickup
{
	// Сюда перетаскиваем префаб wp_mp5.prefab (где висит Mp5Weapon.cs)
	[Property, Group( "Weapon Settings" )] public PrefabScene WeaponPrefab { get; set; }

	public override void OnConsume( GameObject player )
	{
		if ( WeaponPrefab == null ) return;

		//var equipment = player.Components.Get<PlayerEquipment>();
		//if ( equipment != null )
		//{
		//	equipment.EquipWeaponRpc( WeaponPrefab );
		//}
	}
}
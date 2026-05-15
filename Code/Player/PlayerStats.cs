using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Citizen;

public sealed class PlayerStats : Component
{
	[Property, Sync, Group( "Health" )] public float MaxHealth { get; set; } = 100f;
	[Property, Sync, Group( "Health" )] public float Health { get; set; } = 100f;

	[Property, Sync, Group( "Armor" )] public float MaxArmor { get; set; } = 100f;
	[Property, Sync, Group( "Armor" )] public float Armor { get; set; } = 0f;

	[Property, Sync, Group( "Survival" )] public float Hunger { get; set; } = 100f;
	[Property, Sync, Group( "Survival" )] public float Thirst { get; set; } = 100f;

	private bool _isDead = false;

	public Action OnDeath { get; set; }

	protected override void OnUpdate()
	{
		if ( !IsProxy && Networking.IsHost )
		{
			TickSurvivalStats();
		}
	}

	private void TickSurvivalStats()
	{
		Hunger = (Hunger - 0.01f * Time.Delta).Clamp( 0, 100 );
		Thirst = (Thirst - 0.02f * Time.Delta).Clamp( 0, 100 );

		if ( Hunger <= 0 || Thirst <= 0 )
		{
			TakeDamage( 1f * Time.Delta );
		}
	}

	public void TakeDamage( float damage )
	{
		if ( IsProxy || _isDead ) return;

		if ( Armor > 0 )
		{
			float armorDamage = damage * 0.7f;
			float healthDamage = damage * 0.3f;

			if ( Armor >= armorDamage )
			{
				Armor -= armorDamage;
				Health -= healthDamage;
			}
			else
			{
				float leftover = armorDamage - Armor;
				Armor = 0;
				Health -= (healthDamage + leftover);
			}
		}
		else
		{
			Health -= damage;
		}

		Health = Health.Clamp( 0, MaxHealth );

		if ( Health <= 0 && !_isDead )
		{
			_isDead = true; 
			BroadcastDeath();
		}
	}

	public void AddArmor( float amount )
	{
		if ( IsProxy ) return;
		Armor = (Armor + amount).Clamp( 0, MaxArmor );
	}

	public void AddThirst( float amount )
	{
		// Прокси-объекты (клиенты) не должны сами менять свои статы напрямую,
		// это делает только хост/сервер.
		if ( IsProxy ) return;
		
		// Прибавляем значение и не даем ему подняться выше 100
		Thirst = (Thirst + amount).Clamp( 0, 100f );
	}

[Rpc.Broadcast]
public void EquipArmorVisualRpc( string clothingPath )
{
	var armorClothing = ResourceLibrary.Get<Clothing>( clothingPath );
	if ( armorClothing == null ) return;

	// Пытаемся найти хелпер анимации, который вы скинули
	var animHelper = Components.Get<CitizenAnimationHelper>( FindMode.EverythingInSelfAndDescendants );
	var playerRenderer = animHelper?.Target ?? Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
	
	if ( playerRenderer == null ) return;

	// --- Логика надевания одежды ---
	foreach ( var child in GameObject.Children.Where( x => x.Name == "VisualArmor" ).ToList() )
	{
		child.Destroy();
	}

	var armorObj = new GameObject( true, "VisualArmor" );
	armorObj.SetParent( GameObject ); 

	var armorRenderer = armorObj.Components.Create<SkinnedModelRenderer>();
	if ( !string.IsNullOrEmpty( armorClothing.Model ) )
	{
		armorRenderer.Model = Model.Load( armorClothing.Model );
		armorRenderer.BoneMergeTarget = playerRenderer;
	}

	// --- Логика анимации ---
	_ = PlayEquipAnimation( playerRenderer );
}

private async Task PlayEquipAnimation( SkinnedModelRenderer renderer )
{
	if ( !renderer.IsValid ) return;

	// Анимация @AvatarMenu_ExamineBody_04 в стандартном Citizen AnimGraph 
	// активируется через этот булевый параметр:
	renderer.Set( "b_admin_examine", true );

	// Ждем 1 секунду (как вы просили)
	await Task.DelaySeconds( 1.0f );

	// Возвращаем в обычное состояние
	if ( renderer.IsValid )
	{
		renderer.Set( "b_admin_examine", false );
	}
}

	[Rpc.Broadcast]
	private void BroadcastDeath()
	{
		OnDeath?.Invoke();
		
		if ( Networking.IsHost )
		{
			CreateRagdoll();
		}

		// 1. Выключаем все визуальные части
		var allRenderers = GameObject.Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var r in allRenderers )
		{
			r.Enabled = false;
		}

		// 2. ОТКЛЮЧАЕМ УПРАВЛЕНИЕ
		// Ищем ваш скрипт передвижения и выключаем его
		var movement = GameObject.Components.Get<PlayerMovementControl>();
		if ( movement != null ) 
		{
			movement.Enabled = false;
		}

		// 3. ОТКЛЮЧАЕМ КОЛЛИЗИИ
		// CharacterController продолжает работать, даже если модель скрыта.
		// Его нужно выключить, чтобы игрок перестал "существовать" физически.
		var controller = GameObject.Components.Get<CharacterController>();
		if ( controller != null )
		{
			controller.Enabled = false;
		}
	}

	private void CreateRagdoll()
	{
		// 1. Создаем пустой объект трупа
		var ragdollObj = new GameObject( true, $"Ragdoll_{GameObject.Name}" );
		ragdollObj.Transform.World = GameObject.Transform.World;

		// 2. Находим главный рендерер (тело игрока)
		var playerBodyRenderer = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelf );
		if ( playerBodyRenderer == null ) 
			playerBodyRenderer = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );

		// Создаем главный рендерер на регдолле
		var ragdollBodyRenderer = ragdollObj.Components.Create<SkinnedModelRenderer>();
		if ( playerBodyRenderer != null )
		{
			ragdollBodyRenderer.Model = playerBodyRenderer.Model;
			ragdollBodyRenderer.CopyFrom( playerBodyRenderer ); // Копируем позу и бодигруппы (дырки в теле)
		}

		// 3. Создаем физику
		var physics = ragdollObj.Components.Create<ModelPhysics>();
		physics.Renderer = ragdollBodyRenderer;
		physics.Enabled = true;

		// 4. ПЕРЕНОСИМ ВСЮ ОДЕЖДУ (и все дочерние меши)
		// Находим ВООБЩЕ ВСЕ рендереры на игроке (включая VisualArmor)
		var allPlayerRenderers = GameObject.Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );

		foreach ( var renderer in allPlayerRenderers )
		{
			// Пропускаем основное тело, мы его уже сделали
			if ( renderer == playerBodyRenderer ) continue;
			if ( renderer.Model == null ) continue;

			// Создаем копию этой части одежды для регдолла
			var clothingPart = new GameObject( true, "Ragdoll_Clothing_Part" );
			clothingPart.SetParent( ragdollObj );
			
			var clothingRenderer = clothingPart.Components.Create<SkinnedModelRenderer>();
			clothingRenderer.Model = renderer.Model;
			
			// ПРИВЯЗЫВАЕМ к физическому телу регдолла
			clothingRenderer.BoneMergeTarget = ragdollBodyRenderer;
		}

		ragdollObj.NetworkSpawn();
		_ = DeleteAfterDelay( ragdollObj, 30f );
	}

	private async Task DeleteAfterDelay( GameObject obj, float delay )
	{
		await Task.DelaySeconds( delay );
		if ( obj.IsValid ) obj.Destroy();
	}
}
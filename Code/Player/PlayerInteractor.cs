using Sandbox;

public sealed class PlayerInteractor : Component
{
	[Property] public float InteractDistance { get; set; } = 150f;

	public IInteractable HoveredInteractable { get; private set; }
	public GameObject HoveredObject { get; private set; }

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		UpdateHover();

		if ( Input.Pressed( "use" ) && HoveredInteractable != null )
		{
			HoveredInteractable.OnUse( GameObject );
		}
	}

	private void UpdateHover()
	{
		var startPos = Scene.Camera.WorldPosition;
		var endPos = startPos + Scene.Camera.WorldRotation.Forward * InteractDistance;

		var tr = Scene.Trace.Ray( startPos, endPos )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithTag( "interactable" )
			.Run();

		if ( tr.Hit && tr.GameObject.IsValid() )
		{
			var interactable = tr.GameObject.Components.GetInAncestorsOrSelf<IInteractable>();
			if ( interactable != null )
			{
				HoveredInteractable = interactable;
				HoveredObject = tr.GameObject;
				return;
			}
		}

		HoveredInteractable = null;
		HoveredObject = null;
	}
}
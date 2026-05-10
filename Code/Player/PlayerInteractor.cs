using Sandbox;

public sealed class PlayerInteractor : Component
{
	[Property] public float InteractDistance { get; set; } = 150f;

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( Input.Pressed( "use" ) )
		{
			PerformTrace();
		}
	}

	private void PerformTrace()
	{
		if ( Scene.Camera == null ) return;

		var startPos = Scene.Camera.WorldPosition;
		var endPos = startPos + Scene.Camera.WorldRotation.Forward * InteractDistance;

		var tr = Scene.Trace.Ray( startPos, endPos )
			.WithTag( "interactable" )
			.Run();

		if ( tr.Hit && tr.GameObject.IsValid() )
		{
			Log.Info("True");
			var interactable = tr.GameObject.Components.GetInAncestorsOrSelf<IInteractable>();
			
			if ( interactable != null )
			{
				interactable.OnUse( GameObject );
			}
		}
	}
}
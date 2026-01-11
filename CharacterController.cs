using Godot;

public partial class CharacterController : ColorRect
{
	[Export] public float Speed = 600f; // pixels per second

	public override void _Ready()
	{
		GD.Print("READY: CharacterController running");
	}

	public override void _Process(double delta)
	{
		// Build an input vector from actions
		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (dir.LengthSquared() > 1f)
			dir = dir.Normalized();

		Position += dir * Speed * (float)delta;
	}
}

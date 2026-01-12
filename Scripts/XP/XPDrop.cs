using Godot;

public partial class XPDrop : Node2D
{

	[Export] public PackedScene XpScene;

	public override void _ExitTree()
	{
		Vector2 spawnPos = GlobalPosition;
		Node parent = GetTree().CurrentScene;
		var xp = XpScene.Instantiate<Node2D>();
		
		parent.AddChild(xp);
		xp.GlobalPosition = spawnPos;
	}
}

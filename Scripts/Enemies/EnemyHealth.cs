using Godot;

public partial class EnemyHealth : Node
{
	[Export] public int MaxHp = 3;
	public int Hp { get; private set; }

	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		Hp = MaxHp;
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || Hp <= 0)
			return;

		Hp = Mathf.Max(0, Hp - amount);
		if (Hp == 0)
		{
			EmitSignal(SignalName.Died);
			EliminateOwner();
		}
	}

	public void Heal(int amount)
	{
		if (amount <= 0 || Hp <= 0)
			return;

		Hp = Mathf.Min(MaxHp, Hp + amount);
	}

	private void EliminateOwner()
	{
		Node parent = GetParent();
		if (parent != null)
		{
			parent.QueueFree();
			return;
		}

		QueueFree();
	}
}

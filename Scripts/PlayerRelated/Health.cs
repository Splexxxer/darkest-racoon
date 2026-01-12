using Godot;

public partial class Health : Node
{
	[Export] public int MaxHp = 5;
	public int Hp { get; private set; }

	[Export(PropertyHint.Range, "0,5,0.05")] public float InvulnSeconds = 0.4f;
	private bool _invulnerable;

	[Signal] public delegate void HpChangedEventHandler(int hp, int maxHp);
	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		Hp = MaxHp;
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || _invulnerable || Hp <= 0)
			return;

		Hp = Mathf.Max(0, Hp - amount);
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
		GD.Print($"Player HP: {Hp}/{MaxHp}");

		if (Hp == 0)
		{
			EmitSignal(SignalName.Died);
			GD.Print("Player died.");
			return;
		}

		StartInvulnerability();
	}

	public void Heal(int amount)
	{
		if (amount <= 0 || Hp <= 0)
			return;

		Hp = Mathf.Min(MaxHp, Hp + amount);
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
	}

	private async void StartInvulnerability()
	{
		_invulnerable = true;
		await ToSignal(GetTree().CreateTimer(InvulnSeconds), SceneTreeTimer.SignalName.Timeout);
		_invulnerable = false;
	}
}

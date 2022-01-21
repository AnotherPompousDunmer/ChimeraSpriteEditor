using Godot;
using System;

public class LayerCanvasTools : TextureRect
{

	Control DrawZoneRect;

	bool MouseInRect = false;

	public override bool HasPoint(Vector2 point)
	{		
		bool Out = Utils.VecHasPoint(DrawZoneRect.RectSize, point + GetParent<Control>().RectPosition);
		Out &= MouseInRect;
		//GD.Print(Out);

		return Out;
	}

	public override void _Ready()
	{
		DrawZoneRect = GetNode<Control>("../../");

		DrawZoneRect.Connect("mouse_entered", this, nameof(_MouseEntered));
		DrawZoneRect.Connect("mouse_exited", this, nameof(_MouseExited));
	}

	public void _MouseEntered()
	{
		MouseInRect = true;
	}
	public void _MouseExited()
	{
		MouseInRect = false;
	}

}

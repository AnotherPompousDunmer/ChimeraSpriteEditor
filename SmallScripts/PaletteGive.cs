using Godot;
using System;

public class PaletteGive : ColorRect
{

	bool Carrying = false;

	Line2D line;
	TextureRect lineRect;
	public override void _Ready()
	{
		line = GetNode<Line2D>("Viewport/Line2D");
		lineRect = GetNode<TextureRect>("LineRect");
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton)
		{
			if (Input.IsActionJustPressed("Mouse1") && GetRect().HasPoint(GetParent<Control>().GetLocalMousePosition()))
			{
				Carrying = true;
				AcceptEvent();
			}
			else if (Carrying == true && Input.IsActionJustReleased("Mouse1"))
			{
				Carrying = false;
				GetTree().CallGroup("PRecieve", "AttemptRecieve", Color);
				AcceptEvent();
			}
		}
	}

	public void SetColor(Color c, bool linePreview)
	{
		Color = c;

		lineRect.Visible = linePreview;
		if (linePreview)
		{
			Vector2[] points = new Vector2[2];
			points[0] = new Vector2(16, 16);
			points[1] = (new Vector2((c.r * 2) - 1, -((c.g * 2) - 1)) * 16) + points[0];
			line.Points = points;

			if (c.b < 0.5f)
			{
				line.DefaultColor = Colors.Crimson;
			}
			else
			{
				line.DefaultColor = Colors.Black;
			}
		}
	}

}

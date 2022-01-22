using Godot;
using System;

public class Palette : Control
{
    Color Primary = new Color(0, 0, 0, 1);
    Color Secondary = new Color(1, 1, 1, 1);

    PaletteGive PrimaryR;
    PaletteGive SecondaryR;

    PaletteControls pControls;

    public override void _Ready()
    {
        PrimaryR = GetNode<PaletteGive>("VBoxContainer2/P1");
        SecondaryR = GetNode<PaletteGive>("VBoxContainer2/P2");

        pControls = GetNode<PaletteControls>("Controls");

        SetColor(Primary, true);
        SetColor(Secondary, false);

        AddToGroup("Palette");  //Since I tend to forget, this is to do with drag and drop
    }

    public void SetColor(Color c, bool target = true, bool fixCursors = true)
    {
        if (target)
        {
            Primary = c;
            PrimaryR.SetColor(Primary, pControls.Mode == 2);

            pControls.SetColor(c, fixCursors);
        }
		else
		{
            Secondary = c;
            SecondaryR.Color = Secondary;
        }
    }

    public void NewColor(bool Pn, PaletteRecieve Rect)
	{
        if (Rect.Color != null)
            SetColor(Rect.Color, Pn);
    }

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("SwitchPalette") && !Input.IsActionPressed("Cut"))
		{
            Color S = Primary;
            SetColor(Secondary, true);
            SetColor(S, false);

            GetTree().CallGroup("Palette", "RemoveMark");
        }
	}

    public Color GetColor(bool Pn)
	{
        return Pn ? Primary : Secondary;
	}

}

using Godot;
using System;

public class PaletteRecieve : PaletteGive
{

    [Export]
    Texture Primary;
    [Export]
    Texture Secondary;

    TextureRect Cover;

    public override void _Ready()
    {
        AddToGroup("PRecieve");
        AddToGroup("Palette");
        Cover = GetNode<TextureRect>("TextureRect");
    }

    public void AttemptRecieve(Color c)
	{
        if (GetRect().HasPoint(GetParent<Control>().GetLocalMousePosition()))
		{
            Color = c;
        }
	}


    bool LClickedHere = false;
    bool RClickedHere = false;

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (@event is InputEventMouseButton && GetRect().HasPoint(GetParent<Control>().GetLocalMousePosition()))
        {
            if (Input.IsActionJustPressed("Mouse1")) LClickedHere = true;
            if (Input.IsActionJustPressed("Mouse2")) RClickedHere = true;

            if (Input.IsActionJustReleased("Mouse1") && LClickedHere)
			{
                SetSelected(0, true);
                GetTree().CallGroup("Palette", "NewColor", true, this);
                AcceptEvent();
                LClickedHere = false;
            }
            if (Input.IsActionJustReleased("Mouse2") && RClickedHere)
			{
                SetSelected(1, true);
                GetTree().CallGroup("Palette", "NewColor", false, this);
                //AcceptEvent();    //This line breaks the program - Fuck knows why.
                RClickedHere = false;
            }
        }
    }
	public override void _Process(float delta)
	{
		if ((LClickedHere || RClickedHere) && !GetRect().HasPoint(GetParent<Control>().GetLocalMousePosition()))
		{
            LClickedHere = false;
            RClickedHere = false;
		}
	}

	bool[] Selections = new bool[] { false, false };

    public void SetSelected(int target, bool result)
	{
        Selections[target] = result;

        if (!Selections[0] && !Selections[1])
		{
            Cover.Texture = null;
        }
        else if (Selections[0] && !Selections[1])
		{
            Cover.Texture = Primary;
        }
        else if (!Selections[0] && Selections[1])
        {
            Cover.Texture = Secondary;
        }
        else
		{
            Cover.Texture = Secondary;
        }
    }

    public void NewColor(bool Pn, PaletteRecieve Rect)
	{
        if (this != Rect)
		{
            if (Pn && Cover.Texture == Primary)
			{
                SetSelected(0, false);
            }
            if (!Pn && Cover.Texture == Secondary)
            {
                SetSelected(1, false);
            }
        }
	}

    public void RemoveMark()
	{
        SetSelected(0, false);
        SetSelected(1, false);
    }

}

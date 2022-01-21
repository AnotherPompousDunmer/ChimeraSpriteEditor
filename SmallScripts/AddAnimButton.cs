using Godot;
using System;

public class AddAnimButton : Button
{
	[Signal]
	delegate void AddAnim(string name);


    Panel NameInput;
	LineEdit LineEdit;

	public override void _Ready()
	{
		NameInput = GetNode<Panel>("NameInput");

		GetNode<Button>("NameInput/VBox/HBox/Cancel").Connect("pressed", this, nameof(ResetDialog));
		GetNode<Button>("NameInput/VBox/HBox/Accept").Connect("pressed", this, nameof(InputConfirm));

		LineEdit = GetNode<LineEdit>("NameInput/VBox/LineEdit");
		LineEdit.Connect("text_entered", this, nameof(InputConfirm));
	}

	public override void _Pressed()
	{
		NameInput.Visible = true;
	}

	public void InputConfirm() { InputConfirm(""); }	//Necessary to combat signal problems
	public void InputConfirm(string _ignore)
	{
		if (LineEdit.Text.Length > 0)
		{
			EmitSignal(nameof(AddAnim), new object[] { LineEdit.Text });
			ResetDialog();
		}
	}

	public void ResetDialog()
	{
		NameInput.Visible = false;
		LineEdit.Clear();
		LineEdit.ReleaseFocus();
	}

	public override void _Input(InputEvent @event)
	{
		if (NameInput.Visible && (@event.IsActionPressed("Mouse1") || @event.IsActionPressed("Mouse2")) && !NameInput.GetRect().HasPoint(GetLocalMousePosition()))
		{
			ResetDialog();
			AcceptEvent();
		}
	}

}

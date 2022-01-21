using Godot;
using System;
using System.Collections.Generic;

public class PaletteSaveLoad : HBoxContainer
{

    public override void _Ready()
    {
        GetNode<OptionButton>("Palettes").Connect("item_selected", this, nameof(LoadPalette));
        GetNode<Button>("SaveButton").Connect("pressed", this, nameof(OpenSaveDialog));
        GetNode<Button>("SaveButton/SaveDialog/VBox/HBox/OK").Connect("pressed", this, nameof(ProcessSaveDialogContents));
        GetNode<Button>("SaveButton/SaveDialog/VBox/HBox/Cancel").Connect("pressed", this, nameof(CloseSaveDialog));
        UpdateFromFolder();
    }

    public void LoadPalette(int id)
	{
        string path = "res://Palettes/" + GetNode<OptionButton>("Palettes").GetItemText(id) + ".png";
        File save = new File();
        save.Open(path, File.ModeFlags.Read);

        Godot.Collections.Array rects = GetNode<GridContainer>("../GridContainer").GetChildren();
        Image palette = new Image();
        palette.Load(path);

        palette.Lock();
        for (int i = 0; i < 40; i++)
        {
            (rects[i] as ColorRect).Color = palette.GetPixel(i%8, i/8);
        }
        palette.Unlock();
    }

    public void OpenSaveDialog()
	{
        GetNode<WindowDialog>("SaveButton/SaveDialog").Popup_();
    }

    public void CloseSaveDialog()
    {
        GetNode<Label>("SaveButton/SaveDialog/VBox/ErrorLabel").Text = "";
        GetNode<WindowDialog>("SaveButton/SaveDialog").Hide();
    }

    public void ProcessSaveDialogContents()
	{
        string field = GetNode<LineEdit>("SaveButton/SaveDialog/VBox/NameField").Text;

        File save = new File();

        if (field == "")
        {
            GetNode<Label>("SaveButton/SaveDialog/VBox/ErrorLabel").Text = "String is empty, please try again";
            return;
        }

        string path = "res://Palettes/" + field + ".png";
        if (save.FileExists(path))
		{
            GetNode<Label>("SaveButton/SaveDialog/VBox/ErrorLabel").Text = "File already exists, please try again";
            return;
        }

        Error err = save.Open(path, File.ModeFlags.Write);
        save.Close();
        
        if (err != Error.Ok)
		{
            GetNode<Label>("SaveButton/SaveDialog/VBox/ErrorLabel").Text = "Error: "+err.ToString();
            return;
        }

        Image palette = new Image();
        palette.Create(8, 5, false, Image.Format.Rgba8);

        Godot.Collections.Array rects = GetNode<GridContainer>("../GridContainer").GetChildren();

        palette.Lock();
        for (int i = 0; i < rects.Count; i++)
		{
            palette.SetPixel(i%8, i/8, (rects[i] as ColorRect).Color);
		}
        palette.Unlock();

        palette.SavePng(path);

        CloseSaveDialog();
        UpdateFromFolder();
    }

    public void UpdateFromFolder()
    {
        OptionButton listButton = GetNode<OptionButton>("Palettes");
        Directory paletteDir = new Directory();

        List<string> paletteNames = new List<string>();

        paletteDir.ChangeDir("res://Palettes");
        paletteDir.ListDirBegin();

        string name = paletteDir.GetNext();
        while (name != string.Empty)
		{
            if (name.Extension() == "png") paletteNames.Add(name.Substring(0, name.Length-4));
            name = paletteDir.GetNext();
        }

        int i = 0;
        while (listButton.GetItemCount() > 1)
		{
            listButton.RemoveItem(i);
            i++;
		}

        for (int n = 0; n < paletteNames.Count; n++)
		{
            listButton.AddItem(paletteNames[n], n);
		}
    }

}

using Godot;
using System;
using System.Collections.Generic;

public class ToolsPopUp : Panel
{

    GridContainer ButtonList;
    Tool[] Tools;
    Tool CurrentTool;

    TextureRect Underlay;
    TextureRect PCanvas;

    Layers Layers
    {
        get { 
            try
			{
                return GetNodeOrNull<Layers>("../Layers/VBox");
            }
			catch (InvalidCastException)
			{
                //No handling here
			}
            return null;
        }
    }

    Image Preview;
    ImageTexture PTex = new ImageTexture();

    Main Main;

    Palette Palette;

    public override void _Ready()
    {
        Main = GetNode<Main>("../../../../../");
        Tools = new Tool[] {new PencilTool(Main, this), new RubberTool(Main, this), new ColorPickerTool(Main, this), new LineTool(Main, this),
                            new ShapeTool(Main, this), new SelectTool(Main, this), new MagicWandTool(Main, this), new FillTool(Main, this),
                            new DitherTool(Main, this)};    //Add New Tools HERE

        PCanvas = GetNode<TextureRect>("../../../ImageContainer/UnderLay/Overlay");
        Underlay = GetNode<TextureRect>("../../../ImageContainer/UnderLay");

        ButtonList = GetNode<GridContainer>("ButtonList");

        //Layers = GetNode<Layers>("../Layers/VBox");

        Palette = GetNode<Palette>("../TabContainer/Palette/HBoxContainer");

        for (int i = 0; i < Tools.Length; i++)
        {
            Tools[i].PreparePreview(PCanvas);

            Button b = Tools[i].GetButton();
            ButtonList.AddChild(b);
            b.Connect("toggled", this, nameof(SetTool), new Godot.Collections.Array() { b , i });
            
            if (i == 0) b.Pressed = true;
        }

        LoadSettings();
    }

    public void CreatePreview()
	{
        Preview = new Image();
        Preview.Create((int)Main.Size.x, (int)Main.Size.y, false, Image.Format.Rgba8);
        Preview.Fill(new Color(1, 1, 1, 0));

        PTex.CreateFromImage(Preview, 0);

        PCanvas.Texture = PTex;
    }

    public void LoadSettings()
	{
        File settings = new File();

        if (settings.Open("res://EditorSettings/settings.json", File.ModeFlags.Read) == Error.Ok)
		{
            Dictionary<string, object> unprocessedDict = Main.GDictionaryConvert((Godot.Collections.Dictionary)JSON.Parse(settings.GetLine()).Result);
            Dictionary<string, Dictionary<string, float>> dict = new Dictionary<string, Dictionary<string, float>>();

            foreach (KeyValuePair<string, object> kvp in unprocessedDict)
			{
                if (kvp.Value is Dictionary<string, object>)
				{
                    Dictionary<string, float> contents = new Dictionary<string, float>();

                    foreach (KeyValuePair<string, object> prop in (Dictionary<string, object>)kvp.Value)
					{
                        if (prop.Value is float)
						{
                            contents.Add(prop.Key, (float)prop.Value);
						}
					}

                    dict.Add(kvp.Key, contents);
				}
			}

            foreach (Tool t in Tools)
			{
                t.LoadSettings(dict);
			}
		}
	}

    public void SaveSettings()
	{
        File settings = new File();

        if (settings.Open("res://EditorSettings/settings.json", File.ModeFlags.Write) == Error.Ok)
		{
            Dictionary<string, Dictionary<string, float>> dict = new Dictionary<string, Dictionary<string, float>>();

            foreach (Tool t in Tools)
			{
                t.SaveSettings(dict);
			}

            settings.StoreLine(JSON.Print(dict));
		}

    }

    public void DestroyOrphans()
	{
        foreach (Tool t in Tools)
		{
            t.DestroyOrphans();
            t.Free();
		}
	}

    public bool IsCurrentActive()
	{
        return CurrentTool.IsSafeForUndo();
	}

    public T setToolType<T>() where T: Tool
	{
        int i = -1;
        T outTool = null;

        for (i = 0; i < Tools.Length; i++)
		{
            if (Tools[i] is T)
			{
                outTool = Tools[i] as T;
                break;
			}
		}

        SetTool(true, ButtonList.GetChild<Button>(i), i);
        return outTool;
    }

    public void SetTool(Tool t)
	{
        int i = -1;

        for (i = 0; i < Tools.Length; i++)
        {
            if (Tools[i] == t)
            {
                break;
            }
        }

        SetTool(true, ButtonList.GetChild<Button>(i), i);
    }

    public void SetTool(bool _b, Button sender, int t)
	{
        foreach (Button b in ButtonList.GetChildren())
		{
            b.SetPressedNoSignal(b == sender);
		}
        if (CurrentTool != null && CurrentTool != Tools[t])
		{
            CurrentTool.OnToolSwitch(Layers.GetActive());
		}
        CurrentTool = Tools[t];

        PCanvas.Call("setMarchingAnts", CurrentTool.marchingAntsPreview);

        Control HelpBar = GetNode<Control>("../../../../Info");
        foreach (Control c in HelpBar.GetChildren())
		{
            HelpBar.RemoveChild(c);
		}
        HelpBar.AddChild(CurrentTool.GetHelpBar());
	}

    public void FinishFocusTool()
	{
        if (Layers != null)
		{
            CurrentTool.Finish(Layers.GetActive());
        }
	}

    Vector2 MouseMotionTracker = Vector2.Zero;
	public override void _Process(float delta)
	{
        if (CurrentTool.NeedsUpdate() || MouseMotionTracker != GetGlobalMousePosition()) UpdateTool();
        MouseMotionTracker = GetGlobalMousePosition();
	}

	public void UpdateTool()
	{
        BrushInfo BrushState = new BrushInfo(Palette);

        Vector2 MousePos = Underlay.GetLocalMousePosition();

        MousePos /= Underlay.RectSize;
        MousePos *= Main.Size;

        MousePos = MousePos.Floor();

        CurrentTool.Process(Layers.GetActive(), MousePos, BrushState);

        Preview.Fill(new Color(1, 1, 1, 0));
        Preview.Lock();

        CurrentTool.PreviewProcess(PCanvas, Preview, MousePos, BrushState);

        Preview.Unlock();
        PTex.CreateFromImage(Preview, 0);
    }

    public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey)
		{
            //HACK: because lineedit input is broken as per https://github.com/godotengine/godot/issues/15071
            if (GetFocusOwner() is LineEdit) return;
            if (Input.IsKeyPressed((int)KeyList.Control)) return;
            foreach (Tool t in Tools)
			{
                if ((uint)t.shortcut == (@event as InputEventKey).Scancode && t.IsSafeForUndo())
				{
                    SetTool(t);
				}
			}
		}
	}
}

public class BrushInfo
{

    public Color Albedo1
	{
        get
		{
            return palette.GetColor(true);
		}
        set
		{
            palette.SetColor(value, true);
		}
	}
    public Color Albedo2
    {
        get
        {
            return palette.GetColor(false);
        }
        set
        {
            palette.SetColor(value, false);
        }
    }

    Palette palette;

    public BrushInfo(Palette palette)
	{
        this.palette = palette;
    }

}
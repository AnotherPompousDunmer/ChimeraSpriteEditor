using Godot;
using System;
using System.Collections.Generic;

public class Tool : Godot.Object, IUndoable
{
    protected static PackedScene Button = GD.Load<PackedScene>("res://UiScenes/ToolButton.tscn");

    protected Main Main;
    protected ToolsPopUp parent;

    public virtual KeyList shortcut { get { return KeyList.A; } }

    public virtual bool marchingAntsPreview { get { return false; } }

    public Tool() { }
    public Tool(Main m, ToolsPopUp p)
	{
        Main = m;
        parent = p;
	}

    public virtual void OnToolSwitch(Layer focus) { }

    public virtual Button GetButton()
    {
        return null;
    }

    public virtual Control GetHelpBar()
	{
        return null;
	}

	public virtual bool Undo(Layer img) { return false; }

    public virtual void Process(Layer Layer, Vector2 pix, BrushInfo info) {}
    public virtual void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info) { }
    public virtual void PreparePreview(TextureRect overlay) { }

    public virtual void Finish(Layer layer) {}

    public virtual bool IsSafeForUndo() { return true; }

    public virtual void DestroyOrphans() { }

    public virtual void SaveSettings(Dictionary<string, Dictionary<string, float>> dict) { }
    public virtual void LoadSettings(Dictionary<string, Dictionary<string, float>> dict) { }

    public virtual bool NeedsUpdate() { return true; }

}
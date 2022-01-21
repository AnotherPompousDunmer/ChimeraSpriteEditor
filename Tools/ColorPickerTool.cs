using Godot;
using System;
using System.Collections.Generic;

public class ColorPickerTool : Tool
{
    public override KeyList shortcut { get { return KeyList.C; } }
	public override bool NeedsUpdate()
	{
		return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustPressed("Mouse2");
	}

	public ColorPickerTool() { }
    public ColorPickerTool(Main m, ToolsPopUp p) : base(m, p) { }

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "C";
        Output.HintTooltip = "(C)olor Picker";

        return Output;
    }

    Control Toolbar;
    Label posLabel;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
        {
            Toolbar = GD.Load<PackedScene>("res://Tools/ColorPickerToolbar.tscn").Instance<Control>();
            posLabel = Toolbar.GetNode<Label>("Pos");
        }
        return Toolbar;
    }
    public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

    public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
    {
        if (Input.IsActionJustPressed("Mouse1") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()))
        {
            Layer.Img.Lock();
            info.Albedo1 = Layer.Img.GetPixelv(pix);
            Layer.Img.Unlock();
        }
        if (Input.IsActionJustPressed("Mouse2") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()))
        {
            Layer.Img.Lock();
            info.Albedo2 = Layer.Img.GetPixelv(pix);
            Layer.Img.Unlock();
        }
    }

    public override void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info)
    {
        posLabel.Text = "" + pix;
    }
}

using Godot;
using System;
using System.Collections.Generic;

public class MagicWandTool : Tool
{
    public override KeyList shortcut { get { return KeyList.W; } }
    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustPressed("Mouse2");
    }

    public MagicWandTool() { }
    public MagicWandTool(Main m, ToolsPopUp p) : base(m, p) { }

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "G";
        Output.HintTooltip = "Magic (W)and";

        return Output;
    }

    Control Toolbar;
    Label posLabel;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
        {
            Toolbar = GD.Load<PackedScene>("res://Tools/MagicWandToolbar.tscn").Instance<Control>();
            Toolbar.GetNode<SpinBox>("Sensitivity").Connect("value_changed", this, nameof(SetSens));
            posLabel = Toolbar.GetNode<Label>("Pos");
        }
        return Toolbar;
    }
    public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

    float sensitivity = 0.01f;
    public void SetSens(float f)
    {
        sensitivity = f;
    }

    public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
    {
        //Contiguous fill if mouse1
        if (Input.IsActionJustPressed("Mouse1") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()))
        {
            List<Vector2> contigPix = new List<Vector2>();

            Layer.Img.Lock();
            ContiguousPixels(contigPix, Layer.Img, pix, Layer.Img.GetPixelv(pix), sensitivity);
            Layer.Img.Unlock();

            parent.setToolType<SelectTool>().SetSelection(contigPix, Layer);
        }

        //Whole-screen fill if mouse2
        if (Input.IsActionJustPressed("Mouse2") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()))
        {
            List<Vector2> contigPix = new List<Vector2>();


            Layer.Img.Lock();
            Color start = Layer.Img.GetPixelv(pix);
            for (int x = 0; x < (int)Layer.Img.GetSize().x; x++)
                for (int y = 0; y < (int)Layer.Img.GetSize().y; y++)
                {
                    if (ColorDifference(Layer.Img.GetPixel(x, y), start) < sensitivity)
                    {
                         contigPix.Add(new Vector2(x, y));
                    }
                }
            Layer.Img.Unlock();

            parent.setToolType<SelectTool>().SetSelection(contigPix, Layer);
        }
    }

	public override void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info)
	{
        posLabel.Text = "" + pix;
    }

	private static float ColorDifference(Color a, Color b)
    {
        float R = a.r - b.r;
        float G = a.g - b.g;
        float B = a.b - b.b;
        float A = a.a - b.a;

        float length = (R * R) + (G * G) + (B * B) + (A * A);

        return Mathf.Sqrt(length);
    }


    private static void ContiguousPixels(List<Vector2> list, Image i, Vector2 p, Color c, float sensitivity)
    {
        if (ColorDifference(i.GetPixelv(p), c) < sensitivity)
        {
            list.Add(p);
        }
        else
        {
            return;
        }

        Vector2[] neighbours = new Vector2[] { new Vector2(p.x + 1, p.y), new Vector2(p.x, p.y + 1), new Vector2(p.x - 1, p.y), new Vector2(p.x, p.y - 1) };

        foreach (Vector2 v in neighbours)
        {
            if (Utils.VecHasPoint(i.GetSize(), v) && !list.Contains(v))
            {
                ContiguousPixels(list, i, v, c, sensitivity);
            }
        }
    }

    const string key = "wand";
    public override void SaveSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (Toolbar == null) return;

        dict.Add(key, new Dictionary<string, float>());
        dict[key].Add("sensitivity", (float)Toolbar.GetNode<SpinBox>("Sensitivity").Value);
    }
    public override void LoadSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (!dict.ContainsKey(key)) return;

        float sens;
        if (dict[key].TryGetValue("sensitivity", out sens))
        {
            GetHelpBar();
            Toolbar.GetNode<SpinBox>("Sensitivity").Value = sens;
        }
    }
}

using Godot;
using System;
using System.Collections.Generic;

public class FillTool : Tool
{
    public override KeyList shortcut { get { return KeyList.F; } }
    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustPressed("Mouse2");
    }

    public FillTool() { }
    public FillTool(Main m, ToolsPopUp p) : base(m, p) { }

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "H";
        Output.HintTooltip = "(F)ill Tool";

        return Output;
    }

    List<PencilUndo> Undos = new List<PencilUndo>();


    Control Toolbar;
    Label posLabel;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
        {
            Toolbar = GD.Load<PackedScene>("res://Tools/FillToolbar.tscn").Instance<Control>();
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
            Undos.Insert(0, new PencilUndo() { FocusLayer = Layer, UndoState = new Dictionary<Vector2, Color>() });
            Layer.AddUndoable(this);
            Layer.Lock();

            List<Vector2> contigPix = new List<Vector2>();

            ContiguousPixels(contigPix, Layer.Img, pix, Layer.Img.GetPixelv(pix), sensitivity);

            foreach (Vector2 v in contigPix)
			{
                Undos[0].UndoState.Add(v, Layer.Img.GetPixelv(v));
                ReplacePixel(Layer.Img, v, info.Albedo1);
            }

            Layer.Unlock();
        }


        //Whole-screen fill if mouse2
        if (Input.IsActionJustPressed("Mouse2") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()))
        {
            Undos.Insert(0, new PencilUndo() { FocusLayer = Layer, UndoState = new Dictionary<Vector2, Color>() });
            Layer.AddUndoable(this);
            Layer.Lock();

            Color start = Layer.Img.GetPixelv(pix);

            for (int x = 0; x < (int)Layer.Img.GetSize().x; x++)
                for (int y = 0; y < (int)Layer.Img.GetSize().y; y++)
				{
                    if (ColorDifference(Layer.Img.GetPixel(x, y), start) < sensitivity)
					{
                        Undos[0].UndoState.Add(new Vector2(x, y), Layer.Img.GetPixel(x, y));
                        ReplacePixel(Layer.Img, new Vector2(x, y), info.Albedo1);
					}
				}

            Layer.Unlock();
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

        Vector2[] neighbours = new Vector2[] { new Vector2(p.x + 1, p.y), new Vector2(p.x, p.y + 1) , new Vector2(p.x - 1, p.y) , new Vector2(p.x, p.y - 1) };

        foreach (Vector2 v in neighbours)
		{
            if (Utils.VecHasPoint(i.GetSize(), v) && !list.Contains(v))
            {
                ContiguousPixels(list, i, v, c, sensitivity);
            }
        }
	}

    private void ReplacePixel(Image To, Vector2 pix, Color color)
    {
        if (Utils.VecHasPoint(To.GetSize(), pix))
        {
            Color Old = To.GetPixelv(pix);

            To.SetPixelv(pix, color);

            if (!Undos[0].UndoState.ContainsKey(pix))
                Undos[0].UndoState[pix] = Old;
        }
    }

    public override bool Undo(Layer target)
    {
        int LastAction = -1;
        for (int i = 0; i < Undos.Count; i++)
        {
            if (Undos[i].FocusLayer == target)
            {
                LastAction = i;
                break;
            }
        }
        if (LastAction == -1) return true;

        foreach (Vector2 v in Undos[LastAction].UndoState.Keys)
        {
            target.Img.SetPixelv(v, Undos[LastAction].UndoState[v]);
        }
        Undos.RemoveAt(LastAction);

        return true;
    }

    const string key = "fill";
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


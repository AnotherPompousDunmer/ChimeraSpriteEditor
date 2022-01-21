using Godot;
using System;
using System.Collections.Generic;

public class DitherTool : Tool
{
    public override KeyList shortcut { get { return KeyList.D; } }

    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustReleased("Mouse1");
    }

    public DitherTool() { }
    public DitherTool(Main m, ToolsPopUp p) : base(m, p) { }

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "I";
        Output.HintTooltip = "(D)ither Tool";

        return Output;
    }

    List<PencilUndo> Undos = new List<PencilUndo>();


    Control Toolbar;
    OptionButton intensity;
    Label posLabel;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
        {
            Toolbar = GD.Load<PackedScene>("res://Tools/DitherToolbar.tscn").Instance<Control>();
            intensity = Toolbar.GetNode<OptionButton>("Intensity");
            posLabel = Toolbar.GetNode<Label>("Pos");
        }
        return Toolbar;
    }
    public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

    bool Active = false;
    Vector2 lastPix = Vector2.Zero;

    public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
    {
        if (Input.IsActionJustPressed("Mouse1") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()) && !Active)
        {
            Active = true;
            lastPix = pix;
            Undos.Insert(0, new PencilUndo() { FocusLayer = Layer, UndoState = new Dictionary<Vector2, Color>() });
        }
        if (Input.IsActionJustReleased("Mouse1") && Active)
        {
            if (Undos[0].UndoState.Count > 0) Layer.AddUndoable(this);
            else Undos.RemoveAt(0);
            Active = false;
        }

        if (Active)
        {
            Layer.Lock();
            DitherDraw(Layer.Img, pix, info);
            Layer.Unlock();

            if ((lastPix - pix).Length() > 1.9f)
			{
                Layer.Lock();
                foreach (Vector2 v in Utils.GetBresenhamLine(lastPix, pix))
                {
                    DitherDraw(Layer.Img, v, info);
                }
                Layer.Unlock();
            }
        }
        lastPix = pix;
    }

    public override void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info)
    {
        posLabel.Text = "" + pix;
        OverlayImg.SetPixelv(Utils.ClampV(Vector2.Zero, pix, OverlayImg.GetSize() - Vector2.One), GetColor(pix, info));
    }

    private void DitherDraw(Image To, Vector2 pix, BrushInfo info)
    {
        if (Utils.VecHasPoint(To.GetSize(), pix))
        {
            Color Old = To.GetPixelv(pix);
            Color New = GetColor(pix, info);

            To.SetPixelv(pix, New);

            if (!Undos[0].UndoState.ContainsKey(pix))
                Undos[0].UndoState[pix] = Old;
        }
    }

    static Dictionary<int, ushort> ditherPatterns = new Dictionary<int, ushort>()
    {
        {0, 32768},
        {1, 32800},
        {2, 41120},
        {3, 42145},
        {4, 42405}
    };

    private Color GetColor(Vector2 pix, BrushInfo info)
	{
        int mode = intensity.Selected;
        bool flip = mode > 4;
        mode = 4-Utils.AbsI(mode - 4);

        ushort pos = (ushort)(((ushort)pix.x) % 4);
        pos = (ushort)(pos + ((((ushort)pix.y) % 4)*4));

        flip = ((ditherPatterns[mode] >> pos) & 1) == 1 == flip;
        
        return flip ? info.Albedo1 : info.Albedo2;
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

    const string key = "dither";
    public override void SaveSettings(Dictionary<string, Dictionary<string, float>> dict) 
    {
        if (Toolbar == null) return;

        dict.Add(key, new Dictionary<string, float>());
        dict[key].Add("amt_index", intensity.Selected);
    }
    public override void LoadSettings(Dictionary<string, Dictionary<string, float>> dict) 
    {
        if (!dict.ContainsKey(key)) return;

        float amountIndex;
        if (dict[key].TryGetValue("amt_index", out amountIndex))
		{
            GetHelpBar();
            intensity.Selected = (int)amountIndex;
		}
    }
}

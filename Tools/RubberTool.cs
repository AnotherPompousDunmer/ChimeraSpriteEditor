using Godot;
using System;
using System.Collections.Generic;

public class RubberTool : Tool
{
    public override KeyList shortcut { get { return KeyList.R; } }
    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustReleased("Mouse1");
    }

    public RubberTool() { }
    public RubberTool(Main m, ToolsPopUp p) : base(m, p) { }

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "B";
        Output.HintTooltip = "(R)ubber Tool";

        return Output;
    }

    List<PencilUndo> Undos = new List<PencilUndo>();


    Control Toolbar;
    Label posLabel;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
        {
            Toolbar = GD.Load<PackedScene>("res://Tools/RubberToolbar.tscn").Instance<Control>();
            posLabel = Toolbar.GetNode<Label>("Pos");
        }
        return Toolbar;
    }
    public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

    bool Active = false;

    public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
    {
        if (Input.IsActionJustPressed("Mouse1") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()) && !Active)
        {
            Active = true;
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
            Erase(Layer.Img, pix);
            Layer.Unlock();
        }
    }

    public override void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info)
    {
        posLabel.Text = "" + pix;
    }

    private void Erase(Image To, Vector2 pix)
    {
        if (Utils.VecHasPoint(To.GetSize(), pix))
        {
            Color Old = To.GetPixelv(pix);
            Color New = Old;
            New.a = 0;

            To.SetPixelv(pix, New);

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
}

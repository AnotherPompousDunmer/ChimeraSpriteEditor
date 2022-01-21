using Godot;
using System;
using System.Collections.Generic;

public class LineTool : Tool
{
    public override KeyList shortcut { get { return KeyList.L; } }
    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustReleased("Mouse2");
    }

    public LineTool() { }
    public LineTool(Main m, ToolsPopUp p) : base(m, p) {}

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "D";
        Output.HintTooltip = "(L)ine Tool";

        return Output;
    }

    bool curved = false;

    public void SetCurved(bool b)
	{
        curved = b;
	}

    List<PencilUndo> Undos = new List<PencilUndo>();


    Control Toolbar;
    PackedScene ToolbarScene = GD.Load<PackedScene>("res://Tools/LineToolbar.tscn");
    Label posLabel;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
        {
            Toolbar = ToolbarScene.Instance<Control>();
            Toolbar.GetNode<CheckButton>("Curved").Connect("toggled", this, nameof(SetCurved));
            posLabel = Toolbar.GetNode<Label>("Pos");
        }
        return Toolbar;
    }
    public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

    bool Active = false;
    List<Vector2> points = new List<Vector2>();

    public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
    {
        if (Active)
        {
            if (Input.IsActionJustPressed("Mouse1"))
            {
                points.Add(pix);
            }
        }

        if (Input.IsActionJustPressed("Mouse1") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()) && !Active)
        {
            Active = true;
            points.Add(pix);
            Undos.Insert(0, new PencilUndo() { FocusLayer = Layer, UndoState = new Dictionary<Vector2, Color>() });
        }
        if (Input.IsActionJustReleased("Mouse2") && Active)
        {
            points.Add(pix);

            Layer.Lock();
            DrawPoints(info.Albedo1, Layer.Img, points, false);
            Layer.Unlock();

            points.Clear();

            if (Undos[0].UndoState.Count > 0) Layer.AddUndoable(this);
            else Undos.RemoveAt(0);
            Active = false;
        }
    }

    public override void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info)
    {
        posLabel.Text = "" + pix;
        OverlayImg.SetPixelv(Utils.ClampV(Vector2.Zero, pix, OverlayImg.GetSize() - Vector2.One), info.Albedo1);
        if (Active)
		{
            List<Vector2> previewPoints = new List<Vector2>(points);
            previewPoints.Add(pix);
            DrawPoints(info.Albedo1, OverlayImg, previewPoints, true);
		}
    }

    private void DrawPoints(Color Add, Image To, List<Vector2> points, bool preview)
    {
        //List for the points
        HashSet<Vector2> fullLine = new HashSet<Vector2>();

        //Calculate the points
        switch (curved)
		{
            case false:
                for (int i = 0; i < points.Count - 1; i++)
                {
                    HashSet<Vector2> lineSegment = Utils.GetBresenhamLine(points[i], points[i + 1]);
                    fullLine.UnionWith(lineSegment);
                }
                break;
            case true:
                fullLine = Utils.GetCurvedLine(points);
                break;
        }
        
        //Draw the points
        foreach (Vector2 p in fullLine)
		{
            if (Utils.VecHasPoint(To.GetSize(), p))
            {
                Color Old = To.GetPixelv(p);
                Color Set;
                Set = Old;
                Set *= 1 - Add.a;
                Set += Add * Add.a;
                Set.a = Add.a + Old.a;

                To.SetPixelv(p, Set);

                if (!preview && !Undos[0].UndoState.ContainsKey(p))
                    Undos[0].UndoState[p] = Old;
            }
        }
    }

	public override bool IsSafeForUndo()
	{
		return !Active;
	}

	public override bool Undo(Layer target)
    {
        if (Active) return false;

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

    const string key = "line";
    public override void SaveSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (Toolbar == null) return;

        dict.Add(key, new Dictionary<string, float>());
        dict[key].Add("mode", Utils.b2i(Toolbar.GetNode<CheckButton>("Curved").Pressed));
    }
    public override void LoadSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (!dict.ContainsKey(key)) return;

        float sens;
        if (dict[key].TryGetValue("mode", out sens))
        {
            GetHelpBar();
            Toolbar.GetNode<CheckButton>("Curved").Pressed = sens > 0;
        }
    }
}

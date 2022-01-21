using Godot;
using System;
using System.Collections.Generic;

public class ShapeTool : Tool
{

    public ShapeTool() { }
    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1");
    }

    public ShapeTool(Main m, ToolsPopUp p) : base(m, p) { }
    public override KeyList shortcut { get { return KeyList.H; } }

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "E";
        Output.HintTooltip = "S(H)ape Tool";

        return Output;
    }

    List<PencilUndo> Undos = new List<PencilUndo>();


    Control Toolbar;
    Label posLabel;
    Label shapeInf;
    SpinBox thickness;
    OptionButton shape;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
        {
            Toolbar = GD.Load<PackedScene>("res://Tools/ShapeToolbar.tscn").Instance<Control>();
            posLabel = Toolbar.GetNode<Label>("Pos");
            shapeInf = Toolbar.GetNode<Label>("ShapeInf");
            shape = Toolbar.GetNode<OptionButton>("Shape");
            thickness = Toolbar.GetNode<SpinBox>("Thickness");
        }
        return Toolbar;
    }
	public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

	bool active = false;
    Vector2 startPoint;
    public enum Shape { RECT, OVAL};

    public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
    {
        if (Input.IsActionJustPressed("Mouse1") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()))
		{
            switch (active)
			{
                case false:
                    startPoint = pix;
                    active = true;
                    break;
                case true:
                    Layer.Lock();

                    Undos.Insert(0, new PencilUndo() { FocusLayer = Layer,
                        UndoState = DrawShape(Utils.MinV(pix, startPoint), Utils.MaxV(pix, startPoint), (Shape)shape.Selected, info, Layer.Img)});
                    Layer.AddUndoable(this);

                    Layer.Unlock();
                    active = false;
                    break;
			}
		}
    }

    public override void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info)
    {
        if (active)
		{
            Vector2 start = Utils.MinV(pix, startPoint);
            Vector2 end = Utils.MaxV(pix, startPoint);
            shapeInf.Text = "| From " + start + " to " + end + " | Size: " + (end-start) + "| ";
            DrawShape(start, end, (Shape)shape.Selected, info, OverlayImg);
        }
        else
		{
            shapeInf.Text = "| From (0, 0) to (0, 0) | Size: (0, 0) | ";
        }
        posLabel.Text = "" + pix;
    }

    private Dictionary<Vector2, Color> DrawShape(Vector2 start, Vector2 end, Shape s, BrushInfo inf, Image img)
	{
        Dictionary<Vector2, Color> undo = new Dictionary<Vector2, Color>();
        Rect2 bounds = new Rect2();
        bounds.Position = Utils.MaxV(start, Vector2.Zero);
        bounds.End = Utils.MinV(end, img.GetSize());
        switch (s)
		{
            case Shape.RECT:
                for (int x = (int)bounds.Position.x; x < bounds.End.x; x++) for (int y = (int)bounds.Position.y; y < bounds.End.y; y++)
					{
                        if (x - start.x < thickness.Value || end.x - x <= thickness.Value || y - start.y < thickness.Value || end.y - y <= thickness.Value)
						{
                            undo.Add(new Vector2(x, y), img.GetPixel(x, y));
                            img.SetPixel(x, y, inf.Albedo1);
						}
					}
                return undo;
            case Shape.OVAL:
                HashSet<Vector2> circlePoints = EllipsePoints((int)start.x, (int)start.y, (int)end.x-1, (int)end.y-1, (int)thickness.Value);
                foreach (Vector2 p in circlePoints)
				{
                    if (bounds.HasPoint(p))
					{
                        undo.Add(p, img.GetPixelv(p));
                        img.SetPixelv(p, inf.Albedo1);
                    }
                }
                return undo;
		}
        throw new Exception("Unknown Shape");
	}


    enum Quadrant {TL, TR, BL, BR}
    //Adapted from https://github.com/zingl/Bresenham
    private static HashSet<Vector2> EllipsePoints(int x0, int y0, int x1, int y1, int thickness)
    {
        HashSet<Vector2> points = new HashSet<Vector2>();

        int a = Utils.AbsI(x1 - x0), b = Utils.AbsI(y1 - y0), b1 = b & 1; /* values of diameter */
        long dx = 4 * (1 - a) * b * b, dy = 4 * (b1 + 1) * a * a; /* error increment */
        long err = dx + dy + b1 * a * a, e2; /* error of 1.step */

        if (x0 > x1) { x0 = x1; x1 += a; } /* if called with swapped points */
        if (y0 > y1) y0 = y1; /* .. exchange them */
        y0 += (b + 1) / 2; y1 = y0 - b1;   /* starting pixel */
        a *= 8 * a; b1 = 8 * b * b;

        do
        {
            AddToSetWithNeighbours(points, new Vector2(x1, y0), thickness, Quadrant.TL); /*   I. Quadrant */
            AddToSetWithNeighbours(points, new Vector2(x0, y0), thickness, Quadrant.TR); /*  II. Quadrant */
            AddToSetWithNeighbours(points, new Vector2(x0, y1), thickness, Quadrant.BR); /* III. Quadrant */
            AddToSetWithNeighbours(points, new Vector2(x1, y1), thickness, Quadrant.BL); /*  IV. Quadrant */
            e2 = 2 * err;
            if (e2 <= dy) { y0++; y1--; err += dy += a; }  /* y step */
            if (e2 >= dx || 2 * err > dy) { x0++; x1--; err += dx += b1; } /* x step */
        } while (x0 <= x1);

        while (y0 - y1 < b)
        {  /* too early stop of flat ellipses a=1 */
            AddToSetWithNeighbours(points, new Vector2(x0 - 1, y0), thickness, Quadrant.TR); /* -> finish tip of ellipse */
            AddToSetWithNeighbours(points, new Vector2(x1 + 1, y0++), thickness, Quadrant.TL);
            AddToSetWithNeighbours(points, new Vector2(x0 - 1, y1), thickness, Quadrant.BR);
            AddToSetWithNeighbours(points, new Vector2(x1 + 1, y1--), thickness, Quadrant.BL);
        }

        return points;
    }

    static Vector2[] quadVecs = new Vector2[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) };

    private static void AddToSetWithNeighbours(HashSet<Vector2> set, Vector2 pos, int thickness, Quadrant q)
	{
        for (int i = 0; i < thickness; i++)
		{
            switch (q)
			{
                case Quadrant.TR:
                    set.Add(pos + i * quadVecs[0]);
                    set.Add(pos + i * quadVecs[3]);
                    break;
                case Quadrant.BR:
                    set.Add(pos + i * quadVecs[0]);
                    set.Add(pos + i * quadVecs[2]);
                    break;
                case Quadrant.BL:
                    set.Add(pos + i * quadVecs[1]);
                    set.Add(pos + i * quadVecs[2]);
                    break;
                case Quadrant.TL:
                    set.Add(pos + i * quadVecs[1]);
                    set.Add(pos + i * quadVecs[3]);
                    break;
            }
        }
	}

    public override bool IsSafeForUndo()
    {
        return !active;
    }

    public override bool Undo(Layer target)
    {
        if (active) return false;

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

    const string key = "shape";
    public override void SaveSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (Toolbar == null) return;

        dict.Add(key, new Dictionary<string, float>());
        dict[key].Add("shape", shape.Selected);
        dict[key].Add("thickness", (float)thickness.Value);
    }
    public override void LoadSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (!dict.ContainsKey(key)) return;

		GetHelpBar();
		if (dict[key].TryGetValue("shape", out float shape))
        {
            this.shape.Selected = (int)shape;
        }
        if (dict[key].TryGetValue("thickness", out float thcknss))
        {
            thickness.Value = thcknss;
        }
    }
}

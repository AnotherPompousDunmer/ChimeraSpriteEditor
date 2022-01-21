using Godot;
using System;
using System.Collections.Generic;

public class PencilTool : Tool
{
    public override KeyList shortcut { get { return KeyList.P; } }
    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustReleased("Mouse1") || Input.IsActionJustPressed("Mouse2");
    }

    public PencilTool() { }
    public PencilTool(Main m, ToolsPopUp p) : base(m, p) { }

    bool Rounded = true;
    Vector2 LastPix = RoundOffVec;
    Vector2 LastDrawPoint = RoundOffVec;

    List<PencilUndo> Undos = new List<PencilUndo>();

    public void SetRawInput(bool b)
	{
        Rounded = !b;
	}

    static Vector2 RoundOffVec = new Vector2(-1, -1);

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "A";
        Output.HintTooltip = "(P)encil";

        return Output;
    }

    Control Toolbar;
    Label posLabel;
	public override Control GetHelpBar()
	{
		if (Toolbar is null)
		{
            Toolbar = GD.Load<PackedScene>("res://Tools/PencilToolbar.tscn").Instance<Control>();
            Toolbar.GetNode<CheckButton>("RawInput").Connect("toggled", this, nameof(SetRawInput));
            posLabel = Toolbar.GetNode<Label>("Pos");
        }
        return Toolbar;
	}
    public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

    bool Active = false;
	public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
	{
        if (Input.IsActionJustPressed("Mouse1") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize/Layer.Img.GetSize()))
        {
            Active = true;
            Undos.Insert(0, new PencilUndo() { FocusLayer = Layer, UndoState = new Dictionary<Vector2, Color>()});
            LastDrawPoint = RoundOffVec;
        }
        if (Input.IsActionJustReleased("Mouse1") && Active)
        {
            if (Undos[0].UndoState.Count > 0) Layer.AddUndoable(this);
            else Undos.RemoveAt(0);
            LastPix = RoundOffVec;
            Active = false;
        }
        
        if (Active)
        {
            Layer.Lock();

            if (Input.IsActionJustPressed("Mouse1"))
			{
                Draw(info.Albedo1, Layer.Img, pix);
            }

            if (Rounded)
            {
                if (LastPix == RoundOffVec)
                {
                    LastPix = pix;
                }
                if (LastDrawPoint == RoundOffVec)
				{
                    LastDrawPoint = pix;
				}

                if ((pix - LastDrawPoint).Length() < 1.9f)   //Comparitive Number is more than root 2 but less than 2
                {
                    LastPix = pix;
                }
                else if ((pix - LastDrawPoint).Length() < 2.1f && (pix - LastPix).Length() < 1.9f) //More than 2, less than root 5  //Also we have to check that LastPix was Actually Updated
                {
                    Draw(info.Albedo1, Layer.Img, LastPix);
                }
                else
				{
                    HashSet<Vector2> bresLine = Utils.GetBresenhamLine(LastPix, pix);
                    
                    if (bresLine.Contains(LastDrawPoint))
                    {
                        bresLine.Remove(LastDrawPoint);
                    }

                    //string tmp = "";

                    foreach (Vector2 v in bresLine)
					{
                        Draw(info.Albedo1, Layer.Img, v);
                        //tmp +=  v + ", ";
                    }
                    //GD.Print(tmp);
                    LastPix = LastDrawPoint;
                }
            }
            else
            {
                Draw(info.Albedo1, Layer.Img, pix);
            }

            Layer.Unlock();
        }

        if (!Active && Input.IsActionJustPressed("Mouse2") && Layer.Canvas.HasPoint(pix * Layer.Canvas.RectSize / Layer.Img.GetSize()))
        {
            Layer.Img.Lock();
            info.Albedo1 = Layer.Img.GetPixelv(pix);
            Layer.Img.Unlock();
        }
    }

	public override void PreviewProcess(TextureRect Overlay, Image OverlayImg, Vector2 pix, BrushInfo info)
	{
        posLabel.Text = ""+pix;
        OverlayImg.SetPixelv(Utils.ClampV(Vector2.Zero, pix, OverlayImg.GetSize() - Vector2.One), info.Albedo1);
    }

	private void Draw(Color Add, Image To, Vector2 Loc)
	{
        if (Utils.VecHasPoint(To.GetSize(), Loc))
        {
            Color Old = To.GetPixelv(Loc);
            Color Set;
            Set = Old;
            Set *= 1 - Add.a;
            Set += Add * Add.a;
            Set.a = Add.a + Old.a;

            To.SetPixelv(Loc, Set);

            if (!Undos[0].UndoState.ContainsKey(Loc))
                Undos[0].UndoState[Loc] = Old;
        }

        LastDrawPoint = Loc;
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

    const string key = "pencil";
    public override void SaveSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (Toolbar == null) return;

        dict.Add(key, new Dictionary<string, float>());
        dict[key].Add("mode", Utils.b2i(Toolbar.GetNode<CheckButton>("RawInput").Pressed));
    }
    public override void LoadSettings(Dictionary<string, Dictionary<string, float>> dict)
    {
        if (!dict.ContainsKey(key)) return;

        float mode;
        if (dict[key].TryGetValue("mode", out mode))
        {
            GetHelpBar();
            Toolbar.GetNode<CheckButton>("RawInput").Pressed = mode > 0;
        }
    }

}

struct PencilUndo
{
    public Layer FocusLayer;
    public Dictionary<Vector2, Color> UndoState;
}
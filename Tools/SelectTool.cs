using Godot;
using System;
using System.Collections.Generic;

public class SelectTool : Tool
{
    public override bool marchingAntsPreview { get { return true; } }
    public override KeyList shortcut { get { return KeyList.S; } }
    public override bool NeedsUpdate()
    {
        return Input.IsActionJustPressed("Mouse1") || Input.IsActionJustReleased("Mouse1") || Input.IsActionJustPressed("Mouse2")
            || Input.IsActionJustPressed("Copy") || Input.IsActionJustPressed("Cut") || Input.IsActionJustPressed("Paste") || Input.IsActionJustPressed("Delete")
            || Input.IsActionPressed("ui_up") || Input.IsActionPressed("ui_down") || Input.IsActionPressed("ui_left") || Input.IsActionPressed("ui_right");
    }

    static readonly Color Alpha = new Color(1, 1, 1, 0);

    public SelectTool() { }
    public SelectTool(Main m, ToolsPopUp p) : base(m, p) { }

    public override Button GetButton()
    {
        Button Output = Button.Instance<Button>();

        Output.Text = "F";
        Output.HintTooltip = "(S)election Tool";

        return Output;
    }

    Dictionary<Layer, Stack<SelectUndo>> Undos = new Dictionary<Layer, Stack<SelectUndo>>();
    private void PushToUndo(SelectUndo undo, Layer target)
	{
        if (!Undos.ContainsKey(target))
		{
            Undos.Add(target, new Stack<SelectUndo>());
		}
        
        if (undo.oldContents != null) undo.oldContents = CopyOf(undo.oldContents);
        if (undo.SelectContents != null) undo.SelectContents = CopyOf(undo.SelectContents);

        Undos[target].Push(undo);
        target.AddUndoable(this);
	}
    private Image CopyOf(Image img)
	{
        Image copy = new Image();
        copy.CopyFrom(img);
        return copy;
	}

    Control Toolbar;
    Label posLabel;
    Label selectInf;
    public override Control GetHelpBar()
    {
        if (Toolbar is null)
		{
            Toolbar = GD.Load<PackedScene>("res://Tools/SelectToolbar.tscn").Instance<Control>();
            posLabel = Toolbar.GetNode<Label>("Pos");
            selectInf = Toolbar.GetNode<Label>("SelectInf");
        }
        return Toolbar;
    }
    public override void DestroyOrphans() { if (Toolbar != null) Toolbar.QueueFree(); }

    enum SelectState { NONE, DRAWING, ADD_DRAW, RESIZING, STRETCHING, STATIC, ROTATING}
    SelectState state = SelectState.NONE;

    //NONE
    Vector2 RectStart;

    //STATIC
    Vector2? dragMouseCoords = null;
    string inputAxis = "";
    float inputTime = -1;
    static readonly Dictionary<string, Vector2> axisToVec = new Dictionary<string, Vector2>() {
                                                                                                { "ui_up", new Vector2(0, -1) },
                                                                                                { "ui_right", new Vector2(1, 0) },
                                                                                                { "ui_down", new Vector2(0, 1) },
                                                                                                { "ui_left", new Vector2(-1, 0) }
                                                                                              };

    //RESIZING
    Vector2 ResizeCorner;

    //STRETCHING
    Vector2 StretchCorner;
    Image StretchImage = new Image();

    //ADD_DRAW
    Vector2 addRectStart;
    Image dataCopy = new Image();

    //ROTATING
    Vector2 startPix;
    Image rotDataCopy = new Image();

    //MULTI
    Rect2 Selection = new Rect2(Vector2.Zero, Vector2.Zero);
    Image SelectionData = new Image();

    public override void Process(Layer Layer, Vector2 pix, BrushInfo info)
    {
        switch (state)
        {
            case SelectState.NONE:
                if (Input.IsActionPressed("Mouse1") && Layer.Canvas.HasPoint(pix * getLayerPPP(Layer)))
                {
                    RectStart = pix;
                    //GD.Print("here");
                    state = SelectState.DRAWING;
                    Selection = new Rect2(pix, Vector2.Zero);

                    Vector2 Ppp = getLayerPPP(Layer);
                    Box.MakeRectSelection(new Rect2(Selection.Position * Ppp, Vector2.Zero));
                }
                if (Input.IsActionPressed("Paste"))
				{
                    ParseClipboard();
                    state = SelectState.STATIC;

                    Vector2 Ppp = getLayerPPP(Layer);
                    Box.MakeRectSelection(new Rect2(Selection.Position * Ppp, Selection.Size * Ppp));

                    PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.PASTE}, Layer);
                }
                break;
            case SelectState.DRAWING:
                if (Layer.Canvas.HasPoint(pix * getLayerPPP(Layer)))
                {
                    if (Input.IsActionPressed("Mouse1"))
                    {
                        var corners = Utils.GetRectCorners(Selection);

                        Selection = new Rect2(Utils.MinV(pix, RectStart), Utils.MaxV(pix, RectStart) - Utils.MinV(pix, RectStart));

                        Vector2 Ppp = getLayerPPP(Layer);
                        Box.MakeRectSelection(new Rect2(Selection.Position * Ppp, Selection.Size * Ppp));

                        selectInf.Text = "| From " + Selection.Position + " to " + Selection.End + " | Size: " + Selection.Size;
                    }
                }
                if (Input.IsActionJustReleased("Mouse1"))
                {
                    FinalizeSelection(Layer);
                }
                break;
            case SelectState.RESIZING:
                if (Input.IsActionPressed("Mouse1"))
				{
                    Selection = new Rect2(Utils.MinV(pix, ResizeCorner), Utils.MaxV(pix, ResizeCorner) - Utils.MinV(pix, ResizeCorner));
                    Vector2 Ppp = getLayerPPP(Layer);
                    Box.MakeRectSelection(new Rect2(Selection.Position * Ppp, Selection.Size * Ppp));
                    selectInf.Text = "| From " + Selection.Position + " to " + Selection.End + " | Size: " + Selection.Size;
                }
                else
				{
                    FinalizeSelection(Layer);
                }
                break;
            case SelectState.STRETCHING:
                Selection = new Rect2(Utils.MinV(pix, StretchCorner), Utils.MaxV(pix, StretchCorner) - Utils.MinV(pix, StretchCorner));
                StretchImage.Create((int)Selection.Size.x, (int)Selection.Size.x, true, Image.Format.Rgba8);
                StretchImage.Lock();
                SelectionData.Lock();
                for (int x = 0; x < StretchImage.GetWidth(); x++) for (int y = 0; y < StretchImage.GetHeight(); y++)
					{
                        Vector2 pos = new Vector2(x, y) / StretchImage.GetSize();
                        pos *= SelectionData.GetSize();
                        pos = Utils.FloorV(pos);
                        StretchImage.SetPixel(x, y, SelectionData.GetPixelv(pos));
					}
                StretchImage.Unlock();
                SelectionData.Unlock();

                Vector2 StretchPpp = getLayerPPP(Layer);
                Box.MakeRectSelection(new Rect2(Selection.Position * StretchPpp, Selection.Size * StretchPpp));

                selectInf.Text = "| From " + Selection.Position + " to " + Selection.End + " | Size: " + Selection.Size;

                if (!Input.IsActionPressed("Mouse2"))
                {
                    SelectionData.CopyFrom(StretchImage);
                    state = SelectState.STATIC;
                }
                break;
            case SelectState.ROTATING:
                int buttonMask = Input.IsActionPressed("Mouse1") ? 1 : 0;
                buttonMask += Input.IsActionPressed("Mouse2") ? 2 : 0;
                
                //Maths
                if (buttonMask > 0)
				{
                    float angle = (pix - startPix).Angle() + (Mathf.Pi * 0.5f);
                    angle = Mathf.Stepify(angle, Mathf.Pi / (buttonMask > 1 ? 4f : 20f));    //Quantize

                    Vector2 oSize = SelectionData.GetSize();
                    Vector2 Size = Vector2.Zero;
                    Size.x = Mathf.Abs(oSize.y * Mathf.Sin(angle)) + Mathf.Abs(oSize.x * Mathf.Cos(angle));
                    Size.y = Mathf.Abs(oSize.x * Mathf.Sin(angle)) + Mathf.Abs(oSize.y * Mathf.Cos(angle));
                    Size = Utils.FloorV(Size);
                    rotDataCopy.Create((int)Size.x, (int)Size.y, false, Image.Format.Rgba8);

                    SelectionData.Lock();
                    rotDataCopy.Lock();
                    
                    for (int y = 0; y < rotDataCopy.GetHeight(); y++) for (int x = 0; x < rotDataCopy.GetWidth(); x++)
                        {
                            //TODO: rotation algorithm not quite perfect
                            Vector2 fromCenter = new Vector2(x + 0.5f, y + 0.5f) - (Size / 2f);
                            fromCenter = fromCenter.Rotated(-angle);
                            fromCenter += (oSize) / new Vector2(2f, 2f);
                            fromCenter = Utils.FloorV(fromCenter);
                            if (Utils.VecHasPoint(oSize, fromCenter))
                            {
                                rotDataCopy.SetPixel(x, y, SelectionData.GetPixelv(fromCenter));
                            }
                        }
                    SelectionData.Unlock();
                    rotDataCopy.Unlock();
                    Selection = new Rect2(Utils.RoundV(startPix - (rotDataCopy.GetSize() / 2f)), rotDataCopy.GetSize());
                }
                
                Vector2 rotPpp = getLayerPPP(Layer);
                Box.MakeRectSelection(new Rect2(Selection.Position * rotPpp, Selection.Size * rotPpp));

                selectInf.Text = "| From " + Selection.Position + " to " + Selection.End + " | Size: " + Selection.Size;

                if (buttonMask < 1)   //close
				{
                    SelectionData.CopyFrom(rotDataCopy);
                    state = SelectState.STATIC;
                }
                break;
            case SelectState.STATIC:
                Vector2 dragInf = Vector2.Zero;

                if (Box.GetDragging() != SelectionBox.DragDir.NONE) //Handle input on selection handles
                {
                    if (Input.IsActionPressed("Mouse1"))
                    {
                        state = SelectState.RESIZING;
                        ResizeCorner = Utils.GetRectCorners(Selection)[((int)Box.GetDragging() + 1) % 4];

                        PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.DRAW, OldPos = Selection, oldContents = Layer.Img.GetRect(Selection), SelectContents = SelectionData }, Layer);

                        Layer.Lock();
                        Layer.Img.BlendRect(SelectionData, new Rect2(Vector2.Zero, SelectionData.GetSize()), Selection.Position);
                        Layer.Unlock();
                    }
                    else if (Input.IsActionPressed("Mouse2"))
                    {
                        StretchCorner = Utils.GetRectCorners(Selection)[((int)Box.GetDragging() + 1) % 4];

                        PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.STRETCH, SelectContents = SelectionData, OldPos = Selection }, Layer);
                        state = SelectState.STRETCHING;
                    }
                }
                if (Box.GetRotating())
				{
                    startPix = Selection.Position + (Selection.Size/2f);
                    state = SelectState.ROTATING;
                    PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.ROTATE, SelectContents = SelectionData, OldPos = Selection }, Layer);
                }
                if (Layer.Canvas.HasPoint(pix * getLayerPPP(Layer)))
                {
                    if (Input.IsActionJustPressed("Mouse1") && !Selection.HasPoint(pix))   //Disable selection if left click outside box
                    {
                        state = SelectState.NONE;

                        PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.DRAW, OldPos = Selection, oldContents = Layer.Img.GetRect(Selection), SelectContents = SelectionData }, Layer);

                        Layer.Lock();
                        Layer.Img.BlendRect(SelectionData, new Rect2(Vector2.Zero, SelectionData.GetSize()), Selection.Position);
                        Layer.Unlock();
                    }
                    else if (Input.IsActionJustPressed("Mouse2"))   //Else move to additive draw
					{
                        addRectStart = pix;
                        state = SelectState.ADD_DRAW;
					}
                }

                if (Input.IsActionJustPressed("Delete"))
				{
                    state = SelectState.NONE;

                    PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.DELETE, OldPos = Selection, SelectContents = SelectionData}, Layer);
                }
                if (Input.IsActionJustPressed("Cut"))
				{
                    OS.Clipboard = GetSaveJSON(SelectionData);

                    state = SelectState.NONE;

                    PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.DELETE, OldPos = Selection, SelectContents = SelectionData }, Layer);
                }
                if (Input.IsActionJustPressed("Copy"))
				{
                    OS.Clipboard = GetSaveJSON(SelectionData);
                }
                if (Input.IsActionPressed("Paste"))
                {
                    //Finish the selection
                    PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.DRAW, OldPos = Selection, oldContents = Layer.Img.GetRect(Selection), SelectContents = SelectionData }, Layer);
                    Layer.Lock();
                    Layer.Img.BlendRect(SelectionData, new Rect2(Vector2.Zero, SelectionData.GetSize()), Selection.Position);
                    Layer.Unlock();
                    //Set The new Data
                    ParseClipboard();
                    PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.PASTE }, Layer);
                    //Fix Visual Box
                    Vector2 Ppp = getLayerPPP(Layer);
                    Box.MakeRectSelection(new Rect2(Selection.Position * Ppp, Selection.Size * Ppp));
                }

                //All this handles selection movement
                if (state == SelectState.STATIC && Input.IsActionPressed("Mouse1") && Layer.Canvas.HasPoint(pix * getLayerPPP(Layer)))//Add to selection Drag
                {
                    if (dragMouseCoords != null)
                    {
                        dragInf = pix - (Vector2)dragMouseCoords;
                        inputAxis = "";
                    }
                    else
					{
                        PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.MOVE, moveFrom = Selection.Position }, Layer);
					}
                    dragMouseCoords = pix;
                }

                if (Input.IsActionJustReleased("Mouse1"))
				{
                    dragMouseCoords = null;
				}

                if (dragInf == Vector2.Zero && !Input.IsActionPressed("Mouse1"))
				{
                    foreach (string k in axisToVec.Keys)
					{
                        if (Input.IsActionJustPressed(k))
						{
                            inputAxis = k;
                            inputTime = -1;
                            PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.MOVE, moveFrom = Selection.Position }, Layer);
                        }
					}

                    if (inputAxis != "")
					{
                        if (Input.IsActionPressed(inputAxis) && (inputTime <= -1 || inputTime > 0))
						{
                            dragInf = axisToVec[inputAxis];
						}
					}

                    inputTime += 0.03f;    //using 1/30 seconds because we don't have a delta value
                    inputTime = Mathf.Min(inputTime, 1);
                }

                if (dragInf != Vector2.Zero && state == SelectState.STATIC)
                {
                    Selection.Position += dragInf;

                    Vector2 Ppp = Layer.Canvas.RectSize / Layer.Img.GetSize();
                    Box.MakeRectSelection(new Rect2(Selection.Position * Ppp, Selection.Size * Ppp));
                }
                break;
            case SelectState.ADD_DRAW:
                Rect2 addRect = new Rect2(Utils.MinV(pix, addRectStart), Utils.MaxV(pix, addRectStart) - Utils.MinV(pix, addRectStart));

                Vector2 lowestCorner = Utils.VectorCompare(false, addRect.Position, addRect.Size + addRect.Position, Selection.Position, Selection.Size + Selection.Position);
                Vector2 farthestCorner = Utils.VectorCompare(true, addRect.Position, addRect.Size + addRect.Position, Selection.Position, Selection.Size + Selection.Position);
                //GD.Print("Lowest: " + lowestCorner + ", farthest: " + farthestCorner);

                Vector2 oldRectCorner = Selection.Position;
                Selection = new Rect2(lowestCorner, farthestCorner - lowestCorner);

                dataCopy = SelectionData.GetRect(new Rect2(Vector2.Zero, SelectionData.GetSize()));
                SelectionData.Create((int)Selection.Size.x, (int)Selection.Size.y, false, Image.Format.Rgba8);
                SelectionData.BlendRect(dataCopy, new Rect2(Vector2.Zero, dataCopy.GetSize()), oldRectCorner - Selection.Position);

                Vector2 PppNameDifferentFuckTheCompiler = getLayerPPP(Layer);
                Box.MakeRectSelection(new Rect2(addRect.Position * PppNameDifferentFuckTheCompiler, addRect.Size * PppNameDifferentFuckTheCompiler));

                selectInf.Text = "| From " + Selection.Position + " to " + Selection.End + " | Size: " + Selection.Size;

                if (!Input.IsActionPressed("Mouse2"))
				{
                    SelectionData.BlendRect(Layer.Img, addRect, addRect.Position - Selection.Position);

                    Layer.Lock();
                    for (int x = (int)addRect.Position.x; x < addRect.End.x; x++)
                    {
                        for (int y = (int)addRect.Position.y; y < addRect.End.y; y++)
                        {
                            Layer.Img.SetPixel(x, y, Alpha);
                        }
                    }
                    Layer.Unlock();

                    state = SelectState.STATIC;

                    Box.MakeRectSelection(new Rect2(Selection.Position * PppNameDifferentFuckTheCompiler, Selection.Size * PppNameDifferentFuckTheCompiler));
                }
                break;
		}

    }

    private string GetSaveJSON(Image img)
	{
        Dictionary<string, object> data = new Dictionary<string, object>();
        data.Add("size", img.GetSize());
        data.Add("data", Utils.ImageToString(img));
        return JSON.Print(data);
	}

    private void ParseClipboard()
	{
        object rawData = JSON.Parse(OS.Clipboard).Result;
        if (!(rawData is Dictionary<string, object>)) return;

        Dictionary<string, object> data = (Dictionary<string, object>)rawData;

        if (!(data.ContainsKey("size") && data.ContainsKey("data"))) return;

        Selection.Size = (Vector2)data["size"];
        SelectionData = Utils.StringToImage((string)data["data"], (Vector2)data["size"]);
    }

    public override void OnToolSwitch(Layer Layer)
	{
        if (state == SelectState.STATIC)
		{
            PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.DRAW, OldPos = Selection, oldContents = Layer.Img.GetRect(Selection), SelectContents = SelectionData }, Layer);

            Layer.Lock();
            Layer.Img.BlendRect(SelectionData, new Rect2(Vector2.Zero, SelectionData.GetSize()), Selection.Position);
            Layer.Unlock();
        }
        
        state = SelectState.NONE;
        Box.Hide();
    }

	private void FinalizeSelection(Layer Layer)
	{
        if (Selection.HasNoArea())
        {
            state = SelectState.NONE;
        }
        else
        {
            state = SelectState.STATIC;
            SelectionData = Layer.Img.GetRect(Selection);

            PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.CREATE, oldContents = SelectionData, OldPos = Selection }, Layer);

            Layer.Lock();

            Rect2 bounds = new Rect2();
            bounds.Position =  Utils.MaxV(Selection.Position, Vector2.Zero);
            bounds.End = Utils.MinV(Selection.End, Layer.Img.GetSize());

            for (int x = (int)bounds.Position.x; x < bounds.End.x; x++)
            {
                for (int y = (int)bounds.Position.y; y < bounds.End.y; y++)
                {
                    Layer.Img.SetPixel(x, y, Alpha);
                }
            }
            Layer.Unlock();
        }
    }

    private static Vector2 getLayerPPP(Layer l)
	{
        return l.Canvas.RectSize / l.Img.GetSize();
    }

	public override void PreparePreview(TextureRect texture)
	{
		if (Box is null)
        {
            Box = BoxResource.Instance<SelectionBox>();
            texture.AddChild(Box);
            Box.Hide();
        }
	}

	PackedScene BoxResource = GD.Load<PackedScene>("res://UiScenes/SelectionBox.tscn");
    SelectionBox Box;
    public override void PreviewProcess(TextureRect texture, Image OverlayImg, Vector2 pix, BrushInfo info)
    {
        posLabel.Text = "" + pix;

        switch (state)
		{
            case SelectState.NONE:
                Box.Hide();
                break;
            case SelectState.DRAWING:
                Box.Show();
                break;
            case SelectState.STATIC:
                Box.Show();
                OverlayImg.BlendRect(SelectionData, new Rect2(Vector2.Zero, Selection.Size), Selection.Position);
                break;
            case SelectState.ADD_DRAW:
                goto case SelectState.STATIC;
            case SelectState.STRETCHING:
                Box.Show();
                OverlayImg.BlendRect(StretchImage, new Rect2(Vector2.Zero, Selection.Size), Selection.Position);
                break;
            case SelectState.ROTATING:
                Box.Show();
                OverlayImg.BlendRect(rotDataCopy, new Rect2(Vector2.Zero, Selection.Size), Selection.Position);
                break;
        }
    }

	public override void Finish(Layer layer)
	{
        if (state == SelectState.STATIC)
		{
            PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.DRAW, OldPos = Selection, oldContents = layer.Img.GetRect(Selection), SelectContents = SelectionData }, layer);

            layer.Lock();
            layer.Img.BlendRect(SelectionData, new Rect2(Vector2.Zero, SelectionData.GetSize()), Selection.Position);
            layer.Unlock();

            state = SelectState.NONE;
        }
    }

	public void SetSelection(List<Vector2> pixels, Layer layer)
	{
        Vector2 low = Vector2.Inf;
        Vector2 hi = Vector2.Zero;

        foreach (Vector2 p in pixels)
		{
            hi = Utils.MaxV(hi, p);
            low = Utils.MinV(low, p);
        }

        Selection = new Rect2(low, Vector2.One + hi - low);
        SelectionData.Create((int)Selection.Size.x, (int)Selection.Size.y, false, Image.Format.Rgba8);

        SelectionData.Lock();
        layer.Lock();
        foreach (Vector2 p in pixels)
		{
            SelectionData.SetPixelv(p - low, layer.Img.GetPixelv(p));
            layer.Img.SetPixelv(p, Alpha);
		}
        layer.Unlock();
        SelectionData.Unlock();

        state = SelectState.STATIC;

        UpdateBoxSelection(layer);

        PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.CREATE, oldContents = SelectionData, OldPos = Selection }, layer);
    }

    public void SetSelectionImage(Image contents, Layer layer)
    {
        Selection.Size = contents.GetSize();
        SelectionData = contents;

        state = SelectState.STATIC;

        UpdateBoxSelection(layer);

        PushToUndo(new SelectUndo() { type = SelectUndo.UndoType.CREATE_EMPTY}, layer);
    }

    public void UpdateBoxSelection(Layer l)
	{
        Vector2 Ppp = l.Canvas.RectSize / l.Img.GetSize();
        Box.MakeRectSelection(new Rect2(Selection.Position * Ppp, Selection.Size * Ppp));
    }

	public override bool IsSafeForUndo()
	{
		return (state == SelectState.STATIC || state == SelectState.NONE);
	}

	public override bool Undo(Layer target)
    {

        if (!(state == SelectState.STATIC || state == SelectState.NONE)) return false;
        
        SelectUndo top = Undos[target].Pop();

        switch (top.type)
		{
            case SelectUndo.UndoType.MOVE:
                Selection.Position = top.moveFrom;
                Vector2 MovePpp = target.Canvas.RectSize / target.Img.GetSize();
                Box.MakeRectSelection(new Rect2(Selection.Position * MovePpp, Selection.Size * MovePpp));
                break;
            case SelectUndo.UndoType.PASTE:
                state = SelectState.NONE;
                break;
            case SelectUndo.UndoType.CREATE:
                target.Lock();
                target.Img.BlitRect(top.oldContents, new Rect2(Vector2.Zero, top.oldContents.GetSize()), top.OldPos.Position);
                target.Unlock();
                state = SelectState.NONE;
                break;
            case SelectUndo.UndoType.CREATE_EMPTY:
                state = SelectState.NONE;
                break;
            case SelectUndo.UndoType.DRAW:
                target.Lock();
                target.Img.BlitRect(top.oldContents, new Rect2(Vector2.Zero, top.oldContents.GetSize()), top.OldPos.Position);
                target.Unlock();
                goto case SelectUndo.UndoType.DELETE;
            case SelectUndo.UndoType.DELETE:
                Selection = top.OldPos;
                SelectionData = top.SelectContents;
                Vector2 DrawPpp = target.Canvas.RectSize / target.Img.GetSize();
                Box.MakeRectSelection(new Rect2(Selection.Position * DrawPpp, Selection.Size * DrawPpp));
                state = SelectState.STATIC;
                parent.setToolType<SelectTool>();
                break;
            case SelectUndo.UndoType.STRETCH:
                goto case SelectUndo.UndoType.DELETE;
            case SelectUndo.UndoType.ROTATE:
                goto case SelectUndo.UndoType.DELETE;
        }
        return true;
    }

}

struct SelectUndo
{
    public enum UndoType { MOVE, ROTATE, DRAW, DELETE, CREATE, PASTE, STRETCH, CREATE_EMPTY}
    public UndoType type;

    public Vector2 moveFrom;

    public float rotation;

    public Image oldContents;
    public Image SelectContents;
    public Rect2 OldPos;
}
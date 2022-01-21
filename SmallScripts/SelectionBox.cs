using Godot;
using System;

public class SelectionBox : Control
{

    Line2D BoxOutline;
    Control Window;

    TextureButton TL;
    TextureButton TR;
    TextureButton BL;
    TextureButton BR;

    TextureButton Rotate;

    public override void _Ready()
    {
        Hide();

        BoxOutline = GetNode<Line2D>("Line2D");
        Window = GetNode<Control>("Window");

        TL = Window.GetNode<TextureButton>("TL");
        TR = Window.GetNode<TextureButton>("TR");
        BL = Window.GetNode<TextureButton>("BL");
        BR = Window.GetNode<TextureButton>("BR");

        Rotate = Window.GetNode<TextureButton>("Rotate");
    }


    public enum DragDir {NONE, TL, TR, BR, BL}
    public DragDir GetDragging()
	{
        DragDir res = DragDir.NONE;

        res = TL.Pressed ? DragDir.TL : res;
        res = TR.Pressed ? DragDir.TR : res;
        res = BL.Pressed ? DragDir.BL : res;
        res = BR.Pressed ? DragDir.BR : res;

        return res;
	}

    public bool GetRotating()
	{
        return Rotate.Pressed;
	}

	public override void _Process(float delta)
	{
        if (Visible)
		{
            Vector2[] points = new Vector2[5];
            Utils.GetRectCorners(Window.GetRect()).CopyTo(points, 0);
            points[4] = points[0];
            BoxOutline.Points = points;
        }
    }

	public void MakeRectSelection(Rect2 Area)
	{
        //Window.RectSize = Area.Size;
        //Window.RectPosition = Area.Position;
        Window.AnchorTop = Area.Position.y/RectSize.y;
        Window.AnchorLeft = Area.Position.x / RectSize.x;
        Window.AnchorBottom = Area.End.y / RectSize.y;
        Window.AnchorRight = Area.End.x / RectSize.x;
    }

}

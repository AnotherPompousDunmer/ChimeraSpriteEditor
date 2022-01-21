using Godot;
using System;

public class ImageCont : Control
{

    TextureRect Underlay;
    TextureRect Overlay;

    Main Main;

    public override void _Ready()
	{
        CallDeferred(nameof(Zoom), 0, false);
        Underlay = GetNode<TextureRect>("UnderLay");
        Overlay = GetNode<TextureRect>("UnderLay/Overlay");
        Main = GetNode<Main>("../../../");
        
        Connect("focus_entered", this, nameof(_FocusEntered));
        Connect("focus_exited", this, nameof(_FocusExited));
    }

    public void _FocusEntered()
	{
        FocusMode = FocusModeEnum.None;
	}
    public void _FocusExited()
	{
        FocusMode = FocusModeEnum.Click;
    }

    public override void _GuiInput(InputEvent @event)
    {
        //if (!new Rect2(Vector2.Zero, RectSize).HasPoint(GetLocalMousePosition())) return;

        if (@event is InputEventMouseMotion && Input.IsMouseButtonPressed(3))
        {
            Pan((@event as InputEventMouseMotion).Relative);
            AcceptEvent();
        }
        if (@event is InputEventMouseButton)
        {
            if (Input.IsMouseButtonPressed(4))
            {
                Zoom(1);
                AcceptEvent();
            }
            if (Input.IsMouseButtonPressed(5))
            {
                Zoom(-1);
                AcceptEvent();
            }
        }
    }
   
    const int Z_UP_BOUND = 40;
    const int Z_LOW_BOUND = 1;
    private int ZoomAmount = 8;
    public void Zoom(int dir, bool fixPan = true)
    {
        ZoomAmount = Utils.ClampI(Z_LOW_BOUND, ZoomAmount + dir, Z_UP_BOUND);

        Vector2 MousePos = Underlay.GetLocalMousePosition() / Underlay.RectSize;
        Vector2 RectShift = Underlay.RectSize;

        Underlay.RectSize = Main.Size * ZoomAmount;
        RectShift -= Underlay.RectSize;

        (Overlay.Material as ShaderMaterial).SetShaderParam("pixelRatio", ZoomAmount);
        
        if (!fixPan) Pan(RectShift * 0.5f);
        else Pan(RectShift * MousePos);
    }


    private void Pan(Vector2 amount)
    {
        Underlay.RectPosition += amount;

        Vector2 pos = Underlay.RectPosition;

        if (Underlay.RectPosition.x + Underlay.RectSize.x < RectSize.x / 2)
            pos.x = (RectSize.x / 2) - Underlay.RectSize.x;
        if (Underlay.RectPosition.y + Underlay.RectSize.y < RectSize.y / 2)
            pos.y = (RectSize.y / 2) - Underlay.RectSize.y;

        if (Underlay.RectPosition.x > RectSize.x / 2)
            pos.x = RectSize.x / 2;
        if (Underlay.RectPosition.y > RectSize.y / 2)
            pos.y = RectSize.y / 2;

        Underlay.RectPosition = pos;
        //Underlay.RectPosition = Utils.ClampV(Underlay.RectSize / -2, Underlay.RectPosition, RectSize - Underlay.RectSize / 2);
    }

}

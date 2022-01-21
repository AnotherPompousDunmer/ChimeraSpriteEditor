using Godot;
using System;
using System.Collections.Generic;

public class Layers : Control
{

    VBoxContainer List;
    Control Underlay;

    public Dictionary<int, Layer> LayerImg = new Dictionary<int, Layer>();
    int? IndexDirect = null;
    public void SetLayerIndex(bool _b, int New) { LayerIndex = New; }
    int? LayerIndex
	{
        get { return IndexDirect; }
        set {
            GetNodeOrNull<ToolsPopUp>("../../Tools")?.FinishFocusTool();
            IndexDirect = value;
            Underlay.GetNode<NormalTex>("Overlay/TextureRect").Visible = !LayerImg[(int)IndexDirect].Normal;
            foreach (Layer l in LayerImg.Values)
            {
                l.Listitem.GetNode<Button>("Button").SetPressedNoSignal(false);
                if (l.Normal)
				{
                    l.Visible(LayerImg[(int)IndexDirect].Normal);
                }
            }
            List.GetNode<Button>(value + "/Button").SetPressedNoSignal(true);
        }
	}

    Main Main;

    [Export]
    PackedScene LayersListItem;
    [Export]
    PackedScene LayerBase;

    public override void _Ready()
    {
        Main = GetNode<Main>("../../../../../../");
        List = GetNode<VBoxContainer>("List/VBox");
        Underlay = GetNode<TextureRect>("../../../../ImageContainer/UnderLay");

        foreach (Layer l in LayerImg.Values)
		{
            l.Canvas = LayerBase.Instance<TextureRect>();
            l.Canvas.Name = ""+l.Id;

            l.Tex.CreateFromImage(l.Img, 0);
            l.Canvas.Texture = l.Tex;

            Underlay.AddChild(l.Canvas);
        }

        if (LayerImg.Count > 0)
        {
            CopyLayersListToImages();
            IndexDirect = 0;
            DisableDeleteIfEmpty();
        }
    }

    public void SetOnionSkins(Image last, Image next)
	{
        ImageTexture nextTex = new ImageTexture();
        ImageTexture lastTex = new ImageTexture();

        nextTex.CreateFromImage(next, 0);
        lastTex.CreateFromImage(last, 0);

        Underlay.GetNode<TextureRect>("Overlay/OnionSkinBefore").Texture = lastTex;
        Underlay.GetNode<TextureRect>("Overlay/OnionSkinAfter").Texture = nextTex;
    }

    public void RemoveCanvases()
	{
        foreach (Layer l in LayerImg.Values)
		{
            Underlay.RemoveChild(l.Canvas);
            l.Canvas.QueueFree();
		}
	}

    public int AddLayer()
	{
        //Make the Image
        Image New = new Image();

        New.Create((int)Main.Size.x, (int)Main.Size.y, false, Image.Format.Rgba8);
        New.Fill(new Color(1, 1, 1, 0));

        int id = 0;
        while (LayerImg.ContainsKey(id))
		{
            id++;
		}

        LayerImg[id] = new Layer();
        LayerImg[id].Img = New;
        LayerImg[id].Id = id;

        LayerImg[id].tools = GetNode<ToolsPopUp>("../../Tools");

        //Make the TextureRect
        TextureRect LayerRect = LayerBase.Instance<TextureRect>();
        ImageTexture Tex = new ImageTexture();
        Tex.CreateFromImage(New, 0);
        LayerRect.Texture = Tex;
        LayerRect.Name = ""+id;

        Underlay.AddChild(LayerRect);
        Underlay.MoveChild(LayerRect, 0);

        LayerImg[id].Tex = Tex;
        LayerImg[id].Canvas = LayerRect;

        //Make the UI Item
        PanelContainer Item = LayersListItem.Instance<PanelContainer>();

        Item.Name = "" + id;
        Item.GetNode<Label>("HBox/Label").Text = "Layer " + id;
        List.AddChild(Item);

        LayerImg[id].Listitem = Item;

        //Set up buttons
        Godot.Collections.Array IdArray = new Godot.Collections.Array() { id };
        Item.GetNode<Button>("HBox/VBox/Del").Connect("pressed", this, nameof(DeleteLayer), IdArray);
        Item.GetNode<Button>("HBox/VBox/Merge").Connect("pressed", this, nameof(MergeLayer), IdArray);
        Item.GetNode<Button>("Button").Connect("toggled", this, nameof(SetLayerIndex), IdArray);
        Item.GetNode<Button>("HBox/VBox/Prop").Connect("pressed", LayerImg[id], "ShowPropBox");
        Item.GetNode<TextureButton>("HBox/VBox/Visible").Connect("toggled", LayerImg[id], "UpdateVisibility");

        Item.GetNode<Button>("WindowDialog/Grid/OK").Connect("pressed", LayerImg[id], "CopyFromPropBox");
        Item.GetNode<Button>("WindowDialog/Grid/Cancel").Connect("pressed", LayerImg[id], "HidePropBox");

        if (LayerIndex is null) LayerIndex = id;

        LayerImg[id].Connect("UpdateImage", this, nameof(ContinueSignal));
        LayerImg[id].Connect("UpdateNormal", this, nameof(ContinueNormalSignal));
        LayerImg[id].Connect("SaveUnsafe", Main, nameof(Main.SaveNoLongerSafe));

        DisableDeleteIfEmpty();

        return id;
    }

    void DisableDeleteIfEmpty()
	{
        //Disable Delete buttons if last layer; else enable
        if (LayerImg.Count <= 1)
        {
            foreach (Node n in List.GetChildren())
            {
                n.GetNode<Button>("HBox/VBox/Del").Disabled = true;
            }
        }
		else
		{
            foreach (Node n in List.GetChildren())
			{
                n.GetNode<Button>("HBox/VBox/Del").Disabled = false;
            }
		}

        //Fix Selected Layer
        if (!LayerImg.ContainsKey((int)LayerIndex))
		{
            int i = (int)LayerIndex;
            while (i >= 0)
			{
                i--;
                if (LayerImg.ContainsKey(i))
				{
                    LayerIndex = i;
                    return;
				}
			}
            i = (int)LayerIndex;
            while (i <= 255)
            {
                i++;
                if (LayerImg.ContainsKey(i))
                {
                    LayerIndex = i;
                    return;
                }
            }
            //If we can't find another Layer, make a new one
            LayerIndex = null;
            AddLayer();
        }

    }

	internal List<Image> GetFilteredOrderedImages(bool normal, int exportLayer, bool includeHidden = false)
	{
        List<Image> Images = new List<Image>();

        foreach (Node n in List.GetChildren())
        {
            Layer layer = LayerImg[n.Name.ToInt()];
            if (layer.Normal == normal && layer.ExportLayer == exportLayer && (layer.GetVisToggled() || includeHidden))
            {
                Images.Insert(0, layer.Img);
            }
        }

        return Images;
    }

	void DeleteLayer(int id)
	{
        LayerImg[id].Listitem.QueueFree();
        LayerImg[id].Canvas.QueueFree();

        LayerImg[id].Free();
        LayerImg.Remove(id);

        DisableDeleteIfEmpty();

        ContinueNormalSignal();
	}

    void MergeLayer(int id)
	{
        var layerItems = List.GetChildren();
        Layer top = LayerImg[id];
        int id2 = layerItems.IndexOf(LayerImg[id].Listitem) + 1;
        if (id2 >= layerItems.Count) return;
        Layer bottom = LayerImg[(layerItems[id2] as Node).Name.ToInt()];

        bottom.Lock();
        bottom.Img.BlendRect(top.Img, new Rect2(Vector2.Zero, top.Img.GetSize()), Vector2.Zero);
        bottom.Unlock();

        DeleteLayer(id);
	}


    int BeingDragged = -1;

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton && Input.IsActionJustPressed("Mouse1") && GetRect().HasPoint(GetParent<Control>().GetLocalMousePosition()))
		{
            foreach (Control n in List.GetChildren())
            {
                if (new Rect2(Vector2.Zero, n.RectSize).HasPoint(n.GetLocalMousePosition()))
                {
                    BeingDragged = n.Name.ToInt();
                    break;
                }
            }
        }
        if (@event is InputEventMouseButton && Input.IsActionJustReleased("Mouse1"))
        {
            BeingDragged = -1;
        }
    }

	public override void _Input(InputEvent @event)
	{
        if (@event.IsActionPressed("Undo"))
        {
            LayerImg[(int)IndexDirect].UndoLast();
        }
    }

	public override void _Process(float delta)
	{
		if (BeingDragged != -1)
		{
            Control Target = List.GetNode<Control>(""+BeingDragged);

            if (Target.RectPosition.y + Target.RectSize.y < List.GetLocalMousePosition().y)
			{
                List.MoveChild(Target, Utils.ClampI(0, Target.GetPositionInParent() + 1, List.GetChildren().Count));
                CopyLayersListToImages();
			}
            else if (Target.RectPosition.y > List.GetLocalMousePosition().y)
            {
                List.MoveChild(Target, Utils.ClampI(0, Target.GetPositionInParent() - 1, List.GetChildren().Count));
                CopyLayersListToImages();
            }
        }
	}

    private void CopyLayersListToImages()
	{
        Godot.Collections.Array Children = List.GetChildren();
        int[] Order = new int[Children.Count];

        for (int i = 0; i < Order.Length; i++)
		{
            Order[i] = ((Node)Children[i]).Name.ToInt();
		}

        for (int i = 0; i < Order.Length; i++)
        {
            Underlay.MoveChild(Underlay.GetNode("" + Order[i]), Order.Length - i);
        }
        Underlay.MoveChild(Underlay.GetNode("Overlay"), Order.Length + 1);
    }

    public List<Image> GetOrderedImages(bool background = true)
	{
        List<Image> Out = new List<Image>();

        Out.Add(new Image());
        Out[0].Create((int)Main.Size.x, (int)Main.Size.y, false, Image.Format.Rgba8);
        if (background) Out[0].Fill(Colors.White);

        foreach (Control c in List.GetChildren())
		{
            int id = c.Name.ToInt();
            if (!LayerImg[id].Normal && LayerImg[id].GetVisToggled()) Out.Insert(1, LayerImg[c.Name.ToInt()].Img);
		}

        return Out;
	}

    public Layer GetActive()
	{
        if (LayerIndex != null) return LayerImg[(int)LayerIndex];
        return null;
	}


    [Signal]
    delegate void UpdateImage();
    [Signal]
    delegate void UpdateNormal();
    public void ContinueSignal()
	{
        EmitSignal(nameof(UpdateImage));
	}

    public void ContinueNormalSignal()
    {
        LayerIndex = LayerIndex;  //This Fixes Visibilities for normal layers

        List<Layer> Normals = GetOrderedNormals();

        Underlay.GetNode<NormalTex>("Overlay/TextureRect").UpdateFromList(Normals);

        EmitSignal(nameof(UpdateNormal));
    }

    public List<Layer> GetOrderedNormals()
	{
        List<Layer> Normals = new List<Layer>();

        foreach (Node n in List.GetChildren())
        {
            int id = n.Name.ToInt();
            if (LayerImg.ContainsKey(id) && LayerImg[id].Normal && LayerImg[id].GetVisToggled())
            {
                Normals.Insert(0, LayerImg[id]);
            }
        }

        return Normals;
    }


    public void Save(Dictionary<string, object> save)
	{
        foreach (Control c in List.GetChildren())
		{
            Layer l = LayerImg[c.Name.ToInt()];

            Dictionary<string, object> layerInf = new Dictionary<string, object>
			{
				{ "id", l.Id },
				{ "img", Utils.ImageToString(l.Img) },
				{ "export_layer", l.ExportLayer },
				{ "normal", l.Normal }
			};
			save.Add(""+l.Id, layerInf);
        }
	}

    public void Load(Dictionary<string, object> save)
	{
        foreach (Dictionary<string, object> layerInf in save.Values)
        {
            int id = AddLayer();

            Image newImage = Utils.StringToImage((string)layerInf["img"], Main.Size);

            LayerImg[id].Img = newImage;

            LayerImg[id].Normal = (bool)layerInf["normal"];
            LayerImg[id].Unlock();

            LayerImg[id].ExportLayer = (int)((layerInf["export_layer"] is float) ? (float)layerInf["export_layer"] : (int)layerInf["export_layer"]);

            LayerImg[id].Listitem.GetNode<SpinBox>("WindowDialog/Grid/SpinBox").Value = LayerImg[id].ExportLayer;
            LayerImg[id].Listitem.GetNode<CheckBox>("WindowDialog/Grid/CheckBox").Pressed = LayerImg[id].Normal;
        }

	}

    public void DestroyOrphans()
	{
        foreach (Layer l in LayerImg.Values)
		{
            l.Free();
		}
	}

}

public class Layer : Godot.Object
{
    [Signal]
    delegate void frame_pre_draw(); //Attempt to fix bug

    public int Id;
    public Image Img;
    public Control Listitem;
    public ImageTexture Tex;
    public TextureRect Canvas;

    public ToolsPopUp tools;

    public int ExportLayer = 0;
    public bool Normal = false;

    private bool editorVisible = true;

    Stack<IUndoable> Undos = new Stack<IUndoable>();

    [Signal]
    delegate void UpdateImage();

    [Signal]
    delegate void UpdateNormal();

    [Signal]
    delegate void SaveUnsafe();

    public void UndoLast()
	{
        if (Undos.Count > 0 && tools.IsCurrentActive())
        {
            Lock();
            if (Undos.Peek().Undo(this))
            {
                Undos.Pop();
                tools.UpdateTool();
            }
            Unlock();
            Tex.CreateFromImage(Img, 0);
            Canvas.Texture = Tex;
            Listitem.GetNode<TextureRect>("HBox/TextureRect").Texture = Tex;
        }
    }

    public void ClearUndos()
	{
        Undos.Clear();
	}

    public void AddUndoable(IUndoable u)
	{
        Undos.Push(u);
	}

    public void Lock()
	{
        Img.Lock();
    }

    public void Unlock()
	{
        Img.Unlock();

        Tex.CreateFromImage(Img, 0);
        Canvas.Texture = Tex;
        Listitem.GetNode<TextureRect>("HBox/TextureRect").Texture = Tex;

        EmitSignal(nameof(SaveUnsafe));

        EmitSignal(nameof(UpdateImage));
        if (Normal) EmitSignal(nameof(UpdateNormal));
    }

    public void UpdateSilent()
	{
        Tex.CreateFromImage(Img, 0);
        if (IsInstanceValid(Canvas)) Canvas.Texture = Tex;
        Listitem.GetNode<TextureRect>("HBox/TextureRect").Texture = Tex;
    }

    public void ShowPropBox()
	{
        Listitem.GetNode<WindowDialog>("WindowDialog").Popup_();
        //Tool.Popup = true;
	}
    public void HidePropBox()
    {
        Listitem.GetNode<WindowDialog>("WindowDialog").Hide();
        //Tool.Popup = false;
    }

    public bool GetVisToggled()
	{
        return !Listitem.GetNode<TextureButton>("HBox/VBox/Visible").Pressed;
    }

    public void UpdateVisibility(bool _b)
	{
        Canvas.Visible = GetVisToggled() && editorVisible;

        if (Normal) EmitSignal(nameof(UpdateNormal));
        else EmitSignal(nameof(UpdateImage));
    }

    public void CopyFromPropBox()
    {
        ExportLayer = (int)Listitem.GetNode<SpinBox>("WindowDialog/Grid/SpinBox").Value;
        Normal = Listitem.GetNode<CheckBox>("WindowDialog/Grid/CheckBox").Pressed;
        HidePropBox();
        EmitSignal("UpdateNormal");
    }

    public void Visible(bool b)
	{
        if (editorVisible != b)
		{
            editorVisible = b;
            UpdateVisibility(b);
        }
	}

}

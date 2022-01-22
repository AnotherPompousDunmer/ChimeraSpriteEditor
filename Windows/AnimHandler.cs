using Godot;
using System;
using System.Collections.Generic;

public class AnimHandler : VBoxContainer
{
    [Export]
    PackedScene AnimsItem;
    [Export]
    PackedScene FramesList;
    [Export]
    PackedScene FramesItem;


    Container FrameCont;
    Container AnimsList;

    Dictionary<string, Anim> Anims = new Dictionary<string, Anim>();



    public override void _Ready()
    {
        FrameCont = GetNode<Container>("Frames");
        AnimsList = GetNode<Container>("Anims/VBox/HBox/AnimsListScroll/AnimsList");

        GetNode<AddAnimButton>("Anims/VBox/Add").Connect("AddAnim", this, nameof(AddAnim));
    }

    public override void _Input(InputEvent @event)
	{
        foreach (Anim a in Anims.Values)
		{
            a._Input(@event);
		}
	}

    public void AddAnim(string name) { AddAnim(name, null); }
    public void AddAnim(string name, Dictionary<string, object> FrameInf = null)
	{
        string AltName = name;
        int Attempts = 1;
        while (Anims.ContainsKey(AltName))
		{
            AltName = name + Attempts;
            Attempts++;
		}
        name = AltName;

        Anim New = new Anim();
        New.Name = name;
        Anims[name] = New;

        New.LayersList = GetNode<Control>("../Panel/Panels/Layers");

        Container FList = FramesList.Instance<Container>();
        FrameCont.AddChild(FList);
        FList.GetNode<Button>("Add").Connect("pressed", this, nameof(AddFrameTo), new Godot.Collections.Array() { name });

        New.FramesList = FList;

        Container AItem = AnimsItem.Instance<Container>();
        AItem.GetNode<Label>("HBox/Name").Text = name;
        AnimsList.AddChild(AItem);
        AItem.GetNode<Button>("SetActive").Connect("pressed", this, nameof(SetActive), new Godot.Collections.Array() { name });
        AItem.GetNode<Button>("HBox/VBox/Preview").Connect("pressed", New, "ShowPreview");
        AItem.GetNode<Button>("HBox/VBox/Settings").Connect("pressed", New, "ShowSettings");
        AItem.GetNode<Button>("HBox/VBox/Delete").Connect("pressed", New, "DelDialog");
        AItem.GetNode<ConfirmationDialog>("DeleteWarning").Connect("confirmed", this, nameof(DeleteAnim), new Godot.Collections.Array { New , true});

        AItem.GetNode<AcceptDialog>("Properties").Connect("confirmed", New, "ProcessSettingsChange");

        New.AnimsItem = AItem;

        New.itemPreview = New.AnimsItem.GetNode<TextureRect>("HBox/TextureRect");
        New.windowPreview = New.AnimsItem.GetNode<TextureRect>("HBox/VBox/Preview/PreviewWindow/Texture");

        if (FrameInf == null)
		{
            New.AddFrame();
        }
		else
		{
            New.Load(FrameInf);
		}

        New.itemPreview.Call("run");
        New.windowPreview.Call("run");
        SetActive(New.Name);
    }

    public void DestroyOrphans()
	{
        foreach (Anim a in Anims.Values)
		{
            a.DestroyOrphans();
		}
        Free();
    }

    public void DeleteAnim(Anim anim, bool safe)
	{
        if (safe && Anims.Count <= 1) return;
        
        if (safe && anim.HasActiveFrame())
		{
            string newFocus = "";
            bool breakOnNext = false;
            foreach (string s in Anims.Keys)
			{
                if (s == anim.Name) breakOnNext = true;
                else
                {
                    newFocus = s;
                    if (breakOnNext) break;
                }
            }
            SetActive(newFocus);
            Anims[newFocus].SetActiveFrame(0);
		}
        Anims.Remove(anim.Name);

        anim.DestroyOrphans();
        anim.AnimsItem.QueueFree();
    }

    public Anim GetAnim(string anim)
	{
        return Anims[anim];
	}

    public void SetActive(string anim)
	{
        foreach (Anim a in Anims.Values)
		{
            a.FramesList.Visible = (a.Name == anim);
		}
	}

    public void AddFrameTo(string anim)
	{
        Anims[anim].AddFrame();
	}

    public void Save(Dictionary<string, object> save)
	{
        foreach(Anim a in Anims.Values)
		{
            save.Add(a.Name, new Dictionary<string, object>());
            a.Save((Dictionary<string, object>)save[a.Name]);
		}
	}

    public void Load(Dictionary<string, object> save)
	{
        Anim[] aList = new Anim[Anims.Count];
        Anims.Values.CopyTo(aList, 0);
        foreach(Anim a in aList)
		{
            DeleteAnim(a, false);
		}

        foreach (string k in save.Keys)
		{
            //GD.Print(k);
            //GD.Print(save[k].GetType());
            AddAnim(k, (Dictionary<string, object>)save[k]);
		}
	}

    public void CanvasSize(Vector2 fromSize, Vector2 toSize, Vector2 dir)
	{
        Vector2 BlendLoc = toSize - fromSize;

        switch (dir.x)
		{
            case -1:
                BlendLoc.x = 0;
                break;
            case 0:
                BlendLoc.x = (int)(BlendLoc.x / 2);
                break;
            case 1:
                break;
        }
        switch (dir.y)
        {
            case -1:
                BlendLoc.y = 0;
                break;
            case 0:
                BlendLoc.y = (int)(BlendLoc.y / 2);
                break;
            case 1:
                break;
        }

        Rect2 srcRect = new Rect2(Vector2.Zero, fromSize);

        foreach (Anim a in Anims.Values)
		{
            foreach (Frame f in a.Frames)
			{
                foreach (Layer l in f.Layers.LayerImg.Values)
				{
                    Image newImg = new Image();
                    newImg.Create((int)toSize.x, (int)toSize.y, false, Image.Format.Rgba8);
                    newImg.Fill(new Color(1, 1, 1, 0));

                    newImg.BlendRect(l.Img, srcRect, BlendLoc);

                    l.Img = newImg;
                    /*l.Lock();
                    l.Unlock();*/
                    l.UpdateSilent();
                    l.ClearUndos();
				}
                f.UpdatePreview();
			}
            a.UpdateAnimatedTexture();
		}
	}

	internal Main.SpriteSheet GetExportData()
	{
        Main.SpriteSheet data = new Main.SpriteSheet() {anims = new List<Main.SpriteSheet.SheetAnim>()};

        foreach (Anim a in Anims.Values)    //for each animation
		{
            Main.SpriteSheet.SheetAnim anim = new Main.SpriteSheet.SheetAnim() {name = a.Name, frames = new List<Main.SpriteSheet.SheetAnim.SheetFrame>()};

            int frameNo = 0;
            foreach (Frame f in a.Frames)   //for each frame
			{
                List<Main.SpriteSheet.SheetAnim.SheetFrame.FrameLayer> eLayers = new List<Main.SpriteSheet.SheetAnim.SheetFrame.FrameLayer>();
                Dictionary<int, Layer> layers = f.Layers.LayerImg;

                bool skip = false;
                foreach (Layer l in layers.Values)  //sort the frame into export layers (& normals)
				{
                    //GD.Print("Checking " + anim.name + ": " + l.ExportLayer + ", " + l.Normal + "...");
                    foreach (Main.SpriteSheet.SheetAnim.SheetFrame.FrameLayer checkLayer in eLayers)    //Check if we've already parsed this layer
					{
                        if (checkLayer.exportLayer == l.ExportLayer && checkLayer.normal == l.Normal)
						{
                            skip = true;
                            break;
						}
					}
                    if (skip) continue;
                    //GD.Print("Not a duplicate");

                    List<Image> images = f.Layers.GetFilteredOrderedImages(l.Normal, l.ExportLayer);
                    Image combinedImage = new Image();
                    combinedImage.Create((int)l.Img.GetSize().x, (int)l.Img.GetSize().y, false, Image.Format.Rgba8);

                    foreach (Image i in images)
					{
                        combinedImage.BlendRect(i, new Rect2(Vector2.Zero, l.Img.GetSize()), Vector2.Zero);
					}
                    
                    eLayers.Add(new Main.SpriteSheet.SheetAnim.SheetFrame.FrameLayer() { exportLayer = l.ExportLayer, normal = l.Normal, img = combinedImage});
				}

                anim.frames.Add(new Main.SpriteSheet.SheetAnim.SheetFrame() { frameNo = frameNo, layers = eLayers});
                frameNo++;
            }
            data.anims.Add(anim);
        }

        return data;
    }
}

public class Anim : Godot.Node
{
    public string Name;
    public List<Frame> Frames = new List<Frame>();

    public Container FramesList;
    public Container AnimsItem;

    public Control LayersList;

    public int fps = 10;

    PackedScene FListItem = GD.Load<PackedScene>("res://UiScenes/FramesItem.tscn");
    AnimatedTexture Preview = new AnimatedTexture();

    public TextureRect itemPreview;
    public TextureRect windowPreview;

    public void Save(Dictionary<string, object> save)
	{
        for (int i = 0; i < Frames.Count; i++)
		{
            save.Add(""+i, new Dictionary<string, object>());
            Frames[i].Layers.Save((Dictionary<string, object>) save[""+i]);
		}
	}

    public void Load(Dictionary<string, object> save)
	{
        int[] keys = new int[save.Count];
        int i = 0;
        foreach (string k in save.Keys)
		{
            keys[i] = k.ToInt();
            i++;
		}
        Array.Sort(keys);

        foreach (Dictionary<string, object> d in save.Values)
		{
            AddFrame(d);
		}
	}

    public void DelDialog()
	{
        AnimsItem.GetNode<ConfirmationDialog>("DeleteWarning").Popup_();

    }

    public void AddFrame() { AddFrame(null); }
    public void AddFrame(Dictionary<string, object> layerInf = null, int insertTo = -1)
	{
        Frame frame = new Frame() { Layers = Frame.LayerScene.Instance<Layers>() };
        
        if (insertTo == -1)
		{
            Frames.Add(frame);
        }
        else
		{
            Frames.Insert(insertTo, frame);
		}
        

        frame.Connect("Duplicate", this, nameof(DuplicateFrame), new Godot.Collections.Array() { frame });
        frame.Connect("UpdateAnimPreview", this, nameof(UpdateAnimatedTexture));
        frame.Layers.Connect("UpdateImage", frame, "UpdatePreview");
        frame.Layers.Connect("UpdateNormal", frame, "UpdateNormal");

        Control FrameItem = FListItem.Instance<Control>();
        frame.FrameItem = (AspectRatioContainer)FrameItem;
        FramesList.GetNode("AnimsListScroll/GridContainer").AddChild(FrameItem);
        FrameItem.GetNode<MenuButton>("TextureButton/Options").GetPopup().Connect("id_pressed", frame, "OptionInput");

        frame.Connect("DeleteDialog", this, nameof(OpenDeleteDialog), new Godot.Collections.Array() { frame });
        FrameItem.GetNode<ConfirmationDialog>("TextureButton/DeleteDialog").Connect("confirmed", this, nameof(DeleteFrame), new Godot.Collections.Array() { frame });

        TextureButton FrameButton = FrameItem.GetNode<TextureButton>("TextureButton");
        FrameButton.Connect("button_down", this, nameof(SetDragging), new Godot.Collections.Array() {frame});
        FrameButton.Connect("button_up", this, nameof(SetActiveFrame), new Godot.Collections.Array() { frame });
        frame.PreviewButton = FrameButton;

        SetActiveFrame(Frames.IndexOf(frame));
        if (layerInf == null)
		{
            frame.Layers.AddLayer();
        }
		else
		{
            frame.Layers.Load(layerInf);
		}

        UpdateAnimatedTexture();
        CopyFramesListOrderToUI();
        frame.UpdatePreview();
	}

    private void CopyFramesListOrderToUI()
	{
        GridContainer grid = FramesList.GetNode<GridContainer>("AnimsListScroll/GridContainer");
        for (int i = 0; i < Frames.Count; i++)
		{
            grid.MoveChild(Frames[i].FrameItem, i);
		}
	}

    public void DestroyOrphans()
	{
        FramesList.QueueFree();
        foreach (Frame f in Frames)
		{
            f.DestroyOrphans();
		}
        QueueFree();
	}

    public void DeleteFrame(Frame frame)
	{
        if (Frames.Count <= 1) return;

        int index = Frames.IndexOf(frame);
        Frames.Remove(frame);

        if (frame.Layers.GetParentOrNull<Node>() != null)
		{
            SetActiveFrame(Utils.ClampI(0, index, Frames.Count-1));
		}

        frame.DestroyOrphans();
        frame.FrameItem.QueueFree();
        FramesList.GetNode<GridContainer>("AnimsListScroll/GridContainer").QueueSort();

        UpdateAnimatedTexture();
    }

    public void DuplicateFrame(Frame frame)
	{
        int index = Frames.IndexOf(frame);

        Dictionary<string, object> data = new Dictionary<string, object>();
        frame.Layers.Save(data);

        AddFrame(data, index);
	}

    Frame drag = null;
    public void SetDragging(Frame f)
	{
        drag = f;
	}

    public override void _Input(InputEvent @event)
	{
        if (!Input.IsActionPressed("Mouse1")) drag = null;
        if (@event is InputEventMouseMotion && drag != null)
		{
            if (!drag.FrameItem.HasPoint((@event as InputEventMouseMotion).Position))
			{
                GridContainer grid = FramesList.GetNode<GridContainer>("AnimsListScroll/GridContainer");
                Vector2 gridPos = grid.GetLocalMousePosition() / drag.FrameItem.RectSize;
                gridPos = Utils.FloorV(gridPos);
                int mousePos = (int)(gridPos.x + (gridPos.y * grid.Columns));
                mousePos = Utils.ClampI(0, mousePos, Frames.Count-1);
                Frames.Remove(drag);
                Frames.Insert(mousePos, drag);
                CopyFramesListOrderToUI();
                UpdateAnimatedTexture();
            }
		}
	}

    public void UpdateAnimatedTexture()
    {
        Godot.Collections.Array frames = new Godot.Collections.Array();

        NormalTex norm = AnimsItem.GetNode<NormalTex>("../../../../../../../ImageContainer/UnderLay/Overlay/TextureRect");
        for (int i = 0; i < Frames.Count; i++)
		{
            ImageTexture frame = new ImageTexture();
            frame.CreateFromImage(Frames[i].GetPreviewImage(true, norm), 0);

            frames.Add(frame);
        }

        itemPreview.Call("set_fps", fps);
        itemPreview.Call("set_array", frames);
        windowPreview.Call("set_fps", fps);
        windowPreview.Call("set_array", frames);
    }

    public void SetActiveFrame(Frame f) { SetActiveFrame(Frames.IndexOf(f)); }
    public void SetActiveFrame(int frame)
	{
        //TODO: vomit-inducing tree navigation
        AnimsItem.GetNode<ToolsPopUp>("../../../../../../../Panel/Panels/Tools").FinishFocusTool();

        if (LayersList.GetChildCount() > 0)
		{
            foreach (Layers n in LayersList.GetChildren())
			{
                n.RemoveCanvases();
                LayersList.RemoveChild(n);
			}
		}
        Frames[frame].Layers.RequestReady();
        LayersList.AddChild(Frames[frame].Layers);

        //Set Onion Skins
        Image OnionNext = new Image();
        Image OnionLast = new Image();

        OnionNext.Create(1, 1, false, Image.Format.Rgba8);
        OnionNext.Fill(new Color(1, 1, 1, 0));
        OnionLast.CopyFrom(OnionNext);

        OnionLast = Frames[Utils.ArraySafeMod(frame - 1, Frames.Count)].GetPreviewImage(false);
        OnionNext = Frames[Utils.ArraySafeMod(frame + 1, Frames.Count)].GetPreviewImage(false);

        Frames[frame].Layers.SetOnionSkins(OnionLast, OnionNext);

        Frames[frame].Layers.CallDeferred("ContinueNormalSignal");
    }

    public void ShowPreview()
	{
        AnimsItem.GetNode<WindowDialog>("HBox/VBox/Preview/PreviewWindow").Popup_();
	}

    public bool HasActiveFrame()
	{
        foreach (Frame f in Frames)
		{
            if (f.Layers.GetParentOrNull<Node>() != null) return true;
		}
        return false;
	}

    public void ProcessSettingsChange()
	{
        fps = (int)AnimsItem.GetNode<SpinBox>("Properties/Grid/FPS").Value;
        itemPreview.Call("set_fps", fps);
    }
    public void ShowSettings()
	{
        AnimsItem.GetNode<SpinBox>("Properties/Grid/FPS").Value = fps;
        AnimsItem.GetNode<AcceptDialog>("Properties").Popup_();
	}

    public void OpenDeleteDialog(Frame frame)
    {
        if (Frames.Count > 1)
		{
            frame.FrameItem.GetNode<ConfirmationDialog>("TextureButton/DeleteDialog").Popup_();
        }
    }

}

public class Frame : Godot.Object
{
    [Signal]
    delegate void frame_pre_draw(); //Attempt to fix bug

    public static PackedScene LayerScene = GD.Load<PackedScene>("res://UiScenes/Layers.tscn");

    public Layers Layers;

    public ImageTexture Preview = new ImageTexture();
    public AspectRatioContainer FrameItem;
    public TextureButton PreviewButton;

    public Image GetPreviewImage(bool background = true, NormalTex normal = null)
	{
        Image New = new Image();

        List<Image> LayerImgs = Layers.GetOrderedImages(); //GD.Print(LayerImgs[0].GetSize());

        New.CopyFrom(LayerImgs[0]);

        New.Fill(new Color(1, 1, 1, background ? 1 : 0));
        

        for (int i = 1; i < LayerImgs.Count; i++)
        {
            New.BlendRect(LayerImgs[i], new Rect2(Vector2.Zero, LayerImgs[i].GetSize()), Vector2.Zero);
        }

        if (normal != null)
		{
            Image normImg = normal.GetNormalMult(Layers.GetOrderedNormals());
            Utils.SimpleImageDoubleMult(New, normImg);
        }

        return New;
    }

    public void UpdatePreview()
	{
        Image New = GetPreviewImage();

        Preview.CreateFromImage(New, 0);
        PreviewButton.TextureNormal = Preview;
        
        EmitSignal(nameof(UpdateAnimPreview));
	}

    [Signal]
    delegate void DeleteDialog();
    [Signal]
    delegate void Duplicate();
    [Signal]
    delegate void UpdateAnimPreview();

    public void OptionInput(int id)
	{
        switch(id)
		{
            case 0: //Duplicate
                EmitSignal("Duplicate");
                break;
            case 1: //Delete
                EmitSignal("DeleteDialog");
                break;
		}
	}

    void UpdateNormal()
    {
        
    }

    public void DestroyOrphans()
	{
        //FrameItem.QueueFree();    //Parent already Freed
        Layers.DestroyOrphans();
        Layers.QueueFree();
        Free();
	}

}

using Godot;
using System;
using System.Collections.Generic;

public class Main : Control
{
	//static PackedScene NormalScene = GD.Load<PackedScene>("res://NormalTex.tscn");
	static PackedScene FullEditor = GD.Load<PackedScene>("res://FullEditor.tscn");

	public Vector2 Size;

	AnimHandler AnimHandler;
	Layers Layers;

	File save = new File();
	bool saveUpdated = true;

	public override void _Ready()
	{
		OS.WindowMaximized = true;

		AddToGroup("ActionNotify");
		GetTree().SetAutoAcceptQuit(false);
		GetNode<ConfirmationDialog>("ExitSaveConfirmation").AddButton("Save & Continue", false, "SaveContinue");

		//Connect to popup windows
		GetNode<ConfirmationDialog>("ExitSaveConfirmation").Connect("custom_action", this, nameof(SaveExitWindowUpdateSave));
		GetNode<ConfirmationDialog>("ExitSaveConfirmation").Connect("confirmed", this, nameof(ExitSaveConf));
		GetNode<FileDialog>("SaveDialog").Connect("file_selected", this, nameof(FinalizeSaveAs));
		GetNode<FileDialog>("LoadDialog").Connect("file_selected", this, nameof(Load));
		GetNode<FileDialog>("ImportDialog").Connect("file_selected", this, nameof(ImportImage));
		GetNode<ConfirmationDialog>("NewProj").Connect("confirmed", this, nameof(NewProj));
		GetNode<ConfirmationDialog>("CanvasSize").Connect("confirmed", this, nameof(CanvasSize));
		GetNode<ConfirmationDialog>("Grid").Connect("confirmed", this, nameof(SetGrid));
		GetNode<ConfirmationDialog>("Export").Connect("confirmed", this, nameof(Export));
		GetNode<ConfirmationDialog>("GenNormals").Connect("confirmed", this, nameof(GenNormals));

		MakeEditor(new Vector2(32, 32));

		string[] param = OS.GetCmdlineArgs();
		File openedSave = new File();
		foreach (string s in param)
		{
			string test = s.Replace("\\\\", "/");
			if (test.Extension() == "cse" && openedSave.Open(test, File.ModeFlags.Read) == Error.Ok)
			{
				openedSave.Close();
				Load(test);
			}
		}

		SetWindowTitle();
	}

	Control editor;
	private void MakeEditor(Vector2 size)
	{
		if (AnimHandler != null) AnimHandler.DestroyOrphans();
		
		if (editor != null)
		{
			ToolsPopUp oldTools = editor.GetNodeOrNull<ToolsPopUp>("Main/Panel/Panels/Tools");
			if (oldTools != null) oldTools.DestroyOrphans();
		}

		Size = size;

		foreach (Node n in GetChildren())
		{
			if (n.HasMeta("Editor"))
			{
				n.Free();
			}
		}
		editor = FullEditor.Instance<Control>();
		editor.SetMeta("Editor", true);
		AddChild(editor);

		editor.GetNode<MenuButton>("DropDowns/List/File").GetPopup().Connect("id_pressed", this, nameof(FileMenu));
		editor.GetNode<MenuButton>("DropDowns/List/Canvas").GetPopup().Connect("id_pressed", this, nameof(CanvasMenu));
		editor.GetNode<MenuButton>("DropDowns/List/Tools").GetPopup().Connect("id_pressed", this, nameof(ToolsMenu));


		AnimHandler = editor.GetNode<AnimHandler>("Main/Anim");
		AnimHandler.AddAnim("Idle");
		
		GetNode<ToolsPopUp>("Cont/Main/Panel/Panels/Tools").CreatePreview();
	}

	public void SetWindowTitle()
	{
		OS.SetWindowTitle(string.Format("{0}{1} - Chimera Sprite Editor",
			saveUpdated ? "" : "(*)",
			save.IsOpen() ? save.GetPath().GetFile(): "Untitled"));
	}

	public override void _Notification(int type)
	{
		if (type == MainLoop.NotificationWmQuitRequest)
		{
			if (saveUpdated)
				Quit();
			else
			{
				SaveWindowBehaviour = () => { Quit(); };
				GetNode<ConfirmationDialog>("ExitSaveConfirmation").CallDeferred("popup");
			}
				
		}
	}

	Action SaveWindowBehaviour;
	public void ExitSaveConf()
	{
		SaveWindowBehaviour.DynamicInvoke();
	}

	public async void Quit()
	{
		AnimHandler.DestroyOrphans();
		editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").SaveSettings();
		editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").DestroyOrphans();
		editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").QueueFree();
		await ToSignal(GetTree(), "idle_frame");
		PrintStrayNodes();
		GetTree().Quit();
	}

	public void SaveNoLongerSafe()
	{
		saveUpdated = false;
		SetWindowTitle();
	}

	public void FileMenu(int id)
	{
		editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").FinishFocusTool();
		switch (id)
		{
			case 0:
				UpdateSave();
				break;
			case 1:
				SaveDialog();
				break;
			case 2:
				GetNode<FileDialog>("LoadDialog").Popup_();
				break;
			case 3:
				GetNode<WindowDialog>("NewProj").Popup_();
				break;
			case 4:
				GetNode<ConfirmationDialog>("Export").Popup_();
				break;
			case 5:
				GetNode<FileDialog>("ImportDialog").Popup_();
				break;
		}
	}

	public void CanvasMenu(int id)
	{
		editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").FinishFocusTool();
		switch (id)
		{
			case 0:
				GetNode<ConfirmationDialog>("CanvasSize").Popup_();
				break;
		}
	}

	public void ToolsMenu(int id)
	{
		editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").FinishFocusTool();
		switch (id)
		{
			case 0:
				SwitchOnionSkinVisibility(0);
				break;
			case 1:
				SwitchOnionSkinVisibility(1);
				break;
			case 2:
				GetNode<ConfirmationDialog>("Grid").Popup_();
				break;
			case 3:
				GetNode<ConfirmationDialog>("GenNormals").Popup_();
				break;
		}
	}

	public void SwitchOnionSkinVisibility(int onion)
	{
		MenuButton Tools = GetNode<MenuButton>("Cont/DropDowns/List/Tools");

		Tools.GetPopup().SetItemChecked(onion, !Tools.GetPopup().IsItemChecked(onion));

		editor.GetNode<TextureRect>("Main/ImageContainer/UnderLay/Overlay/OnionSkinBefore").Visible = Tools.GetPopup().IsItemChecked(0);
		editor.GetNode<TextureRect>("Main/ImageContainer/UnderLay/Overlay/OnionSkinAfter").Visible = Tools.GetPopup().IsItemChecked(1);
		
	}

	static Dictionary<string, Vector2> CanvasStringToVec2 = new Dictionary<string, Vector2>() {
		{"TL", new Vector2(1, 1)},
		{"T", new Vector2(0, 1)},
		{"TR", new Vector2(-1, 1)},
		{"L", new Vector2(1, 0)},
		{"C", new Vector2(0, 0)},
		{"R", new Vector2(-1, 0)},
		{"BL", new Vector2(1, -1)},
		{"B", new Vector2(0, -1)},
		{"BR", new Vector2(-1, -1)}
	};
	public void CanvasSize()
	{
		Vector2 dir = CanvasStringToVec2[GetNode<Button>("CanvasSize/HBox/Grid/TL").Group.GetPressedButton().Name];
		Vector2 size = new Vector2((float)GetNode<SpinBox>("CanvasSize/HBox/VBox/X/SpinBox").Value, (float)GetNode<SpinBox>("CanvasSize/HBox/VBox/Y/SpinBox").Value);

		AnimHandler.CanvasSize(this.Size, size, dir);

		Size = size;
		//This is shit and naive. Works though.
		editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").CreatePreview();
		editor.GetNode<ImageCont>("Main/ImageContainer").Zoom(0, false);
	}

	public void ImportImage(string path)
	{
		Image import = new Image();
		if (import.Load(path) == Error.Ok)
		{
			ToolsPopUp tools = editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools");
			tools.FinishFocusTool();
			tools.setToolType<SelectTool>().SetSelectionImage(import, editor.GetNode<Control>("Main/Panel/Panels/Layers").GetChild<Layers>(0).GetActive());
		}
	}

	class NormalsSetting
	{
		float pos;	//0-1
		int divisions;	//0-360
		float finalAngle;	//0-90
		bool concave;

		public NormalsSetting(Control settings)
		{
			pos = (float)settings.GetNode<Slider>("Pos/Slider").Value;
			divisions = (int)settings.GetNode<SpinBox>("Settings/VBox/Division/SpinBox").Value;
			finalAngle = Mathf.Deg2Rad((float)settings.GetNode<Slider>("Settings/VBox/Slope/HSlider").Value);
			concave = settings.GetNode<CheckBox>("Settings/VBox/ConCave").Pressed;
		}

		public float getAngleRad(float relPos)
		{
			//distance on -1 - 1 scale
			float dist = relPos - pos;
			dist *= 1 / Mathf.Max(1 - pos, pos);
			//quantize
			dist += 1;
			dist *= divisions/2f;
			dist = Mathf.Floor(dist);
			dist /= divisions/2f;
			dist -= 1;

			return finalAngle * dist;
		}
	}

	public void GenNormals()
	{
		NormalsSetting xSettings = new NormalsSetting(GetNode<Control>("GenNormals/Sliders/X"));
		NormalsSetting ySettings = new NormalsSetting(GetNode<Control>("GenNormals/Sliders/Y"));

		Layers layers = editor.GetNode<Control>("Main/Panel/Panels/Layers").GetChild<Layers>(0);
		Image Background = Utils.BlendImages(layers.GetOrderedImages(false));

		Image normal = new Image();
		normal.Create((int)Size.x, (int)Size.y, true, Image.Format.Rgba8);
		normal.Fill(new Color(1, 1, 1, 0));
		
		Background.Lock();
		normal.Lock();

		for (int x = 0; x < Size.x; x++) for (int y = 0; y < Size.y; y++)
			{
				if (Background.GetPixel(x, y).a > 0.1)
				{
					Vector3 angle = new Vector3(0, 0, 1);
					Vector2 pos = new Vector2(x, y) / (Size);
					angle = angle.Rotated(Vector3.Left, ySettings.getAngleRad(pos.y)) + angle.Rotated(Vector3.Up, xSettings.getAngleRad(pos.x));
					angle = angle.Normalized();
					//GD.Print("At ("+x+", "+y+": ("+Mathf.Stepify(angle.x, 0.01f)+", " + Mathf.Stepify(angle.y, 0.01f) + ", " + Mathf.Stepify(angle.z, 0.01f) + ")");
					normal.SetPixel(x, y, new Color((angle.x + 1) / 2, (1 - angle.y) / 2, (angle.z + 1) / 2));
				}
			}

		Background.Unlock();
		normal.Unlock();
		
		int id = layers.AddLayer();
		layers.LayerImg[id].Img = normal;

		layers.LayerImg[id].Normal = true;
		layers.LayerImg[id].Unlock();
	}

	public void SetGrid()
	{
		bool active = GetNode<CheckBox>("Grid/VBox/Active").Pressed;
		Vector2 size = new Vector2((float)GetNode<SpinBox>("Grid/VBox/HBox/X").Value, (float)GetNode<SpinBox>("Grid/VBox/HBox/Y").Value);

		ShaderMaterial previewMat = (ShaderMaterial)(editor.GetNode<TextureRect>("Main/ImageContainer/UnderLay/Overlay").Material);
		previewMat.SetShaderParam("gridOn", active);
		previewMat.SetShaderParam("gridSize", size);
	}

	public void FinalizeSaveAs(string path)
	{
		//Think that was broken; not sure; test saving
		//save = new File();

		//Saving format since I'm liable to forget
		//save.StoreLine(JSON.Print(dictionary))
		//btw its snek_case keys
		Dictionary<string, object> mainInf = new Dictionary<string, object>() { { "size_x", Size.x }, { "size_y", Size.y }, {"anims", new Dictionary<string, object>()} };
		
		AnimHandler.Save((Dictionary<string, object>)mainInf["anims"]);

		save.Open(path, File.ModeFlags.WriteRead);
		save.StoreLine(JSON.Print(mainInf));
		save.Flush();

		saveUpdated = true;

		SetWindowTitle();
	}

	public void SaveDialog()
	{
		GetNode<FileDialog>("SaveDialog").Popup_();
	}

	public void SaveExitWindowUpdateSave(string _s) { GetNode<ConfirmationDialog>("ExitSaveConfirmation").Hide();  UpdateSave(); }
	public void UpdateSave()
	{
		if (!save.IsOpen()) SaveDialog();
		else
		{
			string path = save.GetPath();
			File backup = new File();
			backup.Open("res://SaveBackups/backup.cse", File.ModeFlags.Write);
			backup.StoreLine(save.GetAsText());
			FinalizeSaveAs(path);
		}
	}

	public void Load(string path)
	{
		if (!saveUpdated)
		{
			SaveWindowBehaviour = () => { DoLoad(path); };
			GetNode<ConfirmationDialog>("ExitSaveConfirmation").Popup_();
		}
		else
		{
			DoLoad(path);
		}

	}

	public void DoLoad(string path)
	{
		save = new File();

		save.Open(path, File.ModeFlags.Read);
		Dictionary<string, object> data = GDictionaryConvert((Godot.Collections.Dictionary)JSON.Parse(save.GetLine()).Result);
		save.Flush();

		MakeEditor(new Vector2((float)data["size_x"], (float)data["size_y"]));

		//FIXME: This should need to be deferred. It doesn't, but it should.
		AnimHandler.Load((Dictionary<string, object>)data["anims"]);

		saveUpdated = true;
		//old todos, probably non-issues
		//OLD todo - Update the preview size
		//OLD todo - reset the tools
		SetWindowTitle();
	}


	public struct SpriteSheet
	{
		public string path;
		public List<SheetAnim> anims;

		public struct SheetAnim
		{
			public string name;
			public List<SheetFrame> frames;

			public struct SheetFrame
			{
				public int frameNo;
				public List<FrameLayer> layers;

				public struct FrameLayer
				{
					public Image img;
					public bool normal;
					public int exportLayer;
				}
			}
		}
	}

	public void Export()
	{
		int split = GetNode<OptionButton>("Export/VBox/Settings/Split").Selected;
		int normals = GetNode<OptionButton>("Export/VBox/Settings/Normals").Selected;
		int exportLayers = GetNode<OptionButton>("Export/VBox/Settings/ExportLayers").Selected;
		int optimization = GetNode<OptionButton>("Export/VBox/Settings/Optimization").Selected;

		string path = GetNode<LineEdit>("Export/VBox/Path/LineEdit").Text;

		SpriteSheet data = AnimHandler.GetExportData();
		data.path = path.Remove(path.Length - 4);

		List<SpriteSheet> export = new List<SpriteSheet>();

		//Split Dictionary by split mode
		switch (split)
		{
			case 0: //Don't Split
				export.Add(data);
				break;
			case 1:	//By Anim
				foreach (SpriteSheet.SheetAnim anim in data.anims)
				{
					export.Add(new SpriteSheet() { path = data.path+"_"+anim.name, anims = new List<SpriteSheet.SheetAnim>() { anim } });
				}
				break;
			case 2: //By Frame
				foreach (SpriteSheet.SheetAnim anim in data.anims)
				{
					foreach (SpriteSheet.SheetAnim.SheetFrame frame in anim.frames)
					{
						export.Add(new SpriteSheet()
						{
							path = data.path + "_" + anim.name + frame.frameNo,
							anims = new List<SpriteSheet.SheetAnim>()
							{
								new SpriteSheet.SheetAnim()
								{
									name = anim.name, 
									frames = new List<SpriteSheet.SheetAnim.SheetFrame>()
									{
										frame
									}
								}
							} 
						});
					}
				}
				break;
		}

		//Split exports to separate files
		if (exportLayers == 0)
		{
			SpriteSheet[] reference = new SpriteSheet[export.Count];
			export.CopyTo(reference);
			export.Clear();

			foreach (SpriteSheet sheet in reference)
			{
				Dictionary<int, SpriteSheet> sortedSheets = new Dictionary<int, SpriteSheet>();

				//Cycle through every frame in this sheet
				foreach (SpriteSheet.SheetAnim anim in sheet.anims) foreach (SpriteSheet.SheetAnim.SheetFrame frame in anim.frames) foreach (SpriteSheet.SheetAnim.SheetFrame.FrameLayer layer in frame.layers)
						{

							if (sortedSheets.ContainsKey(layer.exportLayer))	//if we've started this collection, put it in there
							{
								int eL = layer.exportLayer;

								int animNo = -1;
								foreach (SpriteSheet.SheetAnim sortAnim in sortedSheets[eL].anims)	//find our anim
								{
									if (sortAnim.name == anim.name)
									{
										animNo = sortedSheets[eL].anims.IndexOf(sortAnim);
									}
								}

								if (animNo == -1)	//create it if its not there
								{
									animNo = sortedSheets[eL].anims.Count;
									sortedSheets[eL].anims.Add(new SpriteSheet.SheetAnim() { name = anim.name, frames = new List<SpriteSheet.SheetAnim.SheetFrame>()});
								}

								int frameIndex = -1;
								foreach (SpriteSheet.SheetAnim.SheetFrame sortFrame in sortedSheets[eL].anims[animNo].frames)	//find our frame
								{
									if (sortFrame.frameNo == frame.frameNo)
									{
										frameIndex = sortedSheets[eL].anims[animNo].frames.IndexOf(sortFrame);
									}
								}

								if (frameIndex == -1)
								{
									frameIndex = sortedSheets[eL].anims[animNo].frames.Count;
									sortedSheets[eL].anims[animNo].frames.Add(new SpriteSheet.SheetAnim.SheetFrame() { frameNo = frame.frameNo, layers = new List<SpriteSheet.SheetAnim.SheetFrame.FrameLayer>() });
								}

								sortedSheets[eL].anims[animNo].frames[frameIndex].layers.Add(layer);
							}
							else	//Otherwise, start it
							{
								sortedSheets.Add(layer.exportLayer, new SpriteSheet()
								{
									path = sheet.path + "_" + layer.exportLayer,
									anims = new List<SpriteSheet.SheetAnim>()
									{
										new SpriteSheet.SheetAnim()
										{
											name = anim.name, 
											frames = new List<SpriteSheet.SheetAnim.SheetFrame>()
											{
												new SpriteSheet.SheetAnim.SheetFrame()
												{
													frameNo = frame.frameNo, 
													layers = new List<SpriteSheet.SheetAnim.SheetFrame.FrameLayer>()
													{
														layer
													}
												}
											}
										}
									}
								});
							}

						}

				foreach (SpriteSheet sortedSheet in sortedSheets.Values)
				{
					export.Add(sortedSheet);
				}
			}
		}

		//Split by normals
		if (normals == 0)
		{
			SpriteSheet[] reference = new SpriteSheet[export.Count];
			export.CopyTo(reference);
			export.Clear();

			foreach (SpriteSheet sheet in reference)
			{
				Dictionary<bool, SpriteSheet> sortedSheets = new Dictionary<bool, SpriteSheet>();

				//Cycle through every frame in this sheet
				foreach (SpriteSheet.SheetAnim anim in sheet.anims) foreach (SpriteSheet.SheetAnim.SheetFrame frame in anim.frames) foreach (SpriteSheet.SheetAnim.SheetFrame.FrameLayer layer in frame.layers)
						{

							if (sortedSheets.ContainsKey(layer.normal))    //if we've started this collection, put it in there
							{
								bool n = layer.normal;

								int animNo = -1;
								foreach (SpriteSheet.SheetAnim sortAnim in sortedSheets[n].anims)  //find our anim
								{
									if (sortAnim.name == anim.name)
									{
										animNo = sortedSheets[n].anims.IndexOf(sortAnim);
									}
								}

								if (animNo == -1)   //create it if its not there
								{
									animNo = sortedSheets[n].anims.Count;
									sortedSheets[n].anims.Add(new SpriteSheet.SheetAnim() { name = anim.name, frames = new List<SpriteSheet.SheetAnim.SheetFrame>() });
								}

								int frameIndex = -1;
								foreach (SpriteSheet.SheetAnim.SheetFrame sortFrame in sortedSheets[n].anims[animNo].frames)   //find our frame
								{
									if (sortFrame.frameNo == frame.frameNo)
									{
										frameIndex = sortedSheets[n].anims[animNo].frames.IndexOf(sortFrame);
									}
								}

								if (frameIndex == -1)
								{
									frameIndex = sortedSheets[n].anims[animNo].frames.Count;
									sortedSheets[n].anims[animNo].frames.Add(new SpriteSheet.SheetAnim.SheetFrame() { frameNo = frame.frameNo, layers = new List<SpriteSheet.SheetAnim.SheetFrame.FrameLayer>() });
								}

								sortedSheets[n].anims[animNo].frames[frameIndex].layers.Add(layer);
							}
							else    //Otherwise, start it
							{
								sortedSheets.Add(layer.normal, new SpriteSheet()
								{
									path = sheet.path + (layer.normal ? "_normal" : ""),
									anims = new List<SpriteSheet.SheetAnim>()
									{
										new SpriteSheet.SheetAnim()
										{
											name = anim.name,
											frames = new List<SpriteSheet.SheetAnim.SheetFrame>()
											{
												new SpriteSheet.SheetAnim.SheetFrame()
												{
													frameNo = frame.frameNo,
													layers = new List<SpriteSheet.SheetAnim.SheetFrame.FrameLayer>()
													{
														layer
													}
												}
											}
										}
									}
								});
							}

						}

				foreach (SpriteSheet sortedSheet in sortedSheets.Values)
				{
					export.Add(sortedSheet);
				}
			}
		}
		//TODO: above two splitters are nigh identical, I could probably turn them into a function

		//Turn into images
		Dictionary<string, Image> outs = new Dictionary<string, Image>();
		if (optimization == 0)
		{
			foreach (SpriteSheet sheet in export)
			{
				List<Image> allImages = new List<Image>();

				foreach (SpriteSheet.SheetAnim anim in sheet.anims)
				{
					List<int> albLayers = new List<int>();
					List<int> normLayers = new List<int>();
					List<int> frameNos = new List<int>();

					foreach (SpriteSheet.SheetAnim.SheetFrame frame in anim.frames)
					{
						if (!frameNos.Contains(frame.frameNo)) frameNos.Add(frame.frameNo);
						foreach (SpriteSheet.SheetAnim.SheetFrame.FrameLayer layer in frame.layers)
						{
							if (layer.normal)
							{
								if (!normLayers.Contains(layer.exportLayer)) normLayers.Add(layer.exportLayer);
							}
							else if (!albLayers.Contains(layer.exportLayer)) albLayers.Add(layer.exportLayer);
						}
					}

					albLayers.Sort();
					normLayers.Sort();
					frameNos.Sort();
					foreach (int l in albLayers)
					{
						foreach (int f in frameNos)
						{
							foreach (SpriteSheet.SheetAnim.SheetFrame.FrameLayer testL in anim.frames[f].layers)
							{
								if (testL.normal == false && testL.exportLayer == l)
								{
									allImages.Add(testL.img);
								}
							}
						}
					}
					foreach (int l in normLayers)
					{
						foreach (int f in frameNos)
						{
							foreach (SpriteSheet.SheetAnim.SheetFrame.FrameLayer testL in anim.frames[f].layers)
							{
								if (testL.normal == true && testL.exportLayer == l)
								{
									allImages.Add(testL.img);
								}
							}
						}
					}
				}

				int width = 0;
				int height = 0;
				int factor = 0;
				
				for (factor = (int)Mathf.Sqrt(allImages.Count); factor > 0; factor--)
				{
					if (allImages.Count % factor == 0) break;
				}

				height = factor * (int)Size.y;
				width = (allImages.Count / factor) * (int)Size.x;

				Image sheetOut = new Image();
				sheetOut.Create(width, height, false, Image.Format.Rgba8);

				int xCursor = 0;
				int yCursor = 0;

				Rect2 srcRect = new Rect2(Vector2.Zero, Size);

				foreach (Image i in allImages)
				{
					sheetOut.BlitRect(i, srcRect, new Vector2(xCursor, yCursor));
					xCursor += (int)Size.x;
					if (xCursor >= width)
					{
						yCursor += (int)Size.y;
						xCursor = 0;
					}
				}

				outs.Add(sheet.path + ".png", sheetOut);
				//sheetOut.SavePng(sheet.path + ".png");
			}
		}
		else
		{
			foreach (SpriteSheet sheet in export)
			{
				int width = 0;
				foreach (SpriteSheet.SheetAnim anim in sheet.anims) width = Utils.MaxI(anim.frames.Count, width);
				width *= (int)Size.x;

				int height = 0;
				foreach (SpriteSheet.SheetAnim anim in sheet.anims)
				{
					List<int> albLayers = new List<int>();
					List<int> normLayers = new List<int>();

					foreach (SpriteSheet.SheetAnim.SheetFrame frame in anim.frames) foreach (SpriteSheet.SheetAnim.SheetFrame.FrameLayer layer in frame.layers)
						{
							if (layer.normal)
							{
								if (!normLayers.Contains(layer.exportLayer)) normLayers.Add(layer.exportLayer);
							}
							else if (!albLayers.Contains(layer.exportLayer)) albLayers.Add(layer.exportLayer);
						}

					height += albLayers.Count;
					height += normLayers.Count;
				}
				height *= (int)Size.y;

				Image sheetOut = new Image();
				sheetOut.Create(width, height, false, Image.Format.Rgba8);

				int xCursor = 0;
				int yCursor = 0;
				foreach (SpriteSheet.SheetAnim anim in sheet.anims)
				{
					Dictionary<int, Dictionary<int, Image>> albedoFrames = new Dictionary<int, Dictionary<int, Image>>();	//First layer is export layer, second is frame no
					Dictionary<int, Dictionary<int, Image>> normalFrames = new Dictionary<int, Dictionary<int, Image>>();

					foreach (SpriteSheet.SheetAnim.SheetFrame frame in anim.frames) foreach (SpriteSheet.SheetAnim.SheetFrame.FrameLayer layer in frame.layers)
						{
							if (layer.normal)
							{
								if (!normalFrames.ContainsKey(layer.exportLayer)) normalFrames.Add(layer.exportLayer, new Dictionary<int, Image>());
								normalFrames[layer.exportLayer].Add(frame.frameNo, layer.img);
							}
							else
							{
								if (!albedoFrames.ContainsKey(layer.exportLayer)) albedoFrames.Add(layer.exportLayer, new Dictionary<int, Image>());
								albedoFrames[layer.exportLayer].Add(frame.frameNo, layer.img);
							}
						}

					Rect2 srcRect = new Rect2(Vector2.Zero, Size);

					PrintFramesListReadability(albedoFrames, ref xCursor, ref yCursor, srcRect, sheetOut);
					PrintFramesListReadability(normalFrames, ref xCursor, ref yCursor, srcRect, sheetOut);
				}

				outs.Add(sheet.path + ".png", sheetOut);
			}
		}

		//Print
		bool err = false;
		List<string> errorsText = new List<string>();
		foreach (KeyValuePair<string, Image> kvp in outs)
		{
			Error fileOpenErr = kvp.Value.SavePng(kvp.Key);
			if (fileOpenErr != Error.Ok)
			{
				err = true;
				errorsText.Add(string.Format("Error at {0}: {1}", kvp.Key, fileOpenErr.ToString()));
			}
		}

		//Show result
		if (!err)
		{
			GetNode<AcceptDialog>("ExportError").DialogText = "Export was succesful!";
		}
		else
		{
			string errorString = string.Format("Error{0} at Export:", errorsText.Count>1 ? "s" : "");

			foreach (string s in errorsText)
			{
				errorString += "\n" + s;
			}

			GetNode<AcceptDialog>("ExportError").DialogText = errorString;
		}
		GetNode<AcceptDialog>("ExportError").Popup_();
	}
	private void PrintFramesListReadability(Dictionary<int, Dictionary<int, Image>> frames, ref int xCursor, ref int yCursor, Rect2 srcRect, Image sheetOut)
	{
		int[] albedoLayers = Utils.GetSortedDictionaryKeys(frames);
		foreach (int l in albedoLayers)
		{
			int[] layerFrames = Utils.GetSortedDictionaryKeys(frames[l]);
			foreach (int f in layerFrames)
			{
				sheetOut.BlitRect(frames[l][f], srcRect, new Vector2(xCursor, yCursor));
				xCursor += (int)Size.x;
			}
			xCursor = 0;
			yCursor += (int)Size.y;
		}
	}

	internal static Dictionary<string, object> GDictionaryConvert(Godot.Collections.Dictionary inDict)
	{
		Dictionary<string, object> outDict = new Dictionary<string, object>();
		foreach (string k in inDict.Keys)
		{
			if (inDict[k] is Godot.Collections.Dictionary)
			{
				outDict.Add(k, GDictionaryConvert((Godot.Collections.Dictionary)inDict[k]));
			}
			else
			{
				outDict.Add(k, inDict[k]);
			}
		}
		return outDict;
	}

	public void NewProj()
	{
		if (!saveUpdated)
		{
			SaveWindowBehaviour = () => { DoNewProj(); };
			GetNode<ConfirmationDialog>("ExitSaveConfirmation").Popup_();
		}
		else
		{
			DoNewProj();
		}
	}

	public void DoNewProj()
	{
		Vector2 newSize = new Vector2(
			(float)GetNode<SpinBox>("NewProj/VBox/Input/X").Value,
			(float)GetNode<SpinBox>("NewProj/VBox/Input/Y").Value
			);

		MakeEditor(newSize);

		save.Close();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("Save"))
		{
			editor.GetNode<ToolsPopUp>("Main/Panel/Panels/Tools").FinishFocusTool();
			UpdateSave();
		}
	}

}

public interface IUndoable
{
	bool Undo(Layer target);
}

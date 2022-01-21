using Godot;
using System;
using System.Collections.Generic;

public class NormalTex : TextureRect
{

    List<LightSource> Lights = new List<LightSource>() {};
	Color AmbientLight = Colors.White;

	LightsControl lightController;

	public Main Main;

	ImageTexture Tex;

	public override void _Ready()
	{
		Main = GetNode<Main>("../../../../../../");

		Image NormalImage = new Image();
		NormalImage.Create((int)Main.Size.x, (int)Main.Size.y, false, Image.Format.Rgba8);	NormalImage.Fill(AmbientLight);
		Tex = new ImageTexture();
		Tex.CreateFromImage(NormalImage, 0);
		Texture = Tex;

		lightController = GetNode<LightsControl>("../../../../Panel/Panels/TabContainer/Lights");
	}

	public void UpdateFromList(List<Layer> layers)
	{
		Image Lum = GetNormalMult(layers);
		Tex.CreateFromImage(Lum, 0);
		Texture = Tex;
	}

	public Image GetNormalMult(List<Layer> layers)
	{
		AmbientLight = lightController.GetAmbientColor();
		Lights = lightController.GetDirLights();

		//Fill the image with ambient light
		Image Lum = new Image();
		Lum.Create((int)Main.Size.x, (int)Main.Size.y, false, Image.Format.Rgba8);
		Lum.Fill(AmbientLight/2);

		//Return if there's nothing to do
		if (layers.Count < 1)
		{
			Tex.CreateFromImage(Lum, 0);
			Texture = Tex;
			return Lum;
		}

		//Combine the normal layers to a single map
		Image CombinedNormal = new Image();
		CombinedNormal.CopyFrom(layers[0].Img);

		for (int i = 1; i < layers.Count; i++)
		{
			CombinedNormal.BlendRect(layers[i].Img, new Rect2(Vector2.Zero, Main.Size), Vector2.Zero);
		}

		CombinedNormal.Lock();
		Lum.Lock();

		//Calculate each pixel's lighting
		for (int x = 0; x < Lum.GetSize().x; x++) for (int y = 0; y < Lum.GetSize().y; y++)
			{
				Vector2 coords = new Vector2(x, y);
				Color NormColor = CombinedNormal.GetPixelv(coords);

				if (NormColor.a < 1)
				{
					Lum.SetPixelv(coords, new Color(1, 1, 1, 0));
					continue;
				}

				Vector3 NormDir = new Vector3((NormColor.r * 2) - 1, (NormColor.g * 2) - 1, (NormColor.b * 2) - 1);

				Color outColor = Colors.Black;

				foreach (LightSource s in Lights)
				{
					float dist = Mathf.Clamp(NormDir.AngleTo(s.Dir) / (float)Math.PI, 0, 1);
					outColor += new Color(dist, dist, dist, 1) * s.Color;
				}
				Lum.SetPixelv(coords, (AmbientLight + outColor)/2);
			}

		//Finish up
		CombinedNormal.Unlock();
		Lum.Unlock();

		return Lum;
	}

}

public struct LightSource
{
    public Vector3 Dir;
    public Color Color;
}
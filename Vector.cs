using Godot;
using System;

public class Vector : Control
{

	Slider X;
	Slider Y;
	Slider Z;

	Line2D Graphic;

	public override void _Ready()
	{
		X = GetNode<Slider>("X/X");
		Y = GetNode<Slider>("Y/Y");
		Z = GetNode<Slider>("Z/Z");

		Graphic = GetNode<Line2D>("TextureRect/Viewport/Line2D");

		GetNode<TextureRect>("TextureRect").Texture = GetNode<Viewport>("TextureRect/Viewport").GetTexture();

		UpdateGraphic();
	}

	const float LENGTH = 25;

	public void UpdateGraphic(float _unused = 0f)
	{
		Vector2[] NewPoints = Graphic.Points;
		NewPoints[1] = NewPoints[0] + (new Vector2((float)X.Value, (float)Y.Value) * LENGTH);
		Graphic.Points = NewPoints;
	}

	public Color GetNormal()
	{
		return new Color(((float)X.Value + 1)/2f, ((float)Y.Value + 1) / 2f, ((float)Z.Value + 1) / 2f);
	}

}

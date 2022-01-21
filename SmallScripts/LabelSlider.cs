using Godot;
using System;

public class LabelSlider : Control
{
	SpinBox spin;
	Slider Slider;

	GradientTexture gradient;

	public float Val;

	[Signal]
	delegate void ValueChanged();

	public override void _Ready()
	{
		Slider = GetNode<Slider>("VBoxContainer/HSlider");
		spin = GetNode<SpinBox>("SpinBox");
		gradient = (GradientTexture)GetNode<TextureRect>("VBoxContainer/TextureRect").Texture;

		OnChange((float)Slider.Value);
	}

	public void SetGradient(Color l, Color r)
	{
		Color[] p = new Color[] { l, r };
		SetGradient(p);
	}

	public void SetGradient(Color[] points)
	{
		Gradient grad = gradient.Gradient;
		grad.Colors = new Color[0];
		for (int i = 0; i < points.Length; i++)
		{
			grad.AddPoint(i / (float)(points.Length-1), points[i]);
		}
		gradient.Gradient = grad;
	}

	public void SetLabel(string val)
	{
		GetNode<Label>("Label").Text = val;
	}

	public void OnChange(float val)
	{
		Val = val;
		SetRangeValueSilent(spin, val);
		SetRangeValueSilent(Slider, val);
		EmitSignal(nameof(ValueChanged));
	}

	public void SetValue(float val)
	{
		Val = val;
		SetRangeValueSilent(spin, val);
		SetRangeValueSilent(Slider, val);
	}

	private static void SetRangeValueSilent(Range r, float v)
	{
		r.SetBlockSignals(true);
		r.Value = v;
		r.SetBlockSignals(false);
	}

}
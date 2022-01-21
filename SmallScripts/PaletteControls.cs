using Godot;
using System;

public class PaletteControls : VBoxContainer
{

	LabelSlider[] sliders = new LabelSlider[4];
	GradientTexture[] sliderVis = new GradientTexture[4];
	OptionButton mode;

	Palette parent;

	public int Mode { get { return mode.Selected; } }

	public override void _Ready()
	{
		parent = GetParent<Palette>();
		mode = GetNode<OptionButton>("Mode");

		mode.Connect("item_selected", this, nameof(SwitchMode));

		sliders[0] = GetNode<LabelSlider>("RH");
		sliders[1] = GetNode<LabelSlider>("GS");
		sliders[2] = GetNode<LabelSlider>("BV");
		sliders[3] = GetNode<LabelSlider>("A");

		sliders[0].Connect("ValueChanged", this, nameof(OnValueChanged));
		sliders[1].Connect("ValueChanged", this, nameof(OnValueChanged));
		sliders[2].Connect("ValueChanged", this, nameof(OnValueChanged));
		sliders[3].Connect("ValueChanged", this, nameof(OnValueChanged));

		sliders[0].SetLabel("R");
		sliders[1].SetLabel("G");
		sliders[2].SetLabel("B");
		sliders[3].SetLabel("A");
	}

	public void SwitchMode(int mode)
	{
		switch (mode)
		{
			case 0:
				sliders[0].SetLabel("R");
				sliders[1].SetLabel("G");
				sliders[2].SetLabel("B");
				sliders[3].SetLabel("A");
				break;
			case 1:
				sliders[0].SetLabel("H");
				sliders[1].SetLabel("S");
				sliders[2].SetLabel("V");
				sliders[3].SetLabel("A");
				break;
			case 2:
				sliders[0].SetLabel("X");
				sliders[1].SetLabel("Y");
				sliders[2].SetLabel("Z");
				sliders[3].SetLabel("A");
				break;
		}
		SetColor(parent.GetColor(true));
	}

	public void SetColor(Color c, bool fixCursors = true)
	{
		switch (mode.Selected)
		{
			case 0:
				for (int i = 0; i < sliders.Length; i++)
				{
					sliders[i].SetGradient(SetComp(c, i, 0), SetComp(c, i, 1));
					if (fixCursors) sliders[i].SetValue(c[i]);
				}
				break;
			case 1:
				Color[] hPoints = new Color[10];
				for (int i = 0; i < hPoints.Length; i++)
				{
					hPoints[i] = SetCompHSV(c, 0, i / (float)(hPoints.Length-1));
				}
				sliders[0].SetGradient(hPoints);
				sliders[1].SetGradient(SetCompHSV(c, 1, 0), SetCompHSV(c, 1, 1));
				sliders[2].SetGradient(SetCompHSV(c, 2, 0), SetCompHSV(c, 2, 1));
				sliders[3].SetGradient(SetCompHSV(c, 3, 0), SetCompHSV(c, 3, 1));

				if (fixCursors)
				{
					sliders[0].SetValue(c.h);
					sliders[1].SetValue(c.s);
					sliders[2].SetValue(c.v);
					sliders[3].SetValue(c.a);
				}
				break;
			case 2:
				goto case 0;
		}
	}

	private static Color SetComp(Color inC, int component, float val)
	{
		Color outC = inC;
		switch (component)
		{
			case 0: outC.r = val; break;
			case 1: outC.g = val; break;
			case 2: outC.b = val; break;
			case 3: outC.a = val; break;
		}
		return outC;
	}
	private static Color SetCompHSV(Color inC, int component, float val)
	{
		Color outC = inC;
		switch (component)
		{
			case 0: outC.h = val; break;
			case 1: outC.s = val; break;
			case 2: outC.v = val; break;
			case 3: outC.a = val; break;
		}
		return outC;
	}

	public void OnValueChanged()
	{
		switch (mode.Selected)
		{
			case 0:
				parent.SetColor(new Color(sliders[0].Val, sliders[1].Val, sliders[2].Val, sliders[3].Val), true, false);
				break;
			case 1:
				parent.SetColor(Color.FromHsv(sliders[0].Val, sliders[1].Val, sliders[2].Val, sliders[3].Val), true, false);
				break;
			case 2:
				goto case 0;
		}
		
	}
}

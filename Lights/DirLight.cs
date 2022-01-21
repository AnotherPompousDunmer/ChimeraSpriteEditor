using Godot;
using System;

public class DirLight : VBoxContainer
{

    Slider xSlider;
    Slider ySlider;
    Slider zSlider;

    Slider rSlider;
    Slider gSlider;
    Slider bSlider;

    Line2D vPreview;
    ColorRect cPreview;

    public LightsControl parent;

    public override void _Ready()
    {
        xSlider = GetNode<Slider>("DirBox/Dir/X/Slider");
        ySlider = GetNode<Slider>("DirBox/Dir/Y/Slider");
        zSlider = GetNode<Slider>("DirBox/Dir/Z/Slider");

        xSlider.Connect("value_changed", this, nameof(UpdateVector));
        ySlider.Connect("value_changed", this, nameof(UpdateVector));
        zSlider.Connect("value_changed", this, nameof(UpdateVector));

        rSlider = GetNode<Slider>("HBox/VBox/R/Slider");
        gSlider = GetNode<Slider>("HBox/VBox/G/Slider");
        bSlider = GetNode<Slider>("HBox/VBox/B/Slider");

        rSlider.Connect("value_changed", this, nameof(UpdateColor));
        gSlider.Connect("value_changed", this, nameof(UpdateColor));
        bSlider.Connect("value_changed", this, nameof(UpdateColor));

        vPreview = GetNode<Line2D>("DirBox/Viewport/Line");
        cPreview = GetNode<ColorRect>("HBox/ColorRect");
    }

    public void UpdateVector(float _ignore)
	{
        vPreview.Points = new Vector2[] {new Vector2(16, 16), 16 * new Vector2((float)xSlider.Value + 1, -(float)ySlider.Value + 1) };
        parent.RerenderNormals(0f);
	}

    public void UpdateColor(float _ignore)
	{
        cPreview.Color = new Color((float)rSlider.Value / 255f, (float)gSlider.Value / 255f, (float)bSlider.Value / 255f);
        parent.RerenderNormals(0f);
    }

    public LightSource GetData()
	{
        LightSource data = new LightSource();

        data.Color = new Color((float)rSlider.Value / 255f, (float)gSlider.Value / 255f, (float)bSlider.Value / 255f);
        data.Dir = new Vector3((float)xSlider.Value, (float)ySlider.Value, (float)zSlider.Value);

        return data;
	}

}

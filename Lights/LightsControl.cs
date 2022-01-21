using Godot;
using System;
using System.Collections.Generic;

public class LightsControl : VBoxContainer
{
    PackedScene dLightScene = GD.Load<PackedScene>("res://UiScenes/LightControl.tscn");

    Slider AmbientR;
    Slider AmbientG;
    Slider AmbientB;

    TabContainer dirLightsContainer;

    public override void _Ready()
    {
        dirLightsContainer = GetNode<TabContainer>("List");

        AmbientR = GetNode<Slider>("VBox/R/Slider");
        AmbientG = GetNode<Slider>("VBox/G/Slider");
        AmbientB = GetNode<Slider>("VBox/B/Slider");

        AmbientR.Connect("value_changed", this, nameof(RerenderNormals));
        AmbientG.Connect("value_changed", this, nameof(RerenderNormals));
        AmbientB.Connect("value_changed", this, nameof(RerenderNormals));

        GetNode<Button>("AddLight").Connect("pressed", this, nameof(CreateNewLight));
        GetNode<Button>("DeleteLight").Connect("pressed", this, nameof(DeleteCurrentLight));
    }

    public void DeleteCurrentLight()
	{
        dirLightsContainer.GetChild<DirLight>(dirLightsContainer.CurrentTab).QueueFree();
        CallDeferred(nameof(RerenderNormals), 0f);
    }

    public void CreateNewLight()
	{
        DirLight newLight = dLightScene.Instance<DirLight>();
        newLight.parent = this;
        dirLightsContainer.AddChild(newLight);
        NameDLights();
        RerenderNormals(0f);
	}

    public void NameDLights()
	{
        foreach (DirLight l in dirLightsContainer.GetChildren())
        {
            l.Name = "" + (l.GetPositionInParent() + 1);
        }
    }

    public void RerenderNormals(float _ignore)
	{
        GetNode("../../Layers").GetChild<Layers>(0).ContinueNormalSignal();
	}

    public Color GetAmbientColor()
	{
        return new Color((float)AmbientR.Value / 255f, (float)AmbientG.Value / 255f, (float)AmbientB.Value / 255f);
	}

    public List<LightSource> GetDirLights()
	{
        List<LightSource> sources = new List<LightSource>();
        foreach (DirLight l in dirLightsContainer.GetChildren())
		{
            sources.Add(l.GetData());
		}
        return sources;
	}

}

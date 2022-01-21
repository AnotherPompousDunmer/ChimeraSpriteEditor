shader_type canvas_item;

uniform int pixelRatio = 1;

//Marching ants variables
uniform bool marchingAnts = false;
uniform float width = 0.03f;
uniform float antWidth = 0.12f;
uniform float antLength = 0.24f;
uniform float antSpeed = 0.1f;
uniform vec4 antColor = vec4(0,0,0,1);

//Grid variables
uniform bool gridOn = false;
uniform vec2 gridSize = vec2(8, 8);
uniform vec4 gridColor = vec4(0,0,0,1);
uniform float gridWidth = 0.065f;
uniform float gridDashLength = 0.2;

uniform float time = 0;

void fragment()
{
	//Set the texture
	COLOR = texture(TEXTURE, UV);
	//Get a screen-adjusted UV for visuals
	vec2 adjUV = UV*float(pixelRatio);
	//Marching ants
	if (marchingAnts && COLOR.a == 0f)
	{
		float adjWidth = width/float(pixelRatio);
		float a = 0f;
		a += texture(TEXTURE, UV+vec2(0, adjWidth)).a;
		a += texture(TEXTURE, UV+vec2(0, -adjWidth)).a;
		a += texture(TEXTURE, UV+vec2(adjWidth, 0)).a;
		a += texture(TEXTURE, UV+vec2(-adjWidth, 0)).a;
		
		if (a != 0f)
		{
			float dist = mod(adjUV.x+adjUV.y + time*antSpeed, antLength);
			
			if (dist < antWidth)
			{
				COLOR = antColor;
			}
		}
	}
	//Grid
	if (gridOn)
	{
		vec2 pixelUV = UV / TEXTURE_PIXEL_SIZE;
		vec2 distFromGrid = mod(pixelUV, gridSize);
		distFromGrid *= TEXTURE_PIXEL_SIZE;
		distFromGrid *= float(pixelRatio);
		if (distFromGrid.x < gridWidth || distFromGrid.y < gridWidth)
		{
			float dist = mod(adjUV.x+adjUV.y, gridDashLength*2f);
			if (dist < gridDashLength)
			{
				COLOR = gridColor;
			}
		}
	}
}
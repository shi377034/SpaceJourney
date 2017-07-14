// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SpaceRTS/Grid"
{
	Properties
	{
		_Thickness("Thickness", Float) = 0.08
		
		_UnitSpacing("UnitSpacing", Float) = 2.0
		_Divisions("Divisions", Int) = 10
		_Colour("Colour", Color) = (0.5, 1.0, 1.0, 1.0)
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" }

		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			// Define the vertex and fragment shader functions
			#pragma vertex vert
			#pragma fragment frag

			// Access Shaderlab properties
			uniform float _Thickness;
			uniform float _UnitSpacing;
			uniform int _Divisions;
			uniform float4 _Colour;

			// Input into the vertex shader
			struct vertexInput {
				float4 vertex : POSITION;
			};

			// Output from vertex shader into fragment shader
			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 worldPos : TEXCOORD0;
			};

			// VERTEX SHADER
			vertexOutput vert(vertexInput input) 
			{
				vertexOutput output;
				output.pos = UnityObjectToClipPos(input.vertex);
				// Calculate the world position coordinates to pass to the fragment shader
				output.worldPos = mul(unity_ObjectToWorld, input.vertex);
				return output;
			}

			float CalcGridFact(vertexOutput input, float spacing, float thickness)
			{
				if (spacing == 0.0)
					return 0.0f;
				// grid square snap pos
				float2 snapPos = round(input.worldPos.xz / spacing) * spacing;
				// distance to the square snap pos
				float2 dist = abs(snapPos - input.worldPos.xz);
				// factor of distance with thickness to square border 
				float2 fact = 1 - min(1.f, dist / thickness);
				// more transparent as far the point is from the 1 of x and y
				return fact.x + fact.y - (fact.x * fact.y);
			}

			// FRAGMENT SHADER
			float4 frag(vertexOutput input) : COLOR
			{
				fixed4 c = _Colour;
				float a1 = CalcGridFact(input, _UnitSpacing, _Thickness);
				float a2 = CalcGridFact(input, _UnitSpacing / _Divisions, _Thickness);
				c.a *= a1 + 2*a2;
				return c;
			}
			ENDCG
		}
	}
}

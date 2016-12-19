Shader "Custom/GridShader" {
	Properties {
		_Color ("Color", Color) = (1.0,1.0,1.0,1.0)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_GridTex ("Base (RGB)", 2D) = "white" {}
		_GlimmerTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _GridTex;
		sampler2D _GlimmerTex;
		float4 _Color;

		struct Input {
			float2 uv_MainTex;
			float2 uv_GridTex;
			float2 uv_GlimmerTex;
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 ca = tex2D (_MainTex, IN.worldPos / _Object2World._m00 * 4 + float2 (-_Time.x*0.1, 0));
			half4 cb = tex2D (_MainTex, IN.worldPos / _Object2World._m00 * 4 + float2 (_Time.x*0.1, 0.5));
			half4 c = lerp (ca, cb, 0.4 + 0.2 * _SinTime.z);
			c = lerp (c, 1, 0.4);
			half4 d = tex2D (_GridTex, IN.worldPos / _Object2World._m00 * 32 );
			half4 e = tex2D (_GlimmerTex, IN.uv_GlimmerTex + float2 (_Time.x * -0.05, _Time.x * 1.5 + IN.uv_GlimmerTex.x * 2));
			o.Emission = lerp (c.rgb * _Color, 1, d.a * (0.3 + 0.2 * e.r));
		}
		ENDCG
	} 
	FallBack "Diffuse"
}

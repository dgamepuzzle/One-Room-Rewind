Shader "Test/TestCamEffect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Intensity ("Intensity", Range(0, 1)) = 0.01
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float _Intensity;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				
				col.r = tex2D(_MainTex, i.uv + float2(-_Intensity * cos(_Time[2]), -_Intensity * cos(_Time[3]))).r;
				col.g = tex2D(_MainTex, i.uv + float2(_Intensity * cos(_Time[3]), -_Intensity * cos(_Time[1]))).g;
				col.b = tex2D(_MainTex, i.uv + float2(_Intensity * pow(cos(_Time[3]), 2), _Intensity * cos(_Time[3]))).b;
	
				return col;
			}
			ENDCG
		}
	}
}

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Toon/Water" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_WaterEdge ("Edge  (RGB)", 2D) = "white" {}
		_EdgeColor ("Edge Color", Color) = (.5,.5,.5,1)
		_ToonShade ("ToonShader Cubemap(RGB)", CUBE) = "" { }
		_ToonColor ("Toon Color", Color) = (.5,.5,.5,1)
		_Distort ("_Distort A", 2D) = "white" {}
		_DistortX ("Distortion in X", Range (0,2)) = 1
		_DistortY ("Distortion in Y", Range (0,2)) = 0
	}


	SubShader {
		Tags { "RenderType"="Opaque"}
		Lighting Off
		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _WaterEdge;
			samplerCUBE _ToonShade;
			float4 _MainTex_ST;
			sampler2D _Distort;
			float4 _Color;
			float4 _ToonColor;
			float4 _EdgeColor;
			fixed _DistortX;
			fixed _DistortY;
			fixed4 _Distort_ST;
			struct appdata {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float3 normal : NORMAL;
//				float4 tangent : TANGENT;
//				float3 normal : NORMAL;
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD3;
				float3 cubenormal : TEXCOORD1;
				 fixed4 color : COLOR;
				UNITY_FOG_COORDS(2)
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoord1 = TRANSFORM_TEX(v.texcoord,_Distort);
				o.color.xyz = v.normal * 5.5;
				o.cubenormal = UnityObjectToClipPos (float4(v.normal,0));

				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				
				fixed distort = tex2D(_Distort, fixed2(i.texcoord1.x,i.texcoord1.y-_Time.x/2)).a;
				fixed4 edge =  (1-tex2D(_WaterEdge,fixed2(i.texcoord1.x-distort*0,i.texcoord1.y-distort*_DistortY ))) * _EdgeColor;
				fixed4 edge2 =  (tex2D(_WaterEdge,fixed2(i.texcoord1.x-distort*0,i.texcoord1.y-distort*_DistortY ))) * _EdgeColor;
				edge2.a = 0.11;
				fixed4 col = _Color * tex2D(_MainTex,fixed2(i.texcoord.x-distort*_DistortX,i.texcoord.y-distort*_DistortY-_Time.y/2));
				fixed4 cube = texCUBE(_ToonShade,float3(i.cubenormal)   ) * _ToonColor ;
				fixed4 c = fixed4(cube.rgb + col + edge,1);

				UNITY_APPLY_FOG(i.fogCoord, c);

				return  c;
			}
			ENDCG			
		}
	} 

	Fallback "VertexLit"
}

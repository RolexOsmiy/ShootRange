// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'



// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Toon/MarioWater" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_WaterEdge ("Edge  (RGB)", 2D) = "white" {}
		_EdgeColor ("Edge Color", Color) = (.5,.5,.5,1)
		_ToonShade ("ToonShader Cubemap(RGB)", CUBE) = "" { }
//		_ToonColor ("Toon Color", Color) = (.5,.5,.5,1)
//		_Distort ("_Distort A", 2D) = "white" {}
//		_DistortX ("Distortion in X", Range (0,2)) = 1
//		_DistortY ("Distortion in Y", Range (0,2)) = 0
		_Noise ("_Noise A", 2D) = "bump" {}
		_NoiseOffset ("Main Color", Vector) = (.5,.5,.5,1)
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
			sampler2D _Noise;
			fixed4 _NoiseOffset;
			sampler2D _WaterEdge;
			samplerCUBE _ToonShade;
			float4 _MainTex_ST;
//			sampler2D _Distort;
			float4 _Color;
//			float4 _ToonColor;
			float4 _EdgeColor;
//			fixed _DistortX;
//			fixed _DistortY;
			fixed4 _WaterEdge_ST;
			struct appdata {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD3;
				float3 cubenormal : TEXCOORD1;
				UNITY_FOG_COORDS(2)
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoord1 = TRANSFORM_TEX(v.texcoord,_WaterEdge);
				o.cubenormal = UnityObjectToClipPos (float4(v.texcoord,0,0));
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			fixed4 edge;
			fixed4 frag (v2f i) : SV_Target
			{
//				fixed distort = tex2D(_Distort, fixed2(i.texcoord1.x,i.texcoord1.y-_Time.x/2)).r;

//				fixed4 col = _Color;

				i.texcoord.y -=  _Time.y/2;

				half tnormal1 = tex2D(_Noise,i.texcoord * _NoiseOffset.xy);

				i.texcoord.x += _SinTime.y/10;
				i.texcoord.y += _Time.y/10;

				half tnormal2 = tex2D(_Noise,i.texcoord * _NoiseOffset.xy);

				half norm = (tnormal1 - tnormal2);
				i.texcoord1.y -= norm * _NoiseOffset.w;
				i.texcoord1.x -= norm * _NoiseOffset.w;
				edge =  (1-tex2D(_WaterEdge,i.texcoord1)) * _EdgeColor;

				fixed4 cube = texCUBE(_ToonShade,float3(i.cubenormal - (norm * _NoiseOffset.z)) );
				fixed4 c = fixed4(cube.rgb + _Color + edge.rgb,1);

				UNITY_APPLY_FOG(i.fogCoord, c);

				return c;
			}
			ENDCG			
		}
	} 

	Fallback "VertexLit"
}



//			fixed4 frag (v2f i) : SV_Target
//			{
//				fixed distort = tex2D(_Distort, fixed2(i.texcoord1.x,i.texcoord1.y-_Time.x/2)).r;
//
//				fixed4 col = _Color * tex2D(_MainTex,i.texcoord);
//
////				i.texcoord.x -= distort*_DistortX;
////				i.texcoord.y -= distort*_DistortY + _Time.y/2;
////				i.texcoord *= _NoiseOffset.xy;
//
//				i.texcoord.y -=  _Time.y/2;
//
//				half3 tnormal1 = tex2D(_Noise,i.texcoord * _NoiseOffset.xy);
//
//				i.texcoord.x += _SinTime.y/10;
//				i.texcoord.y += _Time.y/10;
//
//				half3 tnormal2 = tex2D(_Noise,i.texcoord * _NoiseOffset.xy);
//
//				i.texcoord1.y -= distort*0.15;
//				fixed4 edge =  (1-tex2D(_WaterEdge,i.texcoord1)) * _EdgeColor;
//
//				fixed4 cube = texCUBE(_ToonShade,float3(i.cubenormal - ((tnormal1 - tnormal2) * _NoiseOffset.z)) ) * _ToonColor;
//				fixed4 c = fixed4(cube.rgb + col + edge.rgb,1);
//
//				UNITY_APPLY_FOG(i.fogCoord, c);
//
//				return c;
//			}



//fixed4 frag (v2f i) : SV_Target
//			{
//				fixed distort = tex2D(_Distort, fixed2(i.texcoord1.x,i.texcoord1.y-_Time.x/2)).r;
//
//				i.texcoord.x -= distort*_DistortX;
//				i.texcoord.y -= distort*_DistortY + _Time.y/2;
//
////				half3 tnormal1 = UnpackNormal(tex2D(_Noise,fixed2(i.texcoord.x + 0.1,i.texcoord.y - _Time.y/2) * _NoiseOffset.xy));
////				half3 tnormal2 = UnpackNormal(tex2D(_Noise,fixed2(i.texcoord.x,i.texcoord.y - _Time.y/3) * _NoiseOffset.xy)) ;
//				half3 tnormal1 = UnpackNormal(tex2D(_Noise,i.texcoord * _NoiseOffset.xy));
//				fixed4 col = _Color * tex2D(_MainTex,i.texcoord);
//				i.texcoord.x += 0.1;
//				i.texcoord.y += _Time.y/10;
//				half3 tnormal2 = UnpackNormal(tex2D(_Noise,i.texcoord * _NoiseOffset.xy)) ;
//
//
////				fixed noise1 = tex2D(_Noise,fixed2(i.texcoord.x-distort*_DistortX + _NoiseOffset.z,i.texcoord.y-distort*_DistortY-_Time.y/4) * _NoiseOffset.xy).a;
////				fixed noise2 = tex2D(_Noise,fixed2(i.texcoord.x-distort*_DistortX,i.texcoord.y-distort*_DistortY-_Time.y/2) * _NoiseOffset.xy).a;
////				fixed4 noise = (noise1  (1-noise2)) * 20 ;
//
//				fixed4 edge =  (1-tex2D(_WaterEdge,fixed2(i.texcoord1.x-distort*0,i.texcoord1.y-distort*0.15 ))) * _EdgeColor;
////				fixed4 col = _Color * tex2D(_MainTex,fixed2(i.texcoord.x,i.texcoord.y - _Time.y/2));
//				
//				fixed4 cube = texCUBE(_ToonShade,float3(i.cubenormal - ((tnormal1 - tnormal2) * _NoiseOffset.z)) ) * _ToonColor;
//				fixed4 c = fixed4(cube.rgb + col + edge.rgb,1);
//
//				UNITY_APPLY_FOG(i.fogCoord, c);
//
//				return c;
//			}
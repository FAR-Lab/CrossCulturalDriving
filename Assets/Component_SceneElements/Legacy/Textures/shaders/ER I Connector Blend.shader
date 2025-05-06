// © 2021 EasyRoads3D
// This shader can be used for road material transitions on I Connectors
// Standard 3D Project Usage: Set Material Render Queue to AlphaTest 2450 

Shader "EasyRoads3D/ER I Connector Blend"
{
	Properties
	{
		[Space]
		[Header(Road 1)]
		[Space]
		_MainTex("Albedo", 2D) = "white" {}
		_Color0("Color", Color) = (1,1,1,1)
		_MetallicGlossMap("Metallic (R) AO (G) Smoothness (A)", 2D) = "gray" {}
		_MainMetallicPower3("Metallic Power", Range( 0 , 2)) = 0
		_MainSmoothnessPower3("Smoothness Power", Range( 0 , 2)) = 1
		_OcclusionStrength3("Ambient Occlusion Power", Range( 0 , 2)) = 1
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale1("Normal Map Scale", Range( 0 , 4)) = 1
		[Space]
		[Space]
		[Header(Road 2)]
		[Space]
		_Albedo("Albedo", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_MetallicGlossMap2("Metallic (R) AO (G) Smoothness (A)", 2D) = "gray" {}
		_MainMetallicPower4("Metallic Power", Range( 0 , 2)) = 0
		_MainSmoothnessPower4("Smoothness Power", Range( 0 , 2)) = 1
		_OcclusionStrength4("Ambient Occlusion Power", Range( 0 , 2)) = 1
		_BumpMap2("Normal Map", 2D) = "bump" {}
		_BumpScale2("Normal Map Scale", Range( 0 , 4)) = 1
		[Space]
		[Space]
		[Header(Blend Level)]
		_Threshold("Distance", Range( 0.1 , 1)) = 0.5
		[Space]
		[Space]
		[Header(Terrain Z Fighting Offset)]
		[Space]
		_OffsetFactor ("Offset Factor", Range(0.0,-10.0)) = -1
        _OffsetUnit ("Offset Unit", Range(0.0,-10.0)) = -1
		[HideInInspector] _texcoord4( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+2450" "IgnoreProjector" = "True" }
		LOD 200
		Cull Back
		Offset  [_OffsetFactor1] , [_OffsetUnit1]
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
			float2 uv4_texcoord4;
			float4 vertexColor : COLOR;
		};

		uniform half _BumpScale1;
		uniform sampler2D _BumpMap;
		uniform half _BumpScale2;
		uniform sampler2D _BumpMap2;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float4 _Color0;
		uniform sampler2D _Albedo;
		uniform float4 _Color;
		uniform float _Threshold;
		uniform sampler2D _MetallicGlossMap;
		uniform float4 _MetallicGlossMap_ST;
		uniform half _MainMetallicPower3;
		uniform sampler2D _MetallicGlossMap2;
		uniform half _MainMetallicPower4;
		uniform half _OcclusionStrength3;
		uniform half _OcclusionStrength4;
		uniform half _MainSmoothnessPower3;
		uniform half _MainSmoothnessPower4;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = BlendNormals( UnpackScaleNormal( tex2D( _BumpMap, i.uv_texcoord ), _BumpScale1 ) , UnpackScaleNormal( tex2D( _BumpMap2, i.uv4_texcoord4 ), _BumpScale2 ) );
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode1 = tex2D( _MainTex, uv_MainTex );
			float temp_output_22_0 = ( i.vertexColor.a / _Threshold );
			float4 lerpResult16 = lerp( ( tex2DNode1 * _Color0 ) , ( tex2D( _Albedo, i.uv4_texcoord4 ) * _Color ) , temp_output_22_0);
			o.Albedo = lerpResult16.rgb;
			float2 uv_MetallicGlossMap = i.uv_texcoord * _MetallicGlossMap_ST.xy + _MetallicGlossMap_ST.zw;
			float4 tex2DNode2 = tex2D( _MetallicGlossMap, uv_MetallicGlossMap );
			float4 tex2DNode25 = tex2D( _MetallicGlossMap2, i.uv4_texcoord4 );
			float lerpResult26 = lerp( ( tex2DNode2.r * _MainMetallicPower3 ) , ( tex2DNode25.r * _MainMetallicPower4 ) , temp_output_22_0);
			o.Metallic = lerpResult26;
			float lerpResult27 = lerp( ( tex2DNode2.a * _OcclusionStrength3 ) , ( tex2DNode25.a * _OcclusionStrength4 ) , temp_output_22_0);
			o.Smoothness = lerpResult27;
			float lerpResult29 = lerp( ( tex2DNode2.g * _MainSmoothnessPower3 ) , ( tex2DNode25.g * _MainSmoothnessPower4 ) , temp_output_22_0);
			o.Occlusion = lerpResult29;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	
}
Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
        
        _SandTex ("Sand", 2D) = "white" {}
        _SandBumpMap ("SandNormal", 2D) = "bump" {}
        
        _DirtTex ("Dirt", 2D) = "white" {}
        _DirtBumpMap ("DirtNormal", 2D) = "bump" {}
        
        _RoadTex ("Road", 2D) = "white" {}
        _RoadBumpMap ("RoadNormal", 2D) = "bump" {}
        
        _TopRockTex ("TopRock", 2D) = "white" {}
        _TopRockBumpMap ("TopRockNormal", 2D) = "bump" {}
        
        _ReliefMap ("Reliefmap", 2D) = "black" {}
        _RestrictionMap ("Restriction", 2D) = "white" {}
        
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        
        sampler2D _SandTex;
        sampler2D _SandBumpMap;
        
        sampler2D _TopRockTex;
        sampler2D _TopRockBumpMap;
        
        sampler2D _RoadTex;
        sampler2D _RoadBumpMap;
        
        sampler2D _DirtTex;
        sampler2D _DirtBumpMap;
        
        sampler2D _ReliefMap;
        sampler2D _RestrictionMap;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float4 c;

            float height = tex2D (_ReliefMap, IN.uv_MainTex).r;
            

            float sandAmount = (height <= 0.09) + (height > 0.09 && height < 0.1) * (1-(height-0.09) * 100);
            float grassAmount = (height > 0.1 && height < 0.24) + (height > 0.24 && height < 0.25) * (1-(height-0.24) * 100) + (height > 0.09 && height < 0.1) * ((height-0.09) * 100);
            
            float topRockAmount = (height > 0.25) + (height > 0.24 && height < 0.25) * ((height-0.24) * 100);

            
            float relief = clamp(0, 1,
                abs(height - tex2D (_ReliefMap, IN.uv_MainTex - float2(0.001, 0)).r) +
                abs(height - tex2D (_ReliefMap, IN.uv_MainTex + float2(0.001, 0)).r) +
                abs(height - tex2D (_ReliefMap, IN.uv_MainTex - float2(0, 0.001)).r) +
                abs(height - tex2D (_ReliefMap, IN.uv_MainTex + float2(0, 0.001)).r)
                );

            float reliefTextureWeight = (relief > 0.01) + (relief < 0.01 && relief > 0.005) * (1-(0.01 - relief) / 0.005); 
            float dirtAmount = reliefTextureWeight;
            sandAmount *= 1-reliefTextureWeight;
            grassAmount *= 1-reliefTextureWeight;
            topRockAmount *= 1-reliefTextureWeight;

            
            float restriction = tex2D (_RestrictionMap, IN.uv_MainTex).r;
            dirtAmount *= restriction;
            sandAmount *= restriction;
            grassAmount *= restriction;
            topRockAmount *= restriction;
            float roadAmount = 1-restriction;
            

            o.Normal =  UnpackNormal(tex2D (_BumpMap, IN.uv_MainTex*30)) * grassAmount +
                        UnpackNormal(tex2D (_SandBumpMap, IN.uv_MainTex*30)) * sandAmount +
                        UnpackNormal(tex2D (_DirtBumpMap, IN.uv_MainTex*30)) * dirtAmount +
                        UnpackNormal(tex2D (_RoadBumpMap, IN.uv_MainTex*60)) * roadAmount +
                        UnpackNormal(tex2D (_TopRockBumpMap, IN.uv_MainTex*60)) * topRockAmount;
            
            c = tex2D (_MainTex, IN.uv_MainTex*30) * grassAmount +
                tex2D (_SandTex, IN.uv_MainTex*30) * sandAmount +
                tex2D (_DirtTex, IN.uv_MainTex*30) * dirtAmount+
                tex2D (_RoadTex, IN.uv_MainTex*60) * roadAmount+
                tex2D (_TopRockTex, IN.uv_MainTex*60) * topRockAmount;
            c *= _Color;

            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        
        _Layer1Tex ("Layer1", 2D) = "white" {}
        _Layer1BumpMap ("Layer1Normal", 2D) = "bump" {}
        
        _Layer2Tex ("Layer2", 2D) = "white" {}
        _Layer2BumpMap ("Layer2Normal", 2D) = "bump" {}
        
        _SteepTex ("Steep", 2D) = "white" {}
        _SteepBumpMap ("SteepNormal", 2D) = "bump" {}
        
        _RoadTex ("Road", 2D) = "white" {}
        _RoadBumpMap ("RoadNormal", 2D) = "bump" {}
        
        _Layer3Tex ("Layer3", 2D) = "white" {}
        _Layer3BumpMap ("Layer3Normal", 2D) = "bump" {}
        
        _MainTex ("Reliefmap", 2D) = "black" {}
        _RestrictionMap ("Restriction", 2D) = "white" {}
        
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _grassLine ("grassLine", Range(0, 1)) = 0.1
        _grassMergeSize ("grassMergeSize", Range(0, 0.3)) = 0.01
        _gravelLine ("gravelLine", Range(0, 1)) = 0.25
        _gravelMergeSize ("gravelMergeSize", Range(0, 0.3)) = 0.02
        _rockLine ("rockLine", Range(0, 1)) = 0.3
        _rockMergeSize ("rockMergeSize", Range(0, 0.3)) = 0.05
        _reliefLimit ("reliefLimit", Range(0, 0.1)) = 0.01
        _reliefMergeSize ("reliefMergeSize", Range(0, 0.03)) = 0.003
        
        
        _Layer1Scale ("Layer 1 Scale", Range(0.1, 200)) = 30
        _Layer2Scale ("Layer 2 Scale", Range(0.1, 200)) = 30
        _Layer3Scale ("Layer 3 Scale", Range(0.1, 200)) = 30
        _RoadScale ("Road Scale", Range(0.1, 200)) = 60
        _SteepScale ("Steep Scale", Range(0.1, 200)) = 30
        
        _UseSteepLayer ("UseSteepLayer", Range(0, 1)) = 1
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

        sampler2D _Layer2Tex;
        sampler2D _Layer2BumpMap;
        
        sampler2D _Layer1Tex;
        sampler2D _Layer1BumpMap;
        
        sampler2D _Layer3Tex;
        sampler2D _Layer3BumpMap;
        
        sampler2D _RoadTex;
        sampler2D _RoadBumpMap;
        
        sampler2D _SteepTex;
        sampler2D _SteepBumpMap;
        
        sampler2D _MainTex;
        sampler2D _RestrictionMap;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        half _grassLine;
        half _grassMergeSize;
        half _gravelLine;
        half _gravelMergeSize;
        half _rockLine;
        half _rockMergeSize;
        half _reliefLimit;
        half _reliefMergeSize;

        half _Layer1Scale;
        half _Layer2Scale;
        half _Layer3Scale;
        half _RoadScale;
        half _SteepScale;

        int _UseSteepLayer;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float weighter(float height, float lower, float lowerMerge, float upper, float mergeSize)
        {
            if (height < upper-mergeSize && (lower == 0 || lowerMerge == 0))
            {
                return 1;
            }
            return (height > lower && height < upper-mergeSize)
                + (height > (lower-lowerMerge) && height < lower && lowerMerge > 0) * ((height-(lower-lowerMerge)) / lowerMerge)
                + (height > (upper-mergeSize) && height < upper) * (1-(height-(upper-mergeSize)) / mergeSize);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float4 c;

            float height = tex2D (_MainTex, IN.uv_MainTex).r;

            float grassLine = _grassLine;
            float grassMergeSize = _grassMergeSize;

            
            float gravelLine = _gravelLine;
            float gravelMergeSize = _gravelMergeSize;
            
            float rockLine = _rockLine;
            float rockMergeSize = _rockMergeSize;

            
            float reliefLimit = _reliefLimit;
            float reliefMergeSize = _reliefMergeSize;

            int _NotUseSteepLayer = 1-_UseSteepLayer;

            float sandAmount = weighter(height, 0, 0, grassLine, grassMergeSize);
            float grassAmount = weighter(height, grassLine, grassMergeSize, gravelLine*_UseSteepLayer+rockLine*_NotUseSteepLayer, gravelMergeSize*_UseSteepLayer+rockMergeSize*_NotUseSteepLayer);
            float dirt2Amount = weighter(height, gravelLine, gravelMergeSize, rockLine, rockMergeSize);
            float topRockAmount = weighter(height, rockLine, rockMergeSize, 1, 0);

            
            float relief = clamp(0, 1,
                abs(height - tex2D (_MainTex, IN.uv_MainTex - float2(0.001, 0)).r) +
                abs(height - tex2D (_MainTex, IN.uv_MainTex + float2(0.001, 0)).r) +
                abs(height - tex2D (_MainTex, IN.uv_MainTex - float2(0, 0.001)).r) +
                abs(height - tex2D (_MainTex, IN.uv_MainTex + float2(0, 0.001)).r)
                );

            float reliefTextureWeight = (relief > reliefLimit)
                + (relief < reliefLimit && relief > reliefLimit-reliefMergeSize) * ((relief - (reliefLimit - reliefMergeSize)) / reliefMergeSize);

            reliefTextureWeight *= (height < gravelLine && reliefTextureWeight < 0.7) * sqrt(reliefTextureWeight) + (height >= gravelLine || reliefTextureWeight >= 0.7);
            float dirtAmount = reliefTextureWeight;
            sandAmount *= 1-reliefTextureWeight;
            grassAmount *= 1-reliefTextureWeight;
            topRockAmount *= 1-reliefTextureWeight;
            dirt2Amount *= 1-reliefTextureWeight;

            
            float restriction = tex2D (_RestrictionMap, IN.uv_MainTex).r;
            dirtAmount *= restriction;
            sandAmount *= restriction;
            grassAmount *= restriction;
            topRockAmount *= restriction;
            dirt2Amount *= restriction;
            float roadAmount = 1-restriction;
            

            o.Normal =  UnpackNormal(tex2D (_Layer2BumpMap, IN.uv_MainTex*_Layer2Scale)) * grassAmount +
                        UnpackNormal(tex2D (_Layer1BumpMap, IN.uv_MainTex*_Layer1Scale)) * sandAmount +
                        UnpackNormal(tex2D (_SteepBumpMap, IN.uv_MainTex*_SteepScale)) * (dirtAmount + dirt2Amount) +
                        UnpackNormal(tex2D (_RoadBumpMap, IN.uv_MainTex*_RoadScale)) * roadAmount +
                        UnpackNormal(tex2D (_Layer3BumpMap, IN.uv_MainTex*_Layer3Scale)) * topRockAmount;
            
            c = tex2D (_Layer2Tex, IN.uv_MainTex*_Layer2Scale) * grassAmount +
                tex2D (_Layer1Tex, IN.uv_MainTex*_Layer1Scale) * sandAmount +
                tex2D (_SteepTex, IN.uv_MainTex*_SteepScale) * (dirtAmount + dirt2Amount)+
                tex2D (_RoadTex, IN.uv_MainTex*_RoadScale) * roadAmount+
                tex2D (_Layer3Tex, IN.uv_MainTex*_Layer3Scale) * topRockAmount;
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

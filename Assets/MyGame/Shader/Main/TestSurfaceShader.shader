Shader "Custom/TestSurfaceShader"
{
    Properties
    {
        [Header(BaseColor)]
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

        [Header(Alpha)]
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

            
        [Header(Metallic)]
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

        [Header(Smoothness)]
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        [Header(NormalMap)]
        [Toggle(_NORMALMAP)]_NORMALMAP("_NORMALMAP?", Float) = 1
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Scale", Float) = 1.0

        [Header(SharedDataTexture)]
        _MetallicR_OcclusionG_SmoothnessA_Tex("_MetallicR_OcclusionG_SmoothnessA_Tex", 2D) = "white" {}

        [Header(Emission)]
        [HDR]_EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        [Header(Example_GameplayUse_FinalColorOverride)]
        [Toggle(_IsSelected)]_IsSelected("_IsSelected?", Float) = 0
        [HDR]_SelectedLerpColor("_SelectedLerpColor", Color) = (1,0,0,0.8)

        [Header(VertAnim)]
        _NoiseStrength("_NoiseStrength", Range(-4,4)) = 0
        [Header(Outline)]
        _OutlineWidthOS("_OutlineWidthOS", Range(0,4)) = 0
    }

    HLSLINCLUDE

    #pragma prefer_hlslcc gles
    #pragma exclude_renderers d3d11_9x
    #pragma target 2.0
    #pragma multi_compile_fog
    #pragma multi_compile_instancing

    #pragma multi_compile _ LIGHTMAP_ON
    #pragma multi_compile _ DIRLIGHTMAP_COMBINED
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
    #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
    #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
    #pragma multi_compile _ _SHADOWS_SOFT
    #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
    #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
    #pragma shader_feature _NORMALMAP 
    #pragma multi_compile _ _IsSelected
    
    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    TEXTURE2D(_BumpMap);
    SAMPLER(sampler_BumpMap);
    TEXTURE2D(_MetallicR_OcclusionG_SmoothnessA_Tex);
    SAMPLER(sampler_MetallicR_OcclusionG_SmoothnessA_Tex);
    TEXTURE2D(_EmissionMap);
    SAMPLER(sampler_EmissionMap);

    CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Metallic;
    half _Smoothness;
    float4 _BumpMap_ST;
    half _BumpScale;
    half4 _EmissionColor;
    half _Cutoff;
    float _OutlineWidthOS;
    half4 _SelectedLerpColor;
    float _NoiseStrength;
    CBUFFER_END

    struct Attributes
    {
        float3  positionOS  : POSITION;
        float3  normalOS    : NORMAL;
        float4  tangentOS   : TANGENT;
        half4   color       : COLOR;
        float2  uv          : TEXCOORD0;
        float2  uv2         : TEXCOORD1; 

        float2  uv3         : TEXCOORD2;
        float2  uv4         : TEXCOORD3;
        float2  uv5         : TEXCOORD4;
        float2  uv6         : TEXCOORD5;
        float2  uv7         : TEXCOORD6;
        float2  uv8         : TEXCOORD7;

#if UNITY_ANY_INSTANCING_ENABLED
        uint instanceID : INSTANCEID_SEMANTIC; 
#endif 
    };

    struct Varyings
    {
        float2  uv                          : TEXCOORD0;
        float2  uv2                         : TEXCOORD1; 
        float4  uv34                        : TEXCOORD2;
        float4  uv56                        : TEXCOORD3;
        float4  uv78                        : TEXCOORD4;

        float4  positionWSAndFogFactor      : TEXCOORD5;

        half3   normalWS                    : NORMAL;
        half3   tangentWS                   : TANGENT;
        half3   bitangentWS                 : TEXCOORD6;

        half4   color                       : COLOR;

        float4  positionCS                  : SV_POSITION;

#if UNITY_ANY_INSTANCING_ENABLED
        uint instanceID : CUSTOM_INSTANCE_ID;
#endif

#if VARYINGS_NEED_CULLFACE
        FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
    };

    float3 _LightDirection; 

    float4 GetShadowPositionHClip(Varyings input)
    {
        float3 positionWS = input.positionWSAndFogFactor.xyz;
        float3 normalWS = input.normalWS;

        float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
        positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
        positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif

        return positionCS;
    }

    struct UserGeometryOutputData
    {
        float3 positionOS;
        float3 normalOS;
        float4 tangentOS;
    };
    void UserGeometryDataOutputFunction(Attributes IN, inout UserGeometryOutputData outputData, bool isExtraCustomPass);

    UserGeometryOutputData BuildUserGeometryOutputData(Attributes IN, bool isExtraCustomPass = false)
    {
        UserGeometryOutputData outputData;

        outputData.positionOS = IN.positionOS.xyz;
        outputData.normalOS = IN.normalOS;
        outputData.tangentOS = IN.tangentOS;

        UserGeometryDataOutputFunction(IN, outputData, isExtraCustomPass);

        return outputData;
    }
    Varyings VertAllWork(Attributes IN, bool shouldApplyShadowBias = false, bool isExtraCustomPass = false)
    {
        UserGeometryOutputData geometryData = BuildUserGeometryOutputData(IN, isExtraCustomPass);

        Varyings OUT;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(geometryData.positionOS.xyz);

        VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(geometryData.normalOS, geometryData.tangentOS);

        OUT.uv = IN.uv;
#if LIGHTMAP_ON
        OUT.uv2 = IN.uv2.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#else
        OUT.uv2 = IN.uv2;
#endif
        OUT.uv34 = float4(IN.uv3, IN.uv4);
        OUT.uv56 = float4(IN.uv5, IN.uv6);
        OUT.uv78 = float4(IN.uv7, IN.uv8);

        OUT.color = IN.color;

        float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
        OUT.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor); 

        OUT.normalWS = vertexNormalInput.normalWS;
        OUT.tangentWS = vertexNormalInput.tangentWS;
        OUT.bitangentWS = vertexNormalInput.bitangentWS;

        OUT.positionCS = vertexInput.positionCS;

        if (shouldApplyShadowBias)
        {
            OUT.positionCS = GetShadowPositionHClip(OUT);
        }
        return OUT;
    }

    Varyings vertUniversalForward(Attributes IN)
    {
        return VertAllWork(IN);
    }
    Varyings vertShadowCaster(Attributes IN)
    {
        return VertAllWork(IN, true, false);
    }
    Varyings vertExtraCustomPass(Attributes IN)
    {
        return VertAllWork(IN, false, true);
    }
#if SHADER_LIBRARY_VERSION_MAJOR < 9
    float3 GetWorldSpaceViewDir(float3 positionWS)
    {
        if (unity_OrthoParams.w == 0)
        {
            return _WorldSpaceCameraPos - positionWS;
        }
        else
        {
            float4x4 viewMat = GetWorldToViewMatrix();
            return viewMat[2].xyz;
        }
    }
#endif
    struct UserSurfaceOutputData
    {
        half3   albedo;
        half3   normalTS;
        half3   emission;
        half    metallic;
        half    smoothness;
        half    occlusion;
        half    alpha;
        half    alphaClipThreshold;
    };

    struct LightingsData
    {
        Light   mainDirectionalLight;   
        int     additionalLightCount;   
        half3   bakedIndirectDiffuse;   
        half3   bakedIndirectSpecular;  
        half3   viewDirectionWS;
        half3   reflectionDirectionWS;
        half3   normalWS;
        float3  positionWS;
    };

    void UserSurfaceOutputDataFunction(Varyings IN, inout UserSurfaceOutputData surfaceData, bool isExtraCustomPass);

    UserSurfaceOutputData BuildUserSurfaceOutputData(Varyings IN, bool isExtraCustomPass)
    {
        UserSurfaceOutputData surfaceData;

        surfaceData.albedo = 1;                 
        surfaceData.normalTS = half3(0, 0, 1);  
        surfaceData.emission = 0;               
        surfaceData.metallic = 0;               
        surfaceData.smoothness = 0.5;           
        surfaceData.occlusion = 1;              
        surfaceData.alpha = 1;                  
        surfaceData.alphaClipThreshold = 0;     

        UserSurfaceOutputDataFunction(IN, surfaceData, isExtraCustomPass);

        surfaceData.albedo = max(0, surfaceData.albedo);
        surfaceData.normalTS = normalize(surfaceData.normalTS);
        surfaceData.emission = max(0, surfaceData.emission);
        surfaceData.metallic = saturate(surfaceData.metallic);
        surfaceData.smoothness = saturate(surfaceData.smoothness);
        surfaceData.occlusion = saturate(surfaceData.occlusion);
        surfaceData.alpha = saturate(surfaceData.alpha);
        surfaceData.alphaClipThreshold = saturate(surfaceData.alphaClipThreshold);

        return surfaceData;
    }

    half4 CalculateSurfaceFinalResultColor(Varyings IN, UserSurfaceOutputData surfaceData, LightingsData lightingData);

    void FinalPostProcessFrag(Varyings IN, UserSurfaceOutputData surfaceData, LightingsData lightingData, inout half4 inputColor)
    {
#if _IsSelected
        inputColor.rgb = lerp(inputColor.rgb, _SelectedLerpColor.rgb, _SelectedLerpColor.a * (sin(_Time.y * 5) * 0.5 + 0.5));
#endif
    }

    half4 fragAllWork(Varyings IN, bool shouldOnlyDoAlphaClipAndEarlyExit = false, bool isExtraCustomPass = false)
    {
        IN.normalWS = normalize(IN.normalWS);
        IN.tangentWS.xyz = normalize(IN.tangentWS);
        IN.bitangentWS = normalize(IN.bitangentWS);

        UserSurfaceOutputData surfaceData = BuildUserSurfaceOutputData(IN, isExtraCustomPass);

        clip(surfaceData.alpha - surfaceData.alphaClipThreshold);

        if (shouldOnlyDoAlphaClipAndEarlyExit)
        {
            return 0;
        }

        LightingsData lightingData;
        half3 T = IN.tangentWS.xyz;
        half3 B = IN.bitangentWS;
        half3 N = IN.normalWS;

        lightingData.normalWS = TransformTangentToWorld(surfaceData.normalTS, half3x3(T, B, N));

        float3 positionWS = IN.positionWSAndFogFactor.xyz;
        half3 viewDirectionWS = normalize(GetWorldSpaceViewDir(positionWS));
        half3 reflectionDirectionWS = reflect(-viewDirectionWS, lightingData.normalWS);

        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
        lightingData.mainDirectionalLight = GetMainLight(shadowCoord);

        lightingData.bakedIndirectDiffuse = SAMPLE_GI(IN.uv2, SampleSH(lightingData.normalWS), lightingData.normalWS);

        lightingData.bakedIndirectSpecular = GlossyEnvironmentReflection(reflectionDirectionWS, 1 - surfaceData.smoothness, 1);

        lightingData.viewDirectionWS = viewDirectionWS;
        lightingData.reflectionDirectionWS = reflectionDirectionWS;

        lightingData.additionalLightCount = GetAdditionalLightsCount();
        lightingData.positionWS = positionWS;

        half4 finalColor = CalculateSurfaceFinalResultColor(IN, surfaceData, lightingData);
        FinalPostProcessFrag(IN, surfaceData, lightingData, finalColor);

        return finalColor;
    }
    half4 fragUniversalForward(Varyings IN) : SV_Target
    {
        return fragAllWork(IN);
    }
        half4 fragDoAlphaClipOnlyAndEarlyExit(Varyings IN) : SV_Target
    {
        return fragAllWork(IN, true);
    }
        half4 fragExtraCustomPass(Varyings IN) : SV_Target
    {
        return fragAllWork(IN, false, true);
    }

    half3 DirectBDRFCelShade(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
    {
#ifndef _SPECULARHIGHLIGHTS_OFF
        float3 halfDir = SafeNormalize(float3(lightDirectionWS)+float3(viewDirectionWS));

        float NoH = saturate(dot(normalWS, halfDir));
        half LoH = saturate(dot(lightDirectionWS, halfDir));

        float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

        half LoH2 = LoH * LoH;
        half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);
        specularTerm = floor(specularTerm + 0.5);
#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
        specularTerm = specularTerm - HALF_MIN;
        specularTerm = clamp(specularTerm, 0.0, 100.0); 
#endif

        half3 color = specularTerm * brdfData.specular + brdfData.diffuse;
        return color;
#else
        return brdfData.diffuse;
#endif
    }

    half3 LightingPhysicallyBasedCelShade(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS)
    {
        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        NdotL = smoothstep(0.45, 0.5, NdotL);
        half3 radiance = lightColor * (lightAttenuation * NdotL);
        return DirectBDRFCelShade(brdfData, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
    }
    half3 LightingPhysicallyBasedCelShade(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS)
    {
        light.shadowAttenuation = smoothstep(0.45, 0.5, light.shadowAttenuation);
        return LightingPhysicallyBasedCelShade(brdfData, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS);
    }

    half4 CalculateSurfaceFinalResultColor(Varyings IN, UserSurfaceOutputData surfaceData, LightingsData lightingData)
    {
        BRDFData brdfData;
        InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, 0, surfaceData.smoothness, surfaceData.alpha, brdfData);

        half3 rgb = GlobalIllumination(brdfData, lightingData.bakedIndirectDiffuse, surfaceData.occlusion, lightingData.normalWS, lightingData.viewDirectionWS);

        rgb += LightingPhysicallyBasedCelShade(brdfData, lightingData.mainDirectionalLight, lightingData.normalWS, lightingData.viewDirectionWS);

        int additionalLightCount = lightingData.additionalLightCount;
        for (int i = 0; i < additionalLightCount; i++)
        {
            Light light = GetAdditionalLight(i, lightingData.positionWS);
            rgb += LightingPhysicallyBasedCelShade(brdfData, light, lightingData.normalWS, lightingData.viewDirectionWS);
        }

        rgb += surfaceData.emission * surfaceData.occlusion;

        float fogFactor = IN.positionWSAndFogFactor.w;
        rgb = MixFog(rgb, fogFactor);

        return half4(rgb, surfaceData.alpha);
    }
    void UserGeometryDataOutputFunction(Attributes IN, inout UserGeometryOutputData geometryOutputData, bool isExtraCustomPass)
    {
        geometryOutputData.positionOS += sin(_Time.y * dot(float3(1, 1, 1), geometryOutputData.positionOS) * 10) * _NoiseStrength * 0.0125; 

        if (isExtraCustomPass)
        {
            geometryOutputData.positionOS += geometryOutputData.normalOS * _OutlineWidthOS * 0.025; 
        }
    }
    void UserSurfaceOutputDataFunction(Varyings IN, inout UserSurfaceOutputData surfaceData, bool isExtraCustomPass)
    {
        float2 uv = TRANSFORM_TEX(IN.uv, _BaseMap);
        float2 bumpUV = uv * _BumpMap_ST;
        half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
        surfaceData.albedo = color.rgb;
        surfaceData.alpha = color.a;
        surfaceData.alphaClipThreshold = _Cutoff;

#if _NORMALMAP
        surfaceData.normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, bumpUV), _BumpScale);
#endif

        half4 MetallicR_OcclusionG_SmoothnessA = SAMPLE_TEXTURE2D(_MetallicR_OcclusionG_SmoothnessA_Tex, sampler_MetallicR_OcclusionG_SmoothnessA_Tex, uv);
        surfaceData.occlusion = MetallicR_OcclusionG_SmoothnessA.g; 
        surfaceData.metallic = _Metallic * MetallicR_OcclusionG_SmoothnessA.r; 
        surfaceData.smoothness = _Smoothness * MetallicR_OcclusionG_SmoothnessA.a; 

        surfaceData.emission = _EmissionColor.rgb * _EmissionColor.aaa * SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;

        if (isExtraCustomPass)
        {
            
            surfaceData.albedo = 0;
            surfaceData.smoothness = 0;
            surfaceData.metallic = 0;
            surfaceData.occlusion = 0;
        }
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
            "Queue" = "Geometry+0"
            "RenderType" = "Opaque"
            "DisableBatching" = "False"
            "ForceNoShadowCasting" = "False"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZTest LEqual
            ZWrite On
            Offset 0,0
            Blend One Zero
            ColorMask RGBA

            Stencil
            {
            
            }

            HLSLPROGRAM
            #pragma vertex vertUniversalForward
            #pragma fragment fragUniversalForward
            ENDHLSL
        }


        Pass
        {
            Cull front
            HLSLPROGRAM
            #pragma vertex vertExtraCustomPass
            #pragma fragment fragExtraCustomPass
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0 

            HLSLPROGRAM

            #pragma vertex vertShadowCaster
            #pragma fragment fragDoAlphaClipOnlyAndEarlyExit

            ENDHLSL
        }

        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0 

            HLSLPROGRAM
        
            #pragma vertex vertExtraCustomPass
            #pragma fragment fragDoAlphaClipOnlyAndEarlyExit

            ENDHLSL
        }
    }
}

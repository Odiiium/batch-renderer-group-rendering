Shader "Unlit/GrassWindInstanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [Space(20)]
        _WindTexture("Wind Texture", 2D) = "white" {}
        _WindTextureTiling ("Wind Texture Tiling", Range(0,1)) = .1
        _PlaneSize ("Plane Size", Float) = 25 
            
        [Space(20)]
        _WindStrength ("Wind Strength", Range(0,1)) = 0.1
        _WindSpeed ("Wind Speed", Range(0,5)) = 1
        _WindNoiseFrequency("Wind Noise Frequency", Range(100, 1000)) = 250

        [Space(20)]
        _AffectorStrength ("Affector Strength", Range(0, 1)) = .5
        _AffectorPosition ("Affector Position", Vector) = (0,0,0,0)
        _InteractionDistance ("Interaction Distance", Float) = 1
        _RefractCoefficient ("Refraction Coefficient", Range(.5, 5)) = 1
        _SqueezeCoefficient ("Squeeze Coefficient", Range(0.01, 3)) = .5
        _SquareAffectCoefficient ("Square Affection", Range(0.5, 10)) = 1 

        [Space(20)]
        _StaticGrassHeight("Static Grass Height", Float) = 1

        [Space(20)]
        _LowColor ("Low Color", Color) = (0,0,0,0)
        _MediumColor ("Medium Color", Color) = (0,0,0,0)
        _HighColor ("High Color", Color) = (0,0,0,0)
        _LightColor ("Light Color", Color) = (1,1,1,1)

        [Space(20)]
        _WorldSpaceLightPos ("World Space Light Position", Vector) = (0,0,0,0)

        //[Space(20)]
        //_Glosiness ("Glosiness", Float) = 1
        //_LightingIntensity ("Lighting Intensity", Float) = 1
    }

    SubShader
    {
        Cull Off

        Pass
        {
            Tags { "RenderType"="Opaque" }

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            static const float UNITY_PI = 3.1415926;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float4 ws_pos : TEXCOORD1;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            sampler2D _WindTexture;
            sampler2D _MainTex;


            CBUFFER_START(UnityPerMaterial)

                half _WindStrength;
                half _WindSpeed;
                half _WindTextureTiling;
                half _PlaneSize;
                half _WindNoiseFrequency;

                float4 _AffectorPosition;
                half _AffectorStrength;
                half _InteractionDistance;
                half _RefractCoefficient;
                half _SqueezeCoefficient;
                half _SquareAffectCoefficient;

                half _MinimalGrassHeight;
                half _MaximalGrassHeight;
                half _StaticGrassHeight;

                half _LightingIntensity;
                half _Glosiness;

                half4 _MainTex_ST;
                half4 _LowColor;
                half4 _MediumColor;
                half4 _HighColor;
                half4 _LightColor;
                half4 _WorldSpaceLightPos;


            CBUFFER_END

            #ifdef DOTS_INSTANCING_ON

                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _StaticGrassHeight)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _StaticGrassHeight UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _StaticGrassHeight)

            #endif


            float3x3 AngleAxis3x3(float angle, float3 axis)
            {
            	float c, s;
            	sincos(angle, s, c);
            
            	float t = 1 - c;
            	float x = axis.x;
            	float y = axis.y;
            	float z = axis.z;
            
            	return float3x3(
            		t * x * x + c, t * x * y - s * z, t * x * z + s * y,
            		t * x * y + s * z, t * y * y + c, t * y * z - s * x,
            		t * x * z - s * y, t * y * z + s * x, t * z * z + c
            		);
            }

            float simpleWind(float3 ws_pos, float scaleModifier, float timeScale, float windWidth01)
            {
                return sin((ws_pos.x * ws_pos.z + ws_pos.y * scaleModifier) / scaleModifier + _Time.w * timeScale);
            }
            
            float randFloat(float2 seed)
            {
                return sin(frac(seed.x * 132.169 + seed.y * 211.567));
            }
            
            half4 randomizeColor(float2 seed)
            {
                return half4(randFloat(seed * 12.157), randFloat(seed * 124.211), randFloat(seed + 285.317), 1);
            }
            
            float remap(float In, float2 InMinMax, float2 OutMinMax)
            {
                return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
            }
            
            float4 calculateAffectionVector(float uvY, float4 worldPos)
            {
                float affectorDistance = distance(worldPos.xz, _AffectorPosition.xz);
                float affectCoeff = affectorDistance > _InteractionDistance ? 0 : remap(affectorDistance, float2(0, _InteractionDistance), float2(1, 0));
                affectCoeff *= pow(_RefractCoefficient, uvY) * _AffectorStrength;
            
                float4 affectVector = (worldPos - _AffectorPosition) * affectCoeff;
            
                affectVector.xz /= _SquareAffectCoefficient;
                affectVector.y = -pow(uvY, _SqueezeCoefficient) * affectCoeff;
            
                return affectVector;
            }
            
            float4 calculateWind(float4 vertex, v2f o, float4 worldPos)
            {
                float2 texPos = worldPos.xz / _PlaneSize * _WindTextureTiling;
                float2 windUv = (texPos + float2(_Time.x, _Time.x)) * _WindSpeed % 1;
            
                float4 windPixel = tex2Dlod(_WindTexture, float4(windUv,0,0));
                float3 windVector = float3(windPixel.r, windPixel.g, windPixel.g * windPixel.r);
                float windStrength = windPixel.r * windPixel.g * _WindStrength; 
            
                windStrength *= lerp(0, 1, o.uv.y);
            
                float3x3 windRotationMatrix = AngleAxis3x3(UNITY_PI * windStrength, windVector);
                float4 vertexResult = float4(mul(windRotationMatrix, vertex), 1);
            
                return vertexResult - vertex;
            }

            float4 calculateSimpleWind(float4 vertex, v2f o, float4 worldPos)
            {
                float xWind = simpleWind(worldPos.xyz * 2, _WindNoiseFrequency, _WindSpeed, .6);
                float zWind = simpleWind(worldPos.zyx * 2, _WindNoiseFrequency, _WindSpeed, .6);

                float2 windVector = float2(xWind, zWind) * _WindStrength;
                windVector *= lerp(0, 1, o.uv.y);

                float3x3 windRotationMatrix = AngleAxis3x3(UNITY_PI * .5, float3(windVector.x, 1, windVector.y));
                float4 result = float4(mul(windRotationMatrix, vertex), 1);

                return result - vertex;
            }
            
            float getRandomGrassHeight(float seed)
            {
                float randSin = sin(seed + 11.245);
                randSin = remap(randSin, float2(-1, 1), float2(.2, .3));
                float interpolator = floor(seed * 3 + randSin);
                float result = lerp(_MinimalGrassHeight, _MaximalGrassHeight, interpolator);
                return result;
            }

            void setRandomRotationY(float seed, float2 remapBounds, inout float4 vertex)
            {
                float angle = 2 * UNITY_PI * remap(seed, remapBounds, float2(0,1)); 
                vertex.xyz = mul(AngleAxis3x3(angle, float3(0,1,0)), vertex.xyz);
            }

            float3 recalculateNormals(appdata v, v2f o, float4 vertex_ws_pos)
            {
                float4 normal = float4(v.normal, 1);
                float3 normal_ws_pos = mul(UNITY_MATRIX_M, v.normal);
                setRandomRotationY(_StaticGrassHeight, float2(.815, 1.23), v.vertex);

                normal += calculateSimpleWind(normal, o, vertex_ws_pos);
                normal += max(0,calculateAffectionVector(v.uv.y, vertex_ws_pos));
                
                return normal;
            }

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                v.vertex.y *= _StaticGrassHeight;
                float4 worldPos = mul(UNITY_MATRIX_M, v.vertex);
                float4 vertex = v.vertex; 

                o.normal = recalculateNormals(v, o, worldPos);

                setRandomRotationY(_StaticGrassHeight, float2(.815, 1.23), vertex);

                vertex += calculateSimpleWind(vertex, o, worldPos);
                vertex += calculateAffectionVector(v.uv.y, worldPos);
                vertex.y = v.uv.y > .1 ? vertex.y : 0;

                o.ws_pos = worldPos;
                o.vertex = mul(UNITY_MATRIX_MVP , vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = i.uv.y > .5 ?  lerp(_MediumColor, _HighColor, i.uv.y * 2 - 1) : lerp(_LowColor, _MediumColor, i.uv.y * 2);

                half4 ws_pos = i.ws_pos;


                half3 view = normalize(mul(UNITY_MATRIX_V,ws_pos));   
                half3 halfVector = normalize(_WorldSpaceLightPos + view);
                half NdotL = dot(_WorldSpaceLightPos, i.normal);
                
                half4 light = NdotL * _LightColor; 
                
                return col * light;
            }
            ENDHLSL
        }
    }
}

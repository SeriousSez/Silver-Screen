Shader "SilverScreen/Display Artwork Text"
{
    Properties { _MainTex ("Static font atlas", 2D) = "white" {} _Color ("Text color", Color) = (1,1,1,1) }
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color;
            struct Input { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Output Vert(Input v) { Output o; o.position = UnityObjectToClipPos(v.position); o.uv = v.uv; return o; }
            float4 Frag(Output i) : SV_Target
            {
                float distance = tex2D(_MainTex, i.uv).a;
                float edge = max(fwidth(distance), 0.001);
                return float4(_Color.rgb, _Color.a * smoothstep(0.5 - edge, 0.5 + edge, distance));
            }
            ENDHLSL
        }
    }
}

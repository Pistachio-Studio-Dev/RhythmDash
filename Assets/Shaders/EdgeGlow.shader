// [ Written by Jiayi ]

Shader "Rhythm/EdgeGlow"
{
    Properties
    {
        _MainTex  ("Texture", 2D)                = "white" {}
        _Color    ("Glow Color", Color)          = (1, 0.4, 0.1, 1)
        _Intensity("Intensity",  Float)          = 0
        _EdgeWidth("Edge Width", Range(0, 0.5))  = 0.12
        _Softness ("Softness",   Range(0.01, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;
            float  _Intensity;
            float  _EdgeWidth;
            float  _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dL = i.uv.x;
                float dR = 1.0 - i.uv.x;
                float dB = i.uv.y;
                float dT = 1.0 - i.uv.y;

                float edgeDist = min(min(dL, dR), min(dB, dT));
                
                float glow = 1.0 - smoothstep(0, _EdgeWidth + _EdgeWidth * _Softness, edgeDist);
                
                float rim = 1.0 - smoothstep(0, _EdgeWidth * 0.25, edgeDist);
                glow = max(glow, rim * 0.6);

                float alpha = glow * _Intensity;
                return float4(_Color.rgb * (1.0 + rim * 0.4), saturate(alpha * _Color.a));
            }
            ENDCG
        }
    }
    FallBack Off
}

Shader "VideoPlaneNoLight" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" { }
	}
	SubShader {
		Pass {
			Material {
				Diffuse [_Color]
			}
			Lighting Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			SeparateSpecular Off
			SetTexture [_MainTex] {
				constantColor [_Color]
				Combine texture * constant, texture * constant
			}
		}
	}
}
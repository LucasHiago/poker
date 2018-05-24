layout(location=0) in vec3 position_in;
layout(location=1) in vec3 normal_in;
layout(location=2) in vec3 tangent_in;
layout(location=3) in vec2 texCoord_in;

layout(location=0) out vec3 normal_out;
layout(location=1) out vec3 tangent_out;
layout(location=2) out vec2 texCoord_out;
layout(location=3) out vec3 worldPos_out;

uniform float textureScale;

#include "ViewProj.glh"

void main()
{
	normal_out = normal_in;
	tangent_out = tangent_in;
	texCoord_out = texCoord_in * textureScale;
	worldPos_out = position_in;
	
	gl_Position = viewProjTransform * vec4(position_in, 1.0);
}

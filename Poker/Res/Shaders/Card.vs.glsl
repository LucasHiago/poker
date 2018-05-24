layout(location=0) in vec2 position_in;

layout(location=0) out vec2 texCoord_out;
layout(location=1) out vec3 worldPos_out;
layout(location=2) out vec3 normal_out;

uniform mat4 worldTransform;

#include "ViewProj.glh"

void main()
{
	normal_out = (worldTransform * vec4(0, 1, 0, 0)).xyz;
	
	texCoord_out = (position_in + 1.0) / 2.0;
	texCoord_out.y = 1.0 - texCoord_out.y;
	
	worldPos_out = (worldTransform * vec4(position_in.x, 0.0, position_in.y, 1.0)).xyz;
	gl_Position = viewProjTransform * vec4(worldPos_out, 1.0);
}
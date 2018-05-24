layout(location=0) in vec3 position_in;
layout(location=1) in vec2 texCoord_in;
layout(location=2) in uint playerIndex_in;

layout(location=0) out vec2 texCoord_out;
layout(location=1) out vec4 color_out;

#include "ViewProj.glh"

layout(binding=1, std140) uniform ColorsUB
{
	vec4 colors[10];
};

void main()
{
	gl_Position = viewProjTransform * vec4(position_in, 1.0);
	texCoord_out = texCoord_in;
	color_out = colors[playerIndex_in];
}
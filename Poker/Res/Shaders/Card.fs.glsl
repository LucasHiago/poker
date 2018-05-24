layout(location=0) in vec2 texCoord_in;
layout(location=1) in vec3 worldPos_in;
layout(location=2) in vec3 normal_in;

layout(location=0) out vec4 color_out;

layout(binding=0) uniform sampler2D frontTexSampler;
layout(binding=1) uniform sampler2D backTexSampler;

uniform vec4 texSourceRegion;

#include "Lighting.glh"

void main()
{
	vec3 normal = normalize(normal_in);
	
	vec4 color;
	if (gl_FrontFacing)
	{
		color = texture(frontTexSampler, mix(texSourceRegion.xy, texSourceRegion.zw, texCoord_in));
		normal = -normal;
	}
	else
	{
		color = texture(backTexSampler, vec2(1.0 - texCoord_in.x, texCoord_in.y));
	}
	
	vec3 lighting = calcLighting(color.rgb, normal, worldPos_in, 0.5, 5.0);
	
	color_out = vec4(pow(lighting, vec3(1.0 / 2.2)), color.a);
}
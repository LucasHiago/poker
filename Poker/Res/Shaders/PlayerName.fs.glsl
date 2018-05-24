layout(location=0) in vec2 texCoord_in;
layout(location=1) in vec4 color_in;

layout(location=0) out vec4 color_out;

layout(binding=0) uniform sampler2D fontSampler;

const float SMOOTHNESS = 2;

void main()
{
	float alpha = texture(fontSampler, texCoord_in).a;
	alpha = clamp((alpha - 0.5) * SMOOTHNESS + 0.5, 0.0, 1.0);
	
	color_out = color_in;
	color_out.a *= alpha;
}

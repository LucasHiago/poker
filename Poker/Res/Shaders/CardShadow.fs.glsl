layout(location=0) in vec2 texCoord_in;

layout(binding=0) uniform sampler2D backTexSampler;

void main()
{
	if (texture(backTexSampler, texCoord_in).a < 0.5)
		discard;
}
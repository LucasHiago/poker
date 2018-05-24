layout(location=0) in vec3 eyeVector_in;

layout(binding=0) uniform samplerCube skyboxSampler;

layout(location=0) out vec4 color_out;

void main()
{
	color_out = texture(skyboxSampler, eyeVector_in);
}
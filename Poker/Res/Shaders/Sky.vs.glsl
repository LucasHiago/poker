#include "ViewProj.glh"

layout(location=0) out vec3 eyeVector_out;

const vec2 positions[] = vec2[]
(
	vec2(-1, -1),
	vec2(-1,  3),
	vec2( 3, -1)
);

void main()
{
	gl_Position = vec4(positions[gl_VertexID], 1 - 1E-4, 1);
	
	vec4 nearFrustumVertexWS = invViewProjTransform * vec4(gl_Position.xy, 0, 1);
	vec4 farFrustumVertexWS = invViewProjTransform * vec4(gl_Position.xy, 1, 1);
	
	nearFrustumVertexWS.xyz /= nearFrustumVertexWS.w;
	farFrustumVertexWS.xyz /= farFrustumVertexWS.w;
	
	eyeVector_out = farFrustumVertexWS.xyz - nearFrustumVertexWS.xyz;
}

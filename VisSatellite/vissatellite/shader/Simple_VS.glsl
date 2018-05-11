#version 410
precision highp float;

in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

uniform mat4 modelview_projection_matrix;

out vec2 fragTexcoord;

void main()
{
	fragTexcoord = in_uv;
	gl_position = modelview_projection_matrix * vec4(in_position, 1);
}


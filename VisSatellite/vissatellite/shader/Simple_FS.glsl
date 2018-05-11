#version 330
precision highp float;

uniform sampler2D sampler;

in vec2 fragTexcoord;

out vec4 outputColor;

void main()
{
    vec4 color = texture(sampler, fragTexcoord);
	outputColor = color;
}


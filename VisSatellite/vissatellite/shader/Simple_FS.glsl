#version 410
precision highp float;

uniform sampler2D sampler; 

in vec2 fragTexcoord;

out vec4 outputColor;

void main()
{
    outputColor = texture(sampler, fragTexcoord);
}

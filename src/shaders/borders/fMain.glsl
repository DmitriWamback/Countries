#version 410 core

out vec4 fragc;

in vec3 normal;
uniform vec3 cameraPosition;
uniform vec3 color;

void main() {

    fragc = vec4(color, 1.0);
}
#version 410 core

out vec4 fragc;

in vec3 normal;
uniform vec3 cameraPosition;

void main() {

    fragc = vec4(1.0, 1.0, 1.0, 1.0);
}
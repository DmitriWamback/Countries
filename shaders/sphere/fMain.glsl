#version 410 core

out vec4 fragc;

in vec3 normal;
in vec2 uv;
in vec3 fragp;

uniform vec3 cameraPosition;

uniform sampler2D surfaceTexture;
uniform float time;

void main() {

    vec3 color = vec3(0.05);
    vec3 lightPosition = vec3(1000);
    vec3 lightDir = -normalize(fragp - lightPosition);
    vec3 lightColor = vec3(0.8, 0.9, 0.6);

    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;
    vec3 albedo = diffuse + color;

    fragc = vec4(albedo, 1.0);
}
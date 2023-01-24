#version 410 core

out vec4 fragc;

in vec3 normal;
in vec2 uv;
in vec3 fragp;

uniform vec3 cameraPosition;

uniform sampler2D surfaceTexture;
uniform float time;

void main() {

    vec3 lightPosition = vec3(20.0);
    vec3 lightColor = vec3(0.6, 0.2, 0.5);
    vec3 color = vec3(0.4, 0.5, 1.0);

    vec3 lightDir = normalize(lightPosition - fragp);
    float diffuseIntensity = max(dot(normal, lightDir), 0.0);

    vec3 ambient = color * 0.2;
    vec3 diffuse = lightColor * diffuseIntensity;

    fragc = vec4((diffuse + ambient) * color, 1.0);
}
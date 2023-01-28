#version 410 core

out vec4 fragc;

in vec3 normal;
in vec2 uv;
in vec3 fragp;

uniform vec3 cameraPosition;

uniform sampler2D surfaceTexture;
uniform float time;

bool isTextured = true;

void main() {

    vec3 lightPosition = vec3(-20.0, 0.0, 20.0);
    vec3 lightColor = vec3(0.6, 0.2, 0.5);
    vec3 color = vec3(0.4, 0.5, 1.0);

    if (isTextured) {
        color = texture(surfaceTexture, uv).rgb;
        lightColor = vec3(1.0);
    }

    vec3 lightDir = normalize(lightPosition - fragp);
    float diffuseIntensity = max(dot(normal, lightDir), 0.0);

    vec3 ambient = color * 0.1;
    vec3 diffuse = lightColor * diffuseIntensity;
    vec3 albedo = (diffuse + ambient) * color;

    float gamma = 1.2;
    vec3 mapped = albedo;
    if (isTextured) mapped = pow(albedo, vec3(1.0/gamma));

    fragc = vec4(mapped, 1.0);
}
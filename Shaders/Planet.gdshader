shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;

uniform vec3 ocean_color : source_color = vec3(0.03, 0.16, 0.38);
uniform vec3 land_color : source_color = vec3(0.16, 0.48, 0.22);
uniform vec3 ice_color : source_color = vec3(0.82, 0.91, 0.96);
uniform float seed_offset = 0.0;

float hash(vec2 point) {
	return fract(sin(dot(point, vec2(127.1, 311.7))) * 43758.5453 + seed_offset * 97.0);
}

float noise(vec2 point) {
	vec2 cell = floor(point);
	vec2 local = fract(point);
	local = local * local * (3.0 - 2.0 * local);
	return mix(
		mix(hash(cell), hash(cell + vec2(1.0, 0.0)), local.x),
		mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0, 1.0)), local.x),
		local.y);
}

void fragment() {
	vec2 map_uv = vec2(UV.x * 8.0, UV.y * 4.0);
	float terrain = noise(map_uv) * 0.65 + noise(map_uv * 2.1) * 0.25 + noise(map_uv * 4.3) * 0.1;
	float latitude = abs(UV.y - 0.5) * 2.0;
	vec3 surface = mix(ocean_color, land_color, smoothstep(0.47, 0.56, terrain));
	surface = mix(surface, ice_color, smoothstep(0.78, 0.96, latitude + noise(map_uv) * 0.12));
	ALBEDO = surface;
	ROUGHNESS = mix(0.28, 0.78, smoothstep(0.47, 0.56, terrain));
	METALLIC = 0.0;
}

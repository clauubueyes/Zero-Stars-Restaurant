shader_type canvas_item;
// Elimina el fondo negro (casi negro) de la imagen del reloj.
// Ajusta 'umbral' en el inspector si quedan bordes negros o se ven agujeros.

uniform float umbral : hint_range(0.0, 0.5, 0.005) = 0.1;
uniform float suavizado : hint_range(0.0, 0.3, 0.005) = 0.06;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	float brillo = max(tex.r, max(tex.g, tex.b));
	tex.a *= smoothstep(umbral, umbral + suavizado, brillo);
	COLOR = tex * COLOR;
}

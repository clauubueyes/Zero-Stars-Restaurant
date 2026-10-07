extends CharacterBody3D

const SPEED = 4.0
const MOUSE_SENSITIVITY = 0.002

@onready var head = $Head
@onready var camera = $Head/Camera3D
@onready var raycast = $Head/RayCast3D

func _ready():
	# Bloquea y oculta el puntero del ratón en la pantalla
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _unhandled_input(event):
	# Salir de la captura de ratón al pulsar Escape
	if event.is_action_pressed("ui_cancel"):
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)

	# Control de cámara con el ratón
	if event is InputEventMouseMotion and Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		rotate_y(-event.relative.x * MOUSE_SENSITIVITY)
		head.rotate_x(-event.relative.y * MOUSE_SENSITIVITY)
		head.rotation.x = clamp(head.rotation.x, deg_to_rad(-80), deg_to_rad(80))

func _physics_process(delta):
	# Gravedad
	if not is_on_floor():
		velocity += get_gravity() * delta

	# Movimiento del personaje (Teclas WASD o Flechas)
	var input_dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	var direction = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	move_and_slide()

	# Detectar interacciones con objetos
	if Input.is_action_just_pressed("ui_accept"): # Tecla Espacio o Enter por defecto
		if raycast.is_colliding():
			var objeto = raycast.get_collider()
			print("Mirando a: ", objeto.name)

extends Control

@onready var boton_iniciar: Button = $BotonIniciar


func _ready() -> void:
	boton_iniciar.pressed.connect(_on_boton_iniciar_pressed)


func _on_boton_iniciar_pressed() -> void:
	AutoloadJuego.cambiar_fase("INVESTIGACION")
	get_tree().change_scene_to_file("res://escenas/EscenaInvestigacion.tscn")

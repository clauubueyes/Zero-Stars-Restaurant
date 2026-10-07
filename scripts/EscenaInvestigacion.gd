extends Control

@onready var titulo: Label = $Titulo
@onready var boton_examinar: Button = $BotonExaminar
@onready var texto_dialogo: Label = $TextoDialogo
@onready var boton_juicio: Button = $BotonJuicio


func _ready() -> void:
	boton_examinar.pressed.connect(_on_boton_examinar_pressed)
	boton_juicio.pressed.connect(_on_boton_juicio_pressed)
	texto_dialogo.text = ""


func _on_boton_examinar_pressed() -> void:
	texto_dialogo.text = "¡Has encontrado un Reloj de Bolsillo roto! Las manecillas se detuvieron a las 10:15... Tiene las iniciales L.A. grabadas."
	AutoloadJuego.registrar_pista("reloj_alba")
	boton_juicio.visible = true


func _on_boton_juicio_pressed() -> void:
	AutoloadJuego.cambiar_fase("JUICIO")
	get_tree().change_scene_to_file("res://escenas/EscenaJuicio.tscn")

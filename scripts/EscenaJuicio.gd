extends Control

const TESTIMONIO_INICIAL: String = "Lady Alba: '¡Es imposible que yo lo hiciese! A las 10 en punto me fui a dormir a mi camarote y no volví a salir en toda la noche...'"
const TEXTO_OBJECION: String = "¡OBJECIÓN! Detective: 'Este reloj fue hallado junto al cuerpo a las 10:15... ¡Y tiene sus iniciales grabadas, Lady Alba!'. Lady Alba se desmorona y confiesa el crimen. ¡HAS GANADO EL CASO!"
const TEXTO_GAME_OVER: String = "¡ERROR! El juez golpea el mazo. Has acusado falsamente a un inocente. GAME OVER."
const TEXTO_NO_CONTRADICE: String = "Juez: 'Esa prueba no contradice su testimonio, detective. ¡Cuidado!'."

@onready var testimonio: RichTextLabel = $CajaDialogo/Testimonio
@onready var acciones: HBoxContainer = $Acciones
@onready var boton_reloj: Button = $Acciones/BotonReloj
@onready var boton_coartada: Button = $Acciones/BotonCoartada
@onready var vidas_label: Label = $VidasLabel


func _ready() -> void:
	testimonio.text = TESTIMONIO_INICIAL
	actualizar_vidas()
	boton_reloj.pressed.connect(_on_boton_reloj_pressed)
	boton_coartada.pressed.connect(_on_boton_coartada_pressed)


func actualizar_vidas() -> void:
	vidas_label.text = "Vidas: " + "❤️".repeat(AutoloadJuego.vidas)


func ocultar_pruebas() -> void:
	acciones.visible = false


func _on_boton_reloj_pressed() -> void:
	testimonio.text = TEXTO_OBJECION
	ocultar_pruebas()


func _on_boton_coartada_pressed() -> void:
	var game_over: bool = AutoloadJuego.restar_vida()
	actualizar_vidas()
	if game_over:
		testimonio.text = TEXTO_GAME_OVER
		ocultar_pruebas()
	else:
		testimonio.text = TEXTO_NO_CONTRADICE

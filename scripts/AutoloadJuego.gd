extends Node

var fase_actual: String = "CRIMEN"
var pistas_encontradas: Array[String] = []
var vidas: int = 3


func registrar_pista(nombre_pista: String) -> void:
	if nombre_pista in pistas_encontradas:
		print("La pista '%s' ya estaba registrada." % nombre_pista)
		return
	pistas_encontradas.append(nombre_pista)
	print("Pista registrada: %s" % nombre_pista)


func cambiar_fase(nueva_fase: String) -> void:
	print("Fase cambiada de '%s' a '%s'." % [fase_actual, nueva_fase])
	fase_actual = nueva_fase


func restar_vida() -> bool:
	vidas -= 1
	if vidas <= 0:
		vidas = 0
		print("Sin vidas. GAME OVER.")
		return true
	print("Vidas restantes: %d" % vidas)
	return false

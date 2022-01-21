extends TextureRect

var marchingAntsOn: bool = false
var time: float = 0
onready var timer: Timer = $MarchingAntsTimer

func _ready():
	asyncMarchingAntsUpdate()

func asyncMarchingAntsUpdate():
	while (marchingAntsOn):
		(material as ShaderMaterial).set_shader_param("time", time)
		time += 1
		yield (timer, "timeout")

func setMarchingAnts(state: bool):
	marchingAntsOn = state
	(material as ShaderMaterial).set_shader_param("marchingAnts", marchingAntsOn)
	if state:
		asyncMarchingAntsUpdate()

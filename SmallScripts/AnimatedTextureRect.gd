extends TextureRect

var frames: Array = []
onready var timer: Timer = Timer.new()

var old_frame: int = -1
var frame: int = 0

func _ready():
	add_child(timer)

func set_array(new_frames):
	frames = new_frames
	old_frame = -1

func set_fps(new_fps: int):
	timer.wait_time = 1.0/new_fps

func run():
	timer.start()
	while true:
		frame += 1
		frame %= frames.size()
		if (old_frame != frame):
			texture = frames[frame]
		old_frame = frame
		yield(timer, "timeout")

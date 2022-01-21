extends FileDialog

var focus_holder: Control

func _ready():
	get_vbox().get_children()[2].get_children()[0].connect("cell_selected", self, "fix_focus")
	get_vbox().get_children()[2].get_children()[0].connect("focus_entered", self, "fix_focus")

func fix_focus():
	(get_vbox().get_children()[3].get_children()[2] as Control).grab_focus()

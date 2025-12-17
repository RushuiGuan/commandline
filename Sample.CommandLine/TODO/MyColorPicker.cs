namespace Sample.CommandLine {
	public class MyColorPicker {
		public MyColorPicker(string color) {
			this.Color = color;
		}

		public string Color { get; private set; }
	}
}
namespace GodotGRPC.Input;

public class InputMouseButton
{
    public int ResolutionWidth { get; set; }
    public int ResolutionHeight { get; set; }
    public int MousePositionX { get; set; }
    public int MousePositionY { get; set; }
    public bool ButtonPressed { get; set; }
    public int ButtonIndex { get; set; }
}
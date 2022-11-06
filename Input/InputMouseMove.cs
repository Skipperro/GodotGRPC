using System.Numerics;

namespace GodotGRPC.Input;

public class InputMouseMove
{
    public int ResolutionWidth { get; set; }
    public int ResolutionHeight { get; set; }
    public int MousePositionX { get; set; }
    public int MousePositionY { get; set; }
    public int RelativeX { get; set; }
    public int RelativeY { get; set; }
    
    public List<int> Buttons { get; set; }
}
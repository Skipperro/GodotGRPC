using System.Drawing;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace GodotGRPC;

public class FFMpegProcessor
{
    public static void EncodeFrame(byte[] frame)
    {
        FFMpegArguments.FromPipeInput(new RawVideoPipeSource(new List<IVideoFrame>()))
            .OutputToPipe(new StreamPipeSink((stream, token) => { return new Task(null); }));
    }
}
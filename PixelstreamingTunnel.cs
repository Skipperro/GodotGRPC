using System;
using Grpc.Core;
using pixelstreaminggrpc;

namespace GodotGRPC
{
    public class PixelstreamingTunnel
    {
        public string ClientGuid { get; set; }
        public readonly IAsyncStreamReader<ControlInput> ClientStream;
        public readonly IAsyncStreamWriter<FrameData> ServerStream;
        public DateTime LastCommunication = DateTime.Now;

        public PixelstreamingTunnel(IAsyncStreamReader<ControlInput> clientStream,
            IAsyncStreamWriter<FrameData> serverStream, string clientGuid)
        {
            ClientStream = clientStream;
            ServerStream = serverStream;
            ClientGuid = clientGuid;
        }

        public void SendFrame(FrameData fd)
        {
            ServerStream.WriteAsync(fd);
        }
    }
}
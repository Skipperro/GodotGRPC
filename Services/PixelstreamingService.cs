using Grpc.Core;
using pixelstreaminggrpc;

namespace GodotGRPC.Services;

public class PixelstreamingService : PixelStreamingGrpcService.PixelStreamingGrpcServiceBase
{
    private readonly ILogger<PixelstreamingService> _logger;

    public PixelstreamingService(ILogger<PixelstreamingService> logger)
    {
        _logger = logger;
    }

    public override async Task RunPixelStreaming(IAsyncStreamReader<ControlInput> clientStream, IServerStreamWriter<FrameData> serverStream, ServerCallContext context)
    {
        PixelstreamingTunnel sourceChannel = null;
        try
        {
            var tGuid = Guid.NewGuid();
            _logger.LogInformation($"New subscription {tGuid.ToString()}.");
            string sourceGuid = "";

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            while (await clientStream.MoveNext(tokenSource.Token))
            {
                try
                {
                    if (tokenSource.IsCancellationRequested) return;
                    if (sourceChannel != null)
                    {
                        _logger.LogDebug($"Processing gRPC message [{clientStream.Current.Command.ToUpper()}] from client [{clientStream.Current.Guid.ToUpper()}].");
                    }

                    var message = clientStream.Current;

                    if (message.Guid.Length < 6)
                    {
                        _logger.LogWarning("Client with empty or too short GUID tried to connect!");
                        tokenSource.Cancel();
                        return;
                    }

                    if (sourceGuid == "") // New connection
                    {
                        if (ChannelController.ChannelExists(message.Guid.ToUpper()))
                        {
                            _logger.LogWarning($"Client with same GUID [{message.Guid.ToUpper()}] is already connected!");
                            ChannelController.RemoveChannel(message.Guid.ToUpper());
                            tokenSource.Cancel();
                            return;
                        }
                    }

                    if (sourceGuid != "" && sourceGuid != message.Guid.ToUpper())
                    {
                        _logger.LogWarning($"Message spoofing detected! Client [{sourceGuid.ToUpper()}] sent message with SourceGUID [{message.Guid.ToUpper()}]");
                        continue;
                    }

                    sourceGuid = message.Guid.ToUpper();
                    if (sourceGuid == "")
                    {
                        _logger.LogWarning($"Source GUID is empty!!");
                        tokenSource.Cancel();
                        return;
                    }

                    if (ChannelController.ChannelExists(sourceGuid.ToUpper()))
                    {
                        sourceChannel = ChannelController.GetChannel(sourceGuid.ToUpper());
                    }
                    else
                    {
                        sourceChannel = new PixelstreamingTunnel(clientStream, serverStream, sourceGuid);
                        ChannelController.AddChannel(sourceGuid.ToUpper(), sourceChannel);
                    }

                    sourceChannel.LastCommunication = DateTime.Now;

                    GrpcMaster.ProcessControlInput(message);
                }
                catch (IOException ioException)
                {
                    _logger.LogWarning("GRPC stopped in unclean way");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    _logger.LogError(e.StackTrace);
                }
            }
        }
        catch (IOException ioException)
        {
            _logger.LogWarning("gRPC request stopped in unclean way");
            if (sourceChannel != null)
            {
                ChannelController.RemoveChannel(sourceChannel.ClientGuid);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            if (sourceChannel != null)
            {
                ChannelController.RemoveChannel(sourceChannel.ClientGuid);
            }
        }
    }
}
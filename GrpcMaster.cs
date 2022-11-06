using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using GodotGRPC.Services;
using Grpc.Core;
using Grpc.Net.Client;
using pixelstreaminggrpc;

namespace GodotGRPC;

public class GrpcMaster
{
    private static WebApplication? _serverApp;
    
    // OnCloseEvent is called when the server is closed
    public static event Action? OnCloseEvent;
    
    public delegate void OnIncomingFrameEventHandler(FrameData fd);
    public static event OnIncomingFrameEventHandler? OnIncomingFrameEvent;
    
    public delegate void OnIncomingInputEventHandler(ControlInput ci);
    
    public static event OnIncomingInputEventHandler? OnIncomingInputEvent;

    private static IClientStreamWriter<ControlInput>? _clientStreamWriter;
    
    private static ConcurrentQueue<FrameData> _frameBuffer = new ConcurrentQueue<FrameData>();
    private static Task? _frameTransitLoopTask;

    public static async void StartServerAsync(int port, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddGrpc();
        _serverApp = builder.Build();
        _serverApp.MapGrpcService<PixelstreamingService>();
        _serverApp.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        _serverApp.Urls.Clear();
        _serverApp.Urls.Add($"https://*:{port}");
        await _serverApp.StartAsync(cancellationToken);
        _serverApp.Lifetime.ApplicationStopping.Register(() =>
        {
            if (OnCloseEvent != null) OnCloseEvent.Invoke();
        });
        _frameTransitLoopTask = new Task(() => FrameTransitLoop(cancellationToken), cancellationToken);
        _frameTransitLoopTask.Start();
        Console.WriteLine("App is running!");
    }

    public static async void RunClientAsync(string serverAddress, CancellationToken cancellationToken)
    {
        try
        {
            var options = new GrpcChannelOptions();
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            httpHandler.SslProtocols = SslProtocols.None;
            options.HttpHandler = httpHandler;

            var channel = GrpcChannel.ForAddress(serverAddress, options);
            var client = new PixelStreamingGrpcService.PixelStreamingGrpcServiceClient(channel);
            var tunnel = client.RunPixelStreaming(deadline: DateTime.UtcNow.AddHours(24), cancellationToken: cancellationToken);
            _clientStreamWriter = tunnel.RequestStream;
            while (await tunnel.ResponseStream.MoveNext(cancellationToken))
            {
                var fd = tunnel.ResponseStream.Current;
                if (fd != null)
                {
                    ProcessFrameData(fd);
                }
            }
        }
        catch (Grpc.Core.RpcException rpcException)
        {
            _clientStreamWriter?.CompleteAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static void FrameTransitLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_frameBuffer.TryDequeue(out var fd))
            {
                var channels = ChannelController.GetAllChannels().ToList();
                foreach (var ch in channels)
                {
                    try
                    {
                        ch.ServerStream.WriteOptions = (new WriteOptions(WriteFlags.NoCompress));
                        ch.ServerStream.WriteAsync(fd).Wait(500);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    
                }
            }
            else
            {
                Thread.Sleep(5);
            }
        }
        Console.WriteLine("FrameTransitLoop is stopped");
    }

    public static void ProcessFrameData(FrameData fd)
    {
        if (OnIncomingFrameEvent != null) OnIncomingFrameEvent.Invoke(fd);
    }
    
    public static void SendFrameData(FrameData fd)
    {
        if (_frameBuffer.Count > 3) _frameBuffer.TryDequeue(out _);
        _frameBuffer.Enqueue(fd);
    }

    public static void ProcessControlInput(ControlInput ci)
    {
        if (OnIncomingInputEvent != null) OnIncomingInputEvent.Invoke(ci);
    }
    
    public static void SendControlInput(ControlInput ci)
    {
        if (_clientStreamWriter == null) return;
        _clientStreamWriter?.WriteAsync(ci);
    }
}
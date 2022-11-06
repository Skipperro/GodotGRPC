using System.Collections.Generic;
using System.Linq;

namespace GodotGRPC
{
    public class ChannelController
    {
        private static Dictionary<string, PixelstreamingTunnel> _channels = new Dictionary<string, PixelstreamingTunnel>();
        private static readonly object ThreadSafeLock = new ();

        public static bool ChannelExists(string guid)
        {
            lock (ThreadSafeLock)
            {
                if (_channels.ContainsKey(guid))
                {
                    return true;
                }

                return false;
            }
        }


        public static PixelstreamingTunnel GetChannel(string guid)
        {
            lock (ThreadSafeLock)
            {
                if (_channels.ContainsKey(guid))
                {
                    return _channels[guid];
                }

                return null;
            }
        }

        public static List<PixelstreamingTunnel> GetAllChannels()
        {
            lock (ThreadSafeLock)
            {
                return _channels.Values.ToList();
            }
        }

        public static void AddChannel(string guid, PixelstreamingTunnel channel)
        {
            lock (ThreadSafeLock)
            {
                _channels.TryAdd(guid, channel);
            }
        }

        public static void RemoveChannel(string guid)
        {
            lock (ThreadSafeLock)
            {
                if (_channels.ContainsKey(guid))
                {
                    _channels.Remove(guid);
                }
            }
        }

        public static void ClearChannels()
        {
            lock (ThreadSafeLock)
            {
                _channels = new Dictionary<string, PixelstreamingTunnel>();
            }
        }

        public static int GetChannelsCount()
        {
            lock (ThreadSafeLock)
            {
                return _channels.Count;
            }
        }
    }
}
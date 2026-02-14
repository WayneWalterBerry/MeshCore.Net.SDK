// <copyright file="IChannelProvider.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Providers
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using MeshCore.Net.SDK.Interfaces;
    using MeshCore.Net.SDK.Models;

    internal class Campaign : IChannelProvider, IDisposable
    {
        private readonly MeshCoreClient meshCoreClient;

        /// <summary>
        /// Channel cache to store channel information received from the radio
        /// </summary>
        private readonly ConcurrentDictionary<uint, Channel> channelCache = new();

        private readonly SemaphoreSlim _channelsLoadGate = new(1, 1);
        private bool channelsLoaded;

        public Campaign(MeshCoreClient client)
        {
            this.meshCoreClient = client;
            this.meshCoreClient.Channel += MeshCoreClient_Channel;
        }

        private void MeshCoreClient_Channel(object? sender, Channel channel)
        {
            channelCache.AddOrUpdate(channel.Index, channel!, (key, oldValue) => channel!);
        }

        /// <inheritdoc />
        public async Task<Channel?> TryGetChannelAsync(uint channelIndex, CancellationToken cancellationToken = default)
        {
            if (channelCache.TryGetValue(channelIndex, out Channel? cachedChannel))
            {
                return (Channel?)cachedChannel;
            }

            // MeshCoreClient will automatically update the channel cache when it receives channel info
            // from the radio by calling the MeshCoreClient_Channel event handler.
            Channel? channel = await meshCoreClient.TryGetChannelAsync(channelIndex, cancellationToken);
            return channel;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Channel>> GetChannelsAsync(CancellationToken cancellation = default)
        {
            // Only return the cached channels if we've already loaded them, otherwise we
            // might return an incomplete list while we're still loading from the radio
            if (this.channelsLoaded)
            {
                return channelCache.Values;
            }

            await this._channelsLoadGate.WaitAsync(cancellation);
            try
            {
                if (this.channelsLoaded)
                {
                    return channelCache.Values;
                }

                IEnumerable<Channel> channels = await meshCoreClient.GetChannelsAsync(cancellation);
                this.channelsLoaded = true;
                return channels;
            }
            finally
            {
                this._channelsLoadGate.Release();
            }
        }

        /// <inheritdoc />
        public Task SetChannelAsync(ChannelParams channel, CancellationToken cancellation = default)
        {
            // MeshCoreClient will automatically update the channel cache when it receives channel info
            // from the radio by calling the MeshCoreClient_Channel event handler.
            return this.meshCoreClient.SetChannelAsync(channel, cancellation);
        }

        public Task AddChannelAsync(string name, string encryptionKey, CancellationToken cancellationToken = default)
        {
            // MeshCoreClient will automatically update the channel cache when it receives channel info
            // from the radio by calling the MeshCoreClient_Channel event handler.
            return this.meshCoreClient.AddChannelAsync(name, encryptionKey, cancellationToken);
        }

        public void Dispose()
        {
            this.meshCoreClient.Channel -= MeshCoreClient_Channel;
            this._channelsLoadGate.Dispose();
        }
    }
}

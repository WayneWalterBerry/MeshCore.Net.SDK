// <copyright file="IChannelProvider.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MeshCore.Net.SDK.Models;

    internal interface IChannelProvider
    {
        public Task<Channel?> TryGetChannelAsync(uint channelIndex, CancellationToken cancellationToken = default);

        public Task<IEnumerable<Channel>> GetChannelsAsync(CancellationToken cancellation = default);

        public Task SetChannelAsync(ChannelParams channelParams, CancellationToken cancellation = default);

        /// <summary>
        /// Adds Channel with the specified name and encryption key to the radio's channel list.
        /// </summary>
        /// <param name="name">Channel Name</param>
        /// <param name="encryptionKey">Encryption Key</param>
        /// <param name="cancellationToken"></param>
        public Task AddChannelAsync(string name, ChannelSecret encryptionKey, CancellationToken cancellationToken = default);
    }
}

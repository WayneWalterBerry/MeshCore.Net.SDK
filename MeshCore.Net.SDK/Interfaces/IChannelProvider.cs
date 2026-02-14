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

        public Task AddChannelAsync(string name, string EncryptionKey, CancellationToken cancellationToken = default);
    }
}

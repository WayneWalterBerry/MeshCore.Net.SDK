// <copyright file="IChannelProvider.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// The radio provides a database of channels that have a primary key of channel index. 
    /// Implementations of this interface should provide methods to retrieve channel information,
    /// add new channels, and update existing channels.
    /// </summary>
    internal interface IChannelProvider
    {
        /// <summary>
        /// Attempts to retrieve the channel associated with the specified index asynchronously.
        /// </summary>
        /// <param name="channelIndex">The zero-based index of the channel to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the channel if found; otherwise,
        /// null.</returns>
        public Task<Channel?> TryGetChannelAsync(uint channelIndex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a collection of available channels.
        /// </summary>
        /// <param name="cancellation">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of
        /// channels.</returns>
        public Task<IEnumerable<Channel>> GetChannelsAsync(CancellationToken cancellation = default);

        /// <summary>
        /// Asynchronously sets the channel configuration using the specified parameters.
        /// </summary>
        /// <param name="channelParams">The parameters that define the channel configuration to apply. Cannot be null.</param>
        /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

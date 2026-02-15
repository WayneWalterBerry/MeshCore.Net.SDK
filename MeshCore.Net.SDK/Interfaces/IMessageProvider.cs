// <copyright file = "IMessageProvider.cs" company = "Wayne Walter Berry" >
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MeshCore.Net.SDK.Models;

    internal interface IMessageProvider
    {
        public IEnumerable<Message> GetMessages();

        public Task<Message> SendMessageAsync(Contact contact, string content, CancellationToken cancellationToken = default);

        public Task SendChannelMessageAsync(Channel channel, string content, CancellationToken cancellationToken = default);
    }
}

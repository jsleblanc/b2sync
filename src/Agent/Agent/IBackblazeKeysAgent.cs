﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Bytewizer.Backblaze.Models;

namespace Bytewizer.Backblaze.Agent
{
    public interface IBackblazeKeysAgent
    {
        Task<IApiResults<ListKeysResponse>> GetAsync();
        Task<IApiResults<ListKeysResponse>> GetAsync(ListKeysRequest request);

        Task<IApiResults<CreateKeyResponse>> CreateAsync(CreateKeyRequest request);
        Task<IApiResults<CreateKeyResponse>> CreateAsync(string[] capabilities, string keyName);
        Task<IApiResults<CreateKeyResponse>> CreateAsync(string accountId, string[] capabilities, string keyName);

        Task<IApiResults<DeleteKeyResponse>> DeleteAsync(string applicationKeyId);
    }
}

﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Extension;
using Snap.Hutao.Model.Binding.User;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.Service.Abstraction;
using Snap.Hutao.Web.Hoyolab.Annotation;
using Snap.Hutao.Web.Hoyolab.DynamicSecret;
using Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.Avatar;
using Snap.Hutao.Web.Response;
using System.Net.Http;

namespace Snap.Hutao.Web.Hoyolab.Takumi.GameRecord;

/// <summary>
/// 游戏记录提供器
/// </summary>
[UseDynamicSecret]
[HttpClient(HttpClientConfigration.XRpc)]
internal class GameRecordClient
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions options;
    private readonly ILogger<GameRecordClient> logger;

    /// <summary>
    /// 构造一个新的游戏记录提供器
    /// </summary>
    /// <param name="httpClient">请求器</param>
    /// <param name="options">json序列化选项</param>
    /// <param name="logger">日志器</param>
    public GameRecordClient(HttpClient httpClient, JsonSerializerOptions options, ILogger<GameRecordClient> logger)
    {
        this.httpClient = httpClient;
        this.options = options;
        this.logger = logger;
    }

    /// <summary>
    /// 异步获取实时便笺
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="uid">查询uid</param>
    /// <param name="token">取消令牌</param>
    /// <returns>实时便笺</returns>
    [ApiInformation(Cookie = CookieType.CookieToken | CookieType.Ltoken | CookieType.Mid, Salt = SaltType.X4)]
    public async Task<DailyNote.DailyNote?> GetDailyNoteAsync(Model.Entity.User user, PlayerUid uid, CancellationToken token = default)
    {
        Response<DailyNote.DailyNote>? resp = await httpClient
            .SetUser(user, CookieType.CookieToken | CookieType.Ltoken)
            .UseDynamicSecret(DynamicSecretVersion.Gen2, SaltType.X4, false)
            .TryCatchGetFromJsonAsync<Response<DailyNote.DailyNote>>(ApiEndpoints.GameRecordDailyNote(uid), options, logger, token)
            .ConfigureAwait(false);

        // We hava a verification procedure to handle
        if (resp?.ReturnCode == (int)KnownReturnCode.CODE1034)
        {
            CardVerifier cardVerifier = Ioc.Default.GetRequiredService<CardVerifier>();

            if (await cardVerifier.TryGetXrpcChallengeAsync(user, token).ConfigureAwait(false) is string challenge)
            {
                Ioc.Default.GetRequiredService<IInfoBarService>().Success("无感验证成功");

                resp = await httpClient
                    .SetUser(user, CookieType.CookieToken | CookieType.Ltoken)
                    .SetXrpcChallenge(challenge)
                    .UseDynamicSecret(DynamicSecretVersion.Gen2, SaltType.X4, false)
                    .TryCatchGetFromJsonAsync<Response<DailyNote.DailyNote>>(ApiEndpoints.GameRecordDailyNote(uid), options, logger, token)
                    .ConfigureAwait(false);
            }
            else
            {
                Ioc.Default.GetRequiredService<IInfoBarService>().Warning("无感验证失败，请前往「米游社-我的角色-实时便笺」页面查看");
            }
        }

        return resp?.Data;
    }

    /// <summary>
    /// 获取玩家基础信息
    /// </summary>
    /// <param name="userAndRole">用户</param>
    /// <param name="token">取消令牌</param>
    /// <returns>玩家的基础信息</returns>
    public Task<PlayerInfo?> GetPlayerInfoAsync(UserAndRole userAndRole, CancellationToken token = default)
    {
        return GetPlayerInfoAsync(userAndRole.User, userAndRole.Role, token);
    }

    /// <summary>
    /// 获取玩家基础信息
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="uid">uid</param>
    /// <param name="token">取消令牌</param>
    /// <returns>玩家的基础信息</returns>
    [ApiInformation(Cookie = CookieType.Ltoken, Salt = SaltType.X4)]
    public async Task<PlayerInfo?> GetPlayerInfoAsync(Model.Entity.User user, PlayerUid uid, CancellationToken token = default)
    {
        Response<PlayerInfo>? resp = await httpClient
            .SetUser(user, CookieType.Ltoken)
            .UseDynamicSecret(DynamicSecretVersion.Gen2, SaltType.X4, false)
            .TryCatchGetFromJsonAsync<Response<PlayerInfo>>(ApiEndpoints.GameRecordIndex(uid), options, logger, token)
            .ConfigureAwait(false);

        return resp?.Data;
    }

    /// <summary>
    /// 获取玩家深渊信息
    /// </summary>
    /// <param name="userAndRole">用户</param>
    /// <param name="schedule">1：当期，2：上期</param>
    /// <param name="token">取消令牌</param>
    /// <returns>深渊信息</returns>
    public Task<SpiralAbyss.SpiralAbyss?> GetSpiralAbyssAsync(UserAndRole userAndRole, SpiralAbyssSchedule schedule, CancellationToken token = default)
    {
        return GetSpiralAbyssAsync(userAndRole.User, userAndRole.Role, schedule, token);
    }

    /// <summary>
    /// 获取玩家深渊信息
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="uid">uid</param>
    /// <param name="schedule">1：当期，2：上期</param>
    /// <param name="token">取消令牌</param>
    /// <returns>深渊信息</returns>
    [ApiInformation(Cookie = CookieType.Ltoken, Salt = SaltType.X4)]
    public async Task<SpiralAbyss.SpiralAbyss?> GetSpiralAbyssAsync(Model.Entity.User user, PlayerUid uid, SpiralAbyssSchedule schedule, CancellationToken token = default)
    {
        Response<SpiralAbyss.SpiralAbyss>? resp = await httpClient
            .SetUser(user, CookieType.Ltoken)
            .UseDynamicSecret(DynamicSecretVersion.Gen2, SaltType.X4, false)
            .TryCatchGetFromJsonAsync<Response<SpiralAbyss.SpiralAbyss>>(ApiEndpoints.GameRecordSpiralAbyss(schedule, uid), options, logger, token)
            .ConfigureAwait(false);

        return resp?.Data;
    }

    /// <summary>
    /// 异步获取角色基本信息
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="uid">uid</param>
    /// <param name="token">取消令牌</param>
    /// <returns>角色基本信息</returns>
    [ApiInformation(Cookie = CookieType.Ltoken, Salt = SaltType.X4)]
    public async Task<BasicRoleInfo?> GetRoleBasicInfoAsync(Model.Entity.User user, PlayerUid uid, CancellationToken token = default)
    {
        Response<BasicRoleInfo>? resp = await httpClient
            .SetUser(user, CookieType.Ltoken)
            .UseDynamicSecret(DynamicSecretVersion.Gen2, SaltType.X4, false)
            .TryCatchGetFromJsonAsync<Response<BasicRoleInfo>>(ApiEndpoints.GameRecordRoleBasicInfo(uid), options, logger, token)
            .ConfigureAwait(false);

        return resp?.Data;
    }

    /// <summary>
    /// 获取玩家角色详细信息
    /// </summary>
    /// <param name="userAndRole">用户与角色</param>
    /// <param name="playerInfo">玩家的基础信息</param>
    /// <param name="token">取消令牌</param>
    /// <returns>角色列表</returns>
    public Task<List<Character>> GetCharactersAsync(UserAndRole userAndRole, PlayerInfo playerInfo, CancellationToken token = default)
    {
        return GetCharactersAsync(userAndRole.User, userAndRole.Role, playerInfo, token);
    }

    /// <summary>
    /// 获取玩家角色详细信息
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="uid">uid</param>
    /// <param name="playerInfo">玩家的基础信息</param>
    /// <param name="token">取消令牌</param>
    /// <returns>角色列表</returns>
    [ApiInformation(Cookie = CookieType.Ltoken, Salt = SaltType.X4)]
    public async Task<List<Character>> GetCharactersAsync(Model.Entity.User user, PlayerUid uid, PlayerInfo playerInfo, CancellationToken token = default)
    {
        CharacterData data = new(uid, playerInfo.Avatars.Select(x => x.Id));

        Response<CharacterWrapper>? resp = await httpClient
            .SetUser(user, CookieType.Ltoken)
            .UseDynamicSecret(DynamicSecretVersion.Gen2, SaltType.X4, false)
            .TryCatchPostAsJsonAsync<CharacterData, Response<CharacterWrapper>>(ApiEndpoints.GameRecordCharacter, data, options, logger, token)
            .ConfigureAwait(false);

        return EnumerableExtension.EmptyIfNull(resp?.Data?.Avatars);
    }

    private class CharacterData
    {
        public CharacterData(PlayerUid uid, IEnumerable<AvatarId> characterIds)
        {
            CharacterIds = characterIds;
            Uid = uid.Value;
            Server = uid.Region;
        }

        [JsonPropertyName("character_ids")]
        public IEnumerable<AvatarId> CharacterIds { get; }

        [JsonPropertyName("role_id")]
        public string Uid { get; }

        [JsonPropertyName("server")]
        public string Server { get; }
    }
}
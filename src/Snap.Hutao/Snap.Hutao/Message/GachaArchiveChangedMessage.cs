﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Entity;

namespace Snap.Hutao.Message;

/// <summary>
/// 祈愿记录存档切换消息
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
internal class GachaArchiveChangedMessage : ValueChangedMessage<GachaArchive>
{
}
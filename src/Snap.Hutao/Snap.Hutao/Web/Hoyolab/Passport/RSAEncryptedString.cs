﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace Snap.Hutao.Web.Hoyolab.Passport;

/// <summary>
/// 支持RSA转换
/// </summary>
internal class RSAEncryptedString
{
    private const string RsaPublicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDDvekdPMHN3AYhm/vktJT+YJr7cI5DcsNKqdsx5DZX0gDuWFuIjzdwButrIYPNmRJ1G8ybDIF7oDW2eEpm5sMbL9zs\n9ExXCdvqrn51qELbqj0XxtMTIpaCHFSI50PfPpTFV9Xt/hmyVwokoOXFlAEgCn+Q\nCgGs52bFoYMtyi+xEQIDAQAB\n";

    private readonly string encryptedSource;

    private RSAEncryptedString(string encryptedSource)
    {
        this.encryptedSource = encryptedSource;
    }

    public static implicit operator string(RSAEncryptedString encryptedString)
    {
        return encryptedString.encryptedSource;
    }

    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="source">源</param>
    /// <returns>字节数组</returns>
    public static RSAEncryptedString Encrypt(string source)
    {
        RSA rsa = RSA.Create();
        rsa.ImportFromPem($"-----BEGIN PUBLIC KEY-----\n{RsaPublicKey}-----END PUBLIC KEY-----");
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        string encrypted = Convert.ToBase64String(rsa.Encrypt(bytes, RSAEncryptionPadding.Pkcs1));

        return new(encrypted);
    }
}
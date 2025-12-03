using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Network.Models
{
    // ==============================================
    // 认证相关模型
    // ==============================================

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
        public string deviceId;
        public string platform = "Unity";
        public string version = Application.version;
    }

    [Serializable]
    public class RegisterRequest : LoginRequest
    {
        public string email;
        public string nickname;
        public int age;
        public string inviteCode;
    }

    [Serializable]
    public class AuthResponse
    {
        public int code;
        public string message;
        public UserData data;
        public string token;
        public string refreshToken;
        public long tokenExpire;
    }

    [Serializable]
    public class RefreshTokenRequest
    {
        public string refreshToken;
        public string deviceId;
    }
}
using System;

namespace TeamStride.Domain.Identity;

public class AuthenticationException : Exception
{
    public string Code { get; }

    public AuthenticationException(string message, string code = null) 
        : base(message)
    {
        Code = code ?? "AUTH_ERROR";
    }

    public static class ErrorCodes
    {
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string EmailNotConfirmed = "EMAIL_NOT_CONFIRMED";
        public const string InvalidToken = "INVALID_TOKEN";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string TenantNotFound = "TENANT_NOT_FOUND";
        public const string InvalidRole = "INVALID_ROLE";
        public const string EmailAlreadyExists = "EMAIL_EXISTS";
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string ExternalAuthError = "EXTERNAL_AUTH_ERROR";
    }
} 
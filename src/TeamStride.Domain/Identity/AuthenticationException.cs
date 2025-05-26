using System;

namespace TeamStride.Domain.Identity;

public class AuthenticationException : Exception
{
    public ErrorCodes ErrorCode { get; }

    public AuthenticationException(string message, ErrorCodes errorCode = ErrorCodes.Unknown) 
        : base(message ?? throw new ArgumentNullException(nameof(message)))
    {
        ErrorCode = errorCode;
    }

    public enum ErrorCodes
    {
        Unknown,
        InvalidCredentials,
        AccountLocked,
        EmailNotConfirmed,
        TenantNotFound,
        UserNotFound,
        EmailAlreadyExists,
        InvalidToken,
        ExternalAuthError
    }
} 
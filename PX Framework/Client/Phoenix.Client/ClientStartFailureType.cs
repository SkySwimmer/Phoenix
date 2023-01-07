﻿namespace Phoenix.Client
{
    /// <summary>
    /// Client startup failure type enum
    /// </summary>
    public enum ClientStartFailureType
    {
        UNKOWN_ERROR,
        CONNECT_FAILED,
        CONNECTION_ALREADY_OPEN,
        HANDSHAKE_FAILURE_NONPHOENIX,
        HANDSHAKE_FAILURE_GENERIC,
        HANDSHAKE_FAILURE_VERSION_MISMATCH,
        HANDSHAKE_FAILURE_GAME_MISMATCH,
        HANDSHAKE_FAILURE_ENCRYPTION_FAILURE,
        HANDSHAKE_FAILURE_INVALID_CERTIFICATE,
        HANDSHAKE_FAILURE_UNEXPECTED_TRAFFIC,
        AUTHENTICATION_FAILURE,
        COMPONENT_ERROR,
        ENDED_TOO_EARLY
    }
}

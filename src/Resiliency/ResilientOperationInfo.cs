﻿namespace Resiliency
{
    public interface IResilientOperationInfo
        : IHaveCurrentAttempt
    {
    }

    public interface IHaveCurrentAttempt
    {
        int CurrentAttempt { get; }
    }

    public class ResilientOperationHandlerInfo
        : IResilientOperationInfo
    {
        internal int _attemptsExhausted = 0;

        public int CurrentAttempt { get => _attemptsExhausted + 1; }
    }

    public class ResilientOperationTotalInfo
        : IResilientOperationInfo
    {
        internal int _attemptsExhausted = 0;

        public int CurrentAttempt { get => _attemptsExhausted + 1; }
    }
}

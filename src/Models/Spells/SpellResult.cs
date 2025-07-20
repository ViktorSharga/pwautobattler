using System;

namespace GameAutomation.Models.Spells
{
    public class SpellResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public TimeSpan ExecutionTime { get; private set; }
        public DateTime ExecutedAt { get; private set; }

        private SpellResult(bool success, string? errorMessage = null, TimeSpan executionTime = default)
        {
            Success = success;
            ErrorMessage = errorMessage;
            ExecutionTime = executionTime;
            ExecutedAt = DateTime.Now;
        }

        public static SpellResult Successful(TimeSpan executionTime = default)
        {
            return new SpellResult(true, null, executionTime);
        }

        public static SpellResult Failed(string errorMessage)
        {
            return new SpellResult(false, errorMessage);
        }
    }
}
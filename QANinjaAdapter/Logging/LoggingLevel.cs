using System;

namespace QANinjaAdapter.Logging
{
    /// <summary>
    /// Represents logging levels for the application
    /// </summary>
    public class LoggingLevel
    {
        public static readonly LoggingLevel Debug = new LoggingLevel("Debug");
        public static readonly LoggingLevel Information = new LoggingLevel("Information");
        public static readonly LoggingLevel Warning = new LoggingLevel("Warning");
        public static readonly LoggingLevel Error = new LoggingLevel("Error");
        
        private readonly string _name;

        private LoggingLevel(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString()
        {
            return _name;
        }
        
        // Equality operators for comparison with other LoggingLevel instances
        public static bool operator ==(LoggingLevel left, LoggingLevel right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
                
            return left.Equals(right);
        }
        
        public static bool operator !=(LoggingLevel left, LoggingLevel right)
        {
            return !(left == right);
        }
        
        public override bool Equals(object obj)
        {
            if (obj is LoggingLevel other)
                return _name == other._name;
                
            return false;
        }
        
        public override int GetHashCode()
        {
            return _name?.GetHashCode() ?? 0;
        }
    }
}

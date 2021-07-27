using System;

namespace Skender.Stock.Indicators
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "Just testing an idea")]

    public readonly struct RsiResult
    {
        public readonly DateTime Date { get; }
        public readonly decimal? Rsi { get; }

        public RsiResult(DateTime d, decimal? r)
        {
            Date = d;
            Rsi = r;
        }
    }
}

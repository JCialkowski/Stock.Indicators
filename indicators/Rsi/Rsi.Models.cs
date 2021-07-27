using System;

namespace Skender.Stock.Indicators
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "Just testing an idea")]

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1051:Do not declare visible instance fields",
        Justification = "Just testing an idea")]

    public struct RsiResult
    {
        public DateTime Date;
        public decimal? Rsi;
    }
}

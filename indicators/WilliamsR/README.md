﻿# Williams %R

Created by Larry Williams, the [Williams %R](https://en.wikipedia.org/wiki/Williams_%25R) momentum indicator is a stochastic oscillator with scale of -100 to 0.  It is exactly the same as the Fast variant of [Stochastic Oscillator](../Stochastic/README.md), but with a different scaling.
[[Discuss] :speech_balloon:](https://github.com/DaveSkender/Stock.Indicators/discussions/229 "Community discussion about this indicator")

![image](chart.png)

```csharp
// usage
IEnumerable<WilliamsResult> results =
  quotes.GetWilliamsR(lookbackPeriods);  
```

## Parameters

| name | type | notes
| -- |-- |--
| `lookbackPeriods` | int | Number of periods (`N`) in the lookback period.  Must be greater than 0.  Default is 14.

### Historical quotes requirements

You must have at least `N` periods of `quotes`.

`quotes` is an `IEnumerable<TQuote>` collection of historical price quotes.  It should have a consistent frequency (day, hour, minute, etc).  See [the Guide](../../docs/GUIDE.md#historical-quotes) for more information.

## Response

```csharp
IEnumerable<WilliamsResult>
```

- This method returns a time series of all available indicator values for the `quotes` provided.
- It always returns the same number of elements as there are in the historical quotes.
- It does not return a single incremental indicator value.
- The first `N-1` periods will have `null` Oscillator values since there's not enough data to calculate.

### WilliamsResult

| name | type | notes
| -- |-- |--
| `Date` | DateTime | Date
| `WilliamsR` | decimal | Oscillator over prior `N` lookback periods

### Utilities

- [.Find(lookupDate)](../../docs/UTILITIES.md#find-indicator-result-by-date)
- [.RemoveWarmupPeriods()](../../docs/UTILITIES.md#remove-warmup-periods)
- [.RemoveWarmupPeriods(qty)](../../docs/UTILITIES.md#remove-warmup-periods)

See [Utilities and Helpers](../../docs/UTILITIES.md#utilities-for-indicator-results) for more information.

## Example

```csharp
// fetch historical quotes from your feed (your method)
IEnumerable<Quote> quotes = GetHistoryFromFeed("SPY");

// calculate WilliamsR(14)
IEnumerable<WilliamsResult> results = quotes.GetWilliamsR(14);
```

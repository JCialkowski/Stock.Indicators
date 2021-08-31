using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Skender.Stock.Indicators
{
    public class RsiIndicator : IAsyncEnumerable<RsiResult>, IDisposable
    {
        private Channel<IQuote> _channel =
            Channel.CreateUnbounded<IQuote>();

        private Channel<RsiResult> _resultChannel = Channel.CreateUnbounded<RsiResult>();

        private readonly RsiIndicatorCalculation _indicatorCalculation;

        public RsiIndicator(int lp)
        {
            _indicatorCalculation = new RsiIndicatorCalculation(lp);
            _ = Task.Run(async () => await Worker()).ConfigureAwait(false);
        }

        private RsiIndicator(IEnumerable<IQuote> quotes, int lp) : this(lp)
        {
            foreach (var data in quotes)
            {
                _channel.Writer.TryWrite(data);
            }
        }

        private async Task Worker()
        {
            await foreach (var item in _channel.Reader.ReadAllAsync())
            {
                await _resultChannel.Writer.WriteAsync(CalculateRsi(item));
            }
        }

        public void AddQuote(IQuote quote)
        {
            if (quote == null)
            {
                throw new ArgumentNullException(nameof(quote));
            }

            _channel.Writer.TryWrite(quote);
        }

        private RsiResult CalculateRsi(IQuote quote)
        {
            return _indicatorCalculation.Calculate(quote);
        }

        public static RsiIndicator Create(int loopbackPeriod)
        {
            return new RsiIndicator(loopbackPeriod);
        }


        public IAsyncEnumerator<RsiResult> GetAsyncEnumerator(CancellationToken cancellationToken = new()) =>
            _resultChannel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        public void Dispose()
        {
            _channel.Writer.Complete();
            _resultChannel.Writer.Complete();
        }
    }

    public interface IIndicatorDataBase<out T> where T : class, IResult
    {
        T Result();
    }

    public class IndicatorDataBase<T> : IIndicatorDataBase<T> where T : class, IResult
    {
        protected readonly T _result;
        public T Result() => _result;

        protected IndicatorDataBase(T result)
        {
            _result = result;
        }
    }

    public class RsiIndicatorData : IndicatorDataBase<RsiResult>
    {
        public IQuote Quote { get; }
        public decimal Loss { get; set; }
        public decimal Gain { get; set; }
        public decimal? AverageLoss { get; set; }
        public decimal? AverageGain { get; set; }

        public RsiIndicatorData(IQuote quote, RsiResult result) : base(result)
        {
            Quote = quote;
        }

        public void CalculateRsi()
        {
            if (AverageGain.HasValue)
            {
                _result.Rsi = AverageLoss > 0 ? 100 - (100 / (1 + AverageGain / AverageLoss)) : 100;
            }
        }
    }

    public interface IIndicatorCalculation<out T> where T : class, IResult
    {
        public T Calculate(IQuote quote);
    }

    // public abstract class IndicatorBase<T, TData> : IIndicatorCalculation<T>
    //     where T : class, IResult, new()
    // {
    //     public IndicatorBase()
    //     {
    //         // IIndicatorCalculation<IIndicatorDataBase<IResult>> asd = new RsiIndicatorCalculation
    //     }
    //     // public IResult Calculate(IQuote quote, in IEnumerable<IResult> results)
    //     // {
    //     // }
    //     public T Calculate(IQuote quote)
    //     {
    //         return CalculateIndicator(quote, previousData);
    //     }
    //
    //     protected abstract TData CalculateIndicator(IQuote quote, IEnumerable<TData> previousData);
    // }

    public class RsiIndicatorCalculation : IIndicatorCalculation<RsiResult>
    {
        private readonly int _loopbackPeriod;
        private readonly List<RsiIndicatorData> _previousData = new();

        public RsiIndicatorCalculation(int loopbackPeriod)
        {
            _loopbackPeriod = loopbackPeriod;
        }

        public RsiResult Calculate(IQuote quote)
        {
            var result = new RsiResult() { Date = quote.Date };
            var currentData = new RsiIndicatorData(quote, result);

            var h = quote.ConvertToBasic("C");

            var lastItem = _previousData.LastOrDefault();
            if (lastItem != null)
            {
                var lastValue = lastItem.Quote.ConvertToBasic("C").Value;
                currentData.Gain = (h.Value > lastValue) ? h.Value - lastValue : 0;
                currentData.Loss = (h.Value < lastValue) ? lastValue - h.Value : 0;

                if (_previousData.Count > _loopbackPeriod)
                {
                    currentData.AverageGain = (lastItem.AverageGain * (_loopbackPeriod - 1) + currentData.Gain) / _loopbackPeriod;
                    currentData.AverageLoss = (lastItem.AverageLoss * (_loopbackPeriod - 1) + currentData.Loss) / _loopbackPeriod;
                }
                else if (_previousData.Count == _loopbackPeriod)
                {
                    var collection = _previousData.Skip(1).ToList();
                    var sumGain = collection.Sum(x => x.Gain) + currentData.Gain;
                    var sumLoss = collection.Sum(x => x.Loss) + currentData.Loss;
                    currentData.AverageGain = sumGain / _loopbackPeriod;
                    currentData.AverageLoss = sumLoss / _loopbackPeriod;
                }

                currentData.CalculateRsi();
            }

            _previousData.Add(currentData);

            return result;
        }

        public class QuoteCalculator : IEnumerable<IEnumerable<IResult>>
        {
            private readonly List<IIndicatorCalculation<IResult>> _calculations = new() { new RsiIndicatorCalculation(14) };

            private readonly Dictionary<IQuote, List<IResult>> _items = new();

            public QuoteCalculator()
            {
                // _size = size;
            }

            public QuoteCalculator(IEnumerable<IQuote> collection) : this()
            {
                foreach (var item in collection.OrderBy(x => x.Date))
                {
                    Add(item);
                }
            }

            public void Add(IQuote quote)
            {
                // if (_items.Count == _size)
                // {
                //     _items.RemoveAt(_size - 1);
                // }
                //
                // _items.Insert(0, item);
                var results = new List<IResult>();
                foreach (var calculation in _calculations)
                {
                    var result = calculation.Calculate(quote);
                    results.Add(result);
                }

                _items.Add(quote, results);
            }

            internal void AddCalculation(IIndicatorCalculation<IResult> calculation)
            {
                foreach (var item in _items)
                {
                    var result = calculation.Calculate(item.Key);
                    item.Value.Add(result);
                }

                _calculations.Add(calculation);
            }

            public IEnumerator<IEnumerable<IResult>> GetEnumerator() => _items.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

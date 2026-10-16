using System.Globalization;
using Piggy.Helpers;

namespace Piggy.Tests;

public class MoneyTests
{
    [Fact]
    public void Text_WrapsFormattedAmountInMoneySpan()
    {
        using var _ = new CultureScope(CultureInfo.InvariantCulture);

        var html = Money.Text(1234.5m).ToString();

        Assert.Equal("<span class=\"money\">1,234.50 EUR</span>", html);
    }

    [Fact]
    public void Text_FormatsNegativeAmount()
    {
        using var _ = new CultureScope(CultureInfo.InvariantCulture);

        var html = Money.Text(-50m).ToString();

        Assert.Equal("<span class=\"money\">-50.00 EUR</span>", html);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _original;

        public CultureScope(CultureInfo culture)
        {
            _original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = culture;
        }

        public void Dispose() => CultureInfo.CurrentCulture = _original;
    }
}

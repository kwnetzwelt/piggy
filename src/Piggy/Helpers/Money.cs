using Microsoft.AspNetCore.Html;

namespace Piggy.Helpers;

public static class Money
{
    public static HtmlString Text(decimal amount) =>
        new($"<span class=\"money\">{amount.ToString("N2")} EUR</span>");
}

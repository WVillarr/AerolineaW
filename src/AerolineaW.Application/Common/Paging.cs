using FluentValidation;

namespace AerolineaW.Application.Common;

/// <summary>A requested page: 1-based, so <c>Page = 1</c> is the first page.</summary>
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Math.Max(Page, 1) - 1) * PageSize;
}

public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

using FluentValidation;

namespace DevOpsDays2026.Models;

public sealed class StockNewsRequestValidator : AbstractValidator<StockNewsRequest>
{
    public StockNewsRequestValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Ticker)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("Ticker must be 1-10 letters or digits.");

        RuleFor(request => request.Text)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(request => request.Date)
            .NotEmpty();
    }
}

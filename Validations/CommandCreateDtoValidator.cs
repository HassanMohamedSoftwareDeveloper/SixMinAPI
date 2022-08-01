using FluentValidation;
using SixMinAPI.Dtos;

namespace SixMinAPI.Validations;

public class CommandCreateDtoValidator : AbstractValidator<CommandCreateDto>
{
    public CommandCreateDtoValidator()
    {
        RuleFor(x => x.HowTo).NotEmpty();
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(5);
        RuleFor(x => x.CommandLine).NotEmpty();
    }
}

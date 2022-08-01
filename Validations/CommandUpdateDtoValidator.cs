using FluentValidation;
using SixMinAPI.Dtos;

namespace SixMinAPI.Validations;

public class CommandUpdateDtoValidator : AbstractValidator<CommandUpdateDto>
{
	public CommandUpdateDtoValidator()
	{
		RuleFor(x => x.HowTo).NotEmpty();
		RuleFor(x => x.Platform).NotEmpty().MaximumLength(5);
		RuleFor(x => x.CommandLine).NotEmpty();
	}
}

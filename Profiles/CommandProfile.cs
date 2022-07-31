using AutoMapper;
using SixMinAPI.Dtos;
using SixMinAPI.Models;

namespace SixMinAPI.Profiles;

public class CommandProfile : Profile
{
    public CommandProfile()
    {
        // Source --> Target
        CreateMap<Command, CommandReadDto>();
        CreateMap<CommandCreateDto, Command>();
        CreateMap<CommandUpdateDto, Command>();
    }
}

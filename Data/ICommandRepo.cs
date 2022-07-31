using SixMinAPI.Models;

namespace SixMinAPI.Data;

public interface ICommandRepo
{
    Task SaveChanges();
    Task<Command> GetCommandById(int id);
    Task<IEnumerable<Command>> GetAllCommands();
    Task CreateCommand(Command command);
    void DeleteCommand(Command command);
}

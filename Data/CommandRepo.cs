using Microsoft.EntityFrameworkCore;
using SixMinAPI.Models;

namespace SixMinAPI.Data;

public class CommandRepo : ICommandRepo
{
    private readonly AppDbContext _context;

    public CommandRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateCommand(Command command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));
        await _context.Commands.AddAsync(command);
    }

    public void DeleteCommand(Command command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));
        _context.Commands.Remove(command);
    }

    public async Task<IEnumerable<Command>> GetAllCommands() => await _context.Commands.ToListAsync();

    public async Task<Command> GetCommandById(int id) => await _context.Commands.FirstOrDefaultAsync(x => x.Id == id);

    public async Task SaveChanges() => await _context.SaveChangesAsync();
}

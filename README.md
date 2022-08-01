# What are minimal APIs?
Minimal APIs are architected to create HTTP APIs with minimal dependencies. They are ideal for microservices and apps that want to include only the minimum files, features, and dependencies in ASP.NET Core.

---

# Model-View-Controller
-	Software design pattern (created @Xerox Parc in the late 1970s)
-	Splits program into three (interconnected) elements.
-	Widley adopted in web development frameworks:
    - Ruby in Rails
	- Spring
	- ASP.NET MVC

![Model-View-Controller!](/DocsImg/1.png "Model-View-Controller")

---

# ASP.NET MVC API Architecture
![ASP.NET MVC API Architecture!](/DocsImg/2.png "ASP.NET MVC API Architecture")

---
# What does this have to do with minimal APIs?
## Minimal APIs don’t have a controller…
>(Or use the MVC framework as a whole…)

This means *….

**Minimal APIs:**
- Don’t support model validation.
- Don’t support for JSONPatch.
- Don’t support filters.
- Don’t support custom model binding (Support for IModelBinder).

---
# Minimal APIs
![Minimal APIs!](/DocsImg/3.png "Minimal APIs")

---
## Start Coding:
1. Create Application
```cli
dotnet  new webapi -minimal -n SixMinAPI
```
2. Trust Https
```cli
dotnet  dev-certs https --trust
```
3. Run Application 
```cli
dotnet  run || dotnet watch run
```
4. Add some Packages
``` cli
dotnet  add package Microsoft.EntityFrameworkCore
dotnet  add package Microsoft.EntityFrameworkCore.Design
dotnet  add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```
5. Initiate User Secrets to store Passwords and User Ids
```cli
dotnet  user-secrets init
```
6. Install Docker if not installed
7. Check Docker installed 
```cli
docker --version
```
8. Check installed containers
```cli
docker ps
```
9. Add docker-compose.yaml file
``` yaml
version: '3.9'
services:
  sqlserver:
    image: "mcr.microsoft.com/mssql/server:2022-latest"
    environment: 
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "P@ssw0rd"
      MSSQL_PID: "Express"
    ports:
      -  "1433:1433"
```
10. Run docker-compose file 
``` cli
docker-compose up -d
```
11. Add Models and Dtos
``` C#
using System.ComponentModel.DataAnnotations;

namespace SixMinAPI.Models;

public class Command
{
    [Key] public int Id { get; set; }
    [Required] public string HowTo { get; set; }
    [Required][MaxLength(5)] public string Platform { get; set; }
    [Required] public string CommandLine { get; set; }
}
```
``` C#
using System.ComponentModel.DataAnnotations;

namespace SixMinAPI.Dtos;

public class CommandReadDto
{
    public int Id { get; set; }
    public string HowTo { get; set; }
    public string Platform { get; set; }
    public string CommandLine { get; set; }
}

public class CommandCreateDto
{
    [Required] public string HowTo { get; set; }
    [Required][MaxLength(5)] public string Platform { get; set; }
    [Required] public string CommandLine { get; set; }
}

public class CommandUpdateDto
{
    [Required] public string HowTo { get; set; }
    [Required][MaxLength(5)] public string Platform { get; set; }
    [Required] public string CommandLine { get; set; }
}
```
12. Add DbContext
``` C#
using Microsoft.EntityFrameworkCore;
using SixMinAPI.Models;

namespace SixMinAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Command> Commands => Set<Command>();
}
```
13. Register DbContext
    a. Add connection string to AppSettings
    ```json
    "SQLDbConnection": "Server=localhost,1433;Initial Catalog=CommandDB;"
    ```
    b. Create user secret for User Id
    ```cli
    dotnet user-secrets set “UserId” “sa”
    ```
      c. Create user secret for Password
    ```cli
    dotnet user-secrets set “Password” “P@ssw0rd”
    ```
    d. Register AppDbContext in program
    ```C#
    var sqlConBuilder = new SqlConnectionStringBuilder
    {
        ConnectionString = builder.Configuration.GetConnectionString("SQLDbConnection"),
        UserID = builder.Configuration["UserId"],
        Password = builder.Configuration["Password"],
    };
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(sqlConBuilder.ConnectionString));
    ```
14. Generate Migration
```cli
    dotnet ef migrations add initialmigration

    dotnet tool install –global dotnet-ef   ->  if error occured
```
15. Create Database
```cli
    dotnet ef database update
```
16.	Add Repository
```C#
using Microsoft.EntityFrameworkCore;
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
```
17.	Register Repositories in DI
```C#
    builder.Services.AddScoped<ICommandRepo, CommandRepo>();
```
18.	Add Automapper
```C#
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
```
19.	Register Automapper 
```C#
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
```
20.	Add Minimal APIs in Program file 
```C#
app.MapGet("api/v1/commands", async (ICommandRepo repo, IMapper mapper) =>
{
    var commands = await repo.GetAllCommands();
    return Results.Ok(mapper.Map<IEnumerable<CommandReadDto>>(commands));
});

app.MapGet("api/v1/commands/{id}", async (ICommandRepo repo, IMapper mapper, [FromRoute] int id) =>
{
    var command = await repo.GetCommandById(id);
    if (command is not null)
        return Results.Ok(mapper.Map<CommandReadDto>(command));
    return Results.NotFound();
});

app.MapPost("api/v1/commands", async (ICommandRepo repo, IMapper mapper, [FromBody] CommandCreateDto commandDto) =>
    {
        var commandModel = mapper.Map<Command>(commandDto);

        await repo.CreateCommand(commandModel);
        await repo.SaveChanges();

        var cmdReadDto = mapper.Map<CommandReadDto>(commandModel);
        return Results.Created($"api/v1/commands/{cmdReadDto.Id}", cmdReadDto);
    });

app.MapPut("api/v1/commands/{id}", async (ICommandRepo repo, IMapper mapper, [FromRoute] int id, [FromBody] CommandUpdateDto commandDto) =>
{
    var command = await repo.GetCommandById(id);
    if (command is null)
        return Results.NotFound();
    mapper.Map(commandDto, command);
    await repo.SaveChanges();
    return Results.NoContent();
});

app.MapDelete("api/v1/commands/{id}", async (ICommandRepo repo, IMapper mapper, [FromRoute] int id) =>
{
    var command = await repo.GetCommandById(id);
    if (command is null)
        return Results.NotFound();
    repo.DeleteCommand(command);
    await repo.SaveChanges();
    return Results.NoContent();
});
```

---
# Filters
-	Allow you to run code before or after stages in the Filter Pipeline.
-	Each Filter Type is executed at a different stage in the pipeline:
    -	Authorization
    -	Response
    -	Action
    -	Exception
    -	Rsult
-	Custom filters can be scoped:
    -	Globally (all controllers, actions and Razor Pages)
    -	To a Controller
    -	To an Action
-	We have Synchronous and Asynchronous versions

## Request Vs Filter Pipelines:
![Request Vs Filter Pipelines!](/DocsImg/4.png "Request Vs Filter Pipelines")

## Custom Model Binding (IModelBinder)
-	Allow Controller Actions to work directly with Model Types
    -	(Rather than HTTP requests)
-	Can be used in more complex “binding scenarios”
    -	E.g. if you need to transform data
-	Default model binders support most common .NET types
    -	They meet most developers needs! 
-	Model Binding Occurs before Model Validation
### Example *
![Custom Model Binding (IModelBinder)!](/DocsImg/5.png "Custom Model Binding (IModelBinder)")
![Custom Model Binding (IModelBinder)!](/DocsImg/6.png "Custom Model Binding (IModelBinder)")

---
## Model Validation
-	Model Validation Occurs after Model Binding
-	Reports business rule type errors, e.g.
    -	Input string value length > max allowed by the model
-	Out the box with the [ApiController] attribute
-	Validation can be added to .NET Minimal APIs, e.g.:
    -	FluentValidation, MinimalValidation

# Extend Minimal APIs with Swagger
1. Add Some packages
```cli
    dotnet  add package Swashbuckle.AspNetCore
```
2. Register Swagger Services
```C#
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
```
3. Configure App to use swagger
```C#
     app.UseSwagger();
     app.UseSwaggerUI();
```
## To set sample response with swagger
> You can use attributes

```C#
app.MapGet("api/v1/commands",
[ProducesResponseType(200, Type = typeof(CommandReadDto))]
async (ICommandRepo repo, IMapper mapper) =>
{
    var commands = await repo.GetAllCommands();
    return Results.Ok(mapper.Map<IEnumerable<CommandReadDto>>(commands));
});
```
> Or Use Fluent Manner with <code>Produces</code> Method
```C#
app.MapGet("api/v1/commands/{id}", async (ICommandRepo repo, IMapper mapper, [FromRoute] int id) =>
{
    var command = await repo.GetCommandById(id);
    if (command is not null)
        return Results.Ok(mapper.Map<CommandReadDto>(command));
    return Results.NotFound();
}).Produces<CommandReadDto>(200);
```
# Extend Minimal APIs with authentication
1. Register Services
```C#
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
```
2. Configure App 
```C#
    app.UseAuthentication();
    app.UseAuthorization();
```
> Now Authorization is upported but with the default Schema

> **So we need to use JwtBearer**
Add Some packages
```cli
    dotnet  add package Microsoft.AspNetCore.Authentication.JwtBearer
```
Register Authentication Service to use Jwt default schema
```C#
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();
```
> To Authorize endpoint you can use <code>Authorize</code> attribute
```C#
app.MapPost("api/v1/commands",
    [Authorize] 
async (ICommandRepo repo, IMapper mapper, [FromBody] CommandCreateDto commandDto) =>
    {
        var commandModel = mapper.Map<Command>(commandDto);

        await repo.CreateCommand(commandModel);
        await repo.SaveChanges();

        var cmdReadDto = mapper.Map<CommandReadDto>(commandModel);
        return Results.Created($"api/v1/commands/{cmdReadDto.Id}", cmdReadDto);
    });
```
> Or use <code>RequireAuthorization</code> Method in Fluent manner
```C#
app.MapPut("api/v1/commands/{id}", 
    async (ICommandRepo repo, IMapper mapper, [FromRoute] int id, [FromBody] CommandUpdateDto commandDto) =>
{
    var command = await repo.GetCommandById(id);
    if (command is null)
        return Results.NotFound();
    mapper.Map(commandDto, command);
    await repo.SaveChanges();
    return Results.NoContent();
}).RequireAuthorization();
```

> Or you can authorize all endpoints
```C#
builder.Services.AddAuthorization(opt =>
{
    opt.FallbackPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build();
});
```
> If you need some endpoints to allow anonymous use <code>AllowAnonymous</code> in fluent manner
```C#
app.MapGet("api/v1/commands/{id}", async (ICommandRepo repo, IMapper mapper, [FromRoute] int id) =>
{
    var command = await repo.GetCommandById(id);
    if (command is not null)
        return Results.Ok(mapper.Map<CommandReadDto>(command));
    return Results.NotFound();
}).Produces<CommandReadDto>(200)
.AllowAnonymous();
```

# Extend Minimal APIs with (authentication and swagger)
> register Swagger Gen service with some options
```C#
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the bearer schema",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference=new OpenApiReference
                {
                     Id = "Bearer",
                    Type = ReferenceType.SecurityScheme,
                }
            },
            new List<string>()
        }
    });
});
```
> You will find an Authorize button like below picture
![Minimal APIs with (authentication and swagger)!](/DocsImg/7.png "Minimal APIs with (authentication and swagger)")

# Extend Minimal APIs with Validation
> As we annonce before Minimal APIs <code>Don’t support model validation</code> but still we can validate model 
using <code>FluentValidation</code> or <code>MinimalValidation</code>

>Lets validate model using <code>FluentValidation</code>

1. Add FluentValidation packages
```cli
    dotnet add package FluentValidation.AspNetCore
    dotnet add package FluentValidation.DependencyInjectionExtensions 
```
2. Add folder with name <code>Validations</code>
3. Add Validator classes
```C#
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


public class CommandUpdateDtoValidator : AbstractValidator<CommandUpdateDto>
{
	public CommandUpdateDtoValidator()
	{
		RuleFor(x => x.HowTo).NotEmpty();
		RuleFor(x => x.Platform).NotEmpty().MaximumLength(5);
		RuleFor(x => x.CommandLine).NotEmpty();
	}
}
```
4. Register FluentValidation
```C#
    builder.Services.AddFluentValidation(opt => 
    opt.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
```
5. In Endpoint Inject <code>IValidator< Type></code> and validate manualy.
```C#
app.MapPost("api/v1/commands",
// [Authorize]
async (ICommandRepo repo, IMapper mapper, IValidator<CommandCreateDto> validator, [FromBody] CommandCreateDto commandDto) =>
    {
        var validationResult = await validator.ValidateAsync(commandDto);
        if (validationResult.IsValid is false)
        {
            var errors =new { errors = validationResult.Errors.Select(x => x.ErrorMessage) };
            return Results.BadRequest(errors);
        }
        var commandModel = mapper.Map<Command>(commandDto);

        await repo.CreateCommand(commandModel);
        await repo.SaveChanges();

        var cmdReadDto = mapper.Map<CommandReadDto>(commandModel);
        return Results.Created($"api/v1/commands/{cmdReadDto.Id}", cmdReadDto);
    }).AllowAnonymous();

app.MapPut("api/v1/commands/{id}",
    async (ICommandRepo repo, IMapper mapper, IValidator<CommandUpdateDto> validator, [FromRoute] int id, [FromBody] CommandUpdateDto commandDto) =>
    {
        var validationResult = await validator.ValidateAsync(commandDto);
        if (validationResult.IsValid is false)
        {
            var errors =new { errors = validationResult.Errors.Select(x => x.ErrorMessage) };
            return Results.BadRequest(errors);
        }
        var command = await repo.GetCommandById(id);
        if (command is null)
            return Results.NotFound();
        mapper.Map(commandDto, command);
        await repo.SaveChanges();
        return Results.NoContent();
    }).AllowAnonymous();//.RequireAuthorization();

```
# Topic from Microsoft Docs : 
https://docs.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-6.0&tabs=visual-studio


## Source Code
https://github.com/HassanMohamedSoftwareDeveloper/SixMinAPI
using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SixMinAPI.Data;
using SixMinAPI.Dtos;
using SixMinAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
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


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization(opt =>
{
    opt.FallbackPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build();
});

var sqlConBuilder = new SqlConnectionStringBuilder
{
    ConnectionString = builder.Configuration.GetConnectionString("SQLDbConnection"),
    UserID = builder.Configuration["UserId"],
    Password = builder.Configuration["Password"],
};

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(sqlConBuilder.ConnectionString));
builder.Services.AddScoped<ICommandRepo, CommandRepo>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddFluentValidation(opt => opt.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapGet("api/v1/commands",
[ProducesResponseType(200, Type = typeof(CommandReadDto))]
async (ICommandRepo repo, IMapper mapper) =>
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
}).Produces<CommandReadDto>(200)
.AllowAnonymous();

app.MapPost("api/v1/commands",
// [Authorize]
async (ICommandRepo repo, IMapper mapper, IValidator<CommandCreateDto> validator, [FromBody] CommandCreateDto commandDto) =>
    {
        var validationResult = await validator.ValidateAsync(commandDto);
        if (validationResult.IsValid is false)
        {
            var errors = new { errors = validationResult.Errors.Select(x => x.ErrorMessage) };
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
            var errors = new { errors = validationResult.Errors.Select(x => x.ErrorMessage) };
            return Results.BadRequest(errors);
        }
        var command = await repo.GetCommandById(id);
        if (command is null)
            return Results.NotFound();
        mapper.Map(commandDto, command);
        await repo.SaveChanges();
        return Results.NoContent();
    }).AllowAnonymous();//.RequireAuthorization();

app.MapDelete("api/v1/commands/{id}", async (ICommandRepo repo, IMapper mapper, [FromRoute] int id) =>
{
    var command = await repo.GetCommandById(id);
    if (command is null)
        return Results.NotFound();
    repo.DeleteCommand(command);
    await repo.SaveChanges();
    return Results.NoContent();
});

app.Run();

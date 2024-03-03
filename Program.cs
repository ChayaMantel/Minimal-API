using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Version = "v1" });
});

// Configure services
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.36-mysql")), ServiceLifetime.Singleton);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
});
// Enable CORS
app.UseCors("AllowAll");
// Define your API endpoints
app.MapGet("/", () => "hello");
app.MapGet("/api/items", GetAllTasks);
app.MapPost("/api/items", AddTask);
app.MapPut("/api/items/{id}", UpdateTask);
app.MapDelete("/api/items/{id}", DeleteTask);

app.Run();
// Implement your API endpoint handlers
async Task GetAllTasks(ToDoDbContext dbContext, HttpContext context)
{
    var tasks = await dbContext.Items.ToListAsync();
    await context.Response.WriteAsJsonAsync(tasks);
}

async Task AddTask(ToDoDbContext dbContext, HttpContext context, Item item)
{
    dbContext.Items.Add(item);
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status201Created;
    await context.Response.WriteAsJsonAsync(item);
}

async Task UpdateTask(ToDoDbContext dbContext, HttpContext context, int id, Item updatedItem)
{
    // Validate the updatedItem parameter
   
    var existingItem = await dbContext.Items.FindAsync(id);
    if (existingItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync($"Item with ID {id} not found");
        return;
    }

    existingItem.IsComplete = updatedItem.IsComplete;
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsJsonAsync(existingItem);

}

async Task DeleteTask(ToDoDbContext dbContext, HttpContext context, int id)
{
    var existingItem = await dbContext.Items.FindAsync(id);
    if (existingItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    dbContext.Items.Remove(existingItem);
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status200OK;
}

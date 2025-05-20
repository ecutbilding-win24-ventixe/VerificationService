using Azure.Communication.Email;
using Microsoft.EntityFrameworkCore;
using Presentation.Data.Contexts;
using Presentation.Interfaces;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(x => new EmailClient(builder.Configuration["AzureCommunicationService:ConnectionString"]));
builder.Services.AddDbContext<DataContext>(x => x.UseSqlServer(builder.Configuration["SqlDatabase:SqlConnectionString"]));

builder.Services.AddHttpClient<VerificationService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();

var app = builder.Build();
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verification API V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapControllers();
app.Run();

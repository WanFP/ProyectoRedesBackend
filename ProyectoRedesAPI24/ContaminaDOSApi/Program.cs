var builder = WebApplication.CreateBuilder(args);

// Configurar Kestrel solo para HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP
     options.ListenAnyIP(5001, listenOptions =>
     {
        listenOptions.UseHttps(); // HTTPS
     });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

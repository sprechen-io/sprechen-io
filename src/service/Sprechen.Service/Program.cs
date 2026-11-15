using Sprechen.Service.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddSprechenService();

var app = builder.Build();
app.UseSprechenService();

app.Run();
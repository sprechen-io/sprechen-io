using Sprechen.Contracts.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddSprechenContracts();

var app = builder.Build();
app.UseSprechenContracts();

app.Run();


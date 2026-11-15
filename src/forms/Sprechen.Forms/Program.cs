using Sprechen.Forms.Extensions;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.AddSprechenForms();

var app = builder.Build();
app.UseSprechenForms();

app.Run();


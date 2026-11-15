using Sprechen.Web.HostSupport.Extensions;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.AddHostedVoiceWeb("Sprechen.Voice API");

var app = builder.Build();
app.UseHostedVoiceWeb<Sprechen.Voice.Components.App, Sprechen.Voice.App.Layout.MainLayout>();

app.Run();



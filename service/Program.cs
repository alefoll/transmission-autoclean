using service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => {
    options.ServiceName = "Transmission Auto Clean";
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

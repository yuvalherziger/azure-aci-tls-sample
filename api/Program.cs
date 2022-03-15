using api.Util;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

var app = new CommandLineApplication();

app.HelpOption();

var port = app.Option<int>("-p|--port <PORT>", "The port the service should expose", CommandOptionType.SingleValue);
port.DefaultValue = 443;
var tlsCertPath = app.Option("-c|--cert <TLS_CERT_PATH>", "The path to the TLS certificate on the filesystem", CommandOptionType.SingleValue);
tlsCertPath.DefaultValue = "/tls-cert/tls/cert.crt";
var tlsPrivateKeyPath = app.Option("-k|--private-key <TLS_PRIVATE_KEY_PATH>", "The path to the TLS private key on the filesystem", CommandOptionType.SingleValue);
tlsPrivateKeyPath.DefaultValue = "/tls-cert/pk.key";

app.OnExecute(() =>
{
    var certUtil = new CertUtil();
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Read certificate data:
    X509Certificate2 tlsCert = certUtil.attachPrivateKeyToTLSCert(
        new X509Certificate2(tlsCertPath.Value()), 
        Encoding.Default.GetString(
            certUtil.readFile(tlsPrivateKeyPath.Value())
        )
    );

    builder.WebHost.ConfigureKestrel(serverOptions => {
        serverOptions.ListenAnyIP(port.ParsedValue, listenOption => {
            listenOption.UseHttps(tlsCert);
        });
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
});

return app.Execute(args);

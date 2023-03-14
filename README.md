# ssh-scp
Provides SCP functionality

# features
- **Download a file**
- **Upload a file**
- **Get list of dirs**
- **Get list of files**
- **Check a file is exist**

Follow this project: [Sample-Project](https://github.com/aliasadidev/ssh-scp/tree/main/sample/SshScp.Sample)

# appsettings
```json
{
  "SshSettings": {
    "ServerUrl": "127.0.0.1",
    "Port": 22,
    "User": "ali",
    "PrivateKeyPath": "./keys/id_rsa",
    "MaxConnections": 10,
    "ListCommand": {
      "ls_only_dir": {
        "win32": "Get-ChildItem {0} -Directory -Name",
        "linux": "ls -l {0} | awk '/^d/{{print $9}}'"
      },
      "ls_only_file": {
        "win32": "Get-ChildItem {0} -File -Name",
        "linux": "ls -Ap {0} | egrep -v /$"
      },
      "file_exists": {
        "win32": "Test-Path -Path {0} -PathType Leaf",
        "linux": "[ -f {0} ] && echo True || echo False"
      }
    }
  }
}
```

# Sample
```cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.DevTunnels.Ssh.Tcp;
using Microsoft.DevTunnels.Ssh;
using System.Diagnostics;
using SshScp.Sample.SSH;
using SshScp;
using SshScp.Sample;

IConfiguration configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();

//setup DI
var serviceProvider = new ServiceCollection()
    .AddSingleton<SshClient>(sp => new SshClient(SshSessionConfiguration.Default, new TraceSource(nameof(SshClient))))
    .AddTransient<ICustomSshClientSession, CustomSshClientSession>()
    .AddTransient<ICustomSshClient, CustomSshClient>()
    .AddTransient<ISshService, SshService>()
    .AddTransient<ScpClient>()
    .Configure<SshSettings>(configuration.GetSection("SshSettings"))
    .BuildServiceProvider();


using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Debug).AddConsole());

ILogger logger = loggerFactory.CreateLogger<Program>();



var sshService = serviceProvider.GetService<ISshService>();

var files = await sshService.ListFilesInDirectory("/home/ali/github/ssh-scp/sample/SshScp.Sample", OperatingSystems.linux);
var dirs = await sshService.ListDirectories("/home/ali/github/ssh-scp/", OperatingSystems.linux);

System.Console.WriteLine("------------------------- DIRs -----------------------");
foreach (var dir in dirs)
{
  System.Console.WriteLine(dir);
}

System.Console.WriteLine("------------------------- FILES -----------------------");

foreach (var file in files)
{
  System.Console.WriteLine(file);
}

System.Console.WriteLine("------------------------- Download a file -----------------------");

byte[] byteArray = await sshService.DownloadFile("/home/ali/github/csharp-refactor/images/icon.png", OperatingSystems.linux);
File.WriteAllBytes("icon.png", byteArray);

System.Console.WriteLine("------------------------- Upload a file -----------------------");

FileStream fileStream = new FileStream("/home/ali/github/csharp-refactor/images/icon.png", FileMode.Open, FileAccess.Read);
await sshService.UploadFile(fileStream, "new.png", "/home/ali/github/ssh-scp/", OperatingSystems.linux);



```
# Result
![overview.jpg](sample/SshScp.Sample/images/overview.jpg)

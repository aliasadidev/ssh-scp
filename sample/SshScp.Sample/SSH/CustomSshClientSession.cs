using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DevTunnels.Ssh;
using Microsoft.DevTunnels.Ssh.Algorithms;
using Microsoft.DevTunnels.Ssh.Tcp;
using Microsoft.Extensions.Options;

namespace SshScp.Sample.SSH;

public sealed class CustomSshClientSession : ICustomSshClientSession, IDisposable
{
  private readonly SshClient _sshClient;
  private readonly SshSettings _sshSetting;
  private static int _currentConnections = 0;
  private static readonly object _object = new object();
  private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10);

  public CustomSshClientSession(SshClient sshClient, IOptions<SshSettings> sshOptions)
  {
    _sshClient = sshClient;
    _sshSetting = sshOptions.Value ?? throw new ArgumentNullException(nameof(sshOptions));
  }

  public async Task<SshClientSession> GetSession()
  {
    Interlocked.Increment(ref _currentConnections);
    int currentConnectionNumber = _currentConnections;
    SshClientSession session = await _sshClient.OpenSessionAsync(_sshSetting.ServerUrl, _sshSetting.Port);
    // Handle server public key authentication.
    session.Authenticating += (_, e) =>
    {
      e.AuthenticationTask = Task.Run(() =>
          {
          IKeyPair hostKey = e.PublicKey;
          var serverIdentity = new ClaimsIdentity();
          return new ClaimsPrincipal(serverIdentity);
        });
    };
    var privateKey = KeyPair.ImportKeyFile(_sshSetting.PrivateKeyPath);
    SshClientCredentials credentials = (_sshSetting.User, privateKey);
    bool ignoreRequest = false;
    await _semaphore.WaitAsync();
    try
    {
      if (currentConnectionNumber > _sshSetting.MaxConnections)
      {
        ignoreRequest = true;
      }
      else
      {
        // SSH server's username = _sshSetting.User
        if (!await session.AuthenticateAsync(credentials))
        {
          throw new Exception("Authentication failed.");
        }
      }
    }
    finally
    {
      _semaphore.Release();
    }

    if (ignoreRequest)
    {
      throw new Exception("Maximum number of connections exceeded");
    }

    return session;
  }

  public async Task CloseSession(SshClientSession session)
  {
    await session.CloseAsync(SshDisconnectReason.None);
    Interlocked.Decrement(ref _currentConnections);
  }

  public void Dispose()
  {
    Interlocked.Decrement(ref _currentConnections);
  }
}

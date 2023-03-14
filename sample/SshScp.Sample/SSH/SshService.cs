using Microsoft.Extensions.Options;
using Microsoft.DevTunnels.Ssh;

namespace SshScp.Sample.SSH;

public interface ISshService
{
  Task<bool> Exists(string path, OperatingSystems os);
  Task<string[]> ReadAllLines(string path);
  Task<List<string>> ListFilesInDirectory(string dir, OperatingSystems os);
  Task<List<string>> ListDirectories(string dir, OperatingSystems os);
  Task<byte[]> DownloadFile(string filePath, OperatingSystems os);
  Task UploadFile(Stream content, string fileName, string targetPath, OperatingSystems os);
}

public class SshService : ISshService
{
  private readonly IOptions<SshSettings> _settings;
  private readonly ScpClient _scpClient;
  private readonly ICustomSshClientSession _sshClientSession;
  private readonly ICustomSshClient _sshClient;

  public SshService(ICustomSshClientSession sshClientSession, ICustomSshClient sshClient, ScpClient scpClient, IOptions<SshSettings> settings)
  {
    _sshClientSession = sshClientSession;
    _sshClient = sshClient;
    _scpClient = scpClient;
    _settings = settings;
  }

  public SshSettings Configuration => _settings.Value;

  public async Task<bool> Exists(string path, OperatingSystems os)
  {
    using var session = await _sshClientSession.GetSession();
    SshChannel channel = await session.OpenChannelAsync();

    var file = os == OperatingSystems.win32 ? path.Replace("/", @"\") : path;
    var command = string.Format(Configuration.ListCommand[CommandNames.file_exists][os], file);
    var response = await _sshClient.ExecuteCommand(channel, command);
    var result = false;

    result = response == "True";

    await _sshClientSession.CloseSession(session);
    return result;
  }

  public async Task<string[]> ReadAllLines(string path)
  {
    using var session = await _sshClientSession.GetSession();
    SshChannel channel = await session.OpenChannelAsync();
    var result = await _scpClient.ReadLines(channel, path);
    await _sshClientSession.CloseSession(session);
    return result.ToArray();
  }

  public async Task<List<string>> ListFilesInDirectory(string dir, OperatingSystems os)
  {
    using var session = await _sshClientSession.GetSession();
    SshChannel channel = await session.OpenChannelAsync();
    var path = os == OperatingSystems.win32 ? dir.Replace("/", @"\") : dir;
    var command = string.Format(Configuration.ListCommand[CommandNames.ls_only_file][os], path);
    var result = await _sshClient.GetDirectories(channel, command, os);
    await _sshClientSession.CloseSession(session);
    return result;
  }

  public async Task<List<string>> ListDirectories(string dir, OperatingSystems os)
  {
    using var session = await _sshClientSession.GetSession();
    SshChannel channel = await session.OpenChannelAsync();
    var path = os == OperatingSystems.win32 ? dir.Replace("/", @"\") : dir;
    var command = string.Format(Configuration.ListCommand[CommandNames.ls_only_dir][os], path);
    var result = await _sshClient.GetDirectories(channel, command, os);
    await _sshClientSession.CloseSession(session);
    return result;
  }


  public async Task<byte[]> DownloadFile(string filePath, OperatingSystems os)
  {
    using var mem = new MemoryStream();
    using var session = await _sshClientSession.GetSession();
    SshChannel channel = await session.OpenChannelAsync();
    var path = os == OperatingSystems.win32 ? filePath.Replace("/", @"\") : filePath;
    await _scpClient.Download(channel, filePath, mem);
    await _sshClientSession.CloseSession(session);
    return mem.ToArray();
  }

  public async Task UploadFile(Stream content, string fileName, string targetPath, OperatingSystems os)
  {
    using var mem = new MemoryStream();
    using var session = await _sshClientSession.GetSession();
    SshChannel channel = await session.OpenChannelAsync();
    //var path = os == OperatingSystems.win32 ? filePath.Replace("/", @"\") : filePath;
    await _scpClient.Upload(channel, content, targetPath, fileName);
    await _sshClientSession.CloseSession(session);
  }
}

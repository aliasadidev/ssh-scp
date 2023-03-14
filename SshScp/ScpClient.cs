using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DevTunnels.Ssh;
using Microsoft.DevTunnels.Ssh.Messages;

namespace SshScp;

public class ScpClient
{
  private static char[] _byteToChar;
  private static readonly Regex _fileInfoRe = new Regex(@"C(?<mode>\d{4}) (?<length>\d+) (?<filename>.+)");

  static ScpClient()
  {
    if (_byteToChar == null)
    {
      _byteToChar = new char[128];
      var ch = '\0';
      for (int i = 0; i < 128; i++)
      {
        _byteToChar[i] = ch++;
      }
    }
  }

  public async Task Download(SshChannel channel, string path, Stream output)
  {
    using var input = new PipeStream();

    channel.DataReceived += delegate (object sender, Microsoft.DevTunnels.Ssh.Buffer buffer)
    {
      var bufferArray = buffer.ToArray();
      input.Write(bufferArray, 0, bufferArray.Length);
      input.Flush();
    };

    var commandAuthorized = await channel.RequestAsync(new CommandRequestMessage($"scp -f \"{path}\""));
    if (commandAuthorized)
    {
      //  Send reply
      await channel.SendAsync(new byte[] { 0 }, default);
      var message = ReadString(input);
      var match = _fileInfoRe.Match(message);

      if (match.Success)
      {
        await channel.SendAsync(new byte[] { 0 }, default); //  Send reply

        var mode = match.Result("${mode}");
        var length = long.Parse(match.Result("${length}"));
        var fileName = match.Result("${filename}");

        await InternalDownload(channel, input, output, fileName, length);
      }
    }
    else
    {
      throw new Exception("command is unauthorized");
    }
  }



  public async Task Upload(SshChannel channel, Stream source, string path, string serverFileName)
  {

    using var input = new PipeStream();

    channel.DataReceived += delegate (object sender, Microsoft.DevTunnels.Ssh.Buffer buffer)
    {
      var bufferArray = buffer.ToArray();
      input.Write(bufferArray, 0, bufferArray.Length);
      input.Flush();
    };

    var commandAuthorized = await channel.RequestAsync(new CommandRequestMessage($"scp -t -d \"{path}\""));

    CheckReturnCode(input);

    await channel.SendAsync(System.Text.Encoding.UTF8.GetBytes(string.Format("C0644 {0} {1}\n", source.Length, serverFileName).ToCharArray()), default);

    CheckReturnCode(input);

    await UploadFileContent(channel, input, source, serverFileName);

  }

  private async Task UploadFileContent(SshChannel channel, Stream input, Stream source, string remoteFileName)
  {
    var totalLength = source.Length;
    var BufferSize = 1024 * 16;
    var buffer = new byte[BufferSize];

    var read = source.Read(buffer, 0, buffer.Length);

    long totalRead = 0;

    while (read > 0)
    {
      await channel.SendAsync(buffer, default);

      totalRead += read;

      read = source.Read(buffer, 0, buffer.Length);
    }

    await channel.SendAsync(new byte[] { 0 }, default);
    CheckReturnCode(input);
  }


  private void CheckReturnCode(Stream input)
  {
    var b = ReadByte(input);

    if (b > 0)
    {
      var errorText = ReadString(input);

      throw new Exception(errorText);
    }
  }

  public async Task<List<string>> ReadLines(SshChannel channel, string path)
  {
    using var output = new MemoryStream();
    using var streamReader = new StreamReader(output);

    await Download(channel, path, output);

    var lines = new List<string>();

    output.Seek(0, SeekOrigin.Begin);

    while (!streamReader.EndOfStream)
    {
      var line = streamReader.ReadLine().Replace(Environment.NewLine, string.Empty);
      if (line is not null)
        lines.Add(line);
    }
    return lines;
  }

  private static async Task InternalDownload(SshChannel channel, Stream input, Stream output, string filename, long length)
  {
    var BufferSize = 1024 * 16;
    var buffer = new byte[Math.Min(length, BufferSize)];
    var needToRead = length;

    do
    {
      var read = input.Read(buffer, 0, (int)Math.Min(needToRead, BufferSize));
      output.Write(buffer, 0, read);
      needToRead -= read;
    }
    while (needToRead > 0);
    output.Flush();
    //  Send confirmation byte after last data byte was read
    await channel.SendAsync(new byte[] { 0 }, default);
  }

  private static string ReadString(Stream stream)
  {
    var hasError = false;
    var sb = new StringBuilder();
    var b = ReadByte(stream);

    if (b == 1 || b == 2)
    {
      hasError = true;
      b = ReadByte(stream);
    }

    var ch = _byteToChar[b];

    while (ch != '\n')
    {
      sb.Append(ch);
      b = ReadByte(stream);
      ch = _byteToChar[b];
    }

    if (hasError)
      throw new FileNotFoundException(sb.ToString());

    return sb.ToString();
  }

  private static int ReadByte(Stream stream)
  {
    var b = stream.ReadByte();

    while (b < 0)
    {
      Thread.Sleep(100);
      b = stream.ReadByte();
    }

    return b;
  }
}

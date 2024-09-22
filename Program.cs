using System.Diagnostics;
using System.Web;
using FluentFTP;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var ftpHost = config["Ftps:Host"]!;
var ftpUser = config["Ftps:User"]!;
var ftpPassword = config["Ftps:Password"]!;

var localLogFolder = config["LocalLogsFolder"]!;
var remoteLogFolder = config["RemoteLogsFolder"]!;

var seqTag = $"prod-gif-bot-{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
var commands= $$"""/c cd {{localLogFolder}} && seqcli ingest -i *.txt -x "{@t:timestamp} [{@l:level}] {@m:*}{:n}{@x:*}" -p application="{{seqTag}}" """;
var viewLogsSeqUrl = $"http://localhost:5341/#/events?range=7d&filter=application%20%3D%20'{HttpUtility.UrlEncode(seqTag)}'";

// Prepare directory for logs
if (Directory.Exists(localLogFolder))
{
    Directory.Delete(localLogFolder, true);
}
Directory.CreateDirectory(localLogFolder);
Console.WriteLine("Directory prepared");

// Download logs from Azure via Ftps
var client = new AsyncFtpClient(ftpHost, ftpUser, ftpPassword);
await client.AutoConnect();
await client.DownloadDirectory(localLogFolder, remoteLogFolder);
await client.Disconnect();
Console.WriteLine("Logs downloaded");

// Ingest Seq with logs
var process = new Process();
process.StartInfo.FileName = "cmd.exe";
process.StartInfo.Arguments = commands;
process.StartInfo.CreateNoWindow = true;
process.Start();
process.WaitForExit();
Console.WriteLine($"Seq ingested{Environment.NewLine}");

// Log results
Console.WriteLine($"Command: {commands}{Environment.NewLine}");
Console.WriteLine($"Tag: {seqTag}{Environment.NewLine}");
Console.WriteLine($"Url: {viewLogsSeqUrl}{Environment.NewLine}");
Console.ReadLine();
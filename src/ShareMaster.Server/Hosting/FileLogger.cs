using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;

namespace ShareMaster.Server.Hosting;

/// <summary>ファイルログ設定（appsettings の "FileLogging" セクション）</summary>
public sealed class FileLoggingSettings
{
    public const string SectionName = "FileLogging";

    public bool Enabled { get; set; } = true;

    /// <summary>出力先フォルダ。相対パスはアプリのフォルダ（ContentRoot）基準</summary>
    public string Path { get; set; } = "logs";

    /// <summary>保持日数（これより古いログファイルは削除する）</summary>
    public int RetainedDays { get; set; } = 30;
}

/// <summary>
/// 日付ごとのファイル（sharemaster-yyyyMMdd.log）へログを出力する簡易ロガー。
/// IIS ではコンソール出力を確認できないため、障害調査用に使う。
/// 出力レベルは通常の Logging 設定（プロバイダー名 "File"）に従う。
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _lock = new();
    private readonly string _directory;
    private readonly int _retainedDays;
    private string? _currentDate;
    private StreamWriter? _writer;

    public FileLoggerProvider(IOptions<FileLoggingSettings> options, IHostEnvironment env)
    {
        var s = options.Value;
        _directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(env.ContentRootPath, s.Path));
        _retainedDays = s.RetainedDays;
    }

    public string Directory => _directory;

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));

    internal void Write(string line)
    {
        lock (_lock)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyyMMdd");
                if (_writer is null || _currentDate != today)
                {
                    _writer?.Dispose();
                    System.IO.Directory.CreateDirectory(_directory);
                    var path = System.IO.Path.Combine(_directory, $"sharemaster-{today}.log");
                    // 複数プロセス（IIS のリサイクル中など）からの同時書き込みを許可する
                    var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                    _currentDate = today;
                    DeleteOldFiles();
                }
                _writer.WriteLine(line);
            }
            catch
            {
                // ログ出力の失敗でアプリを止めない（書き込み権限がない場合など）
                _writer?.Dispose();
                _writer = null;
            }
        }
    }

    private void DeleteOldFiles()
    {
        if (_retainedDays <= 0) return;
        var limit = DateTime.Now.Date.AddDays(-_retainedDays);
        foreach (var file in System.IO.Directory.EnumerateFiles(_directory, "sharemaster-*.log"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < limit) File.Delete(file);
            }
            catch
            {
                // 削除できないファイルは次回に回す
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}

internal sealed class FileLogger(string category, FileLoggerProvider provider) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append(' ')
          .Append(LevelText(logLevel)).Append(' ')
          .Append(category).Append(": ")
          .Append(formatter(state, exception));
        if (exception is not null) sb.AppendLine().Append(exception);
        provider.Write(sb.ToString());
    }

    private static string LevelText(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO ",
        LogLevel.Warning => "WARN ",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "FATAL",
        _ => level.ToString(),
    };
}

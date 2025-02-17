using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

public interface IObserver
{
    void Update(string changeType, string filePath);
}


public class DirectoryMonitor
{
    private string _directoryPath;
    private System.Timers.Timer _timer;
    private Dictionary<string, DateTime> _fileLastWriteTimes;
    private List<IObserver> _observers;

    public DirectoryMonitor(string directoryPath, double interval)
    {
        _directoryPath = directoryPath;
        _fileLastWriteTimes = new Dictionary<string, DateTime>();
        _observers = new List<IObserver>();

        // Инициализация таймера
        _timer = new System.Timers.Timer(interval);
        _timer.Elapsed += CheckDirectoryChanges;
        _timer.AutoReset = true;
        _timer.Enabled = true;

        // Инициализация начального состояния файлов
        InitializeFileStates();
    }

    private void InitializeFileStates()
    {
        foreach (var filePath in Directory.GetFiles(_directoryPath))
        {
            _fileLastWriteTimes[filePath] = File.GetLastWriteTime(filePath);
        }
    }

    private void CheckDirectoryChanges(object sender, ElapsedEventArgs e)
    {
        var currentFiles = new HashSet<string>(Directory.GetFiles(_directoryPath));

        // Проверка новых и измененных файлов
        foreach (var filePath in currentFiles)
        {
            if (!_fileLastWriteTimes.ContainsKey(filePath))
            {
                // Новый файл
                _fileLastWriteTimes[filePath] = File.GetLastWriteTime(filePath);
                NotifyObservers("Created", filePath);
            }
            else
            {
                var lastWriteTime = File.GetLastWriteTime(filePath);
                if (lastWriteTime != _fileLastWriteTimes[filePath])
                {
                    // Файл изменен
                    _fileLastWriteTimes[filePath] = lastWriteTime;
                    NotifyObservers("Changed", filePath);
                }
            }
        }

        // Проверка удаленных файлов
        var removedFiles = new List<string>();
        foreach (var filePath in _fileLastWriteTimes.Keys)
        {
            if (!currentFiles.Contains(filePath))
            {
                removedFiles.Add(filePath);
                NotifyObservers("Deleted", filePath);
            }
        }

        // Удаление информации о удаленных файлах
        foreach (var filePath in removedFiles)
        {
            _fileLastWriteTimes.Remove(filePath);
        }
    }

    public void Attach(IObserver observer)
    {
        _observers.Add(observer);
    }

    public void Detach(IObserver observer)
    {
        _observers.Remove(observer);
    }

    private void NotifyObservers(string changeType, string filePath)
    {
        foreach (var observer in _observers)
        {
            observer.Update(changeType, filePath);
        }
    }
}


public class FileChangeObserver : IObserver
{
    public void Update(string changeType, string filePath)
    {
        Console.WriteLine($"File {filePath} was {changeType}");
    }
}

class Program
{
    static void Main(string[] args)
    {
        string directoryPath = @"C:\TestDirectory";
        var monitor = new DirectoryMonitor(directoryPath, 1000); // Проверка каждую секунду

        var observer = new FileChangeObserver();
        monitor.Attach(observer);

        Console.WriteLine("Monitoring directory. Press Enter to exit.");
        Console.ReadLine();
    }
}
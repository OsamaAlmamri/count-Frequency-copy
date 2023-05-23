using System;
using System.IO;
using System.Data.SQLite;
using System.Timers;
using Timer = System.Timers.Timer;

class Program
{
    static int fileCount = 0;
    static SQLiteConnection connection;
    static System.Timers.Timer driveCheckTimer;
   static List<string> monitoredDrives = new List<string>();
    static void Main()
    {
        // Initialize the SQLite connection and create the table if it doesn't exist
        InitializeDatabase();

        // Create a timer to periodically check for new drives
        driveCheckTimer = new Timer();
        driveCheckTimer.Interval = 5000; // Check every 5 seconds
        driveCheckTimer.Elapsed += OnDriveCheckTimerElapsed;
        driveCheckTimer.Start();

        Console.WriteLine("Monitoring started. Press any key to exit.");
        Console.Read();

        // Stop the timer
        driveCheckTimer.Stop();

        // Close the SQLite connection
        connection.Close();
    }

    static void OnDriveCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Get the currently connected drives
        DriveInfo[] drives = DriveInfo.GetDrives();

        foreach (String path in monitoredDrives)
        {
            bool exist=false;
            foreach (DriveInfo driveInfo in drives)
        {
            if(driveInfo.RootDirectory.FullName==path)
            {
                exist=true;

            }
        }
        if(exist==false)
           {  
            monitoredDrives.Remove(path);
               Console.WriteLine($"USB drive removed: {path}");
           }
        }
        foreach (DriveInfo driveInfo in drives)
        {
            // Check if the drive is a USB drive and hasn't been monitored before
            if (driveInfo.DriveType == DriveType.Removable && !IsDriveMonitored(driveInfo.RootDirectory.FullName))
            {
                Console.WriteLine($"USB drive inserted: {driveInfo.Name}");

                // Create a new instance of FileSystemWatcher to monitor the USB drive
                FileSystemWatcher usbWatcher = new FileSystemWatcher();
                usbWatcher.Path = driveInfo.RootDirectory.FullName;
                usbWatcher.IncludeSubdirectories = true;
                usbWatcher.Created += OnFileCopied;

                // Start monitoring the USB drive
                usbWatcher.EnableRaisingEvents = true;

                // Add the drive to the list of monitored drives
                AddMonitoredDrive(driveInfo.RootDirectory.FullName);
            }
        }
    }

    static void OnFileCopied(object sender, FileSystemEventArgs e)
    {
        fileCount++;
        Console.WriteLine($"File copied to USB: {e.FullPath}");
        Console.WriteLine($"Total files copied: {fileCount}");
    Console.WriteLine($"File copied to USB: {e.FullPath.Split('\\').Last()}");
        // Update the file frequency in the SQLite database
        UpdateFileFrequency(e.FullPath,e.FullPath.Split('\\').Last());
    }

    static void InitializeDatabase()
    {
        string dbFilePath = "filestats.db";
        connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;");
        connection.Open();
        string createTableQuery = "CREATE TABLE IF NOT EXISTS FileStats (fileName TEXT , Frequency INTEGER DEFAULT 0, dayDate TEXT nullable); CREATE TABLE IF NOT EXISTS FileLogs (FilePath TEXT , dayDate TEXT nullable);";
        SQLiteCommand createTableCommand = new SQLiteCommand(createTableQuery, connection);
        createTableCommand.ExecuteNonQuery();
    }

    static bool IsDriveMonitored(string drivePath)
    {
        // Implement your logic to check if the drive is already being monitored
        // For simplicity, a placeholder implementation is provided that always returns false.
        return monitoredDrives.Contains(drivePath);
    }

    static void AddMonitoredDrive(string drivePath)
    {
        // Implement your logic to add the drive to the list of monitored drives
        // For simplicity, a placeholder implementation is provided.
         monitoredDrives.Add(drivePath);
        Console.WriteLine($"Adding drive to monitored list: {drivePath}");
    }

    static void UpdateFileFrequency(string filePath,string fileName)
    {
         string dateTime2 = DateTime.Now.ToString("yyyy-MM-dd");
        string selectQuery = "SELECT Frequency FROM FileStats WHERE fileName = @fileName and dayDate=@dateTime2;";
        SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, connection);
        selectCommand.Parameters.AddWithValue("@fileName", fileName);
        selectCommand.Parameters.AddWithValue("@dateTime2", dateTime2);

        object result = selectCommand.ExecuteScalar();

        if (result != null && result != DBNull.Value)
        {
            int frequency = Convert.ToInt32(result);
            frequency++;

            string updateQuery = "UPDATE FileStats SET Frequency = @Frequency WHERE fileName = @fileName and dayDate=@dateTime2;";
            SQLiteCommand updateCommand = new SQLiteCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@Frequency", frequency);
            updateCommand.Parameters.AddWithValue("@fileName", fileName);
            updateCommand.Parameters.AddWithValue("@dateTime2", dateTime2);
            updateCommand.ExecuteNonQuery();
        }
        else
        {
            string insertQuery = "INSERT INTO FileStats (fileName, Frequency,dayDate) VALUES (@fileName, 1,@dateTime2);";
            SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@fileName", fileName);
             insertCommand.Parameters.AddWithValue("@dateTime2", dateTime2);
            insertCommand.ExecuteNonQuery();
        }
        
 string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

          string insertQuery2 = "INSERT INTO FileLogs (FilePath, dayDate) VALUES (@FilePath,@dateTime);";
            SQLiteCommand insertCommand2 = new SQLiteCommand(insertQuery2, connection);
            insertCommand2.Parameters.AddWithValue("@FilePath", filePath);

            insertCommand2.Parameters.AddWithValue("@dateTime", dateTime);
            insertCommand2.ExecuteNonQuery();
    }
}

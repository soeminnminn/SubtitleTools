using SharpConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SubtitleTools.UI.Models
{
    internal class ConfigModel : IDisposable, RecentFiles.IRecentFilesStore
    {
        #region Variables
        private const string DEFAULT_FILENAME = "config.ini";
        private const int MAX_FILE = 10;
        private const int MUTEX_TIMEOUT = 3000;

        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string appDataConfigDir = string.Empty;

        private readonly string _configFileName;
        private readonly Mutex _mutex;

        private readonly object _lock = new object();
        private bool _disposed = false;

        private bool showToolbar = true;
        private bool showStatusbar = true;

        private int defaultEncoding = -1;
        private bool useEncodingWithBom = false;
        private bool useDefaultEncoding = false;

        private string lastOpenedFile = string.Empty;
        private readonly List<string> recentFiles = new List<string>();
        #endregion

        #region Constructors
        public ConfigModel(string fileName = DEFAULT_FILENAME)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            _configFileName = Path.Combine(AppDataConfigDir, fileName);
            _mutex = new Mutex(false, $"Global\\{_configFileName.Replace('\\', '_')}");
        }
        #endregion

        #region Properties
        public static string AppDataConfigDir
        {
            get
            {
                if (!string.IsNullOrEmpty(appDataConfigDir)) return appDataConfigDir;

                var assm = Assembly.GetEntryAssembly();
                if (assm != null)
                {
                    string appName = assm.GetName().Name;
                    var titleAttr = assm.GetCustomAttribute(typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
                    if (titleAttr != null && !string.IsNullOrEmpty(titleAttr.Title))
                    {
                        appName = titleAttr.Title;
                    }

                    string companyName = string.Empty;
                    var companyAttr = assm.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
                    if (companyAttr != null && !string.IsNullOrEmpty(companyAttr.Company))
                    {
                        companyName = companyAttr.Company;
                    }

                    string configDir = Path.Combine(appDataPath, companyName, appName);
                    if (!Directory.Exists(configDir))
                    {
                        try
                        {
                            var dirInfo = Directory.CreateDirectory(configDir);
                            if (dirInfo.Exists)
                            {
                                appDataConfigDir = dirInfo.FullName;
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        appDataConfigDir = configDir;
                    }

                }
                return appDataConfigDir;
            }
        }

        public bool IsLoaded { get; private set; } = false;

        public bool ShowToolbar
        {
            get => showToolbar;
            set { showToolbar = value; }
        }

        public bool ShowStatusbar
        {
            get => showStatusbar;
            set { showStatusbar = value; }
        }

        public Encoding DefaultEncoding
        {
            get => defaultEncoding < 0 ? Encoding.UTF8 : Encoding.GetEncoding(defaultEncoding);
            set { defaultEncoding = value.CodePage; }
        }

        public bool UseEncodingWithBom
        {
            get => useEncodingWithBom;
            set { useEncodingWithBom = value; }
        }

        public bool UseDefaultEncoding
        {
            get => useDefaultEncoding;
            set { useDefaultEncoding = value; }
        }

        public string LastOpenedFile
        {
            get => lastOpenedFile;
            set { lastOpenedFile = value; }
        }

        public List<string> RecentFiles
        {
            get => recentFiles;
        }
        #endregion

        #region Methods
        private static bool HasWriteAccess(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath)) return false;

            var isInRoleWithAccess = false;
            var accessRights = FileSystemRights.Write;

            try
            {
                var dir = new DirectoryInfo(dirPath);
                if (!dir.Exists) return false;

                var acl = dir.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                var currentUser = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(currentUser);
                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return isInRoleWithAccess;
        }

        protected async void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    await SaveAsync();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public Task LoadAsync()
        {
            return Task.Run(() =>
            {
                var filePath = _configFileName;

                try
                {
                    if (_mutex.WaitOne(MUTEX_TIMEOUT))
                    {
                        Configuration config = null;
                        try
                        {
                            var fileInfo = new FileInfo(filePath);
                            if (fileInfo.Exists)
                            {
                                try
                                {
                                    using (var reader = new StreamReader(fileInfo.OpenRead()))
                                    {
                                        var content = reader.ReadToEnd();
                                        config = Configuration.LoadFromString(content);
                                    }
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine(e.Message);
                                }
                            }
                        }
                        catch
                        {
                            return;
                        }
                        finally
                        {
                            _mutex.ReleaseMutex();
                        }

                        lock (_lock)
                        {
                            Deserialize(config);
                            IsLoaded = true;
                        }
                    }
                }
                catch (AbandonedMutexException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            });
        }

        public Task SaveAsync()
        {
            return Task.Run(() =>
            {
                var filePath = _configFileName;

                try
                {
                    if (_mutex.WaitOne(MUTEX_TIMEOUT))
                    {
                        try
                        {
                            lock (_lock)
                            {
                                var fileInfo = new FileInfo(filePath);
                                if (HasWriteAccess(fileInfo.DirectoryName))
                                {
                                    if (fileInfo.Exists)
                                    {
                                        fileInfo.Delete();
                                    }

                                    using (var stream = fileInfo.Create())
                                    {
                                        Serialize(stream);
                                    }
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            _mutex.ReleaseMutex();
                        }
                    }
                }
                catch (AbandonedMutexException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            });
        }

        private bool Deserialize(Configuration config)
        {
            recentFiles.Clear();

            if (config == null) return false;

            try
            {
                var generalSection = config["General"];

                if (generalSection["DefaultEncoding"].TryGetValue(out int codePage) && codePage > 0)
                    defaultEncoding = codePage;

                if (generalSection["UseEncodingWithBom"].TryGetValue(out bool withBom))
                    useEncodingWithBom = withBom;

                if (generalSection["UseDefaultEncoding"].TryGetValue(out bool useEncoding))
                    useDefaultEncoding = useEncoding;

                if (generalSection["LastOpenedFile"].TryGetValue(out string lastFile) && !string.IsNullOrEmpty(lastFile))
                    lastOpenedFile = lastFile;

                var uiSection = config["UI"];

                if (uiSection["ShowToolbar"].TryGetValue(out bool toolbar))
                    showToolbar = toolbar;

                if (uiSection["ShowStatusbar"].TryGetValue(out bool statusbar))
                    showStatusbar = statusbar;

                config["RecentFiles"].ForEach(setting =>
                {
                    if (setting.TryGetValue(out string str))
                    {
                        recentFiles.Add(str);
                    }
                });

                return true;
            }
            catch (Exception)
            { }

            return false;
        }

        private void Serialize(Stream stream)
        {
            if (stream == null) return;
            if (!stream.CanWrite) return;
            
            var config = new Configuration();

            try
            {
                var generalSection = config.Add("General");

                generalSection.Add("DefaultEncoding", defaultEncoding);
                generalSection.Add("UseEncodingWithBom", useEncodingWithBom);
                generalSection.Add("UseDefaultEncoding", useDefaultEncoding);
                generalSection.Add("LastOpenedFile", lastOpenedFile);

                var uiSection = config.Add("UI");

                uiSection.Add("ShowToolbar", showToolbar);
                uiSection.Add("ShowStatusbar", showStatusbar);

                var recentFilesSection = config.Add("RecentFiles");
                for (var i = 0; i < recentFiles.Count; i++)
                {
                    recentFilesSection.Add($"File{i}", recentFiles[i]);
                }

                config.SaveToStream(stream);
            }
            catch (Exception)
            { }
        }

        public async Task<List<string>> GetRecentFiles()
        {
            if (!IsLoaded)
            {
                await LoadAsync();
            }
            return recentFiles;
        }

        public void AddRecentFile(string fileName, int maxFiles = MAX_FILE)
        {
            try
            {
                fileName = Path.GetFullPath(fileName);
            }
            catch
            {
                return;
            }

            lock (recentFiles)
            {
                recentFiles.Remove(fileName);
                recentFiles.Insert(0, fileName);

                if (recentFiles.Count > maxFiles)
                {
                    recentFiles.RemoveAt(maxFiles);
                }
            }
        }

        public void RemoveRecentFile(string fileName)
        {
            lock (recentFiles)
            {
                recentFiles.Remove(fileName);
            }
        }

        public void ClearRecentFiles()
        {
            lock (recentFiles)
            {
                recentFiles.Clear();
            }
        }
        #endregion
    }
}

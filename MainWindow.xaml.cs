using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace PortableBulkFileRenamer;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<RenameRow> _rows = new();
    private readonly HashSet<string> _files = new(StringComparer.OrdinalIgnoreCase);
    private List<RenameRecord> _lastBatch = new();
    private bool _isEnglish;

    private string TextFor(string vi, string en) => _isEnglish ? en : vi;

    public MainWindow()
    {
        InitializeComponent();
        PreviewGrid.ItemsSource = _rows;
        NumberOptions.IsEnabled = false;
        UpdateLanguage();
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || LanguageCombo.SelectedItem is not ComboBoxItem item) return;
        _isEnglish = (item.Tag?.ToString() == "en");
        UpdateLanguage();
    }

    private void UpdateLanguage()
    {
        Title = _isEnglish ? "Portable Bulk File Renamer" : "Portable Bulk File Renamer";
        AppTitle.Text = _isEnglish ? "Portable Bulk File Renamer" : "Portable Bulk File Renamer";
        SourceGroup.Header = TextFor("Nguồn file / File source", "File source");
        RulesGroup.Header = TextFor("Quy tắc đổi tên / Rename rules", "Rename rules");
        ChooseFolderButton.Content = TextFor("Chọn thư mục...", "Choose folder...");
        ChooseFilesButton.Content = TextFor("Chọn file...", "Choose files...");
        ClearButton.Content = TextFor("Xóa danh sách", "Clear list");
        PrefixLabel.Text = TextFor("Tiền tố / Prefix", "Prefix");
        FindLabel.Text = TextFor("Tìm / Find", "Find");
        ReplaceLabel.Text = TextFor("Thay bằng / Replace", "Replace with");
        SuffixLabel.Text = TextFor("Hậu tố / Suffix", "Suffix");
        RemoveLabel.Text = TextFor("Xóa cụm từ / Remove", "Remove text");
        NumberCheck.Content = TextFor("Đánh số / Number", "Number");
        StartLabel.Text = TextFor("Bắt đầu", "Start");
        DigitsLabel.Text = TextFor("Số chữ số", "Digits");
        CaseSensitiveCheck.Content = TextFor("Phân biệt hoa/thường khi tìm / Case-sensitive find", "Case-sensitive find");
        KeepExtensionCheck.Content = TextFor("Giữ nguyên phần mở rộng / Keep extension", "Keep extension");
        PreviewButton.Content = TextFor("Xem trước / Preview", "Preview");
        RenameButton.Content = TextFor("Đổi tên / Rename", "Rename");
        UndoButton.Content = TextFor("Hoàn tác / Undo", "Undo");
        PrivacyText.Text = TextFor("Chạy cục bộ; không gửi file hoặc tên file ra ngoài.", "Runs locally; files and names are never sent externally.");
        StatusColumn.Header = TextFor("Trạng thái / Status", "Status");
        OldColumn.Header = TextFor("Tên hiện tại / Current name", "Current name");
        NewColumn.Header = TextFor("Tên mới / New name", "New name");
        FolderColumn.Header = TextFor("Thư mục / Folder", "Folder");
        UpdateCount();
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = TextFor("Chọn thư mục chứa file", "Choose a folder containing files"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;
        FolderBox.Text = dialog.SelectedPath;
        _files.Clear();
        try
        {
            var option = RecursiveCheck.IsChecked == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var file in Directory.EnumerateFiles(dialog.SelectedPath, "*", option))
                _files.Add(Path.GetFullPath(file));
            PopulateRows();
        }
        catch (Exception ex) { ShowError(TextFor("Không thể đọc thư mục: ", "Unable to read folder: ") + ex.Message); }
    }

    private void ChooseFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = TextFor("Chọn một hoặc nhiều file", "Select one or more files"),
            Multiselect = true,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;
        foreach (var file in dialog.FileNames) _files.Add(Path.GetFullPath(file));
        FolderBox.Text = TextFor($"Đã chọn {_files.Count} file", $"{_files.Count} files selected");
        PopulateRows();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _files.Clear(); _rows.Clear(); FolderBox.Clear(); UpdateCount();
    }

    private void NumberCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (NumberOptions is not null) NumberOptions.IsEnabled = NumberCheck.IsChecked == true;
    }

    private void PopulateRows()
    {
        _rows.Clear();
        foreach (var path in _files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            _rows.Add(new RenameRow { FullPath = path, OldName = Path.GetFileName(path), NewName = Path.GetFileName(path), Folder = Path.GetDirectoryName(path) ?? "", Status = TextFor("Chưa xem trước", "Not previewed") });
        UpdateCount();
    }

    private void Preview_Click(object sender, RoutedEventArgs e) => BuildPreview();

    private void BuildPreview()
    {
        if (_files.Count == 0) { ShowError(TextFor("Hãy chọn file hoặc thư mục trước.", "Choose files or a folder first.")); return; }
        if (!TryGetNumberSettings(out int start, out int digits)) return;

        var ordered = _rows.OrderBy(r => r.FullPath, StringComparer.OrdinalIgnoreCase).ToList();
        var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        foreach (var row in ordered)
        {
            index++;
            try
            {
                string oldName = Path.GetFileName(row.FullPath);
                string extension = KeepExtensionCheck.IsChecked == true ? Path.GetExtension(oldName) : "";
                string stem = KeepExtensionCheck.IsChecked == true ? oldName[..(oldName.Length - extension.Length)] : oldName;
                if (!string.IsNullOrEmpty(RemoveBox.Text)) stem = stem.Replace(RemoveBox.Text, "", CaseSensitiveCheck.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(FindBox.Text)) stem = stem.Replace(FindBox.Text, ReplaceBox.Text, CaseSensitiveCheck.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                string newStem = PrefixBox.Text + stem + SuffixBox.Text;
                if (NumberCheck.IsChecked == true) newStem += "_" + (start + index - 1).ToString("D" + digits);
                string newName = newStem + extension;
                row.NewName = newName;
                string target = Path.Combine(Path.GetDirectoryName(row.FullPath)!, newName);
                if (!IsValidFileName(newName)) row.Status = TextFor("Tên không hợp lệ", "Invalid filename");
                else if (!targetPaths.Add(target)) row.Status = TextFor("Trùng tên đích", "Duplicate target");
                else if (!string.Equals(row.FullPath, target, StringComparison.OrdinalIgnoreCase) && File.Exists(target) && !_files.Contains(target))
                    row.Status = TextFor("Đích đã tồn tại", "Target exists");
                else row.Status = string.Equals(row.OldName, newName, StringComparison.Ordinal) ? TextFor("Không thay đổi", "Unchanged") : TextFor("Sẵn sàng", "Ready");
            }
            catch (Exception ex) { row.Status = TextFor("Lỗi: ", "Error: ") + ex.Message; }
        }
        PreviewGrid.Items.Refresh();
        UpdateCount();
    }

    private bool TryGetNumberSettings(out int start, out int digits)
    {
        start = 1; digits = 3;
        if (NumberCheck.IsChecked != true) return true;
        if (!int.TryParse(NumberStartBox.Text, out start) || start < 0)
        { ShowError(TextFor("Số bắt đầu phải là số nguyên không âm.", "Start number must be a non-negative integer.")); return false; }
        if (!int.TryParse(DigitsBox.Text, out digits) || digits < 1 || digits > 12)
        { ShowError(TextFor("Số chữ số phải nằm trong khoảng 1–12.", "Digits must be between 1 and 12.")); return false; }
        return true;
    }

    private static bool IsValidFileName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && name is not "." and not "..";

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        BuildPreview();
        var ready = _rows.Where(r => r.Status == TextFor("Sẵn sàng", "Ready")).ToList();
        if (ready.Count == 0) { ShowError(TextFor("Không có file hợp lệ để đổi tên.", "No valid files to rename.")); return; }
        var answer = MessageBox.Show(
            TextFor($"Bạn sắp đổi tên {ready.Count} file. Nên sao lưu dữ liệu quan trọng trước khi tiếp tục.\nTiếp tục?", $"You are about to rename {ready.Count} files. Back up important data first.\nContinue?"),
            "Portable Bulk File Renamer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        var pending = ready.Select(r => new RenameRecord { OriginalPath = r.FullPath, FinalPath = Path.Combine(r.Folder, r.NewName) }).ToList();
        var tempPaths = new List<(string Temp, string Final, string Original)>();
        try
        {
            // Stage through unique temporary names to support safe swaps/cycles.
            foreach (var rec in pending)
            {
                string temp = Path.Combine(Path.GetDirectoryName(rec.OriginalPath)!, $".__bfr_{Guid.NewGuid():N}.tmp");
                File.Move(rec.OriginalPath, temp);
                tempPaths.Add((temp, rec.FinalPath, rec.OriginalPath));
            }
            foreach (var item in tempPaths)
            {
                if (File.Exists(item.Final)) throw new IOException(TextFor("Tên đích đã xuất hiện: ", "Target appeared during rename: ") + item.Final);
                File.Move(item.Temp, item.Final);
            }
            _lastBatch = pending;
            SaveHistory(pending);
            _files.Clear();
            foreach (var r in _rows) _files.Add(Path.Combine(r.Folder, r.NewName));
            PopulateRows();
            MessageBox.Show(TextFor($"Đã đổi tên {pending.Count} file.", $"Renamed {pending.Count} files."),
                "Portable Bulk File Renamer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Best-effort rollback if an operation fails midway.
            foreach (var item in tempPaths.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(item.Temp) && !File.Exists(item.Original)) File.Move(item.Temp, item.Original);
                    else if (File.Exists(item.Final) && !File.Exists(item.Original)) File.Move(item.Final, item.Original);
                }
                catch { /* report original error below */ }
            }
            ShowError(TextFor("Đổi tên bị lỗi. Đã thử khôi phục các file đã xử lý.\n", "Rename failed. A rollback was attempted.\n") + ex.Message);
            RefreshAfterOperation();
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_lastBatch.Count == 0)
        {
            var historyPath = GetHistoryPath();
            try
            {
                if (File.Exists(historyPath))
                {
                    var batches = JsonSerializer.Deserialize<List<List<RenameRecord>>>(File.ReadAllText(historyPath));
                    if (batches is { Count: > 0 }) _lastBatch = batches[^1];
                }
            }
            catch { }
        }
        if (_lastBatch.Count == 0) { ShowError(TextFor("Không có lịch sử hoàn tác trong phiên này.", "No undo history is available.")); return; }

        var records = _lastBatch.ToList();
        foreach (var r in records)
        {
            if (!File.Exists(r.FinalPath)) { ShowError(TextFor("Không tìm thấy file đã đổi tên: ", "Renamed file not found: ") + r.FinalPath); return; }
            if (File.Exists(r.OriginalPath)) { ShowError(TextFor("Không thể hoàn tác vì tên gốc đã được sử dụng: ", "Cannot undo because original path is occupied: ") + r.OriginalPath); return; }
        }
        try
        {
            var staged = new List<(string Temp, RenameRecord Record)>();
            foreach (var r in records)
            {
                string temp = Path.Combine(Path.GetDirectoryName(r.FinalPath)!, $".__bfr_undo_{Guid.NewGuid():N}.tmp");
                File.Move(r.FinalPath, temp);
                staged.Add((temp, r));
            }
            foreach (var item in staged) File.Move(item.Temp, item.Record.OriginalPath);
            _lastBatch = new();
            RefreshAfterOperation();
            MessageBox.Show(TextFor("Đã hoàn tác.", "Undo completed."), "Portable Bulk File Renamer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(TextFor("Hoàn tác thất bại: ", "Undo failed: ") + ex.Message); }
    }

    private void SaveHistory(List<RenameRecord> batch)
    {
        try
        {
            var path = GetHistoryPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var batches = new List<List<RenameRecord>>();
            if (File.Exists(path))
                batches = JsonSerializer.Deserialize<List<List<RenameRecord>>>(File.ReadAllText(path)) ?? new();
            batches.Add(batch);
            File.WriteAllText(path, JsonSerializer.Serialize(batches, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* File renaming should not fail only because history could not be saved. */ }
    }

    private string GetHistoryPath()
    {
        string appDir = AppContext.BaseDirectory;
        string portable = Path.Combine(appDir, "History");
        try
        {
            Directory.CreateDirectory(portable);
            string probe = Path.Combine(portable, ".write_test");
            File.WriteAllText(probe, "ok"); File.Delete(probe);
            return Path.Combine(portable, "rename-history.json");
        }
        catch
        {
            string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BulkFileRenamer");
            return Path.Combine(local, "rename-history.json");
        }
    }

    private void RefreshAfterOperation()
    {
        var paths = _rows.Select(r => File.Exists(Path.Combine(r.Folder, r.NewName)) ? Path.Combine(r.Folder, r.NewName) : r.FullPath)
            .Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _files.Clear(); foreach (var p in paths) _files.Add(p);
        PopulateRows();
    }

    private void UpdateCount() => CountText.Text = TextFor($"{_rows.Count} file", $"{_rows.Count} files");
    private void ShowError(string message) => MessageBox.Show(message, "Portable Bulk File Renamer", MessageBoxButton.OK, MessageBoxImage.Warning);
}

public sealed class RenameRow
{
    public string FullPath { get; set; } = "";
    public string OldName { get; set; } = "";
    public string NewName { get; set; } = "";
    public string Folder { get; set; } = "";
    public string Status { get; set; } = "";
}

public sealed class RenameRecord
{
    public string OriginalPath { get; set; } = "";
    public string FinalPath { get; set; } = "";
}

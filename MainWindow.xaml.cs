using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Forms = System.Windows.Forms;

namespace PortableBulkFileRenamer;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<FileRow> _files = new();
    private readonly ObservableCollection<RenameRule> _rules = new();
    private readonly HashSet<string> _knownPaths = new(StringComparer.OrdinalIgnoreCase);
    private List<RenameRecord> _lastBatch = new();
    private bool _isEnglish;

    private string T(string vi, string en) => _isEnglish ? en : vi;

    public MainWindow()
    {
        InitializeComponent();
        PreviewGrid.ItemsSource = _files;
        RulesGrid.ItemsSource = _rules;
        UpdateLanguage();
        RefreshRuleLabels();
        UpdatePositionControls();
        UpdateActionControls();
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || LanguageCombo.SelectedItem is not ComboBoxItem item) return;
        _isEnglish = item.Tag?.ToString() == "en";
        UpdateLanguage();
        RefreshRuleLabels();
        RefreshPreview();
    }

    private void UpdateLanguage()
    {
        Title = "Portable Bulk File Renamer";
        AppTitle.Text = "Portable Bulk File Renamer";
        SourceGroup.Header = T("1. Nguồn file", "1. File source");
        ChooseFolderButton.Content = T("Chọn thư mục", "Choose Folder");
        ChooseFilesButton.Content = T("Chọn file", "Choose Files");
        ClearButton.Content = T("Xóa danh sách", "Clear List");
        RecursiveCheck.Content = T("Bao gồm thư mục con", "Include subfolders");
        FilterLabel.Text = T("Lọc tên file:", "Filter filenames:");
        SelectAllButton.Content = T("Chọn tất cả", "Select All");
        DeselectAllButton.Content = T("Bỏ chọn", "Deselect All");
        KeepSelectedButton.Content = T("Chỉ giữ file đã chọn", "Keep Selected Only");
        RuleBuilderGroup.Header = T("2. Tạo quy tắc đổi tên", "2. Create rename rule");
        SearchGroup.Header = T("A. TÌM KIẾM — Xác định nội dung và vị trí", "A. SEARCH — Specify text and location");
        ActionGroup.Header = T("B. THAO TÁC — Xử lý nội dung tìm được", "B. ACTION — Process the matching text");
        SearchModeLabel.Text = T("Chế độ tìm kiếm", "Search mode");
        ActionLabel.Text = T("Thao tác", "Action");
        FindLabel.Text = T("Tìm cụm ký tự", "Text to find");
        ReplaceLabel.Text = T("Thay bằng", "Replace with");
        PositionLabel.Text = T("Vị trí bắt đầu từ", "Position counted from");
        PositionDirectionLabel.Text = T("Cách xác định vị trí", "Position reference");
        SearchHelpText.Text = T("Ví dụ: tìm cụm PAU tại vị trí thứ 1, tính từ đầu tên file.", "Example: find PAU at position 1, counted from the start of the filename.");
        ActionHelpText.Text = T("Các ô nhập liệu sẽ tự động bật/tắt theo thao tác được chọn.", "Input fields are enabled or disabled automatically for the selected action.");
        SetComboLabels(SearchModeCombo, new[] { ("Bất kỳ vị trí nào", "Anywhere"), ("Đầu tên file", "Prefix"), ("Cuối tên file", "Suffix"), ("Vị trí chính xác", "Exact Position") });
        SetComboLabels(ActionCombo, new[] { ("Thay thế", "Replace"), ("Xóa cụm ký tự", "Remove"), ("Thêm tiền tố", "Add Prefix"), ("Thêm hậu tố", "Add Suffix"), ("Chuyển thành chữ hoa", "Uppercase"), ("Chuyển thành chữ thường", "Lowercase") });
        SetComboLabels(PositionDirectionCombo, new[] { ("Khoảng cách từ đầu tên", "Distance from start of name"), ("Khoảng cách từ cuối tên", "Distance from end of name") });
        CaseSensitiveCheck.Content = T("Phân biệt hoa/thường", "Case-sensitive");
        AllOccurrencesCheck.Content = T("Thay/xóa tất cả kết quả (Bất kỳ vị trí nào)", "Replace/remove all matches (Anywhere)");
        ExtensionNote.Text = T("Tìm kiếm trên tên file, không gồm phần mở rộng; phần mở rộng được giữ nguyên.", "Search applies to the filename without its extension; the extension is preserved.");
        AddRuleButton.Content = T("Thêm quy tắc", "Add Rule");
        RemoveRuleButton.Content = T("Xóa quy tắc đã chọn", "Remove Selected Rule");
        MoveUpButton.Content = T("Lên ↑", "Move Up ↑");
        MoveDownButton.Content = T("Xuống ↓", "Move Down ↓");
        PreviewButton.Content = T("XEM TRƯỚC", "PREVIEW");
        RenameButton.Content = T("ĐỔI TÊN FILE ĐÃ CHỌN", "RENAME SELECTED FILES");
        UndoButton.Content = T("HOÀN TÁC", "UNDO");
        RulesListGroup.Header = T("3. Thứ tự quy tắc (chạy từ trên xuống)", "3. Rule order (top to bottom)");
        RuleOrderColumn.Header = "#";
        RuleSearchColumn.Header = T("Chế độ tìm kiếm", "Search mode");
        RuleActionColumn.Header = T("Thao tác", "Action");
        RuleFindColumn.Header = T("Tìm", "Find");
        RuleReplaceColumn.Header = T("Thay bằng", "Replace with");
        RulePositionColumn.Header = T("Vị trí", "Position");
        IncludeColumn.Header = T("Đổi tên", "Rename");
        StatusColumn.Header = T("Trạng thái", "Status");
        OldColumn.Header = T("Tên hiện tại", "Current name");
        NewColumn.Header = T("Tên mới dự kiến", "Proposed name");
        FolderColumn.Header = T("Thư mục", "Folder");
        PrivacyText.Text = T("Chạy cục bộ; không gửi file hoặc tên file ra ngoài.", "Runs locally; no files or filenames are sent externally.");
        UpdateCount();
        UpdateActionControls();
    }

    private static void SetComboLabels(ComboBox combo, (string Vi, string En)[] labels)
    {
        for (int i = 0; i < combo.Items.Count && i < labels.Length; i++)
            if (combo.Items[i] is ComboBoxItem item) item.Content = labels[i].En; // replaced immediately by caller's language refresh below
    }

    private void RefreshRuleLabels()
    {
        SetComboLabels(SearchModeCombo, _isEnglish
            ? new[] { ("Anywhere", "Anywhere"), ("Prefix", "Prefix"), ("Suffix", "Suffix"), ("Exact Position", "Exact Position") }
            : new[] { ("Bất kỳ vị trí nào", "Bất kỳ vị trí nào"), ("Đầu tên file", "Đầu tên file"), ("Cuối tên file", "Cuối tên file"), ("Vị trí chính xác", "Vị trí chính xác") });
        SetComboLabels(ActionCombo, _isEnglish
            ? new[] { ("Replace", "Replace"), ("Remove", "Remove"), ("Add Prefix", "Add Prefix"), ("Add Suffix", "Add Suffix"), ("Uppercase", "Uppercase"), ("Lowercase", "Lowercase") }
            : new[] { ("Thay thế", "Thay thế"), ("Xóa cụm ký tự", "Xóa cụm ký tự"), ("Thêm tiền tố", "Thêm tiền tố"), ("Thêm hậu tố", "Thêm hậu tố"), ("Chuyển thành chữ hoa", "Chuyển thành chữ hoa"), ("Chuyển thành chữ thường", "Chuyển thành chữ thường") });
        SetComboLabels(PositionDirectionCombo, _isEnglish
            ? new[] { ("Distance from start of name", "Distance from start of name"), ("Distance from end of name", "Distance from end of name") }
            : new[] { ("Khoảng cách từ đầu tên", "Khoảng cách từ đầu tên"), ("Khoảng cách từ cuối tên", "Khoảng cách từ cuối tên") });
    }

    private static string GetComboTag(ComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = T("Chọn thư mục chứa file", "Choose a folder containing files"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;
        _knownPaths.Clear();
        try
        {
            var option = RecursiveCheck.IsChecked == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (string path in Directory.EnumerateFiles(dialog.SelectedPath, "*", option)) _knownPaths.Add(Path.GetFullPath(path));
            SourceText.Text = dialog.SelectedPath;
            LoadRows(_knownPaths);
        }
        catch (Exception ex) { ShowError(T("Không thể đọc thư mục: ", "Unable to read folder: ") + ex.Message); }
    }

    private void ChooseFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = T("Chọn một hoặc nhiều file", "Select one or more files"), Multiselect = true, CheckFileExists = true };
        if (dialog.ShowDialog() != true) return;
        foreach (string path in dialog.FileNames) _knownPaths.Add(Path.GetFullPath(path));
        SourceText.Text = T($"Đã chọn {_knownPaths.Count} file", $"{_knownPaths.Count} files selected");
        LoadRows(_knownPaths);
    }

    private void RecursiveCheck_Changed(object sender, RoutedEventArgs e)
    {
        // The option is applied when the folder is selected again, to avoid silently replacing a mixed file selection.
    }

    private void LoadRows(IEnumerable<string> paths)
    {
        _files.Clear();
        foreach (string path in paths.Where(File.Exists).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            _files.Add(new FileRow { FullPath = path, OldName = Path.GetFileName(path), NewName = Path.GetFileName(path), Folder = Path.GetDirectoryName(path) ?? "", IsSelected = true, Status = T("Chưa xem trước", "Not previewed") });
        ApplyFilter();
        UpdateCount();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _knownPaths.Clear(); _files.Clear(); SourceText.Clear(); FilterBox.Clear(); UpdateCount();
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (PreviewGrid == null) return;
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(_files);
        string filter = FilterBox?.Text?.Trim() ?? "";
        view.Filter = obj => obj is FileRow row && (filter.Length == 0 || row.OldName.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    private void IncludeCheckBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not FileRow clickedRow)
            return;

        // Preserve the highlighted DataGrid rows while toggling their rename checkboxes.
        var selectedRows = PreviewGrid.SelectedItems.OfType<FileRow>().ToList();
        bool newValue = !clickedRow.IsSelected;

        if (selectedRows.Count > 1 && selectedRows.Contains(clickedRow))
        {
            foreach (FileRow row in selectedRows)
                row.IsSelected = newValue;
        }
        else
        {
            clickedRow.IsSelected = newValue;
        }

        UpdateCount();
        e.Handled = true;
    }

    private void PreviewGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Space)
            return;

        var selectedRows = PreviewGrid.SelectedItems.OfType<FileRow>().ToList();
        if (selectedRows.Count == 0)
            return;

        bool newValue = selectedRows.Any(row => !row.IsSelected);
        foreach (FileRow row in selectedRows)
            row.IsSelected = newValue;

        UpdateCount();
        e.Handled = true;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e) { foreach (var row in _files) row.IsSelected = true; PreviewGrid.Items.Refresh(); UpdateCount(); }
    private void DeselectAll_Click(object sender, RoutedEventArgs e) { foreach (var row in _files) row.IsSelected = false; PreviewGrid.Items.Refresh(); UpdateCount(); }
    private void KeepSelected_Click(object sender, RoutedEventArgs e)
    {
        var kept = _files.Where(r => r.IsSelected).ToList();
        _files.Clear(); foreach (var row in kept) _files.Add(row);
        _knownPaths.Clear(); foreach (var row in kept) _knownPaths.Add(row.FullPath);
        ApplyFilter(); UpdateCount();
    }

    private void SearchMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdatePositionControls();
        UpdateActionControls();
    }
    private void UpdatePositionControls()
    {
        if (PositionBox == null) return;
        bool exact = GetComboTag(SearchModeCombo) == "ExactPosition";
        string action = GetComboTag(ActionCombo);
        bool needsSearch = action is "Replace" or "Remove" or "Uppercase" or "Lowercase";
        PositionBox.IsEnabled = needsSearch && exact;
        PositionDirectionCombo.IsEnabled = needsSearch && exact;
    }

    private void Action_Changed(object sender, SelectionChangedEventArgs e) => UpdateActionControls();
    private void UpdateActionControls()
    {
        if (FindBox == null || ActionCombo == null) return;

        string action = GetComboTag(ActionCombo);
        bool findRequired = action is "Replace" or "Remove" or "Uppercase" or "Lowercase";
        bool replacementRequired = action == "Replace";
        bool addPrefix = action == "AddPrefix";
        bool addSuffix = action == "AddSuffix";
        bool needsSearch = findRequired;

        FindBox.IsEnabled = findRequired;
        ReplaceBox.IsEnabled = replacementRequired || addPrefix || addSuffix;
        SearchModeCombo.IsEnabled = needsSearch;
        bool exact = needsSearch && GetComboTag(SearchModeCombo) == "ExactPosition";
        PositionBox.IsEnabled = exact;
        PositionDirectionCombo.IsEnabled = exact;
        CaseSensitiveCheck.IsEnabled = needsSearch;
        AllOccurrencesCheck.IsEnabled = needsSearch && GetComboTag(SearchModeCombo) == "Anywhere";

        ReplaceLabel.Text = action switch
        {
            "AddPrefix" => T("Tiền tố cần thêm", "Prefix to add"),
            "AddSuffix" => T("Hậu tố cần thêm", "Suffix to add"),
            "Replace" => T("Thay bằng", "Replace with"),
            "Remove" => T("Không cần nội dung thay thế", "No replacement needed"),
            "Uppercase" => T("Không cần nội dung thay thế", "No replacement needed"),
            "Lowercase" => T("Không cần nội dung thay thế", "No replacement needed"),
            _ => T("Thay bằng", "Replace with")
        };
        ReplaceBox.ToolTip = action switch
        {
            "AddPrefix" => T("Nội dung sẽ được thêm vào đầu tên file.", "Text to add before the filename."),
            "AddSuffix" => T("Nội dung sẽ được thêm vào cuối tên file, trước phần mở rộng.", "Text to add at the end of the filename, before the extension."),
            "Replace" => T("Nội dung dùng để thay thế cụm ký tự đã tìm.", "Text that replaces the found text."),
            _ => null
        };
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        string action = GetComboTag(ActionCombo);
        string mode = GetComboTag(SearchModeCombo);
        bool findRequired = action is "Replace" or "Remove" or "Uppercase" or "Lowercase";
        if (action is "AddPrefix" or "AddSuffix") mode = "Anywhere";
        string find = findRequired ? FindBox.Text : "";
        if (findRequired && string.IsNullOrEmpty(find))
        { ShowError(T("Nhập cụm ký tự cần tìm.", "Enter the text to find.")); return; }
        if ((action is "Replace" or "AddPrefix" or "AddSuffix") && string.IsNullOrEmpty(ReplaceBox.Text))
        { ShowError(T("Nhập nội dung thay thế hoặc nội dung cần thêm.", "Enter replacement text or the text to add.")); return; }
        if (findRequired && mode == "ExactPosition" && (!int.TryParse(PositionBox.Text, out int pos) || pos < 1))
        { ShowError(T("Vị trí phải là số nguyên bắt đầu từ 1.", "Position must be an integer starting at 1.")); return; }
        var rule = new RenameRule
        {
            Order = _rules.Count + 1, SearchMode = mode, Action = action, Find = find, Replace = ReplaceBox.Text,
            Position = int.TryParse(PositionBox.Text, out int p) ? p : 1, PositionFromEnd = GetComboTag(PositionDirectionCombo) == "End",
            CaseSensitive = CaseSensitiveCheck.IsChecked == true, AllOccurrences = AllOccurrencesCheck.IsChecked == true
        };
        _rules.Add(rule); RulesGrid.SelectedItem = rule; RefreshRuleLabels();
        foreach (var item in _rules) item.RefreshLabels(_isEnglish);
    }

    private void RemoveRule_Click(object sender, RoutedEventArgs e)
    {
        if (RulesGrid.SelectedItem is not RenameRule rule) return;
        _rules.Remove(rule); RenumberRules();
    }
    private void MoveRuleUp_Click(object sender, RoutedEventArgs e) => MoveRule(-1);
    private void MoveRuleDown_Click(object sender, RoutedEventArgs e) => MoveRule(1);
    private void MoveRule(int delta)
    {
        if (RulesGrid.SelectedItem is not RenameRule rule) return;
        int index = _rules.IndexOf(rule), target = index + delta;
        if (target < 0 || target >= _rules.Count) return;
        _rules.Move(index, target); RenumberRules(); RulesGrid.SelectedItem = rule;
    }
    private void RenumberRules() { for (int i = 0; i < _rules.Count; i++) { _rules[i].Order = i + 1; _rules[i].RefreshLabels(_isEnglish); } RulesGrid.Items.Refresh(); }

    private void Preview_Click(object sender, RoutedEventArgs e) => BuildPreview();

    private void BuildPreview()
    {
        if (_files.Count == 0) { ShowError(T("Hãy chọn file hoặc thư mục trước.", "Choose files or a folder first.")); return; }
        var selected = _files.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0) { ShowError(T("Hãy chọn ít nhất một file.", "Select at least one file.")); return; }
        var computed = new Dictionary<FileRow, string>();
        var statuses = new Dictionary<FileRow, string>();
        foreach (var row in selected)
        {
            try
            {
                string oldName = Path.GetFileName(row.FullPath);
                string ext = Path.GetExtension(oldName);
                string stem = oldName[..(oldName.Length - ext.Length)];
                foreach (var rule in _rules) stem = ApplyRule(stem, rule);
                string newName = stem + ext;
                computed[row] = newName;
                if (!IsValidFileName(newName)) statuses[row] = T("Tên không hợp lệ", "Invalid filename");
                else if (string.Equals(oldName, newName, StringComparison.Ordinal)) statuses[row] = T("Không thay đổi", "Unchanged");
                else statuses[row] = T("Sẵn sàng", "Ready");
            }
            catch (RuleMismatchException ex) { computed[row] = row.OldName; statuses[row] = T("Không khớp: ", "No match: ") + ex.Message; }
            catch (Exception ex) { computed[row] = row.OldName; statuses[row] = T("Lỗi: ", "Error: ") + ex.Message; }
        }
        var proposedTargets = new Dictionary<string, List<FileRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in computed.Where(p => statuses[p.Key] == T("Sẵn sàng", "Ready")))
        {
            string target = Path.Combine(pair.Key.Folder, pair.Value);
            if (!proposedTargets.TryGetValue(target, out var list)) proposedTargets[target] = list = new();
            list.Add(pair.Key);
        }
        foreach (var pair in proposedTargets.Where(p => p.Value.Count > 1))
            foreach (var row in pair.Value) statuses[row] = T("Trùng tên đích", "Duplicate target");

        var movingSources = new HashSet<string>(selected.Where(r => statuses.GetValueOrDefault(r) == T("Sẵn sàng", "Ready")).Select(r => r.FullPath), StringComparer.OrdinalIgnoreCase);
        foreach (var row in selected)
        {
            row.NewName = computed.GetValueOrDefault(row, row.OldName);
            if (statuses[row] == T("Sẵn sàng", "Ready"))
            {
                string target = Path.Combine(row.Folder, row.NewName);
                if (File.Exists(target) && !string.Equals(target, row.FullPath, StringComparison.OrdinalIgnoreCase) && !movingSources.Contains(target)) statuses[row] = T("Đích đã tồn tại", "Target exists");
            }
            row.Status = statuses.GetValueOrDefault(row, T("Lỗi", "Error"));
        }
        foreach (var row in _files.Where(r => !r.IsSelected)) { row.NewName = row.OldName; row.Status = T("Bỏ qua", "Skipped"); }
        PreviewGrid.Items.Refresh(); UpdateCount();
    }

    private string ApplyRule(string stem, RenameRule rule)
    {
        StringComparison cmp = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        switch (rule.Action)
        {
            case "AddPrefix": return rule.Replace + stem;
            case "AddSuffix": return stem + rule.Replace;
        }
        if (string.IsNullOrEmpty(rule.Find)) throw new RuleMismatchException(T("cụm tìm kiếm đang trống", "search text is empty"));
        if (rule.Action is "Uppercase" or "Lowercase")
            return ChangeMatchedCase(stem, rule, cmp);
        int index;
        switch (rule.SearchMode)
        {
            case "Anywhere":
                index = stem.IndexOf(rule.Find, cmp);
                if (index < 0) throw new RuleMismatchException(T($"không tìm thấy '{rule.Find}'", $"'{rule.Find}' was not found"));
                if (rule.Action == "Remove") return rule.AllOccurrences ? ReplaceAll(stem, rule.Find, "", cmp) : stem.Remove(index, rule.Find.Length);
                return rule.AllOccurrences ? ReplaceAll(stem, rule.Find, rule.Replace, cmp) : stem.Remove(index, rule.Find.Length).Insert(index, rule.Replace);
            case "Prefix":
                if (!stem.StartsWith(rule.Find, cmp)) throw new RuleMismatchException(T("cụm tìm không nằm ở đầu tên", "text does not match filename prefix"));
                return rule.Action == "Remove" ? stem[rule.Find.Length..] : rule.Replace + stem[rule.Find.Length..];
            case "Suffix":
                if (!stem.EndsWith(rule.Find, cmp)) throw new RuleMismatchException(T("cụm tìm không nằm ở cuối tên", "text does not match filename suffix"));
                return rule.Action == "Remove" ? stem[..^rule.Find.Length] : stem[..^rule.Find.Length] + rule.Replace;
            case "ExactPosition":
                if (rule.Position < 1 || rule.Position > stem.Length) throw new RuleMismatchException(T("vị trí nằm ngoài tên file", "position is outside the filename"));
                index = rule.PositionFromEnd ? stem.Length - rule.Position : rule.Position - 1;
                if (index + rule.Find.Length > stem.Length || !string.Equals(stem.Substring(index, rule.Find.Length), rule.Find, cmp == StringComparison.Ordinal ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                    throw new RuleMismatchException(T("không khớp tại vị trí đã chọn", "text does not match at the specified position"));
                return rule.Action == "Remove" ? stem.Remove(index, rule.Find.Length) : stem.Remove(index, rule.Find.Length).Insert(index, rule.Replace);
            default: return stem;
        }
    }

    private string ChangeMatchedCase(string stem, RenameRule rule, StringComparison comparison)
    {
        string Convert(string value) => rule.Action == "Uppercase" ? value.ToUpperInvariant() : value.ToLowerInvariant();
        int index;
        switch (rule.SearchMode)
        {
            case "Anywhere":
                index = stem.IndexOf(rule.Find, comparison);
                if (index < 0) throw new RuleMismatchException(T($"không tìm thấy '{rule.Find}'", $"'{rule.Find}' was not found"));
                if (!rule.AllOccurrences) return stem.Remove(index, rule.Find.Length).Insert(index, Convert(stem.Substring(index, rule.Find.Length)));
                var result = stem;
                int searchFrom = 0;
                while ((index = result.IndexOf(rule.Find, searchFrom, comparison)) >= 0)
                {
                    string matched = result.Substring(index, rule.Find.Length);
                    string converted = Convert(matched);
                    result = result.Remove(index, rule.Find.Length).Insert(index, converted);
                    searchFrom = index + converted.Length;
                    if (searchFrom >= result.Length) break;
                }
                return result;
            case "Prefix":
                if (!stem.StartsWith(rule.Find, comparison)) throw new RuleMismatchException(T("cụm tìm không nằm ở đầu tên", "text does not match filename prefix"));
                return Convert(stem[..rule.Find.Length]) + stem[rule.Find.Length..];
            case "Suffix":
                if (!stem.EndsWith(rule.Find, comparison)) throw new RuleMismatchException(T("cụm tìm không nằm ở cuối tên", "text does not match filename suffix"));
                return stem[..^rule.Find.Length] + Convert(stem[^rule.Find.Length..]);
            case "ExactPosition":
                if (rule.Position < 1 || rule.Position > stem.Length) throw new RuleMismatchException(T("vị trí nằm ngoài tên file", "position is outside the filename"));
                index = rule.PositionFromEnd ? stem.Length - rule.Position : rule.Position - 1;
                if (index + rule.Find.Length > stem.Length || !string.Equals(stem.Substring(index, rule.Find.Length), rule.Find, comparison))
                    throw new RuleMismatchException(T("không khớp tại vị trí đã chọn", "text does not match at the specified position"));
                return stem.Remove(index, rule.Find.Length).Insert(index, Convert(stem.Substring(index, rule.Find.Length)));
            default:
                return stem;
        }
    }

    private static string ReplaceAll(string input, string find, string replace, StringComparison comparison)
    {
        if (comparison == StringComparison.Ordinal) return input.Replace(find, replace, StringComparison.Ordinal);
        var result = input;
        int start = 0;
        while (true)
        {
            int idx = result.IndexOf(find, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            result = result.Remove(idx, find.Length).Insert(idx, replace);
            start = idx + replace.Length;
            if (start > result.Length) break;
        }
        return result;
    }

    private static bool IsValidFileName(string name) => !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && name is not "." and not "..";

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        BuildPreview();
        var ready = _files.Where(r => r.IsSelected && r.Status == T("Sẵn sàng", "Ready")).ToList();
        if (ready.Count == 0) { ShowError(T("Không có file hợp lệ để đổi tên.", "No valid files to rename.")); return; }
        var answer = MessageBox.Show(T($"Bạn sắp đổi tên {ready.Count} file. Tiếp tục?", $"You are about to rename {ready.Count} files. Continue?"), "Portable Bulk File Renamer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        var records = ready.Select(r => new RenameRecord { OriginalPath = r.FullPath, FinalPath = Path.Combine(r.Folder, r.NewName) }).ToList();
        var staged = new List<(string Temp, RenameRecord Record)>();
        try
        {
            foreach (var record in records)
            {
                string temp = Path.Combine(Path.GetDirectoryName(record.OriginalPath)!, $".__bfr_{Guid.NewGuid():N}.tmp");
                File.Move(record.OriginalPath, temp); staged.Add((temp, record));
            }
            foreach (var item in staged)
            {
                if (File.Exists(item.Record.FinalPath)) throw new IOException(T("Tên đích đã tồn tại: ", "Target already exists: ") + item.Record.FinalPath);
                File.Move(item.Temp, item.Record.FinalPath);
            }
            _lastBatch = records;
            SaveHistory(records);
            _knownPaths.Clear(); foreach (var row in _files) { string p = records.FirstOrDefault(r => r.OriginalPath.Equals(row.FullPath, StringComparison.OrdinalIgnoreCase))?.FinalPath ?? row.FullPath; if (File.Exists(p)) _knownPaths.Add(p); }
            LoadRows(_knownPaths);
            MessageBox.Show(T($"Đã đổi tên {records.Count} file.", $"Renamed {records.Count} files."), "Portable Bulk File Renamer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            foreach (var item in staged.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(item.Temp) && !File.Exists(item.Record.OriginalPath)) File.Move(item.Temp, item.Record.OriginalPath);
                    else if (File.Exists(item.Record.FinalPath) && !File.Exists(item.Record.OriginalPath)) File.Move(item.Record.FinalPath, item.Record.OriginalPath);
                }
                catch { }
            }
            ShowError(T("Đổi tên gặp lỗi; đã thử khôi phục các file. ", "Rename failed; rollback was attempted. ") + ex.Message);
            LoadRows(_knownPaths.Where(File.Exists));
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_lastBatch.Count == 0) _lastBatch = ReadLastHistoryBatch();
        if (_lastBatch.Count == 0) { ShowError(T("Không có lịch sử hoàn tác.", "No undo history available.")); return; }
        foreach (var record in _lastBatch)
        {
            if (!File.Exists(record.FinalPath)) { ShowError(T("Không tìm thấy file đã đổi tên: ", "Renamed file not found: ") + record.FinalPath); return; }
            if (File.Exists(record.OriginalPath)) { ShowError(T("Tên gốc đã được sử dụng: ", "Original path is occupied: ") + record.OriginalPath); return; }
        }
        var staged = new List<(string Temp, RenameRecord Record)>();
        try
        {
            foreach (var record in _lastBatch)
            {
                string temp = Path.Combine(Path.GetDirectoryName(record.FinalPath)!, $".__bfr_undo_{Guid.NewGuid():N}.tmp");
                File.Move(record.FinalPath, temp); staged.Add((temp, record));
            }
            foreach (var item in staged) File.Move(item.Temp, item.Record.OriginalPath);
            _knownPaths.Clear(); foreach (var record in _lastBatch) _knownPaths.Add(record.OriginalPath);
            _lastBatch.Clear(); LoadRows(_knownPaths); MessageBox.Show(T("Đã hoàn tác.", "Undo completed."), "Portable Bulk File Renamer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(T("Hoàn tác thất bại: ", "Undo failed: ") + ex.Message); }
    }

    private List<RenameRecord> ReadLastHistoryBatch()
    {
        try
        {
            string path = GetHistoryPath();
            if (File.Exists(path)) return (JsonSerializer.Deserialize<List<List<RenameRecord>>>(File.ReadAllText(path)) ?? new()).LastOrDefault() ?? new();
        }
        catch { }
        return new();
    }

    private void SaveHistory(List<RenameRecord> batch)
    {
        try
        {
            string path = GetHistoryPath();
            var batches = File.Exists(path) ? JsonSerializer.Deserialize<List<List<RenameRecord>>>(File.ReadAllText(path)) ?? new() : new();
            batches.Add(batch); File.WriteAllText(path, JsonSerializer.Serialize(batches, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private string GetHistoryPath()
    {
        string portable = Path.Combine(AppContext.BaseDirectory, "History");
        try { Directory.CreateDirectory(portable); string test = Path.Combine(portable, ".write_test"); File.WriteAllText(test, "ok"); File.Delete(test); return Path.Combine(portable, "rename-history.json"); }
        catch { string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BulkFileRenamer"); Directory.CreateDirectory(local); return Path.Combine(local, "rename-history.json"); }
    }

    private void RefreshPreview() { if (_files.Count > 0 && _rules.Count > 0) BuildPreview(); }
    private void UpdateCount() => CountText.Text = T($"{_files.Count} file | đã chọn: {_files.Count(r => r.IsSelected)}", $"{_files.Count} files | selected: {_files.Count(r => r.IsSelected)}");
    private void ShowError(string message) => MessageBox.Show(message, "Portable Bulk File Renamer", MessageBoxButton.OK, MessageBoxImage.Warning);
}

public sealed class FileRow : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isSelected;
    private string _newName = "";
    private string _status = "";
    public string FullPath { get; set; } = "";
    public string OldName { get; set; } = "";
    public string Folder { get; set; } = "";
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public string NewName { get => _newName; set { _newName = value; OnPropertyChanged(); } }
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

public sealed class RenameRule : System.ComponentModel.INotifyPropertyChanged
{
    private int _order;
    private bool _caseSensitive;
    public int Order { get => _order; set { _order = value; OnPropertyChanged(); } }
    public string SearchMode { get; set; } = "Anywhere";
    public string Action { get; set; } = "Replace";
    public string Find { get; set; } = "";
    public string Replace { get; set; } = "";
    public int Position { get; set; } = 1;
    public bool PositionFromEnd { get; set; }
    public bool CaseSensitive { get => _caseSensitive; set { _caseSensitive = value; OnPropertyChanged(); } }
    public bool AllOccurrences { get; set; } = true;
    public string SearchModeLabel { get; private set; } = "Anywhere";
    public string ActionLabel { get; private set; } = "Replace";
    public string PositionDescription { get; private set; } = "—";
    public void RefreshLabels(bool english)
    {
        SearchModeLabel = SearchMode switch { "Prefix" => english ? "Prefix" : "Đầu tên file", "Suffix" => english ? "Suffix" : "Cuối tên file", "ExactPosition" => english ? "Exact Position" : "Vị trí chính xác", _ => english ? "Anywhere" : "Bất kỳ vị trí nào" };
        ActionLabel = Action switch { "Remove" => english ? "Remove" : "Xóa cụm ký tự", "AddPrefix" => english ? "Add Prefix" : "Thêm tiền tố", "AddSuffix" => english ? "Add Suffix" : "Thêm hậu tố", "Uppercase" => english ? "Uppercase" : "Chữ hoa", "Lowercase" => english ? "Lowercase" : "Chữ thường", _ => english ? "Replace" : "Thay thế" };
        PositionDescription = SearchMode == "ExactPosition" ? $"{Position} ({(PositionFromEnd ? (english ? "distance from end" : "khoảng cách từ cuối tên") : (english ? "distance from start" : "khoảng cách từ đầu tên"))})" : "—";
        OnPropertyChanged(nameof(SearchModeLabel)); OnPropertyChanged(nameof(ActionLabel)); OnPropertyChanged(nameof(PositionDescription));
    }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

public sealed class RenameRecord
{
    public string OriginalPath { get; set; } = "";
    public string FinalPath { get; set; } = "";
}

public sealed class RuleMismatchException : Exception
{
    public RuleMismatchException(string message) : base(message) { }
}

using System.IO;
using System.Windows;
using JulschaVehicleTool.Core.Services.Import;

namespace JulschaVehicleTool.App.Views;

/// <summary>
/// Shows what a folder/ZIP scan detected and lets the user pick the vehicles to import.
/// Toggling a checkbox writes straight to <see cref="VehicleCandidate.Include"/>.
/// </summary>
public partial class ImportPreviewDialog : Window
{
    public ImportPreviewDialog(ImportScan scan, string sourceName, string targetResourceName)
    {
        InitializeComponent();

        var rows = scan.Vehicles.Select(v => new CandidateRow(v)).ToList();
        VehicleList.ItemsSource = rows;

        HeaderText.Text = $"{scan.Vehicles.Count} vehicle(s) found in \"{sourceName}\"";
        SubHeaderText.Text =
            $"{scan.MetaFiles.Count} meta file(s), {scan.CarCols.SirenSettings.Count} siren group(s). " +
            "Uncheck vehicles you don't want to import.";
        TargetText.Text = $"Target resource: {targetResourceName}";

        var lines = scan.Warnings.ToList();
        if (scan.UnassignedFiles.Count > 0)
        {
            lines.Add("");
            lines.Add("Files not assigned to any vehicle:");
            lines.AddRange(scan.UnassignedFiles.Select(f => "  " + Path.GetRelativePath(scan.RootPath, f)));
        }

        if (lines.Count == 0)
        {
            WarningsExpander.Visibility = Visibility.Collapsed;
        }
        else
        {
            WarningsExpander.Header = $"Notes ({scan.Warnings.Count} warning(s), {scan.UnassignedFiles.Count} unassigned file(s))";
            WarningsText.Text = string.Join(Environment.NewLine, lines);
        }

        ImportButton.IsEnabled = scan.Vehicles.Count > 0;
    }

    private void OnImport(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    /// <summary>Display wrapper; Include is passed through to the candidate.</summary>
    private sealed class CandidateRow
    {
        private readonly VehicleCandidate _c;
        public CandidateRow(VehicleCandidate c) => _c = c;

        public string Name => _c.Name;
        public List<string> Notes => _c.Notes;

        public bool Include
        {
            get => _c.Include;
            set => _c.Include = value;
        }

        public string ImportAs
        {
            get => string.IsNullOrWhiteSpace(_c.ImportAs) ? _c.Name : _c.ImportAs;
            set => _c.ImportAs = string.IsNullOrWhiteSpace(value) || value.Trim() == _c.Name ? null : value.Trim();
        }

        public string FilesSummary
        {
            get
            {
                var parts = new List<string>();
                if (_c.Model != null) parts.Add("model");
                if (_c.ModelHi != null) parts.Add("_hi model");
                if (_c.Texture != null) parts.Add("textures");
                if (_c.TextureHi != null) parts.Add("+hi textures");
                if (_c.ModParts.Count > 0) parts.Add($"{_c.ModParts.Count} mod part(s)");
                if (_c.ExtraTextures.Count > 0) parts.Add($"{_c.ExtraTextures.Count} extra texture dict(s)");
                return "Files: " + (parts.Count > 0 ? string.Join(", ", parts) : "none");
            }
        }

        public string MetaSummary
        {
            get
            {
                static string Mark(bool ok) => ok ? "✓" : "–";
                var siren = _c.CarVariation?.SirenSettings ?? 0;
                return $"vehicles.meta {Mark(_c.VehicleMeta != null)}   handling {Mark(_c.Handling != null)}   " +
                       $"carvariations {Mark(_c.CarVariation != null)}" + (siren != 0 ? $"   siren #{siren}" : "");
            }
        }
    }
}

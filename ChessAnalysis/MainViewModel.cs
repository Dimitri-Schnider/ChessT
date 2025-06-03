using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;


namespace ChessAnalysis
{
    //Test
    public class MainViewModel
    {
        public ObservableCollection<string> Items { get; set; } = new();

        public ICommand LoadJsonCommand => new RelayCommand(LoadJson);

        private void LoadJson()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string json = File.ReadAllText(openFileDialog.FileName);

                try
                {
                    using JsonDocument doc = JsonDocument.Parse(json);
                    JsonElement root = doc.RootElement;

                    Items.Clear();

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement element in root.EnumerateArray())
                        {
                            // Beispielsweise nur die Property "Name" anzeigen
                            if (element.TryGetProperty("Name", out JsonElement nameProp))
                            {
                                Items.Add(nameProp.GetString() ?? "<null>");
                            }
                            else
                            {
                                Items.Add("<kein 'Name' Feld>");
                            }
                        }
                    }
                    else
                    {
                        Items.Add("JSON ist kein Array.");
                    }
                }
                catch
                {
                    Items.Add("Fehler beim Lesen oder Parsen der JSON-Datei.");
                }
            }
        }
    }

}


using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.ComponentModel;
using Marking_TZ.Models;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Marking_TZ.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AppDBContext _context;
        private string _volume;
        public string Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        private int _boxFormat;
        public int BoxFormat
        {
            get { return _boxFormat; }
            set
            {
                _boxFormat = value;
                OnPropertyChanged(nameof(BoxFormat));
            }
        }

        private int _palletFormat;
        public int PalletFormat
        {
            get { return _palletFormat; }
            set
            {
                _palletFormat = value;
                OnPropertyChanged(nameof(PalletFormat));
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private string _gtin;
        public string Gtin
        {
            get { return _gtin; }
            set
            {
                _gtin = value;
                OnPropertyChanged(nameof(Gtin));
            }
        }

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }

        public MainViewModel(AppDBContext context)
        {
            Bottles = new ObservableCollection<Bottle>();
            Boxes = new ObservableCollection<Box>();
            Pallets = new ObservableCollection<Pallet>();
            _context = context;
            _ = InitializeAsync();
        }
        private async Task InitializeAsync()
        {
            await LoadDataAsync();
        }
       

        private RelayCommand writeJsonCommand;
        public RelayCommand WriteJsonCommand
        {
            get
            {
                return writeJsonCommand ??
                  (writeJsonCommand = new RelayCommand(async obj =>
                  {
                      await GenerateJsonAsync();
                  }));
            }
        }

        private RelayCommand importCommand;
        public RelayCommand ImportCommand
        {
            get
            {
                return importCommand ??
                  (importCommand = new RelayCommand(async obj =>
                  {
                      await ReadFileAsync();
                  }));
            }
        }

        public ObservableCollection<Bottle> Bottles { get; set; }
        public ObservableCollection<Box> Boxes { get; set; }
        public ObservableCollection<Pallet> Pallets { get; set; }

        private async Task ReadFileAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt",
                    Title = "Select AllItemCodes.txt"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    var allCodes = await File.ReadAllLinesAsync(filePath);
                    var filteredCodes = allCodes.Where(code => code.Contains(Gtin)).ToList();

                    if (!filteredCodes.Any())
                    {
                        MessageBox.Show("Нет совпадений по GTIN.");
                        return;
                    }

                    MessageBox.Show("Коды импортированы.");
                    await CalculatePalletsAsync(filteredCodes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла: {ex.Message}");
            }
        }

        private async Task CalculatePalletsAsync(List<string> codes)
        {
            try
            {
                if (BoxFormat <= 0 || PalletFormat <= 0)
                {
                    MessageBox.Show("Неверные форматы коробок или паллет.");
                    return;
                }

                int totalBottles = codes.Count;
                int totalBoxes = (int)Math.Ceiling((double)totalBottles / BoxFormat);
                int totalPallets = (int)Math.Ceiling((double)totalBoxes / PalletFormat);

                await DistributeCodesIntoBoxesAndPalletsAsync(codes, totalPallets);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчёте паллет: {ex.Message}");
            }
        }

        private async Task DistributeCodesIntoBoxesAndPalletsAsync(List<string> codes, int totalPallets)
        {
            try
            {
                int bottleIndex = 0;
                int boxId = 1;
                int palletId = 1;

                for (int palletIndex = 0; palletIndex < totalPallets; palletIndex++)
                {
                    var pallet = new Pallet
                    {
                        Code = $"01{Gtin}37{PalletFormat}21{palletId}",
                    };

                    _context.Pallets.Add(pallet);
                    await _context.SaveChangesAsync();

                    for (int boxIndex = 0; boxIndex < PalletFormat; boxIndex++)
                    {
                        if (bottleIndex >= codes.Count)
                        {
                            break;
                        }

                        var box = new Box
                        {
                            Code = $"01{Gtin}37{BoxFormat}21{boxId}",
                            PalletId = pallet.Id
                        };

                        _context.Boxes.Add(box);
                        await _context.SaveChangesAsync();

                        for (int bottleCount = 0; bottleCount < BoxFormat; bottleCount++)
                        {
                            if (bottleIndex >= codes.Count)
                            {
                                break;
                            }

                            var bottle = new Bottle
                            {
                                Code = codes[bottleIndex],
                                BoxId = box.Id
                            };

                            _context.Bottles.Add(bottle);
                            bottleIndex++;
                        }
                        boxId++;
                    }
                    palletId++;
                }
                await LoadProductsDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при распределении кодов по коробкам и паллетам: {ex.Message}");
            }
        }

        public async Task LoadProductsDataAsync()
        {
            try
            {
                Bottles.Clear();
                Boxes.Clear();
                Pallets.Clear();

                await LoadBottlesAsync();
                await LoadBoxesAsync();
                await LoadPalletsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Api client = new Api();
                string url = "http://promark94.marking.by/client/api/get/task/";
                MissionData productInfo = await client.GetMissionDataAsync(url);

                if (productInfo?.Mission?.Lot?.Package != null && productInfo.Mission.Lot.Product != null)
                {
                    Volume = productInfo.Mission.Lot.Package.Volume;
                    BoxFormat = productInfo.Mission.Lot.Package.BoxFormat;
                    PalletFormat = productInfo.Mission.Lot.Package.PalletFormat;
                    Name = productInfo.Mission.Lot.Product.Name;
                    Gtin = productInfo.Mission.Lot.Product.Gtin;
                }
                else
                {
                    MessageBox.Show("Ошибка: данные продукта отсутствуют.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"Ошибка при запросе: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
            await LoadProductsDataAsync();
        }

        private async Task LoadBottlesAsync()
        {
            try
            {
                var bottles = await _context.Bottles.ToListAsync();
                Bottles.Clear();
                foreach (var bottle in bottles)
                {
                    Bottles.Add(bottle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке бутылок: {ex.Message}");
            }
        }

        private async Task LoadBoxesAsync()
        {
            try
            {
                var boxes = await _context.Boxes.ToListAsync();
                Boxes.Clear();
                foreach (var box in boxes)
                {
                    Boxes.Add(box);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке коробок: {ex.Message}");
            }
        }

        private async Task LoadPalletsAsync()
        {
            try
            {
                var pallets = await _context.Pallets.ToListAsync();
                Pallets.Clear();
                foreach (var pallet in pallets)
                {
                    Pallets.Add(pallet);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке паллет: {ex.Message}");
            }
        }

        public async Task GenerateJsonAsync()
        {
            try
            {
                var pallets = await _context.Pallets
                    .Include(p => p.Boxes)
                    .ThenInclude(b => b.Bottles)
                    .ToListAsync();

                var layout = new
                {
                    ProductName = Name,
                    GTIN = Gtin,
                    bf = BoxFormat,
                    pf = PalletFormat,
                    Pallets = pallets.Select(p => new
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Boxes = p.Boxes.Select(b => new
                        {
                            Id = b.Id,
                            Code = b.Code,
                            Bottles = b.Bottles.Select(bt => new
                            {
                                Id = bt.Id,
                                Code = bt.Code
                            }).ToList()
                        }).ToList()
                    }).ToList()
                };

                string json = JsonConvert.SerializeObject(layout, Formatting.Indented);
                File.WriteAllText("data.json", json);
                MessageBox.Show("JSON-файл успешно сгенерирован.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации JSON-файла: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

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

        private string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                _Name = value;
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
            LoadDataAsync();
            _context = context;
        }
        private RelayCommand writeJsonCommand;
        public RelayCommand WriteJsonCommand
        {
            get
            {
                return writeJsonCommand ??
                  (writeJsonCommand = new RelayCommand(obj =>
                  {
                      GenerateJsonAsync();
                  }));
            }
        }

        private RelayCommand importCommand;
        public RelayCommand ImportCommand
        {
            get
            {
                return importCommand ??
                  (importCommand = new RelayCommand(obj =>
                  {
                      ReadFile();
                  }));
            }
        }

        public ObservableCollection<Bottle> Bottles { get; set; }
        public ObservableCollection<Box> Boxes { get; set; }
        public ObservableCollection<Pallet> Pallets { get; set; }





        private async Task ReadFile()
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
                MessageBox.Show("коды импортированы");
                CalculateAndDistributeCodesAsync(filteredCodes);
            }
        }

        private async Task CalculateAndDistributeCodesAsync(List<string> codes)
        {
            int totalBottles = codes.Count; 
            int totalBoxes = (int)Math.Ceiling((double)totalBottles / BoxFormat);
            int totalPallets = (int)Math.Ceiling((double)totalBoxes / PalletFormat);
        
            await DistributeCodesIntoBoxesAndPalletsAsync(codes, totalPallets);
        }

        private async Task DistributeCodesIntoBoxesAndPalletsAsync(List<string> codes, int totalPallets)
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
                    if (bottleIndex <= codes.Count)
                    {
                        var box = new Box
                        {
                            Code = $"01{Gtin}37{BoxFormat}21{boxId}",
                            PalletId = pallet.Id
                        };

                        _context.Boxes.Add(box);
                        await _context.SaveChangesAsync(); 
                        for (int bottleCount = 0; bottleCount < BoxFormat; bottleCount++)
                        {
                            if (bottleIndex >= codes.Count) break; 
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
                }
                palletId++;
            }
            await LoadProductsData();
        }
        public async Task LoadProductsData()
        {
            Bottles.Clear();
            Boxes.Clear();
            Pallets.Clear();

            LoadBottles();
            LoadBoxes();
            LoadPallets();
        }
        private async Task LoadDataAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "http://promark94.marking.by/client/api/get/task/";
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseData = await response.Content.ReadAsStringAsync();
                    Obj productInfo = JsonConvert.DeserializeObject<Obj>(responseData);
                    Volume = productInfo.Mission.Lot.Package.Volume;
                    BoxFormat = productInfo.Mission.Lot.Package.BoxFormat;
                    PalletFormat = productInfo.Mission.Lot.Package.PalletFormat;
                    Name = productInfo.Mission.Lot.Product.Name;
                    Gtin = productInfo.Mission.Lot.Product.Gtin;
                }
            }
            catch (Exception ex)
            {
            }
            LoadProductsData();
           
        }
        private void LoadBottles()
        {
            var bottles = _context.Bottles.ToList();

            Bottles.Clear();
            foreach (var bottle in bottles)
            {
                Bottles.Add(bottle);
            }
        }

        private void LoadBoxes()
        {
            var boxes = _context.Boxes.ToList();

            Boxes.Clear();
            foreach (var box in boxes)
            {
                Boxes.Add(box);
            }
        }

        private void LoadPallets()
        {
            var pallets = _context.Pallets.ToList();

            Pallets.Clear();
            foreach (var pallet in pallets)
            {
                Pallets.Add(pallet);
            }
        }

        public async Task GenerateJsonAsync()
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
            string fileName = $"{layout.GTIN}_result_file_{DateTime.Now:yyMMdd_HHmm}.json";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            await File.WriteAllTextAsync(filePath, json);
            MessageBox.Show("done");
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

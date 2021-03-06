﻿using CustomPresentationControls;
using CustomPresentationControls.Utilities;
using StatementViewer.Costs;
using StatementViewer.Services;
using StatementViewer.Transactions;
using StatementViewer.Utilities;
using StatementViewer.Vendors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace StatementViewer
{
    public class MainWindowModel : ViewModel
    {
        #region Fields
        private ITransactionRepository _transactionRepository = Container.TransactionRepository;
        private IVendorRepository _vendorRepository = Container.VendorRepository;
        private string _searchDirectory;
        private ObservableCollection<CostBreakdown> _monthCostData = new ObservableCollection<CostBreakdown>();
        private ObservableCollection<CostBreakdown> _yearCostData = new ObservableCollection<CostBreakdown>();
        private ObservableCollection<CostBreakdown> _allCostData = new ObservableCollection<CostBreakdown>();
        private AddEditVendorViewModel _addEditVendorViewModel = new AddEditVendorViewModel();
        private TransactionsViewModel _transactionsViewModel = new TransactionsViewModel();
        private VendorsViewModel _vendorsViewModel = new VendorsViewModel();

        private CostsViewModel _monthlyCostsViewModel = new CostsViewModel("Monthly");
        private CostsViewModel _yearlyCostsViewModel = new CostsViewModel("Yearly");
        private CostsViewModel _allCostsViewModel = new CostsViewModel("All");

        private ViewModel _currentViewModel;
        private ViewModel _lastViewModel;
        #endregion
        #region Properties
        public string SearchDirectory
        {
            get { return _searchDirectory; }
            set
            {
                OnPropertyChanged(ref _searchDirectory, value);
                Config.SearchDirectory = value;
            }
        }
        public ViewModel CurrentViewModel
        {
            get { return _currentViewModel; }
            set { OnPropertyChanged(ref _currentViewModel, value); }
        }
        public ObservableCollection<CostBreakdown> MonthCostData
        {
            get { return _monthCostData; }
            set { OnPropertyChanged(ref _monthCostData, value); }
        }
        public ObservableCollection<CostBreakdown> YearCostData
        {
            get { return _monthCostData; }
            set { OnPropertyChanged(ref _monthCostData, value); }
        }
        public ObservableCollection<CostBreakdown> AllCostData
        {
            get { return _monthCostData; }
            set { OnPropertyChanged(ref _monthCostData, value); }
        }
        #endregion
        #region Commands
        public RelayCommand CloseApplicationCommand { get; }
        public RelayCommand<string> NavCommand { get; }
        public RelayCommand<CostBreakdown> ViewMonthCommand { get; }
        public RelayCommand SetDirectoryCommand { get; }
        #endregion
        public MainWindowModel()
        {
            UpdateViewModels();
            SearchDirectory = Config.SearchDirectory;
            CloseApplicationCommand = new RelayCommand(OnApplicationClose);
            NavCommand = new RelayCommand<string>(OnNav);
            ViewMonthCommand = new RelayCommand<CostBreakdown>(OnNavToMonthView);
            SetDirectoryCommand = new RelayCommand(SetDirectory);

            _monthlyCostsViewModel.AddVendor += NavToAddVendor;
            _monthlyCostsViewModel.ModifyTransaction += ModifyTransaction;
            _yearlyCostsViewModel.AddVendor += NavToAddVendor;
            _yearlyCostsViewModel.ModifyTransaction += ModifyTransaction;
            _allCostsViewModel.AddVendor += NavToAddVendor;
            _allCostsViewModel.ModifyTransaction += ModifyTransaction;

            _vendorsViewModel.AddVendor += NavToAddVendor;
            _vendorsViewModel.EditVendor += NavToEditVendor;
            _vendorsViewModel.UpdateTransactionVendors += UpdateTransactionVendors;
            _vendorsViewModel.RemoveVendor += RemoveVendor;
            _transactionsViewModel.AddVendor += NavToAddVendor;
            _transactionsViewModel.ProcessTransactions += ProcessTransactions;
            _transactionsViewModel.RemoveTransaction += RemoveTransaction;
            _addEditVendorViewModel.AddVendor += AddVendor;
            _addEditVendorViewModel.ModifyVendor += ModifyVendor;
            _addEditVendorViewModel.Done += NavToLastViewModel;
        }

        private void ModifyTransaction(object sender, ModifyTransactionEventArgs e)
        {
            _transactionRepository.UpdateTransaction(e.Transaction);
            UpdateViewModels();
        }
        #region Command Methods
        private void OnApplicationClose()
        {
        }
        private void OnNav(string destination)
        {
            switch (destination)
            {
                case "Monthly":
                    OnNavToMonthView(MonthCostData.Where(c => c.TimePeriod.Year == DateTime.Today.Year && c.TimePeriod.Month == DateTime.Today.Month).FirstOrDefault());
                    break;
                case "Yearly":
                    CurrentViewModel = _yearlyCostsViewModel;
                    break;
                case "All":
                    CurrentViewModel = _allCostsViewModel;
                    break;
                case "Transactions":
                    CurrentViewModel = _transactionsViewModel;
                    break;
                case "Vendors":
                    CurrentViewModel = _vendorsViewModel;
                    break;
            }
        }
        private void OnNavToMonthView(CostBreakdown breakdown)
        {
            try
            {
                if (breakdown != null)
                {
                    IEnumerable<Transaction> viewTransactions = _transactionRepository.GetTransactionsByMonth(breakdown.TimePeriod);
                    _monthlyCostsViewModel.SetTransactions(viewTransactions);
                    _monthlyCostsViewModel.SetBreakdown(breakdown);
                    CurrentViewModel = _monthlyCostsViewModel;
                }
                else
                {
                    WpfMessageBox.ShowDialog("Warning", "No transaction data available for the current month.", MessageBoxButton.OK, MessageIcon.Information);
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.ShowDialog("Error", ex.Message, MessageBoxButton.OK, MessageIcon.Error);
            }
        }
        #endregion
        #region Private Methods
        private void NavToAddVendor()
        {
            _addEditVendorViewModel.EditMode = false;
            _addEditVendorViewModel.SetVendor(new Vendor());
            _lastViewModel = CurrentViewModel;
            CurrentViewModel = _addEditVendorViewModel;
        }
        private void NavToEditVendor(Vendor vendor)
        {
            _addEditVendorViewModel.EditMode = true;
            _addEditVendorViewModel.SetVendor(vendor);
            _lastViewModel = CurrentViewModel;
            CurrentViewModel = _addEditVendorViewModel;
        }
        private void AddVendor(Vendor vendor)
        {
            _vendorRepository.AddVendor(vendor);
            _transactionRepository.UpdateTransactionVendors(vendor);
            UpdateViewModels();
        }
        private void ModifyVendor(Vendor vendor)
        {
            _vendorRepository.UpdateVendor(vendor);
            _transactionRepository.UpdateTransactionVendors(vendor);
            UpdateViewModels();
        }
        private void UpdateTransactionsByVendor(Vendor vendor)
        {
            _transactionRepository.UpdateTransactionVendors(vendor);
        }
        private void UpdateTransactionVendors()
        {
            _transactionRepository.UpdateTransactionVendors(_vendorRepository.GetVendors());
            UpdateViewModels();
        }
        private void RemoveVendor(Vendor vendor)
        {
            _vendorRepository.DeleteVendor(vendor.Id);
            _transactionRepository.UpdateTransactionVendors(new Vendor { Name = string.Empty, TransactionKey = string.Empty });
            UpdateViewModels();
        }
        private void ProcessTransactions()
        {
            if (File.Exists(Config.DatabasePath))
            {
                IStatementProcessingService _statementProcessingService = new StatementProcessingService(Config.SearchDirectory);
                IEnumerable<Transaction> processedTransactions = _statementProcessingService.ProcessStatements();
                _transactionRepository.AddBulkTransactions(processedTransactions);
                foreach (Vendor vendor in _vendorRepository.GetVendors())
                {
                    _transactionRepository.UpdateTransactionVendors(vendor);
                }
                UpdateViewModels();
            }
        }
        private void RemoveTransaction(Transaction transaction)
        {
            _transactionRepository.DeleteTransaction(transaction.Id);
            UpdateViewModels();
        }
        private void NavToVendorList()
        {
            CurrentViewModel = _vendorsViewModel;
        }
        private void NavToLastViewModel()
        {
            CurrentViewModel = _lastViewModel;
        }
        private void SetDirectory()
        {
            OpenFolderDialog openFolderWindow = new OpenFolderDialog();
            openFolderWindow.ShowDialog();
            if (openFolderWindow.DialogResult ?? false)
            {
                if (openFolderWindow.Path != null)
                {
                    SearchDirectory = openFolderWindow.Path;
                    Config.SearchDirectory = SearchDirectory;
                }
            }
        }
        private void UpdateViewModels()
        {
            try
            {
                _monthlyCostsViewModel.SetTransactions(_transactionRepository.GetTransactionsByMonth(DateTime.Today));
                _yearlyCostsViewModel.SetTransactions(_transactionRepository.GetTransactionsByYear(DateTime.Today.Year));
                _yearlyCostsViewModel.SetBreakdown(_transactionRepository.GetYearCostBreakdowns().Where(b => b.TimePeriod.Year == DateTime.Today.Year).FirstOrDefault());
                _allCostsViewModel.SetTransactions(_transactionRepository.GetTransactions());
                _allCostsViewModel.SetBreakdown(_transactionRepository.GetLifetimeCostBreakdown());
                _transactionsViewModel.SetTransactions(_transactionRepository.GetTransactions());
                _vendorsViewModel.SetVendors(_vendorRepository.GetVendors());
                MonthCostData = new ObservableCollection<CostBreakdown>(_transactionRepository.GetMonthCostBreakdowns());
                IEnumerable<CostBreakdown> costData = _transactionRepository.GetMonthCostBreakdowns();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                WpfMessageBox.ShowDialog("Data Error", ex.Message, MessageBoxButton.OK, MessageIcon.Error);
            }
        }
        #endregion
    }
}
